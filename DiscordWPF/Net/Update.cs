using DiscordWPF.Shared;
using Microsoft.ApplicationInsights;
using Microsoft.ApplicationInsights.DataContracts;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.IO.Compression;
using System.Linq;
using System.Net.Http;
using System.Net.Http.Headers;
using System.Reflection;
using System.Security.Cryptography;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using System.Web;
using System.Windows;
using WamWooWam.Core;

using static DiscordWPF.Constants;

namespace DiscordWPF.Net
{
    internal static class Update
    {
        static HttpClient _client = new HttpClient();

#if WIN10
        private const string CHANNEL_STRING = "win10";
#else
        private const string CHANNEL_STRING = "win32";
#endif

        private const string RUN_UPDATE_OPERATION_ID = "528D8FBE-E27A-46C0-99FB-DC8446F26174";
        private const string DO_UPDATE_OPERATION_ID = "528D8FBE-E27A-46C0-99FB-DC8446F26175";

        private static string[] _validChannels = new string[] { "stable", "canary" };

        private const string BASE_URL = MAIN_URL + "api/update/";

        static Update()
        {
            _client.DefaultRequestHeaders.Accept.Clear();
            _client.DefaultRequestHeaders.Accept.Add(
                new MediaTypeWithQualityHeaderValue("application/json"));
            _client.BaseAddress = new Uri(BASE_URL);
        }

        internal static async Task<UpdateResponse> CheckForUpdates()
        {
            if (!(new DesktopBridge.Helpers()).IsRunningAsUwp())
            {
                try
                {
                    var request = new UpdateRequest(Assembly.GetEntryAssembly().GetName().Version, GetFullChannelString());
                    var httpResponse = await _client.PostAsJsonAsync("check", request);

                    httpResponse.EnsureSuccessStatusCode();

                    var result = await httpResponse.Content.ReadAsAsync<UpdateResponse>();
                    return result;
                }
                catch
                {
                    return null;
                }
            }

            return null;
        }

        private static string GetFullChannelString() => $"{CHANNEL_STRING}-{GetChannel()}-{(Environment.Is64BitOperatingSystem ? "x64" : "x86")}";

        internal static async Task<UpdateDetails> GetUpdateDetailsAsync(Version version)
        {
            try
            {
                var httpResponse = await _client.GetAsync($"details/{version}?c={GetFullChannelString()}");
                httpResponse.EnsureSuccessStatusCode();

                return await httpResponse.Content.ReadAsAsync<UpdateDetails>();
            }
            catch
            {
                return null;
            }
        }

        internal static async Task RunUpdateAsync(UpdateDetails details, Func<string, int?, int?, Task> progressCallback = null)
        {
            using (var telemetry = App.Telemetry.StartOperation<RequestTelemetry>("RunUpdate", RUN_UPDATE_OPERATION_ID))
            {
                telemetry.Telemetry.Properties.Add("versionFrom", Assembly.GetEntryAssembly().GetName().Version.ToString());
                telemetry.Telemetry.Properties.Add("versionTo", details.NewVersion);

                var location = Assembly.GetEntryAssembly().Location;
                var current = Path.GetDirectoryName(location);
                var temp = Path.Combine(current, "update-temp");
                var channel = GetFullChannelString();

                Directory.CreateDirectory(temp);
                using (var sha = SHA256.Create())
                {
                    try
                    {
                        for (int i = 0; i < details.Files.Count; i++)
                        {
                            await (progressCallback?.Invoke("Downloading Updated Files...", i + 1, details.Files.Count) ?? Task.CompletedTask);

                            var file = details.Files.ElementAt(i);
                            var path = Path.Combine(current, file.Key);

                            
                            if (File.Exists(path))
                            {
                                if (await Task.Run(() => CheckHash(sha, file, path)))
                                {
                                    await DownloadUpdateFileAsync(details, temp, channel, file);
                                }
                            }
                            else
                            {
                                await DownloadUpdateFileAsync(details, temp, channel, file);
                            }
                        }
                    }
                    catch (Exception ex)
                    {
                        telemetry.Telemetry.Stop();
                        telemetry.Telemetry.Success = false;

                        App.Telemetry.TrackException(ex);
                    }
                }

                await (progressCallback?.Invoke("Finalising...", null, null) ?? Task.CompletedTask);

                try
                {
                    var tempExePath = Path.Combine(current, Path.GetFileNameWithoutExtension(location) + ".Temp.exe");

                    if (File.Exists(tempExePath))
                    {
                        File.Delete(tempExePath);
                    }

                    File.Copy(location, tempExePath);
                    Process.Start(tempExePath, $@"do-update ""{location}"" ""{temp}""");
                }
                catch (Exception ex)
                {
                    telemetry.Telemetry.Stop();
                    telemetry.Telemetry.Success = false;

                    App.Telemetry.TrackException(ex);

                    return;
                }

                telemetry.Telemetry.Stop();
                telemetry.Telemetry.Success = true;
            }

            App.Current.Shutdown();
        }

        internal static void DoUpdate(string[] args)
        {
            if (args.Length == 3)
            {
                using (var telemetry = App.Telemetry.StartOperation<RequestTelemetry>("DoUpdate", DO_UPDATE_OPERATION_ID))
                {
                    telemetry.Telemetry.Success = true;

                    var exePath = args[1];
                    var temp = args[2];

                    var assembly = Assembly.GetEntryAssembly();
                    var location = assembly.Location;
                    var current = Path.GetDirectoryName(location);

                    if (File.Exists(exePath))
                    {
                        if (Directory.Exists(temp))
                        {
                            var backup = Path.Combine(current, "backup", assembly.GetName().Version.ToString());
                            if (!Directory.Exists(backup))
                            {
                                Directory.CreateDirectory(backup);
                            }
                            
                            var currentUri = new Uri(current);
                            var tempUri = new Uri(temp + "\\");

                            try
                            {
                                var files = Directory.GetFiles(temp, "*.gz", SearchOption.AllDirectories);
                                foreach (var file in files)
                                {
                                    var relative = tempUri.MakeRelativeUri(new Uri(file)).ToString();
                                    relative = relative.Remove(relative.Length - 3, 3);
                                    
                                    var original = Path.Combine(current, relative);
                                    var backupPath = Path.Combine(backup, relative);

                                    if (!Directory.Exists(Path.GetDirectoryName(original)))
                                        Directory.CreateDirectory(Path.GetDirectoryName(original));

                                    if (!Directory.Exists(Path.GetDirectoryName(backupPath)))
                                        Directory.CreateDirectory(Path.GetDirectoryName(backupPath));

                                    if (File.Exists(original) && !File.Exists(backupPath))
                                    {
                                        Retry(() => File.Move(original, backupPath), 10);
                                    }

                                    using (var oldFile = File.OpenRead(file))
                                    using (var newFile = Retry(() => File.Create(original), 10))
                                    using (var gzip = new GZipStream(oldFile, CompressionMode.Decompress))
                                    {
                                        gzip.CopyTo(newFile);
                                    }
                                }
                            }
                            catch (Exception ex)
                            {
                                App.Telemetry.TrackException(ex);
                                telemetry.Telemetry.Success = false;

                                try
                                {
                                    foreach (var file in Directory.GetFiles(backup))
                                    {
                                        var original = Path.Combine(current, Path.GetFileName(file));
                                        if (File.Exists(original))
                                        {
                                            Retry(() => File.Delete(original), 10);
                                        }

                                        File.Move(file, original);
                                    }
                                }
                                catch (Exception ex1)
                                {
                                    App.Telemetry.TrackException(ex1);

                                    var result = MessageBox.Show(
                                        "An update was attempted and appears to have failed, attempts were made to revert the update, and also failed.\r\n" +
                                        "Your installation may be corrupt, but should work fine. Try it?",
                                        "Update failed.",
                                        MessageBoxButton.YesNo,
                                        MessageBoxImage.Error);

                                    if (result != MessageBoxResult.No)
                                    {
                                        Environment.Exit(0);
                                    }
                                }

                            }

                            Directory.Delete(temp, true);
                        }
                        else
                        {
                            telemetry.Telemetry.Success = false;
                        }

                        Process.Start(exePath, $@"finish-update ""{location}""");
                    }
                    else
                    {
                        telemetry.Telemetry.Success = false;
                        MessageBox.Show($"The exe path specified doesn't exist?\r\nSpecified path: {exePath}", "File doesn't exist??", MessageBoxButton.OK, MessageBoxImage.Error);
                    }

                    telemetry.Telemetry.Stop();
                }

                Environment.Exit(0);
            }
        }

        private static void Retry(Action action, int tries)
        {
            Exception exception = null;

            for (int i = 0; i < tries; i++)
            {
                try
                {
                    action();
                    return;
                }
                catch (Exception ex)
                {
                    exception = ex;
                    Thread.Sleep(100);
                }
            }

            if (exception != null)
                throw exception;
        }

        public static T Retry<T>(Func<T> func, int tries)
        {
            Exception exception = null;

            for (int i = 0; i < tries; i++)
            {
                try
                {
                    return func();
                }
                catch (Exception ex)
                {
                    exception = ex;
                    Thread.Sleep(100);
                }
            }

            if (exception != null)
                throw exception;

            return default;
        }

        internal static void FinishUpdate(string v)
        {
            App.Telemetry.TrackEvent("UpdateComplete");
            if (File.Exists(v))
            {
                try { Retry(() => File.Delete(v), 10); } catch { }
            }
        }

        private static string GetChannel()
        {
            var channel = Settings.GetSetting("UpdateChannel", "canary").ToLowerInvariant();
            if (!_validChannels.Contains(channel))
            {
                channel = _validChannels[0];
                Settings.SetSetting("UpdateChannel", channel);
            }

            return channel;
        }

        private static async Task DownloadUpdateFileAsync(UpdateDetails details, string temp, string channel, KeyValuePair<string, byte[]> file)
        {
            var response = await _client.SendAsync(new HttpRequestMessage(HttpMethod.Get, $"getfile/{string.Join("", file.Value.Select(b => b.ToString("x2")))}?c={channel}&v={details.NewVersion}"));

            response.EnsureSuccessStatusCode();

            var path = Path.Combine(temp, file.Key + ".gz");
            if (!Directory.Exists(Path.GetDirectoryName(path)))
                Directory.CreateDirectory(Path.GetDirectoryName(path));

            using (var remoteStr = await response.Content.ReadAsStreamAsync())
            {
                using (var localStr = File.Create(path))
                {
                    await remoteStr.CopyToAsync(localStr);
                }
            }
        }

        private static bool CheckHash(SHA256 sha, KeyValuePair<string, byte[]> file, string path)
        {
            using (var str = File.OpenRead(path))
            {
                var nHash = sha.ComputeHash(str);
                if (!nHash.SequenceEqual(file.Value))
                {
                    return true;
                }
            }

            return false;
        }
    }
}

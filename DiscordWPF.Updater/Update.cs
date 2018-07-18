using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.IO.Compression;
using System.Linq;
using System.Reflection;
using System.Security.Cryptography;
using System.Text;
using System.Threading.Tasks;
using Textamina.Jsonite;

namespace DiscordWPF.Updater
{
    internal static class Update
    {
        public static async Task DoUpdateAsync(string toVersion)
        {
            var platform = Environment.Is64BitOperatingSystem ? "x64" : "x86";
            var installDir = Path.GetDirectoryName(Assembly.GetExecutingAssembly().Location);
            var window = await App.Current.Dispatcher.InvokeAsync(() => (App.Current.MainWindow as MainWindow));
            var control = window.loadingControl;

            try
            {
                var details = await App.Client.GetStringAsync("versions.json");
                var versions = ((JsonArray)Json.Deserialize(details))
                    .Cast<JsonObject>();

                var version = versions
                    .OrderByDescending(v => Version.Parse(v["version"] as string))
                    .FirstOrDefault(v => v["platform"] as string == platform && (toVersion == "latest" || v["version"] as string == toVersion));

                var backupPath = Path.Combine(Path.GetTempPath(), "update-temp", version["version"].ToString());
                Directory.CreateDirectory(backupPath);

                if (version != null)
                {
                    var verStr = version["version"]?.ToString();
                    var obj = (JsonArray)version["files"];

                    using (var sha = SHA256.Create())
                    {
                        for (int i = 0; i < obj.Count; i++)
                        {
                            var file = obj.ElementAt(i) as JsonObject;
                            try
                            {
                                await control.ChangeStatusAsync("Installing update...", i, obj.Count);

                                var path = Path.Combine(installDir, file["name"] as string);
                                var url = Shared.GetUrlForFile(platform, versions, verStr, file);

                                if (!File.Exists(path))
                                {
                                    if (!Directory.Exists(Path.GetDirectoryName(path)))
                                        Directory.CreateDirectory(Path.GetDirectoryName(path));

                                    await DownloadToFileAsync(path, url);
                                }
                                else
                                {
                                    var equal = false;
                                    using (var str = File.OpenRead(path))
                                    {
                                        var hash = await Task.Run(() => sha.ComputeHash(str));
                                        var newHash = Convert.FromBase64String(file["hash"] as string);
                                        equal = hash.SequenceEqual(newHash);
                                    }

                                    if (!equal)
                                    {
                                        File.Move(path, Path.Combine(backupPath, Path.GetFileName(path)));
                                        await DownloadToFileAsync(path, url);
                                    }
                                }
                            }
                            catch (Exception ex)
                            {
                                await window.Dispatcher.InvokeAsync(() => window.ShowError($"Failed to fetch {file["name"]}. {ex}"));
                                return;
                            }
                        }

                        Process.Start(Path.Combine(installDir, version["main_exe"] as string));
                        App.Current.Shutdown();
                    }
                }
            }
            catch { }
        }

        private static async Task DownloadToFileAsync(string path, string url)
        {
            using (var destStr = File.Create(path))
            using (var remoteStr = await App.Client.GetStreamAsync(url))
            using (var gzip = new GZipStream(remoteStr, CompressionMode.Decompress))
            {
                await gzip.CopyToAsync(destStr);
            }
        }
    }
}

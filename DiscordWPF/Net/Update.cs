using DiscordWPF.Shared;
using Newtonsoft.Json;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Net.Http;
using System.Net.Http.Headers;
using System.Reflection;
using System.Threading.Tasks;
using System.Windows;

using static DiscordWPF.Constants;

namespace DiscordWPF.Net
{
    internal static partial class Update
    {
        static HttpClient _client = new HttpClient();

        static Update()
        {
            _client.DefaultRequestHeaders.Accept.Clear();
            _client.DefaultRequestHeaders.Accept.Add(
                new MediaTypeWithQualityHeaderValue("application/json"));
            _client.BaseAddress = new Uri(UPDATE_BASE_URL);
        }

        private static string UpdaterExecutable => Path.Combine(Path.GetDirectoryName(Assembly.GetExecutingAssembly().Location), "DiscordWPF.Updater.exe");

        internal static bool CanDoUpdate() => File.Exists(UpdaterExecutable);

        internal static async Task<bool> CheckForUpdates()
        {
            try
            {
                var current = Assembly.GetExecutingAssembly().GetName().Version;
                var platform = Environment.Is64BitProcess ? "x64" : "x86";
                var str = await _client.GetStringAsync("versions.json");
                var versions = JsonConvert.DeserializeObject<List<UpdateDetails>>(str);

                var latest = versions
                    .OrderByDescending(v => v.Version)
                    .FirstOrDefault(v => v.Version > current && v.Platform == platform);

                return latest != null;
            }
            catch
            {
                return false;
            }
        }

        internal static async Task RunUpdateAsync()
        {
            Process.Start(UpdaterExecutable, $"--type=update --pid={Process.GetCurrentProcess().Id}");
            await Task.Delay(2000); // not important, just looks cleaner.
            Application.Current.Shutdown();
        }

        internal static async Task<UpdateDetails> GetUpdateDetailsAsync(Version version)
        {
            try
            {
                var str = await _client.GetStringAsync("versions.json");
                var versions = JsonConvert.DeserializeObject<List<UpdateDetails>>(str);

                var latest = versions
                    .OrderByDescending(v => v.Version)
                    .FirstOrDefault(v => v.Version == version);

                return latest;
            }
            catch
            {
                return null;
            }
        }
    }
}

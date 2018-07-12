using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Net.Http;
using System.Text;
using System.Threading.Tasks;
using WamWooWam.Core;

namespace DiscordWPF.Abstractions
{
    internal static class Shared
    {
        private static Lazy<HttpClient> _httpClient = new Lazy<HttpClient>(() => new HttpClient());

        internal static async Task<string> GetImagePathAsync(Uri url)
        {
            try
            {
                var path = Path.Combine(Settings.SettingsDirectory, "Cache");
                var fileName = Path.Combine(path, Path.GetFileName(url.AbsolutePath));
                Directory.CreateDirectory(path);

                if (!File.Exists(fileName))
                {
                    using (var remoteStr = await _httpClient.Value.GetStreamAsync(url))
                    using (var localStr = File.Create(fileName))
                    {
                        await remoteStr.CopyToAsync(localStr);
                    }
                }

                return fileName;
            }
            catch { }

            return url.ToString();
        }
    }
}

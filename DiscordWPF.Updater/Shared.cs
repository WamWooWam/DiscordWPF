using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Textamina.Jsonite;

namespace DiscordWPF.Updater
{
    internal static class Shared
    {
        internal static string GetUrlForFile(string platform, IEnumerable<JsonObject> versions, string version, JsonObject file)
        {
            var hash = file["hash"] as string;

            if (file.TryGetValue("included_version", out var v))
            {
                version = v as string;
            }

            var bytes = Convert.FromBase64String(hash);
            var str = string.Join("", bytes.Select(b => b.ToString("x2")));
            return $"{version}-{platform}/{str}.gz";
        }

    }
}

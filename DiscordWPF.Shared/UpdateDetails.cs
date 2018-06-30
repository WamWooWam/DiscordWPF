using Newtonsoft.Json;
using System;
using System.Collections.Generic;

namespace DiscordWPF.Shared
{
    public class UpdateDetails
    {
        [JsonProperty("new_version")]
        public string NewVersion { get; set; }

        [JsonProperty("new_version_details")]
        public string NewVersionDetails { get; set; }

        [JsonProperty("released_at")]
        public DateTimeOffset ReleaseDate { get; set; }

        [JsonProperty("files")]
        public Dictionary<string, byte[]> Files { get; set; }
    }
}
using Newtonsoft.Json;
using System;
using System.Collections.Generic;
using System.Text;

namespace DiscordWPF.Shared
{
    public class UpdateResponse
    {
        [JsonProperty("available")]
        public bool UpdateAvailable { get; set; }

        [JsonProperty("details")]
        public UpdateDetails Details { get; set; }
    }
}

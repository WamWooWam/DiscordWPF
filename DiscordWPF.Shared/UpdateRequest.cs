using Newtonsoft.Json;
using System;

namespace DiscordWPF.Shared
{
    public class UpdateRequest
    {
        [JsonConstructor]
        private UpdateRequest() { }

        public UpdateRequest(Version current, string channel)
        {
            CurrentVersion = current.ToString();
            CurrentChannel = channel;
        }

        [JsonProperty("version")]
        public string CurrentVersion { get; set; }

        [JsonProperty("channel")]
        public string CurrentChannel { get; set; }
    }
}

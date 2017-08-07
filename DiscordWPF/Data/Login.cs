using Newtonsoft.Json;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace DiscordWPF.Data
{
    /// <summary>
    /// A discord login request
    /// Send to: https://discordapp.com/api/v6/auth/login
    /// </summary>
    class LoginRequest
    {
        [JsonProperty("email")]
        public string EmailAddress { get; set; }

        [JsonProperty("password")]
        public string Password { get; set; }
    }

    /// <summary>
    /// A discord login request
    /// Send to: https://discordapp.com/api/v6/auth/mfa/totp
    /// </summary>
    class Login2FARequest
    {
        [JsonProperty("code")]
        public string Code { get; set; }

        [JsonProperty("ticket")]
        public string Ticket { get; set; }
    }

    /// <summary>
    /// A discord login response
    /// </summary>
    class LoginResponse
    {
        [JsonProperty("mfa")]
        public bool TwoFactor { get; set; }

        [JsonProperty("token")]
        public string Token { get; set; }

        [JsonProperty("ticket")]
        public string Ticket { get; set; }
    }
}

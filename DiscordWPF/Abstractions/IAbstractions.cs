using DSharpPlus.Entities;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace DiscordWPF.Abstractions
{
    interface IAbstractions
    {
        string GetToken(string key);

        void SetToken(string key, string token);

        void ShowNotification(DiscordMessage message);

        void ShowInfoNotification(string title, string content);
    }
}

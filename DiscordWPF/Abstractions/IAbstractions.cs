using DSharpPlus.Entities;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace DiscordWPF.Abstractions
{
    interface IAbstractions
    {
        string GetToken(string key);

        void SetToken(string key, string token);

        Task ShowNotificationAsync(DiscordMessage message);

        void RetractNotification(ulong id);

        void ShowInfoNotification(string title, string content);

        Task SendFileWithProgressAsync(DiscordChannel channel, string message, Stream file, string fileName, IProgress<double?> progress);
    }
}

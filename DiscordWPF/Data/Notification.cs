using Discord;
using Discord.WebSocket;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace DiscordWPF.Data
{
    public class NotificationGroupModel
    {
        public IUser User { get; set; }
        public IMessageChannel Channel { get; set; }
        public IGuild Guild { get; set; }
        public List<IMessage> Messages { get; set; }

        public NotifIcationType Type { get; set; }
    }

    public class Notification
    {
        public ulong MessageId { get; set; }
        public ulong ChannelId { get; set; }
        public ulong? GuildId { get; set; }

        public NotifIcationType Type { get; set; }
    }

    public enum NotifIcationType
    {
        Guild, DM, Group
    }
}

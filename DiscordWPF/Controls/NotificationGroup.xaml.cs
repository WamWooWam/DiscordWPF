using Discord;
using Discord.WebSocket;
using DiscordWPF.Data;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Data;
using System.Windows.Documents;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using System.Windows.Navigation;
using System.Windows.Shapes;

namespace DiscordWPF.Controls
{
    /// <summary>
    /// Interaction logic for NotificationGroup.xaml
    /// </summary>
    public partial class NotificationGroup : UserControl
    {
        public NotificationGroup(NotificationGroupModel model)
        {
            InitializeComponent();

            if (model.Type == NotifIcationType.Guild)
            {
                SocketGuild guild = App.DiscordWindow.Client.GetGuild((model.Channel as IGuildChannel).Guild.Id);
                sourceTitle.Text = guild.Name;
                if (guild.IconUrl != null)
                    sourceImage.ImageSource = Images.GetImage(guild.IconUrl);
            }

            foreach (IMessage msg in model.Messages)
            {
                MessageViewer viewer = new MessageViewer(msg, model.Channel);
                messageViewer.Items.Add(viewer);
            }
        }
    }
}

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
    /// Interaction logic for GuildViewer.xaml
    /// </summary>
    public partial class DMGuildViewer : ListViewItem
    {
        bool group = false;
        public DMGuildViewer(IEnumerable<IDMChannel> channels)
        {
            InitializeComponent();

            guildName.Text = "Direct Messages";
            guildDescription.Text = channels.Count(c => c.Recipient.Status == UserStatus.Online) + " users online";

            foreach (IDMChannel channel in channels)
            {
                DMChannelViewer viewer = new DMChannelViewer(channel);
                channelsList.Items.Add(viewer);
            }

            App.DiscordWindow.Client.ChannelCreated += Client_ChannelCreated;
        }

        public DMGuildViewer(IEnumerable<IGroupChannel> channels)
        {
            InitializeComponent();
            if (channels.Any())
            {
                group = true;
                guildName.Text = "Group Channels";
                guildDescription.Text = channels.Count() + " groups";

                foreach (IGroupChannel channel in channels)
                {
                    DMChannelViewer viewer = new DMChannelViewer(channel);
                    channelsList.Items.Add(viewer);
                }

                App.DiscordWindow.Client.ChannelCreated += Client_ChannelCreated;
            }
            else
            {
                Visibility = Visibility.Collapsed;
            }
        }

        private async Task Client_ChannelCreated(IChannel arg)
        {
            if (arg is IDMChannel && !group)
            {
                await Dispatcher.InvokeAsync(() => channelsList.Items.Clear());
                foreach (IDMChannel channel in App.DiscordWindow.Client.DMChannels)
                {
                    await Dispatcher.InvokeAsync(() =>
                    {
                        DMChannelViewer viewer = new DMChannelViewer(channel);
                        channelsList.Items.Add(viewer);
                    });
                }
            }
            else if(arg is IGroupChannel && group)
            {
                await Dispatcher.InvokeAsync(() => channelsList.Items.Clear());
                foreach (IGroupChannel channel in App.DiscordWindow.Client.GroupChannels)
                {
                    await Dispatcher.InvokeAsync(() =>
                    {
                        DMChannelViewer viewer = new DMChannelViewer(channel);
                        channelsList.Items.Add(viewer);
                    });
                }
            }
        }

        private void ListViewItem_MouseUp(object sender, MouseButtonEventArgs e)
        {
            if (channelsList.Visibility == Visibility.Collapsed)
                channelsList.Visibility = Visibility.Visible;
            else
                if (!channelsList.IsMouseOver)
                channelsList.Visibility = Visibility.Collapsed;
        }
    }
}

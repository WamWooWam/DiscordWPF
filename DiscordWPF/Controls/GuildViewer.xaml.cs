using Discord;
using Discord.WebSocket;
using DiscordWPF.Data;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading;
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
    public partial class GuildViewer : ListViewItem
    {
        public IGuild Guild;
        public bool IsAvailable { get; set; } = false;
        int badgeCount = 0; 

        public GuildViewer(IGuild guild)
        {
            InitializeComponent();

            Guild = guild;

            guildName.Text = guild.Name;

            App.DiscordWindow.Client.ChannelUpdated += Client_ChannelUpdated;
            App.DiscordWindow.Client.ChannelCreated += Client_ChannelCreated;
            App.DiscordWindow.Client.GuildUpdated += Client_GuildUpdated;
            App.DiscordWindow.Client.MessageReceived += Client_MessageReceived;
        }

        private async Task Client_ChannelUpdated(IChannel arg1, IChannel arg2) => await Client_ChannelCreated(arg2);

        private async Task Client_MessageReceived(SocketMessage arg)
        {
            if (arg.Channel is ITextChannel && !arg.Channel.IsMuted())
            {
                ITextChannel chan = (arg.Channel as ITextChannel);
                IGuild guild = chan.Guild;

                if (guild.Id == Guild.Id)
                {
                    ulong currentId = await Dispatcher.InvokeAsync(() => App.DiscordWindow.Client.CurrentUser.Id);
                    if (arg.MentionedUsers.Any( u => u.Id == currentId) || arg.MentionedRoles.Any(r => r.Members.Any(u => u.Id == currentId)))
                    {
                        await Dispatcher.InvokeAsync(() =>
                        {
                            badgeCount += 1;
                            badgeText.Text = badgeCount.ToString();
                            badgeMain.Visibility = Visibility.Visible;
                        });
                    }
                }
            }
        }

        private async Task Client_GuildUpdated(IGuild arg1, IGuild arg2)
        {
            if (arg1.Id == Guild.Id)
            {
                Guild = arg2;
                ITextChannel channel = await (arg2.GetDefaultChannelAsync());
                await Dispatcher.InvokeAsync(() =>
                {
                    guildName.Text = arg2.Name;
                    guildDescription.Text = "#" + channel.Name;
                    guildImage.ImageSource = new BitmapImage(new Uri(Guild.IconUrl));
                });
            }
        }

        private async Task Client_ChannelCreated(IChannel arg)
        {
            if (arg is IGuildChannel && (arg as IGuildChannel).Guild.Id == Guild.Id)
            {
                await Dispatcher.InvokeAsync(() =>
                {
                    channelsList.Items.Clear();
                });

                IEnumerable<ITextChannel> channels = (await (arg as IGuildChannel).Guild.GetTextChannelsAsync());

                foreach (ITextChannel channel in channels.Where(c => (c.GetUsersAsync().Flatten().Result).Any(u => u.Id == App.DiscordWindow.Client.CurrentUser.Id)).OrderBy(c => c.Position))
                {
                    await Dispatcher.InvokeAsync(() =>
                    {
                        ChannelViewer viewer = new ChannelViewer(channel);
                        channelsList.Items.Add(viewer);
                    });
                }

                await Dispatcher.InvokeAsync(() =>
                {
                    channelsList.Items.Add(new Separator());
                });

                foreach (IVoiceChannel channel in await (arg as IGuildChannel).Guild.GetVoiceChannelsAsync())
                {
                    await Dispatcher.InvokeAsync(() =>
                    {
                        ChannelViewer viewer = new ChannelViewer(voiceChan: channel);
                        channelsList.Items.Add(viewer);
                    });
                }
            }
        }

        private void ListViewItem_MouseUp(object sender, MouseButtonEventArgs e)
        {
            if (IsAvailable)
            {
                if (!SystemParameters.SwapButtons)
                {
                    badgeCount = 0;
                    badgeText.Text = badgeCount.ToString();
                    badgeMain.Visibility = Visibility.Hidden;

                    if (channelsList.Visibility == Visibility.Collapsed)
                        channelsList.Visibility = Visibility.Visible;
                    else
                        if (!channelsList.IsMouseOver)
                        channelsList.Visibility = Visibility.Collapsed;
                }
            }
            else
                App.DiscordWindow.StatusShowError("This guild is currently unavailable.", false);
        }

        private async void ListViewItem_Loaded(object sender, RoutedEventArgs e)
        {
            if (await Guild.GetDefaultChannelAsync() != null)
                guildDescription.Text = "#" + (await Guild.GetDefaultChannelAsync()).Name;

            foreach (ITextChannel channel in (await Guild.GetTextChannelsAsync()).Where(c => c.GetUsersAsync().Flatten().Result.Any(u => u.Id == App.DiscordWindow.Client.CurrentUser.Id)).OrderBy(c => c.Position))
            {
                ChannelViewer viewer = new ChannelViewer(channel);
                channelsList.Items.Add(viewer);
            }

            if ((await Guild.GetVoiceChannelsAsync()).Any())
            {
                channelsList.Items.Add(new Separator());

                foreach (IVoiceChannel channel in await Guild.GetVoiceChannelsAsync())
                {
                    ChannelViewer viewer = new ChannelViewer(voiceChan: channel);
                    channelsList.Items.Add(viewer);
                }
            }
        }
    }
}

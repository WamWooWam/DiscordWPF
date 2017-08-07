using Discord;
using Discord.WebSocket;
using DiscordWPF.Data;
using FontAwesome.WPF;
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
    /// Interaction logic for ChannelViewer.xaml
    /// </summary>
    public partial class ChannelViewer : ListViewItem
    {
        public ITextChannel TextChannel;
        public IVoiceChannel VoiceChannel;
        Brush foreground = App.ForegroundBrush;

        public ChannelViewer(ITextChannel txtChan = null, IVoiceChannel voiceChan = null)
        {
            InitializeComponent();

            if (txtChan != null)
            {
                channelIcon.Icon = FontAwesomeIcon.Hashtag;
                channelName.Text = txtChan.Name;
                TextChannel = txtChan;
                App.DiscordWindow.Client.MessageReceived += Client_MessageReceived;

                ContextMenu = new ContextMenu();

                MenuItem mute = new MenuItem();
                mute.Header = $"Mute #{txtChan.Name}";
                mute.IsCheckable = true;
                mute.IsChecked = txtChan.IsMuted();

                mute.Unchecked += (o, e) =>
                {
                    App.Config.MutedChannels.Remove(txtChan.Id);
                    App.Config.Save();

                    ChangeTheme();
                };
                mute.Checked += (o, e) =>
                {
                    App.Config.MutedChannels.Add(txtChan.Id);
                    App.Config.Save();

                    ChangeTheme();
                };

                ContextMenu.Items.Add(mute);
            }
            else if (voiceChan != null)
            {
                channelIcon.Icon = FontAwesomeIcon.VolumeUp;
                channelName.Text = voiceChan.Name;
                VoiceChannel = voiceChan;
            }

            ChangeTheme();

            App.DiscordWindow.Client.ChannelUpdated += Client_ChannelUpdated;
            App.DiscordWindow.Client.ChannelDestroyed += Client_ChannelDestroyed;
        }

        private async Task Client_ChannelDestroyed(IChannel arg)
        {
            if (arg.Id == TextChannel?.Id || arg.Id == VoiceChannel?.Id)
            {
                await (Parent as ListBox).Dispatcher.InvokeAsync(() => (Parent as ListBox).Items.Remove(this));
            }
        }

        private async Task Client_ChannelUpdated(IChannel arg1, IChannel arg2)
        {
            if (arg1.Id == TextChannel?.Id || arg1.Id == VoiceChannel?.Id)
            {
                if (arg2 is ITextChannel)
                {
                    await Dispatcher.InvokeAsync(() =>
                    {
                        channelName.Text = (arg2 as ITextChannel).Name;
                        TextChannel = (arg2 as ITextChannel);
                    });
                }
                else if (arg2 is IVoiceChannel)
                {
                    await Dispatcher.InvokeAsync(() =>
                    {
                        channelName.Text = (arg2 as IVoiceChannel).Name;
                        VoiceChannel = (arg2 as IVoiceChannel);
                    });
                }
            }
        }

        private async Task Client_MessageReceived(IMessage arg)
        {
            if (arg.Channel.Id == TextChannel.Id && DiscordWindow.selectedTextChannel != TextChannel && !TextChannel.IsMuted())
                await Dispatcher.InvokeAsync(() => Foreground = Brushes.Red);
        }

        private void channelName_LayoutUpdated(object sender, EventArgs e)
        {
            //ChangeTheme();
        }

        public void ChangeTheme()
        {
            if (TextChannel != null)
            {
                if (TextChannel.IsMuted())
                    foreground = App.SecondaryForegroundBrush;
                else
                    foreground = App.ForegroundBrush;

                Foreground = foreground;
            }
            else if (VoiceChannel != null)
            {
                Foreground = App.ForegroundBrush;
            }

        }

        private async void ListViewItem_PreviewMouseLeftButtonUp(object sender, MouseButtonEventArgs e)
        {
            if (TextChannel != null)
                await App.DiscordWindow.Refresh(TextChannel);
            ChangeTheme();
        }
    }
}

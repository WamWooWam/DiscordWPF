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
    /// Interaction logic for ChannelViewer.xaml
    /// </summary>
    public partial class DMChannelViewer : ListViewItem
    {
        public IMessageChannel Channel;

        public DMChannelViewer(IGroupChannel channel)
        {
            InitializeComponent();
            Channel = channel;
        }


        public DMChannelViewer(IDMChannel channel)
        {
            InitializeComponent();
            Channel = channel;
        }

        private async Task Client_ChannelDestroyed(IChannel arg)
        {
            if (arg.Id == Channel.Id)
            {
                await Dispatcher.InvokeAsync(() => (Parent as ListView).Items.Remove(this));
            }
        }

        private async void CloseDM_Click(object sender, RoutedEventArgs e)
        {
            if (Channel is IDMChannel)
                await (Channel as IDMChannel).CloseAsync();
            else if (Channel is IGroupChannel)
                await (Channel as IGroupChannel).LeaveAsync();
        }

        private async Task Client_UserUpdated(IUser arg1, IUser arg2)
        {
            if (arg1.Id == (Channel as IDMChannel).Recipient.Id)
            {
                await Dispatcher.InvokeAsync(() =>
                {
                    channelName.Text = arg2.Username;

                    string url = (Channel as IDMChannel).Recipient.GetAvatarUrl();
                    if (url != null)
                        channelIcon.ImageSource = new BitmapImage(new Uri(url));
                    else
                        channelIcon.ImageSource = App.Current.Resources["StockPFP"] as BitmapImage;

                    channelDescription.Text = arg2.Status.ToString();
                    if (arg2.Status == UserStatus.Online)
                        channelDescription.Foreground = Brushes.LimeGreen;
                    else if (arg2.Status == UserStatus.Idle || arg2.Status == UserStatus.AFK)
                        channelDescription.Foreground = Brushes.Orange;
                    else if (arg2.Status == UserStatus.DoNotDisturb)
                        channelDescription.Foreground = Brushes.Red;
                });
            }
        }

        private void channelName_LayoutUpdated(object sender, EventArgs e)
        {

        }

        private async void channelName_MouseUp(object sender, MouseButtonEventArgs e)
        {
            if (Channel != null)
                await App.DiscordWindow.Refresh(Channel);
        }

        private async void ListViewItem_Loaded(object sender, RoutedEventArgs e)
        {  
            if (Channel is IGroupChannel)
            {
                IEnumerable<IUser> users = await Channel.GetUsersAsync().Flatten();
                channelName.Text = Channel.Name ?? string.Join(", ", users.Select(u => u.Username));

                groupIcon.Visibility = Visibility.Visible;
                channelDescription.Text = users.Count() + " users";

                ContextMenu cm = new ContextMenu();
                MenuItem closeDM = new MenuItem();
                closeDM.Header = "Leave group";
                closeDM.Click += CloseDM_Click;
                cm.Items.Add(closeDM);
                ContextMenu = cm;

                App.DiscordWindow.Client.ChannelDestroyed += Client_ChannelDestroyed;
            }
            else
            {
                IDMChannel channel = Channel as IDMChannel;

                channelName.Text = channel.Recipient.Username;

                channelIconContainer.Visibility = Visibility.Visible;

                string url = channel.Recipient.GetAvatarUrl();
                if (url != null)
                    channelIcon.ImageSource = new BitmapImage(new Uri(url));
                else
                    channelIcon.ImageSource = App.Current.Resources["StockPFP"] as BitmapImage;

                channelDescription.Text = channel.Recipient.Status.ToString();

                ContextMenu cm = new ContextMenu();
                MenuItem closeDM = new MenuItem();
                closeDM.Header = "Close DM";
                closeDM.Click += CloseDM_Click;
                cm.Items.Add(closeDM);
                ContextMenu = cm;

                if (channel.Recipient.Status == UserStatus.Online)
                    channelDescription.Foreground = Brushes.LimeGreen;
                else if (channel.Recipient.Status == UserStatus.Idle || channel.Recipient.Status == UserStatus.AFK)
                    channelDescription.Foreground = Brushes.Orange;
                else if (channel.Recipient.Status == UserStatus.DoNotDisturb)
                    channelDescription.Foreground = Brushes.Red;

                App.DiscordWindow.Client.UserUpdated += Client_UserUpdated;
                App.DiscordWindow.Client.ChannelDestroyed += Client_ChannelDestroyed;
            }
        }
    }
}

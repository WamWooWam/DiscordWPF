using Discord;
using Discord.WebSocket;
using DiscordWPF.Data;
using DiscordWPF.Dialogs;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Controls.Primitives;
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
    public partial class UserViewer : ListViewItem
    {
        public IGuildUser user;
        public IGuild guild;

        public UserViewer(IGuildUser user)
        {
            InitializeComponent();
            this.user = user;
            guild = user.Guild;
            MouseUp += UserName_MouseUp;
        }

        private void UserName_MouseUp(object sender, MouseButtonEventArgs e)
        {
            if (!SystemParameters.SwapButtons)
            {
                UserInfo viewer = new UserInfo(user, this, true);
                viewer.Show();
            }
        }

        private async void ListViewItem_Loaded(object sender, RoutedEventArgs e)
        {
            userName.Text = Tools.Name(user);

            System.Windows.Media.Color? fg = await Task.Factory.StartNew(() => Tools.GetUserColour(user, user.Guild));
            if (fg.HasValue)
                await Dispatcher.InvokeAsync(() => userName.Foreground = new SolidColorBrush(fg.Value));

            ContextMenu = await Tools.GetUserContextMenu(user, this);

            userStatus.Text = "";
            await Dispatcher.InvokeAsync(() =>
            {
                if (user.Game != null)
                {
                    Run run = new Run("Playing");
                    run.FontWeight = FontWeights.Bold;
                    userStatus.Inlines.Add(run);
                    Run name = new Run(" " + user.Game.Value.Name);
                    userStatus.Inlines.Add(name);
                }
                else
                    userStatus.Text = user.Status.ToString();

                if (user.Status == Discord.UserStatus.Online)
                    userStatus.Foreground = Brushes.LimeGreen;
                else if (user.Status == Discord.UserStatus.Idle || user.Status == Discord.UserStatus.AFK)
                    userStatus.Foreground = Brushes.Orange;
                else if (user.Status == Discord.UserStatus.DoNotDisturb)
                {
                    userStatus.Text = "Do not disturb";
                    userStatus.Foreground = Brushes.Red;
                }
                else
                {
                    Opacity = 0.75;
                }
                if (user.Game.HasValue && user.Game.Value.StreamType == Discord.StreamType.Twitch)
                {
                    playingText.Text = "Streaming";
                    userStatus.Foreground = Brushes.Purple;
                }
                else
                    playingText.Text = "Playing";
            });

            string url = user.GetAvatarUrl(Discord.ImageFormat.Png);
            if (url != null)
                await Dispatcher.InvokeAsync(() => channelIcon.ImageSource = Images.GetImage(url));
            else
                await Dispatcher.InvokeAsync(() => channelIcon.ImageSource = App.Current.Resources["StockPFP"] as BitmapImage);
        }
    }
}

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
using System.Windows.Shapes;

namespace DiscordWPF.Dialogs
{
    /// <summary>
    /// Interaction logic for UserInfo.xaml
    /// </summary>
    public partial class UserInfo : Window
    {
        IGuildUser user;
        FrameworkElement sourceElement;
        bool showLeft = false;

        public UserInfo(IGuildUser user, FrameworkElement source, bool left = false)
        {
            InitializeComponent();
            this.user = user;
            sourceElement = source;
            showLeft = left;

            System.Windows.Media.Color? fg = Tools.GetUserColour(user, user.Guild);
            if (fg.HasValue)
                userTitle.Foreground = new SolidColorBrush(fg.Value);

            userTitle.Text = Tools.Name(user);
            userSubtitle.Text = $"@{user.Username}#{user.Discriminator}";

            userImage.ImageSource = new BitmapImage(new Uri(user.GetAvatarUrl()));

            userGame.Text = "";

            Run game = new Run("Playing ");
            game.FontWeight = FontWeights.Bold;
            userGame.Inlines.Add(game);

            Run gameText = new Run();
            if (user.Game.HasValue)
                gameText.Text += user.Game.Value;
            else
                gameText.Text += "nothing.";
            userGame.Inlines.Add(gameText);

            if (user.Status == Discord.UserStatus.Online)
                userGame.Foreground = Brushes.LimeGreen;
            else if (user.Status == Discord.UserStatus.Idle || user.Status == Discord.UserStatus.AFK)
                userGame.Foreground = Brushes.Orange;
            else if (user.Status == Discord.UserStatus.DoNotDisturb)
                userGame.Foreground = Brushes.Red;

            if (user.RoleIds.Any())
            {
                foreach (IRole role in user.Guild.Roles.Where(r => user.RoleIds.Contains(r.Id)))
                {
                    ListViewItem item = new ListViewItem();
                    item.Content = role.Name;
                    if (role.Color.RawValue != Discord.Color.Default.RawValue)
                        item.Foreground = new SolidColorBrush(System.Windows.Media.Color.FromRgb(role.Color.R, role.Color.G, role.Color.B));
                    roleList.Items.Add(item);
                }
            }
            else
            {
                ListViewItem item = new ListViewItem();
                item.HorizontalContentAlignment = HorizontalAlignment.Center;
                item.Content = "This user has no roles.";
                roleList.Items.Add(item);
            }

            (App.Current.MainWindow).PreviewMouseDown += (s, e) => Close();
        }

        private void Window_Loaded(object sender, RoutedEventArgs e)
        {
            this.Topmost = true;

            Point mousePositionInApp;
            if (showLeft)
                mousePositionInApp = sourceElement.PointToScreen(new Point(-ActualWidth, sourceElement.ActualHeight));
            else
                mousePositionInApp = sourceElement.PointToScreen(new Point(sourceElement.ActualWidth, sourceElement.ActualHeight));

            Left = mousePositionInApp.X;
            Top = mousePositionInApp.Y - (ActualHeight / 2);

            if ((Left + ActualWidth) > SystemParameters.VirtualScreenWidth)
            {
                Left = SystemParameters.VirtualScreenWidth - ActualWidth - 10;
            }

            if (Top < SystemParameters.VirtualScreenTop)
            {
                Top = SystemParameters.VirtualScreenTop + 10;
            }

        }
    }
}

using DiscordWPF.Pages;
using DiscordWPF.Pages.Sidebar;
using DSharpPlus.Entities;
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

namespace DiscordWPF.Windows
{
    /// <summary>
    /// Interaction logic for GuildWindow.xaml
    /// </summary>
    public partial class GuildWindow : Window
    {
        private GuildChannelPage channelPage;

        public DiscordGuild Guild { get; }

        public GuildWindow(DiscordGuild guild)
        {
            InitializeComponent();
            Guild = guild;
            DataContext = guild;
        }

        private void Window_Loaded(object sender, RoutedEventArgs e)
        {
            channelPage = new GuildChannelPage() { DataContext = Guild };
            sidebarFrame.Navigate(channelPage);
        }

        private void mainFrame_Navigating(object sender, NavigatingCancelEventArgs e)
        {
            if (e.Content is ChannelPage page)
            {
                if ((channelPage.channelsList.SelectedItem as DiscordChannel) != page.Channel)
                {
                    channelPage.ChangeSelection(page.Channel);
                }
            }
        }
    }
}

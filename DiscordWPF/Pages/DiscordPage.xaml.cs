using DiscordWPF.Net;
using DiscordWPF.Pages.Sidebar;
using DSharpPlus;
using DSharpPlus.Entities;
using DSharpPlus.EventArgs;
using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.ComponentModel;
using System.Linq;
using System.Reflection;
using System.Text;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Data;
using System.Windows.Documents;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Media.Animation;
using System.Windows.Media.Imaging;
using System.Windows.Navigation;
using System.Windows.Shapes;
using System.Windows.Threading;
using WamWooWam.Core;
using WamWooWam.Wpf;

using Markdig.Wpf;
using System.Diagnostics;
using System.IO;
using DiscordWPF.Windows;

namespace DiscordWPF.Pages
{
    /// <summary>
    /// Interaction logic for DiscordPage.xaml
    /// </summary>
    public partial class DiscordPage : Page
    {
        public Frame Frame => mainFrame;
        int _navigated = 0;

        public DiscordPage()
        {
            InitializeComponent();
        }

        private async void Page_Loaded(object sender, RoutedEventArgs e)
        {
            mainFrame.Navigate(new FriendsPage());

            var version = Assembly.GetEntryAssembly().GetName().Version;
            if (Version.TryParse(Settings.GetSetting("LastSeenVersion", "1.0.0.0"), out var lastVersion))
            {
                if (version > lastVersion)
                {
                    try
                    {
                        var update = await Update.GetUpdateDetailsAsync(version);
                        if (update != null && !string.IsNullOrWhiteSpace(update.NewVersionDetails))
                        {
                            updateDoc.Document = Markdown.ToFlowDocument(update.NewVersionDetails, new Markdig.MarkdownPipelineBuilder().UseSupportedExtensions().Build());
                            updateOverlay.Visibility = Visibility.Visible;
                        }
                    }
                    finally
                    {
                        Settings.SetSetting("LastSeenVersion", version.ToString());
                    }
                }
            }

            _navigated++;
            if (_navigated == 3)
            {
                await Dispatcher.InvokeAsync(() => this.FindVisualParent<MainWindow>().HideConnectingOverlay(), DispatcherPriority.ApplicationIdle);
            }

            //if(App.Abstractions.GetToken("OAuth") == null)
            //{
            //    var wind = new OAuthLoginWindow() { Owner = this.FindVisualParent<Window>() };
            //    wind.Show();
            //}
        }

        private void hideUpdate_Completed(object sender, EventArgs e)
        {
            updateOverlay.Visibility = Visibility.Hidden;
        }

        private void updateHideButton_Click(object sender, RoutedEventArgs e)
        {
            (Resources["hideUpdate"] as Storyboard).Begin();
        }

        private async void Frame_Navigated(object sender, NavigationEventArgs e)
        {
            _navigated++;
            if (_navigated == 3)
            {
                await Dispatcher.InvokeAsync(() => this.FindVisualParent<MainWindow>().HideConnectingOverlay(), DispatcherPriority.ApplicationIdle);
            }
        }

        private void Button_Click(object sender, RoutedEventArgs e)
        {
            if (sidebarFrame.CanGoBack)
            {
                sidebarFrame.GoBack();
            }
        }

        private void mainFrame_Navigating(object sender, NavigatingCancelEventArgs e)
        {
            if (e.Content is ChannelPage cpage && sidebarFrame.Content is GuildChannelPage gpage)
            {
                if ((gpage.channelsList.SelectedItem as DiscordChannel) != cpage.Channel)
                {
                    gpage.ChangeSelection(cpage.Channel);
                }
            }
        }

        private void CommandBinding_Executed(object sender, ExecutedRoutedEventArgs e)
        {
            Process.Start(e.Parameter.ToString());
        }
    }
}

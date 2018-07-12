using DiscordWPF.Net;
using DiscordWPF.Pages;
using DiscordWPF.Windows;
using DSharpPlus.EventArgs;
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
using System.Windows.Interop;
using System.Windows.Media;
using System.Windows.Media.Animation;
using System.Windows.Media.Imaging;
using System.Windows.Navigation;
using System.Windows.Shapes;
using System.Windows.Threading;
using WamWooWam.Wpf;

namespace DiscordWPF
{
    /// <summary>
    /// Interaction logic for MainWindow.xaml
    /// </summary>
    public partial class MainWindow : Window
    {
        public MainWindow()
        {
            InitializeComponent();
            Width = Math.Min(SystemParameters.PrimaryScreenWidth - 40, Width);
        }

        private async void Window_Loaded(object sender, RoutedEventArgs e)
        {
            ShowConnectingOverlay();

            await loadingControl.ChangeStatusAsync("Checking for Updates...");
            
            if (await Update.CheckForUpdates())
            {
                await loadingControl.ChangeStatusAsync("Preparing to install...");
                await Update.RunUpdateAsync();
                return;
            }

            await loadingControl.ChangeStatusAsync("Initialising...");
            await Dispatcher.InvokeAsync(() => App.InitialiseCEF(), DispatcherPriority.Loaded);

            var token = App.Abstractions.GetToken("Default");
            if (!string.IsNullOrWhiteSpace(token))
            {
                await loadingControl.ChangeStatusAsync("Connecting to Discord...");
                await App.LoginAsync(token, OnReady, OnError);
            }
            else
            {
                rootFrame.Navigate(new LoginPage());
            }
        }

        private async Task OnReady(ReadyEventArgs e)
        {
            await loadingControl.ChangeStatusAsync("Ready!");
            await Dispatcher.InvokeAsync(() => rootFrame.Navigate(new Uri("pack://application:,,,/Pages/DiscordPage.xaml")));
        }

        private async Task OnError(Exception arg)
        {
            if (arg is UnauthorizedAccessException)
            {
                await Dispatcher.InvokeAsync(() =>
                {
                    rootFrame.Navigate(new LoginPage());
                    App.Abstractions.SetToken("Default", null);
                    HideConnectingOverlay();
                });
            }
        }

        internal void ShowConnectingOverlay()
        {
            connectingOverlay.Visibility = Visibility.Visible;
            (Resources["showConnecting"] as Storyboard).Begin();
        }

        internal void HideConnectingOverlay()
        {
            (Resources["hideConnecting"] as Storyboard).Begin();
        }

        private void hideConnecting_Completed(object sender, EventArgs e)
        {
            connectingOverlay.Visibility = Visibility.Hidden;
        }

        private void rootFrame_Navigating(object sender, NavigatingCancelEventArgs e)
        {
            if (e.Uri?.IsAbsoluteUri != true)
                return;
            if (e.Uri?.ToString().StartsWith("pack") == true)
                return;

            e.Cancel = true;
        }
    }
}

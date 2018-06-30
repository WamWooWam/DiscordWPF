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
        }

        private async void Window_Loaded(object sender, RoutedEventArgs e)
        {
            ShowConnectingOverlay();

            await ChangeStatus("Checking for Updates...");

            var update = await Update.CheckForUpdates();
            if (update?.UpdateAvailable == true)
            {
                await Update.RunUpdateAsync(update.Details, ChangeStatus);
                return;
            }

            await ChangeStatus("Initialising...");
            await Dispatcher.InvokeAsync(() => App.InitialiseCEF());

            var token = App.Abstractions.GetToken("Default");
            if (!string.IsNullOrWhiteSpace(token))
            {
                await ChangeStatus("Connecting to Discord...");
                await App.LoginAsync(token, OnReady, OnError);
            }
            else
            {
                rootFrame.Navigate(new LoginPage());
            }
        }

        internal async Task ChangeStatus(string text, int? value = null, int? max = null)
        {
            await Dispatcher.InvokeAsync(() =>
            {
                connectingStatus.Text = text;
                connetingProgress.Maximum = max ?? connetingProgress.Maximum;
                if (value == null)
                {
                    connetingProgress.IsIndeterminate = true;
                }
                else
                {
                    connetingProgress.IsIndeterminate = false;
                    connetingProgress.Value = value.Value;
                }
            });
        }

        private async Task OnReady(ReadyEventArgs e)
        {
            await ChangeStatus("Ready!");
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
            connetingProgress.IsIndeterminate = true;
            (Resources["showConnecting"] as Storyboard).Begin();
        }

        internal void HideConnectingOverlay()
        {
            (Resources["hideConnecting"] as Storyboard).Begin();
        }

        private void hideConnecting_Completed(object sender, EventArgs e)
        {
            connectingOverlay.Visibility = Visibility.Hidden;
            connetingProgress.IsIndeterminate = false;
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

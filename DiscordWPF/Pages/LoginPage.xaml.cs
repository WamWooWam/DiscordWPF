using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Web;
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
using CefSharp.Wpf;
using DSharpPlus.EventArgs;
using DSharpPlus.Exceptions;
using WamWooWam.Core;
using WamWooWam.Wpf;

namespace DiscordWPF.Pages
{
    /// <summary>
    /// Interaction logic for LoginPage.xaml
    /// </summary>
    public partial class LoginPage : Page
    {
        public LoginPage()
        {
            InitializeComponent();
        }

        private async void Button_Click(object sender, RoutedEventArgs e)
        {
            string password = tokenTextBox.Password.Trim('"');

            if (!string.IsNullOrWhiteSpace(password))
            {
                var window = this.FindVisualParent<MainWindow>();
                window.connectingStatus.Text = "Connecting to Discord...";
                window.ShowConnectingOverlay();

                await App.LoginAsync(password, OnReady, OnError);
            }
            else
            {
                App.ShowErrorDialog("For fucks sake", null, "Enter a token dummy!");
            }
        }

        private async Task OnReady(ReadyEventArgs e)
        {
            App.Abstractions.SetToken("Default", await Dispatcher.InvokeAsync(() => tokenTextBox.Password.Trim('"')));
            await Dispatcher.InvokeAsync(() => NavigationService.Navigate(new DiscordPage()));
        }

        private async Task OnError(Exception arg)
        {
            await Dispatcher.InvokeAsync(() =>
            {
                (this).FindVisualParent<MainWindow>().HideConnectingOverlay();

                var content = arg is UnauthorizedAccessException ? "Looks like something's up with your token, and I couldn't log you in! Check and try again." : $"Welp, something went wrong logging you in there! {arg.Message}";
                App.ShowErrorDialog("Unable to login", "Unable to login", content);
            });
        }

        private void Page_Loaded(object sender, RoutedEventArgs e)
        {
            this.FindVisualParent<MainWindow>().HideConnectingOverlay();
        }

        private void loginManualButton_Click(object sender, RoutedEventArgs e)
        {
            manualLogin.Visibility = Visibility.Visible;
            (Resources["showManualLogin"] as Storyboard).Begin();
        }

        private void backButton_Click(object sender, RoutedEventArgs e)
        {
            (Resources["hideManualLogin"] as Storyboard).Begin();
        }

        const ulong ID = 461195843165356034;
        const string REDIRECT_URL = "https://dwpf.wankerr.com/oauth/redirect";

        private void discordLoginButton_Click(object sender, RoutedEventArgs e)
        {
            (Resources["showDiscord"] as Storyboard).Begin();

            var encoded_url = HttpUtility.UrlEncode(REDIRECT_URL);
            var url = $"https://discordapp.com/api/oauth2/authorize?client_id={ID}&redirect_uri={encoded_url}&response_type=code&scope=identify%20email";
            browser.Load(url);
        }

        private void Storyboard_Completed(object sender, EventArgs e)
        {
            grid.Visibility = Visibility.Collapsed;
        }

        private async void browser_FrameLoadStart(object sender, CefSharp.FrameLoadStartEventArgs e)
        {
            if (e.Frame.IsMain)
            {
                Dispatcher.Invoke(() =>
                {
                    browserLoading.IsIndeterminate = true;
                    urlBar.Text = e.Url;
                });

                if (e.Url.StartsWith(REDIRECT_URL))
                {
                    e.Frame.Browser.StopLoad();

                    var uri = new Uri(e.Url);
                    var query = HttpUtility.ParseQueryString(uri.Query);
                    var code = query["code"];

                    App.Abstractions.SetToken("OAuth", code);

                    var t = App.Abstractions.GetToken("Default");
                    if (t != null)
                    {
                        await Dispatcher.InvokeAsync(() =>
                        {
                            grid.Visibility = Visibility.Visible;
                            (Resources["hideDiscord"] as Storyboard).Begin();

                            var window = this.FindVisualParent<MainWindow>();
                            window.connectingStatus.Text = "Connecting to Discord...";
                            window.ShowConnectingOverlay();
                        });
                        await App.LoginAsync(t, OnReady, OnError);
                    }
                }
            }

        }

        private async void browser_FrameLoadEnd(object sender, CefSharp.FrameLoadEndEventArgs e)
        {
            if (e.Frame.IsMain)
            {
                Dispatcher.Invoke(() =>
                {
                    browserLoading.IsIndeterminate = false;
                    urlBar.Text = e.Url;
                });

                var uri = new Uri(e.Url);
                if (uri.Host.EndsWith("discordapp.com"))
                {
                    try
                    {
                        var thing = await e.Frame.EvaluateScriptAsync("document.body.appendChild(document.createElement('iframe')).contentWindow.localStorage.token");
                        if (thing.Success && thing.Result is string s)
                        {
                            App.Abstractions.SetToken("Default", s.Trim('"'));
                        }
                    }
                    catch { }
                }
            }
        }

        private void webBackButton_Click(object sender, RoutedEventArgs e)
        {
            var browser1 = browser.GetBrowser();

            if (browser1.CanGoBack)
            {
                browser1.GoBack();
            }
            else
            {
                grid.Visibility = Visibility.Visible;
                (Resources["hideDiscord"] as Storyboard).Begin();
            }
        }
    }
}

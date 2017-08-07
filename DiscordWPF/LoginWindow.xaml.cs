using DiscordWPF.Data;
using DiscordWPF.Properties;
using Newtonsoft.Json;
using Ookii.Dialogs;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Net.Http;
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

namespace DiscordWPF
{
    /// <summary>
    /// Interaction logic for MainWindow.xaml
    /// </summary>
    public partial class LoginWindow : Window
    {
        public LoginWindow()
        {
            InitializeComponent();
            Background = App.BackgroundBrush;
            Foreground = App.ForegroundBrush;

            if ((App.BackgroundBrush.Color.R * 0.299 + App.BackgroundBrush.Color.G * 0.587 + App.BackgroundBrush.Color.B * 0.114) > 186)
            {
                discordImage.Source = Application.Current.Resources["DiscordLogoBlack"] as BitmapImage;
            }
            else
            {
                discordImage.Source = Application.Current.Resources["DiscordLogoWhite"] as BitmapImage;
            }
        }

        private void Hyperlink_Click(object sender, RoutedEventArgs e)
        {
            //InputDialog dialog = new InputDialog();
            //dialog.MainInstruction = "Enter your Discord token.";
            //dialog.Content = "Playing it safe are we? Hey, I don't blame you. Enter your Discord token here, I'm assuming you know how to get it.";

            //if (dialog.ShowDialog() == System.Windows.Forms.DialogResult.OK)
            //{
            //    DiscordWindow window = new DiscordWindow(dialog.Input);
            //    if (botToken.IsChecked == true)
            //        window.type = Discord.TokenType.Bot;
            //    window.Show();
            //    App.Current.MainWindow = window;
            //    if (rememberMe.IsChecked == true)
            //    {
            //        App.Config.Token = dialog.Input;
            //        App.Config.Save();
            //    }

            //    Close();
            //}

            if (token.Visibility == Visibility.Visible)
            {
                token.Visibility = Visibility.Collapsed;
                usernamePassword.Visibility = Visibility.Visible;

                hyperlinkText.Text = "Use a token to login...";
            }
            else
            {
                token.Visibility = Visibility.Visible;
                usernamePassword.Visibility = Visibility.Collapsed;

                hyperlinkText.Text = "Use an email/password to login...";
            }
        }

        private async void Button_Click(object sender, RoutedEventArgs e)
        {
            if (token.Visibility == Visibility.Visible)
            {
                DiscordWindow window = new DiscordWindow(tokenBox.Password.Trim('"'));
                if (botToken.IsChecked == true)
                    window.type = Discord.TokenType.Bot;
                window.Show();
                App.Current.MainWindow = window;
                if (rememberMe.IsChecked == true)
                {
                    App.Config.Token = tokenBox.Password.Trim('"');
                    App.Config.Save();
                }

                Close();
            }
            else
            {
                usernameBox.BorderBrush = Brushes.Black;
                passwordBox.BorderBrush = Brushes.Black;
                errorText.Visibility = Visibility.Collapsed;


                errorText.Text = "";
                if (string.IsNullOrEmpty(usernameBox.Text))
                {
                    usernameBox.BorderBrush = Brushes.Red;
                    errorText.Text += "Enter an email address dipshit.\r\n";
                }
                else if (!Tools.IsValidEmail(usernameBox.Text))
                {
                    usernameBox.BorderBrush = Brushes.Red;
                    errorText.Text += "Sorry! That email address doesn't seem right.\r\n";
                }
                if (string.IsNullOrEmpty(passwordBox.Password))
                {
                    passwordBox.BorderBrush = Brushes.Red;
                    errorText.Text += "Enter a password you retard.\r\n";
                }

                if (string.IsNullOrEmpty(errorText.Text))
                {
                    bool did2fa = false;
                    string token = null;
                    using (HttpClient client = new HttpClient())
                    {
                        client.DefaultRequestHeaders.Add("user-agent", "Mozilla/5.0 (Windows NT 6.3; WOW64) AppleWebKit/537.36 (KHTML, like Gecko) discord/0.0.32 Chrome/53.0.2785.143 Discord PTB/1.4.12 Safari/537.36");

                        string initialRequest = JsonConvert.SerializeObject(new LoginRequest()
                        {
                            EmailAddress = usernameBox.Text,
                            Password = passwordBox.Password
                        });
                        StringContent cont = new StringContent(initialRequest, Encoding.UTF8, "application/json");
                        HttpResponseMessage initialResponse = await client.PostAsync("https://discordapp.com/api/v6/auth/login", cont);

                        LoginResponse response = JsonConvert.DeserializeObject<LoginResponse>(await initialResponse.Content.ReadAsStringAsync());
                        if (response.TwoFactor)
                        {
                            did2fa = true;
                            Dialogs.InputDialog dialog = new Dialogs.InputDialog();
                            dialog.MainInstruction = "Enter 2-factor code";
                            dialog.Text = "You seem to have two-factor authentication enabled for your account. Please enter the code here.";
                            if (dialog.ShowDialog() == true)
                            {
                                string twoFAString = dialog.Input;
                                if (Int32.TryParse(twoFAString, out int dontneedthislol))
                                {
                                    string twoFARequest = JsonConvert.SerializeObject(new Login2FARequest()
                                    {
                                        Code = twoFAString,
                                        Ticket = response.Ticket
                                    });
                                    cont = new StringContent(twoFARequest, Encoding.UTF8, "application/json");

                                    HttpResponseMessage twoFAResponse = await client.PostAsync("https://discordapp.com/api/v6/auth/mfa/totp", cont);
                                    response = JsonConvert.DeserializeObject<LoginResponse>(await twoFAResponse.Content.ReadAsStringAsync());

                                    token = response.Token;
                                }
                            }
                        }
                        else
                        {
                            token = response.Token;
                        }

                    }
                    if (token != null)
                    {
                        DiscordWindow window = new DiscordWindow(token);
                        if (botToken.IsChecked == true)
                            window.type = Discord.TokenType.Bot;
                        window.Show();
                        App.Current.MainWindow = window;

                        if (rememberMe.IsChecked == true)
                        {
                            App.Config.Token = token;
                            App.Config.Save();
                        }

                        Close();
                    }
                    else
                    {
                        TaskDialog errorDialog = new TaskDialog();
                        errorDialog.MainIcon = TaskDialogIcon.Error;
                        errorDialog.WindowTitle = "Unable to login.";
                        if (did2fa)
                            errorDialog.Content = $"An error occured while attempting to login! It's more than likely that your two factor auth code was incorrect. Please try again.";
                        else
                            errorDialog.Content = $"An error occured while attempting to login! It's more than likely that your email address/password are incorrect. Please try again.";
                        errorDialog.Buttons.Add(new TaskDialogButton(ButtonType.Ok));
                        errorDialog.Show();
                    }
                }
                else
                {
                    errorText.Visibility = Visibility.Visible;
                }
            }
        }
    }
}

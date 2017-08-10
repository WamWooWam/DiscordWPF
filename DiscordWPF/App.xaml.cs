using Discord.WebSocket;
using DiscordWPF.Data;
using DiscordWPF.Properties;
using Microsoft.Win32;
using Newtonsoft.Json;
using Ookii.Dialogs;
using System;
using System.Collections.Generic;
using System.Configuration;
using System.Data;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Reflection;
using System.Runtime.ExceptionServices;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Media;
using System.Windows.Threading;

namespace DiscordWPF
{
    /// <summary>
    /// Interaction logic for App.xaml
    /// </summary>
    public partial class App : Application
    {
        public static Config Config = new Config();

        public static SolidColorBrush ForegroundBrush => new SolidColorBrush(Theme?.Foreground ?? (App.Current.Resources["LightForegroundBrush"] as SolidColorBrush).Color);
        public static SolidColorBrush SecondaryForegroundBrush => new SolidColorBrush(Theme?.SecondaryForeground ?? Color.FromRgb(106, 106, 106));
        public static SolidColorBrush BackgroundBrush => new SolidColorBrush(Theme?.Background ?? (App.Current.Resources["LightBackgroundBrush"] as SolidColorBrush).Color);
        public static SolidColorBrush SecondaryBackgroundBrush => new SolidColorBrush(Theme?.SecondaryBackground ?? (App.Current.Resources["LightSecondaryBrush"] as SolidColorBrush).Color);
        public static SolidColorBrush SelectedBackgroundBrush => new SolidColorBrush(Theme?.SelectedBackground ?? (App.Current.Resources["LightSelectedBackgroundBrush"] as SolidColorBrush).Color);

        public static Color SuccessColour => Theme?.Success ?? Colors.LimeGreen;
        public static Color WarningColour => Theme?.Warning ?? Colors.Orange;
        public static Color ErrorColour => Theme?.Error ?? Colors.Red;

        public static FontFamily Font;

        public static string CurrentDirectory => Path.GetDirectoryName(Assembly.GetExecutingAssembly().Location);

        public static Theme Theme = null;
        public static int SelectedTheme = 0;

        public static new Dispatcher Dispatcher { get; set; }
        public static DiscordWindow DiscordWindow => Dispatcher.Invoke(() => (App.Current.MainWindow as DiscordWindow));
        public static SocketSelfUser CurrentUser => Dispatcher.Invoke(() => DiscordWindow.Client.CurrentUser);

        public App()
        {
            InitializeComponent();
            Dispatcher = base.Dispatcher;
        }

        private void Application_Startup(object sender, StartupEventArgs e)
        {
            AppDomain.CurrentDomain.FirstChanceException += CurrentDomain_FirstChanceException;
            AppDomain.CurrentDomain.UnhandledException += CurrentDomain_UnhandledException;

            System.Windows.Forms.Application.EnableVisualStyles();
            SetBrowserEmulationMode();

            if (File.Exists(Path.Combine(CurrentDirectory, "Config.json"))) // Checks for configuration file
            {
                try
                {
                    // Loads configuration
                    Config = JsonConvert.DeserializeObject<Config>(
                        File.ReadAllText(Path.Combine(CurrentDirectory, "Config.json")));
                }
                catch
                {
                    // Creates new configuration
                    Config.Save();
                }
            }
            else
            {
                try
                {
                    // Creates new configuration
                    File.WriteAllText(Path.Combine(CurrentDirectory, "Config.json"),
                        JsonConvert.SerializeObject(Config, Formatting.Indented));
                }
                catch
                {
                    // Relaunches app as administrator
                    // Helps if app is installed to Program Files and it can't access config.json
                    TaskDialog dialog = new TaskDialog();
                    dialog.WindowTitle = "Unable to write config file";
                    dialog.MainInstruction = "Unable to write configuration file!";
                    dialog.Content = "Sorry! I was unable to write my configuration file! I may be in a protected directory, do you want to try relaunch as an administrator and try again?";
                    dialog.MainIcon = TaskDialogIcon.Error;
                    dialog.AllowDialogCancellation = true;
                    dialog.ButtonStyle = TaskDialogButtonStyle.CommandLinks;
                    TaskDialogButton relaunchAsAdmin = new TaskDialogButton("Relaunch as administrator\nLets hope this works!\0");
                    relaunchAsAdmin.ElevationRequired = true;
                    dialog.Buttons.Add(relaunchAsAdmin);
                    dialog.Buttons.Add(new TaskDialogButton(ButtonType.Cancel));

                    if (dialog.Show() == relaunchAsAdmin)
                    {
                        ProcessStartInfo info = new ProcessStartInfo(Assembly.GetExecutingAssembly().Location);
                        info.UseShellExecute = true;
                        info.Verb = "runas";
                        info.Arguments = "firstrun";
                        Process.Start(info);
                        Environment.Exit(0);
                    }
                    else
                    {
                        Environment.Exit(0);
                    }
                }
            }

            SelectedTheme = Config.Personalisation.SelectedTheme;

            ChangeTheme();

            Config.Save();

            if (!String.IsNullOrEmpty(App.Config.Token))
            {
                DiscordWindow window = new DiscordWindow(App.Config.Token);
                MainWindow = window;
                window.Show();
            }
            else
            {
                LoginWindow window = new LoginWindow();
                MainWindow = window;
                window.Show();
            }
        }

        private void CurrentDomain_FirstChanceException(object sender, FirstChanceExceptionEventArgs e)
        {
            
        }

        private void CurrentDomain_UnhandledException(object sender, UnhandledExceptionEventArgs e)
        {
            
        }

        public void SetBrowserEmulationMode()
        {
            string fileName = Path.GetFileName(Process.GetCurrentProcess().MainModule.FileName);

            if (String.Compare(fileName, "devenv.exe", true) == 0 || String.Compare(fileName, "XDesProc.exe", true) == 0)
                return;
            UInt32 mode = 11000;
            SetBrowserFeatureControlKey("FEATURE_BROWSER_EMULATION", fileName, mode);
        }

        private void SetBrowserFeatureControlKey(string feature, string appName, uint value)
        {
            using (RegistryKey key = Registry.CurrentUser.CreateSubKey(
                String.Concat(@"Software\Microsoft\Internet Explorer\Main\FeatureControl\", feature),
                RegistryKeyPermissionCheck.ReadWriteSubTree))
            {
                key.SetValue(appName, (UInt32)value, RegistryValueKind.DWord);
            }
        }

        public static void ChangeTheme()
        {
            try
            {
                Theme = Config.Personalisation.Themes[SelectedTheme];
                Font = Theme.Font;

                if (Font == null)
                    Font = Fonts.SystemFontFamilies.First();
                UpdateResources();
            }
            catch
            {
                SelectedTheme = 0;
                Config.Personalisation.SelectedTheme = 0;
                Config.Save();
                ChangeTheme();
            }
        }

        public static void UpdateResources()
        {
            App.Current.Resources["BackgroundBrush"] = BackgroundBrush;
            App.Current.Resources["ForegroundBrush"] = ForegroundBrush;
            App.Current.Resources["SecondaryBackgroundBrush"] = SecondaryBackgroundBrush;
            App.Current.Resources["SecondaryForegroundBrush"] = SecondaryForegroundBrush;
            App.Current.Resources["SelectedBackgroundBrush"] = SelectedBackgroundBrush;

            App.Current.Resources["SuccessBrush"] = new SolidColorBrush(SuccessColour);
            App.Current.Resources["WarningBrush"] = new SolidColorBrush(WarningColour);
            App.Current.Resources["ErrorBrush"] = new SolidColorBrush(ErrorColour);

            App.Current.Resources["Font"] = Font;
        }
    }
}

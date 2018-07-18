
using CefSharp;
using DiscordWPF.Abstractions;
using DiscordWPF.Net;
#if WIN10
using DiscordWPF.Specifics.UWP;
#endif
using DSharpPlus;
using DSharpPlus.EventArgs;
using DSharpPlus.Net.WebSocket;
using Microsoft.ApplicationInsights;
using Ookii.Dialogs.Wpf;
using System;
using System.Collections.Generic;
using System.Configuration;
using System.Data;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Reflection;
using System.Runtime.ExceptionServices;
using System.Runtime.InteropServices;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Media;
using WamWooWam.Core;
using WamWooWam.Wpf;

using static DiscordWPF.Constants;

namespace DiscordWPF
{
    /// <summary>
    /// Interaction logic for App.xaml
    /// </summary>
    public partial class App : Application
    {
        private static int _connectionAttempts = 0;
        private static Guid _instanceId;
        private static bool _cleanShutdown;

        internal static readonly List<ulong> VisibleChannels = new List<ulong>();

        internal static DiscordClient Discord { get; private set; }

        internal static IAbstractions Abstractions { get; private set; }

        internal static TelemetryClient Telemetry { get; private set; }

        internal static Guid InstanceId => _instanceId;

        protected override void OnStartup(StartupEventArgs e)
        {
            Telemetry = new TelemetryClient { InstrumentationKey = "d1a2d606-09ca-41a7-bae7-42bb2982ccb5" };

            var ctx = Telemetry.Context;
            ctx.Session.Id = Guid.NewGuid().ToString();
            ctx.Device.OperatingSystem = Environment.OSVersion.ToString();
            ctx.Component.Version = Assembly.GetEntryAssembly().GetName().Version.ToString();

            if (!Settings.TryGetSetting("InstanceId", out _instanceId))
            {
                ctx.Session.IsFirst = true;

                _instanceId = Guid.NewGuid();
                Settings.SetSetting("InstanceId", _instanceId);
            }

            ctx.User.Id = _instanceId.ToString();

            AppDomain.CurrentDomain.ProcessExit += CurrentDomain_ProcessExit;
            AppDomain.CurrentDomain.FirstChanceException += CurrentDomain_FirstChanceException;

            Telemetry.TrackEvent("Launch");

            InitialiseThemes();

            if (Misc.IsWindows7)
            {
                Abstractions = new Win32Abstractions();
            }
            else
            {
                Abstractions = new UwpAbstractions();
            }
        }

        private void CurrentDomain_FirstChanceException(object sender, FirstChanceExceptionEventArgs e)
        {
            Telemetry.TrackException(e.Exception);
        }

        internal static void InitialiseCEF()
        {
            var settings = new CefSettings();

            var cache = Path.Combine(Settings.SettingsDirectory, "CEFCache");

            if (!Directory.Exists(cache))
                Directory.CreateDirectory(cache);

            settings.LogFile = Path.Combine(cache, "debug.log");
            settings.CachePath = cache;
            settings.DisableTouchpadAndWheelScrollLatching();

            Cef.Initialize(settings);
        }

        private static void InitialiseThemes()
        {
            bool? light = Settings.GetSetting<bool?>(USE_LIGHT_THEME, null);
            Color? accent = Settings.GetSetting<Color?>(CUSTOM_ACCENT_COLOUR, null);

            if (Settings.GetSetting(USE_DISCORD_ACCENT_COLOUR, accent != null && Misc.IsWindows7))
            {
                accent = Color.FromArgb(0xFF, 0x72, 0x89, 0xDA);
            }

            ThemeConfiguration configuration = new ThemeConfiguration(light, accent);

            Themes.SetTheme(configuration);
        }

        private void CurrentDomain_ProcessExit(object sender, EventArgs e)
        {
            if (!_cleanShutdown)
            {
                Telemetry.TrackEvent("Exit", new Dictionary<string, string> { ["clean"] = "false" });
            }

            Telemetry.Flush();
            Cef.Shutdown();
        }

        protected override void OnExit(ExitEventArgs e)
        {
            _cleanShutdown = true;
            Telemetry.TrackEvent("Exit", new Dictionary<string, string> { ["clean"] = "true", ["code"] = e.ApplicationExitCode.ToString() });

            Telemetry.Flush();
            Cef.Shutdown();

            if (Abstractions is IDisposable d)
            {
                d.Dispose();
            }
        }

        internal static async Task LoginAsync(string token, AsyncEventHandler<ReadyEventArgs> onReady, Func<Exception, Task> onError)
        {
            Exception taskEx = null;

            await Task.Run(() =>
            {
                try
                {
                    var config = new DiscordConfiguration()
                    {
                        Token = token,
                        TokenType = TokenType.User,
                        AutomaticGuildSync = false,
                        LogLevel = LogLevel.Debug,
                        WebSocketClientFactory = Win32WebSocketClient.CreateNew
                    };

                    Discord = new DiscordClient(config);
                    Discord.DebugLogger.LogMessageReceived += (o, ee) => Debug.WriteLine(ee);
                    Discord.Ready += onReady;
                    Discord.Ready += Discord_Ready;
                    Discord.MessageCreated += Discord_NotificationHandler;
                }
                catch (Exception ex)
                {
                    taskEx = ex;
                }
            });

            if (Discord != null)
            {
                try
                {
                    await Discord.ConnectAsync();
                }
                catch (Exception ex)
                {
                    // TODO: Content dialog.

                    await onError(ex);
                }
            }
            else
            {
                if (taskEx != null)
                {
                    await onError(taskEx);
                }
            }
        }

        private static async Task Discord_SocketErrored(SocketErrorEventArgs e)
        {

        }

        private static Task Discord_NotificationHandler(MessageCreateEventArgs e)
        {
            if (Tools.WillShowToast(e.Message))
            {
                return Abstractions.ShowNotificationAsync(e.Message);
            }

            return Task.CompletedTask;
        }

        private static async Task Discord_Ready(ReadyEventArgs e)
        {
            if (_connectionAttempts > 1)
            {
                await App.Current.Dispatcher.InvokeAsync(() => (App.Current.MainWindow as MainWindow).HideConnectingOverlay());
            }

            _connectionAttempts = 0;
        }

        private static async Task Discord_SocketClosed(SocketCloseEventArgs e)
        {
            //if (_connectionAttempts == 1)
            //{
            //    await App.Current.Dispatcher.InvokeAsync(() => (App.Current.MainWindow as MainWindow).ShowConnectingOverlay());
            //    await (App.Current.MainWindow as MainWindow).ChangeStatus("Connecting to Discord...");
            //}

            //_connectionAttempts += 1;
            //await Task.Delay(5000 * _connectionAttempts);
            //await Discord.ReconnectAsync();
        }

        private static async Task Discord_ClientErrored(ClientErrorEventArgs e)
        {
            await App.Current.Dispatcher.InvokeAsync(() => ShowErrorDialog("", "", e.Exception.Message));
        }
        private static void HandleOSVersions()
        {
            Version version = Environment.OSVersion.Version;
#if WIN10
            if (version.Major != 10 && !(version.Minor == 2 || version.Minor == 3))
            {
                using (TaskDialog dialog = new TaskDialog())
                {
                    dialog.MainIcon = TaskDialogIcon.Warning;
                    dialog.WindowTitle = "Unsupported OS!";
                    dialog.MainInstruction = "Unsupported Operating System!";
                    dialog.Content = "This version of DiscordWPF requires Windows 10 or 8, which you aren't running.\n\nPlease download the appropriate version of DiscordWPF for your OS.";
                    dialog.Footer = version.ToString();
                    using (TaskDialogButton button = new TaskDialogButton(ButtonType.Ok))
                    {
                        dialog.Buttons.Add(button);
                        dialog.ShowDialog();

                        Environment.Exit(0);
                    }
                }
            }
            else
            {
                DesktopNotificationManagerCompat.RegisterAumidAndComServer<UWPNotificationActivator>("DiscordWPF.App");
                DesktopNotificationManagerCompat.RegisterActivator<UWPNotificationActivator>();

                Abstractions = new UwpAbstractions();
            }
#else
            Abstractions = new Win32Abstractions();
            if (Environment.OSVersion.Version.Major == 10 && !Settings.GetSetting(OS_WARNING_DISMISSED, false))
            {
                using (TaskDialog dialog = new TaskDialog())
                {
                    dialog.MainIcon = TaskDialogIcon.Warning;
                    dialog.WindowTitle = "Unsupported OS!";
                    dialog.MainInstruction = "Unsupported Operating System!";
                    dialog.Content = "This version of DiscordWPF is designed to run on Windows 7. While this version will work on Windows 10, there is an improved version available, with more features, more intergration and better security.";
                    dialog.VerificationText = "Don't ask me again.";
                    dialog.ButtonStyle = TaskDialogButtonStyle.CommandLinks;

                    using (TaskDialogButton download = new TaskDialogButton("Download for Windows 10\nTake me to more features and better integration!\0"))
                    using (TaskDialogButton cont = new TaskDialogButton("Continue to DiscordWPF\nI don't care about OS integration, just take me to the app!\0"))
                    using (TaskDialogButton cancel = new TaskDialogButton(ButtonType.Cancel))
                    {
                        dialog.Buttons.Add(download);
                        dialog.Buttons.Add(cont);
                        dialog.Buttons.Add(cancel);

                        var btn = dialog.ShowDialog();

                        if (btn == download)
                        {
                            // TODO: Take user to page.
                        }

                        if (btn == cancel)
                        {
                            Environment.Exit(0);
                        }

                        if (btn == cont)
                        {
                            Settings.SetSetting(OS_WARNING_DISMISSED, dialog.IsVerificationChecked);
                        }
                    }
                }
            }
#endif
        }

        public static void ShowErrorDialog(string title, string instruction, string content)
        {
            using (TaskDialog dialog = new TaskDialog())
            {
                dialog.MainIcon = TaskDialogIcon.Error;
                dialog.MainInstruction = instruction;
                dialog.WindowTitle = title;
                dialog.Content = content;

                using (TaskDialogButton button = new TaskDialogButton(ButtonType.Ok))
                {
                    dialog.Buttons.Add(button);
                    dialog.ShowDialog();
                }
            }
        }
    }
}
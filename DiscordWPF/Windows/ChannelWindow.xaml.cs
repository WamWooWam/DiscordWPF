using DiscordWPF.Effects;
using DiscordWPF.Pages;
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
using System.Windows.Interop;
using System.Windows.Media;
using System.Windows.Media.Animation;
using System.Windows.Media.Imaging;
using System.Windows.Shapes;
using WamWooWam.Core;
using WamWooWam.Wpf;
using Forms = System.Windows.Forms;

using static DiscordWPF.Constants;
using DiscordWPF.Controls;
using DiscordWPF.Pages.Placeholder;
using System.Windows.Navigation;

namespace DiscordWPF.Windows
{
    /// <summary>
    /// Interaction logic for ChannelWindow.xaml
    /// </summary>
    public partial class ChannelWindow : Window
    {
        public Frame Frame => rootFrame;

        public DiscordChannel Channel { get; private set; }

        public static readonly DependencyProperty IsSnappingProperty
            = DependencyProperty.Register("IsSnapping", typeof(bool), typeof(ChannelWindow), new PropertyMetadata(false));

        public bool IsMiniMode { get; internal set; }
        public bool IsDragging { get; private set; }
        public bool IsSnapping { get => (bool)GetValue(IsSnappingProperty); set => SetValue(IsSnappingProperty, value); }

        public ChannelWindow(DiscordChannel channel)
        {
            Channel = channel;
            InitializeComponent();
        }

        private void MiniMode_MouseEnter(object sender, MouseEventArgs e)
        {
            if (IsMiniMode)
            {

            }
        }

        private void MiniMode_MouseLeave(object sender, MouseEventArgs e)
        {
            if (IsMiniMode)
            {

            }
        }

        public new void DragMove()
        {
            IsDragging = true;
            base.DragMove();
        }

        internal void FinishDrag()
        {
            IsDragging = false;

            if (IsSnapping)
            {
                RunSnap();
            }
        }

        private void Window_Loaded(object sender, RoutedEventArgs e)
        {
            if (IsMiniMode)
            {
                if (Misc.IsWindows10)
                {
                    Background = Brushes.Transparent;
                    backdrop.Visibility = Visibility.Visible;
                    Accent.SetAccentState(new WindowInteropHelper(this).Handle, AccentState.EnableBlurBehind);
                }
                else if (Misc.IsWindows7)
                {
                    Background = Brushes.Transparent;

                    backdrop.Opacity = 0.75;
                    backdrop.Effect = null;
                    backdrop.Visibility = Visibility.Visible;

                    Accent.SetDWMBlurBehind(new WindowInteropHelper(this).Handle, true);
                }
            }

            if (IsSnapping)
            {
                string pid = null;
                if (Settings.TryGetSetting<Dictionary<ulong, string>>(MINI_MODE_POSITIONS, out var positions) && positions.TryGetValue(Channel.Id, out pid))
                {
                    RunSnap(pid, false);
                }
                else
                {
                    RunSnap(animated: false);
                }
            }
        }

        private void RunSnap(string pid = null, bool animated = true)
        {
            var rawPoint = PointToScreen(new Point(0, 0));

            bool top = true;
            bool left = true;
            Forms.Screen screen = Forms.Screen.PrimaryScreen;

            if (pid != null)
            {
                string[] split = pid.Split('-');
                if (split.Length == 3)
                {
                    string display = split[0];
                    string topStr = split[1];
                    string leftStr = split[2];

                    var newScreen = Forms.Screen.AllScreens.FirstOrDefault(s => s.DeviceName.EndsWith(display));

                    if (newScreen != null)
                    {
                        screen = newScreen;
                        top = topStr.ToLowerInvariant() == "top";
                        left = leftStr.ToLowerInvariant() == "left";
                    }
                }
            }
            else
            {
                screen = Forms.Screen.FromHandle(new WindowInteropHelper(this).Handle);

                var point = PointToScreen(new Point(ActualWidth / 2, ActualHeight / 2));
                point.Offset(-screen.Bounds.X, -screen.Bounds.Y);

                var w = screen.WorkingArea.Width / 2;
                var h = screen.WorkingArea.Height / 2;

                top = point.Y <= h;
                left = point.X <= w;
            }

            InternalRunSnap(animated, rawPoint, top, left, screen);
        }

        private void InternalRunSnap(bool animated, Point rawPoint, bool top, bool left, Forms.Screen screen)
        {
            var end = new Point();

            string pid = screen.DeviceName.TrimStart('\\', '.', '\\') + "-";

            var dpiX = 96D;
            var dpiY = 96D;

            using (var g = System.Drawing.Graphics.FromHwnd(new WindowInteropHelper(this).Handle))
            {
                dpiX = g.DpiX;
                dpiY = g.DpiY;
            }

            var scrWidth = (screen.WorkingArea.Width / dpiX) * 96;
            var scrHeight = (screen.WorkingArea.Height / dpiY) * 96;

            if (top)
            {
                // top
                pid += "top";
                end.Y = 20;
            }
            else
            {
                // bottom
                pid += "bottom";
                end.Y = (scrHeight - 20 - ActualHeight);
            }

            pid += "-";

            if (left)
            {
                // left
                pid += "left";
                end.X = 20;
            }
            else
            {
                // right
                pid += "right";
                end.X = (scrWidth - 20 - ActualWidth);
            }

            if (!Settings.TryGetSetting<Dictionary<ulong, string>>(MINI_MODE_POSITIONS, out var positions))
            {
                positions = new Dictionary<ulong, string>();
            }

            positions[Channel.Id] = pid;

            Settings.SetSetting(MINI_MODE_POSITIONS, positions);

            end.Offset(screen.WorkingArea.X, screen.WorkingArea.Y);

            if (animated)
            {
                Storyboard board = new Storyboard();
                CircleEase ease = new CircleEase() { EasingMode = EasingMode.EaseInOut };

                DoubleAnimation leftAnim = new DoubleAnimation(rawPoint.X, end.X, new Duration(TimeSpan.FromMilliseconds(500))) { EasingFunction = ease };
                Storyboard.SetTarget(leftAnim, this);
                Storyboard.SetTargetProperty(leftAnim, new PropertyPath(LeftProperty));

                DoubleAnimation topAnim = new DoubleAnimation(rawPoint.Y, end.Y, new Duration(TimeSpan.FromMilliseconds(500))) { EasingFunction = ease };
                Storyboard.SetTarget(topAnim, this);
                Storyboard.SetTargetProperty(topAnim, new PropertyPath(TopProperty));

                board.Children.Add(leftAnim);
                board.Children.Add(topAnim);

                board.Begin();
            }
            else
            {
                Left = end.X;
                Top = end.Y;
            }
        }

        private DiscordFrame lastSeen;
        private ChannelPage rootPage;

        private void Window_LocationChanged(object sender, EventArgs e)
        {
            if (IsLoaded && Mouse.LeftButton == MouseButtonState.Pressed && !IsMiniMode)
            {
                var mouse = Mouse.GetPosition(this);
                mouse = PointToScreen(mouse);

                DiscordFrame frame = null;

                foreach (var window in Tools.SortWindowsTopToBottom(App.Current.Windows.OfType<Window>()))
                {
                    if (window is MainWindow mainWind)
                    {
                        var mainFrame = (mainWind.rootFrame.Content as DiscordPage)?.mainFrame;
                        if (mainFrame != null)
                        {
                            if (IsOverFrame(mouse, mainFrame))
                            {
                                frame = mainFrame;
                                break;
                            }
                        }
                    }

                    if (window is GuildWindow guildWind)
                    {
                        if (guildWind.Guild?.Id == Channel.GuildId)
                        {
                            if (IsOverFrame(mouse, guildWind.mainFrame))
                            {
                                frame = guildWind.mainFrame;
                                break;
                            }
                        }
                    }
                }

                if (frame != null)
                {
                    if (frame != lastSeen)
                    {
                        HandleOverFrame(frame);
                    }
                }
                else if (lastSeen != null)
                {
                    Show();
                    rootFrame.Navigate(rootPage);
                    if (lastSeen.CanGoBack)
                    {
                        lastSeen.GoBack();
                    }

                    lastSeen = null;
                }
            }
        }

        private bool IsOverFrame(Point mouse, DiscordFrame frame)
        {
            if (frame.Content?.GetType() != typeof(SelectChannelPage))
            {
                var p = frame.PointFromScreen(mouse);
                if ((p.X > 0 && p.X < frame.ActualWidth) && (p.Y > 0 && p.Y < frame.ActualHeight))
                {
                    return true;
                }
            }

            return false;
        }

        private void HandleOverFrame(DiscordFrame frame)
        {
            lastSeen = frame;

            Hide();
            rootFrame.Navigate(new SelectChannelPage());
            frame.Navigate(rootPage);
        }

        private void rootFrame_Navigated(object sender, NavigationEventArgs e)
        {
            if (e.Content is ChannelPage page)
            {
                rootPage = page;
            }
        }

        private void rootFrame_Navigating(object sender, NavigatingCancelEventArgs e)
        {
            if(e.WebRequest != null)
            {
                e.Cancel = true;
            }
        }
    }
}

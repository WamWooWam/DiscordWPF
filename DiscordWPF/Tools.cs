using DiscordWPF.Controls;
using DiscordWPF.Pages;
using DiscordWPF.Pages.Placeholder;
using DiscordWPF.Windows;
using DSharpPlus;
using DSharpPlus.Entities;
using Markdig;
using Markdig.Helpers;
using Markdig.Parsers;
using Markdig.Renderers;
using Markdig.Renderers.Wpf;
using Markdig.Syntax;
using Markdig.Syntax.Inlines;
using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Reflection;
using System.Runtime.InteropServices;
using System.Text;
using System.Text.RegularExpressions;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Documents;
using System.Windows.Interop;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using System.Windows.Shapes;
using WamWooWam.Core;
using WamWooWam.Wpf;
using XamlAnimatedGif;
using static DiscordWPF.Constants;

namespace DiscordWPF
{
    public static class MessageViewerFactory
    {
        private static ConcurrentDictionary<ulong, ConcurrentDictionary<ulong, MessageViewer>> _messageViewerCache = new ConcurrentDictionary<ulong, ConcurrentDictionary<ulong, MessageViewer>>();
        public static ConcurrentQueue<MessageViewer> ViewerQueue { get; } = new ConcurrentQueue<MessageViewer>();

        static MessageViewerFactory()
        {
            for (int i = 0; i < 150; i++)
            {
                ViewerQueue.Enqueue(new MessageViewer());
            }
        }

        public static MessageViewer GetViewerForMessage(DiscordMessage message)
        {
            if (_messageViewerCache.TryGetValue(message.Channel.Id, out var channel))
            {
                if (channel.TryGetValue(message.Id, out var viewer))
                {
                    if (viewer.Parent is Panel p)
                        p.Children.Remove(viewer);

                    return viewer;
                }
            }
            else
            {
                _messageViewerCache[message.Channel.Id] = new ConcurrentDictionary<ulong, MessageViewer>();
            }

            if (ViewerQueue.TryDequeue(out var newv))
            {
                if (newv.Message != null)
                {
                    foreach (var things in _messageViewerCache)
                    {
                        things.Value.TryRemove(newv.Message.Id, out _);
                    }
                }

                if (newv.Parent is Panel p)
                    p.Children.Remove(newv);

                newv.Message = message;
                _messageViewerCache[message.Channel.Id][message.Id] = newv;

                return newv;
            }

            return null;
        }
    }

    internal static class Tools
    {
        [DllImport("User32")]
        private static extern IntPtr GetTopWindow(IntPtr hWnd);
        [DllImport("user32.dll")]
        private static extern IntPtr GetWindow(IntPtr hWnd, GetWindow_Cmd uCmd);

        [DllImport("shcore.dll")]
        public static extern uint GetDpiForMonitor(IntPtr hMonitor, uint dpiType, out uint dpiX, out uint dpiY);

        [DllImport("user32.dll")]
        public static extern IntPtr MonitorFromPoint(System.Drawing.Point pt, uint dwFlags);

        public static IEnumerable<IEnumerable<T>> Split<T>(this IEnumerable<T> locations, int size = 30)
        {
            var count = locations.Count();
            for (int i = 0; i < count; i += size)
            {
                yield return locations.Skip(i).Take(Math.Min(size, count - i));
            }
        }

        internal static bool WillShowToast(DiscordMessage message)
        {
            bool willNotify = false;

            if (message.MentionedUsers.Any(m => m?.Id == message.Discord.CurrentUser.Id))
            {
                willNotify = true;
            }

            if (message.Channel is DiscordDmChannel)
            {
                willNotify = true;
            }

            if (message.Channel.Guild != null)
            {
                DiscordMember usr = message.Channel.Guild.CurrentMember;
                if (message.MentionedRoles.Any(r => (usr.Roles.Contains(r))))
                {
                    willNotify = true;
                }
            }

            if (message.Author.Id == message.Discord.CurrentUser.Id)
            {
                willNotify = false;
            }

            if ((message.Discord as DiscordClient)?.UserSettings.Status == "dnd")
            {
                willNotify = false;
            }

            return willNotify;
        }

        public static string GetMessageTitle(DiscordMessage message) => message.Channel.Guild != null ?
                               $"{(message.Author as DiscordMember)?.DisplayName ?? message.Author.Username} in {message.Channel.Guild.Name}" :
                               $"{message.Author.Username}";

        public static string GetMessageContent(DiscordMessage message)
        {
            string messageText = message.Content;

            foreach (DiscordUser user in message.MentionedUsers)
            {
                if (user != null)
                {
                    messageText = messageText
                        .Replace($"<@{user.Id}>", $"@{user.Username}")
                        .Replace($"<@!{user.Id}>", $"@{user.Username}");
                }
            }

            if (message.Channel.Guild != null)
            {
                foreach (DiscordChannel channel in message.MentionedChannels)
                {
                    messageText = messageText.Replace(channel.Mention, $"#{channel.Name}");
                }

                foreach (DiscordRole role in message.MentionedRoles)
                {
                    messageText = messageText.Replace(role.Mention, $"@{role.Name}");
                }
            }

            return messageText;
        }

        static Dictionary<ulong, ContextMenu> _guildContextMenuCache = new Dictionary<ulong, ContextMenu>();

        internal static ContextMenu GetContextMenuForGuild(DiscordGuild dg, ImageBrush fill = null)
        {
            if (!Settings.GetSetting(NO_CACHE_CONTEXT_MENUS, false) && _guildContextMenuCache.TryGetValue(dg.Id, out var menu))
            {
                return menu;
            }
            else
            {
                menu = new ContextMenu() { Tag = dg };
                var header = new MenuItem()
                {
                    Header = dg.Name,
                    IsEnabled = false,
                    Icon = new Ellipse()
                    {
                        Width = 18,
                        Height = 18,
                        Fill = fill ?? (dg.IconUrl != null ? new ImageBrush() { ImageSource = new BitmapImage(new Uri(dg.IconUrl)) } : null)
                    }
                };

                menu.Items.Add(header);

                menu.Items.Add(new Separator());

                var open = new MenuItem() { Header = "Open in new window..." };
                open.Click += (o, e) => OpenInNewWindow(dg);
                menu.Items.Add(open);

                menu.Items.Add(new Separator());

                var leave = new MenuItem() { Header = $"Leave {dg.Name}", Foreground = Brushes.Red };
                menu.Items.Add(leave);

                _guildContextMenuCache[dg.Id] = menu;

                return menu;
            }
        }

        internal static void NavigateToChannel(DiscordChannel channel, Frame f)
        {
            if (f != null)
            {
                var window = Application.Current.Windows
                    .OfType<ChannelWindow>()
                    .FirstOrDefault(w => w.Channel == channel);

                if (window != null && !Settings.GetSetting(ALLOW_MULTIPLE_CHANNEL_PAGES, false))
                {
                    var page = window.Frame.Content as ChannelPage;
                    window.Frame.Navigate(null);
                    window.Close();

                    f.Navigate(page);
                }
                else
                {
                    if (f.Content is ChannelPage cpage)
                    {
                        cpage.Channel = channel;
                    }
                    else
                    {
                        cpage = new ChannelPage { Channel = channel };
                        f.Navigate(cpage);
                    }
                }
            }
        }

        internal static void OpenInNewWindow(ChannelPage page)
        {
            var f = page.FindVisualParent<Frame>();
            if (f != null)
            {
                var p = f.PointToScreen(new Point(0, 0));
                f.Navigate(new SelectChannelPage());

                var window = App.Current.Windows.OfType<ChannelWindow>().FirstOrDefault(w => w.Channel == page.Channel && w.Frame.Content != null);

                if (window != null && !Settings.GetSetting(ALLOW_MULTIPLE_CHANNEL_WINDOWS, false))
                {
                    window.Frame.Navigate(page);
                    SetupWindow(page, f, p, window);

                    window.Show();
                    window.Activate();

                    if (window.WindowState == WindowState.Minimized)
                    {
                        window.WindowState = WindowState.Normal;
                    }
                }
                else
                {
                    window = new ChannelWindow(page.Channel);
                    SetupWindow(page, f, p, window);
                    window.Show();
                }
            }
        }

        private static void SetupWindow(ChannelPage page, Frame f, Point p, ChannelWindow window)
        {
            window.Top = p.Y;
            window.Left = p.X;
            window.Width = f.ActualWidth;
            window.Height = f.ActualHeight;
            window.Frame.Navigate(page);
        }

        internal static void OpenInNewWindow(DiscordGuild dg)
        {
            var window = App.Current.Windows.OfType<GuildWindow>().FirstOrDefault(w => w.Guild == dg);
            if (window != null && !Settings.GetSetting(ALLOW_MULTIPLE_GUILD_WINDOWS, false))
            {
                window.Activate();
                if (window.WindowState == WindowState.Minimized)
                {
                    window.WindowState = WindowState.Normal;
                }
            }
            else
            {
                window = new GuildWindow(dg);
                window.Show();
            }
        }

        internal static IEnumerable<Window> SortWindowsTopToBottom(IEnumerable<Window> unsorted)
        {
            var byHandle = unsorted.ToDictionary(win =>
              ((HwndSource)PresentationSource.FromVisual(win)).Handle);

            for (IntPtr hWnd = GetTopWindow(IntPtr.Zero); hWnd != IntPtr.Zero; hWnd = GetWindow(hWnd, GetWindow_Cmd.GW_HWNDNEXT))
            {
                if (byHandle.ContainsKey(hWnd))
                {
                    yield return byHandle[hWnd];
                }
            }
        }

        private enum GetWindow_Cmd : uint
        {
            GW_HWNDFIRST = 0,
            GW_HWNDLAST = 1,
            GW_HWNDNEXT = 2,
            GW_HWNDPREV = 3,
            GW_OWNER = 4,
            GW_CHILD = 5,
            GW_ENABLEDPOPUP = 6
        }
    }

    public class DiscordMarkdownExtension : IMarkdownExtension
    {
        public DiscordChannel Channel { get; internal set; }
        public DiscordGuild Guild { get; internal set; }

        public void Setup(MarkdownPipelineBuilder pipeline) { pipeline.InlineParsers.AddIfNotAlready<MentionParser>(); }

        public void Setup(MarkdownPipeline pipeline, IMarkdownRenderer renderer)
        {
            if (renderer is WpfRenderer)
            {
                renderer.ObjectRenderers.Add(new MentionInlineRenderer(Channel, Guild));
            }
        }
    }

    public class MentionParser : InlineParser
    {
        private static Dictionary<MentionType, Regex> _regexes = new Dictionary<MentionType, Regex>();

        static MentionParser()
        {
            _regexes[MentionType.User] = new Regex("<[@!]+([0-9]+)>", RegexOptions.ECMAScript | RegexOptions.Compiled);
            _regexes[MentionType.Channel] = new Regex("<#(\\d+)>", RegexOptions.ECMAScript | RegexOptions.Compiled);
            _regexes[MentionType.Role] = new Regex("<@&(\\d+)>", RegexOptions.ECMAScript | RegexOptions.Compiled);
            _regexes[MentionType.Emote] = new Regex(@"<a?:([a-zA-Z0-9_]+?):(\d+?)>", RegexOptions.ECMAScript | RegexOptions.Compiled);
        }

        public MentionParser()
        {
            OpeningCharacters = new[] { '<' };
        }

        public override bool Match(InlineProcessor processor, ref StringSlice slice)
        {
            ulong id = 0;

            foreach (var regex in _regexes)
            {
                var m = regex.Value.Match(slice.Text, slice.Start);
                if (m.Success && (ulong.TryParse(m.Groups[1].Value, out id) || ulong.TryParse(m.Groups[2].Value, out id)))
                {
                    processor.Inline = new MentionInline()
                    {
                        Span = new SourceSpan(processor.GetSourcePosition(m.Index, out var line, out var column), m.Index + m.Length),
                        Line = line,
                        Column = column,
                        IsClosed = true,
                        Type = regex.Key,
                        Id = id,
                        Value = m.Value
                    };

                    slice.Start = m.Index + m.Length;

                    return true;
                }
            }

            return false;
        }
    }

    public class MentionInline : Markdig.Syntax.Inlines.Inline
    {
        public ulong Id { get; internal set; }
        public MentionType Type { get; internal set; }
        public string Value { get; internal set; }
    }

    public class MentionInlineRenderer : WpfObjectRenderer<MentionInline>
    {
        private DiscordChannel _channel;
        private DiscordGuild _guild;
        private static MethodInfo _writeInline;

        public MentionInlineRenderer(DiscordChannel channel, DiscordGuild guild)
        {
            _channel = channel;
            _guild = guild;

            if (_writeInline == null)
            {
                _writeInline = typeof(WpfRenderer).GetMethod("WriteInline", BindingFlags.Instance | BindingFlags.NonPublic);
            }
        }

        protected override void Write(WpfRenderer renderer, MentionInline obj)
        {
            if (obj.Type != MentionType.Emote)
            {
                Run mentionRun = new Run() { FontWeight = FontWeights.Bold };
                switch (obj.Type)
                {
                    case MentionType.User:
                        DoUserInline(obj, mentionRun);
                        break;
                    case MentionType.Channel:
                        DoChannelInline(obj, mentionRun);
                        break;
                    case MentionType.Role:
                        DoRoleInline(obj, mentionRun);
                        break;
                    default:
                        break;
                }

                _writeInline?.Invoke(renderer, new[] { mentionRun });
            }
            else
            {
                var uri = $"https://cdn.discordapp.com/emojis/{obj.Id}?size=32";
                InlineUIContainer cont = new InlineUIContainer() { BaselineAlignment = BaselineAlignment.Center };
                Image image = new Image() { Width = 24, Height = 24 };
                if(obj.Value.StartsWith("<a:"))
                {
                    AnimationBehavior.SetSourceUri(image, new Uri(uri));
                }
                else
                {
                    image.Source = new BitmapImage(new Uri(uri));
                }

                cont.Child = image;
                _writeInline?.Invoke(renderer, new[] { cont });
            }
        }

        private static void DoRoleInline(MentionInline obj, Run mentionRun)
        {
            var role = App.Discord.Guilds.SelectMany(g => g.Value.Roles).FirstOrDefault(r => r.Id == obj.Id);
            if (role != null)
            {
                mentionRun.Text = $"@{role.Name}";
                if (role.Color.Value != default)
                {
                    mentionRun.Foreground = new SolidColorBrush(Color.FromRgb(role.Color.R, role.Color.G, role.Color.B));
                    var back = mentionRun.Foreground.Clone();
                    back.Opacity = 0.2;
                    mentionRun.Background = back;
                }
            }
        }

        private void DoChannelInline(MentionInline obj, Run mentionRun)
        {
            var channel = _guild != null ? _guild.GetChannel(obj.Id) : null;
            if (channel == null)
            {
                channel = App.Discord.Guilds
                    .SelectMany(g => g.Value.Channels)
                    .Union(App.Discord.PrivateChannels.AsParallel())
                    .AsParallel()
                    .FirstOrDefault(c => c.Id == obj.Id);
            }

            if (channel != null)
            {
                mentionRun.Text = $"#{channel.Name}";
            }
            else
            {
                mentionRun.Text = $"#deleted-channel";
            }
        }

        private void DoUserInline(MentionInline obj, Run mentionRun)
        {
            var user = _guild != null ? _guild.Members.FirstOrDefault(m => m.Id == obj.Id) : App.Discord.UserCache.TryGetValue(obj.Id, out var u) ? u : null;
            if (user != null)
            {
                if (user is DiscordMember member)
                {
                    mentionRun.Text = $"@{member.DisplayName}";
                    if (member.ColorBrush != null)
                    {
                        mentionRun.Foreground = member.ColorBrush;
                        var back = mentionRun.Foreground.Clone();
                        back.Opacity = 0.2;
                        mentionRun.Background = back;
                    }
                }
                else
                {
                    mentionRun.Text = $"@{user.Username}";
                }
            }
            else
            {
                mentionRun.Text = $"Unknown user: {obj.Id}";
            }
        }
    }

    public enum MentionType
    {
        User, Channel, Role, Emote
    }
}

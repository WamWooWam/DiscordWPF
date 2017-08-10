using Discord;
using Discord.WebSocket;
using DiscordWPF.Data;
using Microsoft.Win32;
using Microsoft.WindowsAPICodePack.Dialogs;
using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Diagnostics;
using System.Drawing;
using System.Globalization;
using System.IO;
using System.Linq;
using System.Media;
using System.Net.Http;
using System.Runtime.InteropServices;
using System.Text;
using System.Text.RegularExpressions;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Documents;
using System.Windows.Input;
using System.Windows.Interop;
using System.Windows.Media;
using System.Windows.Media.Imaging;

using MediaColor = System.Windows.Media.Color;

namespace DiscordWPF
{
    public static class Tools
    {
        public static bool IsMuted(this IChannel channel) => App.Config.MutedChannels?.Contains(channel.Id) == true;

        public static ImageSource ToImageSource(this Icon icon)
        {
            Bitmap bitmap = icon.ToBitmap();
            IntPtr hBitmap = bitmap.GetHbitmap();

            ImageSource wpfBitmap = Imaging.CreateBitmapSourceFromHBitmap(
                hBitmap,
                IntPtr.Zero,
                Int32Rect.Empty,
                BitmapSizeOptions.FromEmptyOptions());

            return wpfBitmap;
        }

        public static T GetChildOfType<T>(this DependencyObject depObj) where T : DependencyObject
        {
            if (depObj == null) return null;

            for (int i = 0; i < VisualTreeHelper.GetChildrenCount(depObj); i++)
            {
                DependencyObject child = VisualTreeHelper.GetChild(depObj, i);

                T result = (child as T) ?? GetChildOfType<T>(child);
                if (result != null) return result;
            }
            return null;
        }

        public static string Name(IUser user)
        {
            if (user is IGuildUser && App.Config.General.Nicknames)
                return (user as IGuildUser).Nickname ?? (user as IGuildUser).Username;
            else
                return user.Username;
        }

        public static void PlayIMSound()
        {
            try
            {
                if (App.Config.Sounds.MessageRecievedSound != null && App.Config.Sounds.Enabled)
                {
                    if (App.Config.Sounds.MessageRecievedSound == "")
                    {
                        RegistryKey key = Registry.CurrentUser.OpenSubKey(@"AppEvents\Schemes\Apps\.Default\Notification.IM\.Current");
                        SoundPlayer snd = new SoundPlayer(key.GetValue(null) as string);
                        snd.Play();
                    }
                    else
                    {
                        SoundPlayer snd = new SoundPlayer(App.Config.Sounds.MessageRecievedSound);
                        snd.Play();
                    }
                }
            }
            catch
            {
                SystemSounds.Asterisk.Play();
            }
        }

        public static async Task SaveAttachment(string url)
        {
            CommonSaveFileDialog dialog = new CommonSaveFileDialog();
            dialog.Title = "Save image as...";
            dialog.Filters.Add(new CommonFileDialogFilter("Original file extension", Path.GetExtension(url)));
            dialog.DefaultFileName = Path.GetFileName(url);
            if (dialog.ShowDialog() == CommonFileDialogResult.Ok)
            {
                App.DiscordWindow.StatusShowWarning($"Downloading {Path.GetFileName(dialog.FileName)}...", false, false);
                using (HttpClient client = new HttpClient())
                {
                    using (Stream str = await client.GetStreamAsync(url))
                    {
                        using (FileStream file = File.OpenWrite(dialog.FileName))
                        {
                            await str.CopyToAsync(file);
                        }
                    }
                }
                App.DiscordWindow.StatusShowSuccess($"Downloaded {Path.GetFileName(dialog.FileName)}!");
            }
        }

        public static string GetName(this IGroupChannel channel) => channel.Name ?? string.Join(", ", (channel.GetUsersAsync().Flatten().Result).Select(u => u.Username));

        public static string GetText(this RichTextBox textBox)
        {
            TextRange textRange = new TextRange(textBox.Document.ContentStart, textBox.Document.ContentEnd);
            return textRange.Text;
        }

        public static void SetText(this RichTextBox textBox, string text)
        {
            TextRange textRange = new TextRange(textBox.Document.ContentStart, textBox.Document.ContentEnd);
            textRange.Text = text;
        }

        public static void AppendText(this RichTextBox textBox, string text)
        {
            TextRange textRange = new TextRange(textBox.Document.ContentStart, textBox.Document.ContentEnd);
            textRange.Text += text;
        }

        public static void Clear(this RichTextBox textBox)
        {
            TextRange textRange = new TextRange(textBox.Document.ContentStart, textBox.Document.ContentEnd);
            textRange.Text = "";
        }

        public static async Task<string> ProcessMessageText(IMessage msg, IGuild guild)
        {
            string message = msg.Content;
            if (guild != null)
            {
                foreach (ulong userID in msg.MentionedUserIds)
                {
                    IGuildUser mentionedUser = (await guild.GetUsersAsync()).FirstOrDefault(u => u.Id == userID);
                    if (mentionedUser != null)
                    {
                        Console.WriteLine(mentionedUser.Mention);
                        string replacement = mentionedUser.Nickname != null ? mentionedUser.Nickname : mentionedUser.Username;
                        message = message.Replace(mentionedUser.Mention, $"@{replacement}");
                        message = message.Replace(mentionedUser.Mention.Replace("!", ""), $"@{replacement}");
                    }
                }
                foreach (ulong channelID in msg.MentionedChannelIds)
                {
                    ITextChannel mentionedChannel = (await guild.GetTextChannelsAsync()).FirstOrDefault(c => c.Id == channelID);
                    if (mentionedChannel != null)
                    {
                        Console.WriteLine(mentionedChannel.Mention);
                        message = message.Replace(mentionedChannel.Mention, $"#{mentionedChannel.Name}");
                    }
                }
                foreach (ulong roleID in msg.MentionedRoleIds)
                {
                    IRole mentionedRole = guild.Roles.FirstOrDefault(r => r.Id == roleID);
                    if (mentionedRole != null)
                    {
                        Console.WriteLine(mentionedRole.Mention);
                        message = message.Replace(mentionedRole.Mention, $"@{mentionedRole.Name}");
                    }
                }
            }

            foreach (KeyValuePair<string, GuildEmote> emote in DiscordWindow.AvailableEmotes)
            {
                if (message.Contains(emote.Key))
                {
                    message = message.Replace(emote.Key, $":{emote.Value.Name}:");
                }
            }

            if (msg is ISystemMessage)
            {
                switch ((msg as ISystemMessage).Type)
                {
                    case (MessageType.Call):
                        message += $"Incomming call from @{msg.Author.Username}";
                        break;
                }
            }

            return message;
        }

        public static void Hyperlink_Click(object sender, EventArgs e)
        {
            Process.Start((sender as Hyperlink).NavigateUri.AbsoluteUri);
        }

        static readonly string[] SizeSuffixes =
                  { "bytes", "KB", "MB", "GB", "TB", "PB", "EB", "ZB", "YB" };

        public static string SizeSuffix(Int64 value, int decimalPlaces = 1)
        {
            if (value < 0) { return "-" + SizeSuffix(-value); }

            int i = 0;
            decimal dValue = (decimal)value;
            while (Math.Round(dValue, decimalPlaces) >= 1000)
            {
                dValue /= 1024;
                i++;
            }

            return string.Format("{0:n" + decimalPlaces + "} {1}", dValue, SizeSuffixes[i]);
        }

        public static async Task<ContextMenu> GetUserContextMenu(IUser target, FrameworkElement baseElement)
        {
            ContextMenu menu = new ContextMenu();

            MenuItem profileMenuItem = new MenuItem();
            profileMenuItem.Header = "Profile";
            profileMenuItem.Click += (object s, RoutedEventArgs e) => App.DiscordWindow.ShowUserDetails(target, baseElement);
            profileMenuItem.Icon = new FontAwesome.WPF.ImageAwesome() { Icon = FontAwesome.WPF.FontAwesomeIcon.User };
            menu.Items.Add(profileMenuItem);

            MenuItem mentionMenuItem = new MenuItem();
            mentionMenuItem.Header = "Mention";
            mentionMenuItem.Click += (object s, RoutedEventArgs e) => App.DiscordWindow.MentionUser(target);
            mentionMenuItem.Icon = new FontAwesome.WPF.ImageAwesome() { Icon = FontAwesome.WPF.FontAwesomeIcon.At };

            menu.Items.Add(mentionMenuItem);

            if (target is IGuildUser)
            {
                menu.Items.Add(new Separator());
                MenuItem messageMenuItem = new MenuItem();
                messageMenuItem.Header = $"Message";
                messageMenuItem.Icon = new FontAwesome.WPF.ImageAwesome() { Icon = FontAwesome.WPF.FontAwesomeIcon.Envelope };

                if (App.DiscordWindow.Client.DMChannels.Any(r => r.Recipient.Id == target.Id))
                    messageMenuItem.Click += async (object s, RoutedEventArgs e) => await App.DiscordWindow.Refresh(App.DiscordWindow.Client.DMChannels.First(r => r.Recipient.Id == target.Id));
                else
                    messageMenuItem.Click += async (object s, RoutedEventArgs e) =>
                    {
                        await target.GetOrCreateDMChannelAsync();
                        await App.DiscordWindow.Refresh(App.DiscordWindow.Client.DMChannels.First(r => r.Recipient.Id == target.Id));
                    };
                menu.Items.Add(messageMenuItem);

                IGuildUser sockTarget = (target as IGuildUser);
                IGuild guildTarget = sockTarget.Guild;
                IGuildUser me = await guildTarget.GetCurrentUserAsync();

                IEnumerable<IRole> targetRoles = guildTarget.Roles.Where(r => sockTarget.RoleIds.Any(i => i == r.Id));
                IEnumerable<IRole> meRoles = guildTarget.Roles.Where(r => me.RoleIds.Any(i => i == r.Id));

                int? highestRole = meRoles.OrderByDescending(r => r.Position).FirstOrDefault()?.Position;
                int? targetHighestRole = targetRoles.OrderByDescending(r => r.Position).FirstOrDefault()?.Position;

                menu.Items.Add(new Separator());
                MenuItem rolesMenuItem = new MenuItem();
                rolesMenuItem.Header = $"Roles";

                foreach (IRole role in targetRoles)
                {
                    MenuItem roleMenuItem = new MenuItem();
                    roleMenuItem.Header = role.Name;

                    if (role.Color.RawValue != Discord.Color.Default.RawValue)
                    {
                        roleMenuItem.Foreground = new SolidColorBrush(System.Windows.Media.Color.FromRgb(role.Color.R, role.Color.G, role.Color.B));
                    }

                    rolesMenuItem.Items.Add(roleMenuItem);
                }

                menu.Items.Add(rolesMenuItem);


                bool mynick = DiscordWindow.GuildPermissions?.ChangeNickname == true;
                bool othernick = DiscordWindow.GuildPermissions?.ManageNicknames == true;

                bool kick = DiscordWindow.GuildPermissions?.KickMembers == true && targetHighestRole < highestRole;
                bool ban = DiscordWindow.GuildPermissions?.BanMembers == true && targetHighestRole < highestRole;

                if ((target.Id == App.DiscordWindow.Client.CurrentUser.Id && mynick) || othernick)
                {
                    menu.Items.Add(new Separator());
                    MenuItem changeNickname = new MenuItem();
                    changeNickname.Header = "Change Nickname";
                    changeNickname.Click += (object s, RoutedEventArgs e) => App.DiscordWindow.ChangeNickname(target);
                    menu.Items.Add(changeNickname);
                }

                if (kick)
                {
                    menu.Items.Add(new Separator());
                    MenuItem kickMenuItem = new MenuItem();
                    kickMenuItem.Header = $"Kick {target.Username}";
                    kickMenuItem.Foreground = System.Windows.Media.Brushes.Red;
                    kickMenuItem.Icon = new FontAwesome.WPF.ImageAwesome() { Icon = FontAwesome.WPF.FontAwesomeIcon.UserTimes };

                    kickMenuItem.Click += (object s, RoutedEventArgs e) => App.DiscordWindow.Kick(target, guildTarget);
                    menu.Items.Add(kickMenuItem);
                    if (!ban)
                        menu.Items.Add(new Separator());
                }
                if (ban)
                {
                    if (!kick)
                        menu.Items.Add(new Separator());
                    MenuItem banMenuItem = new MenuItem();
                    banMenuItem.Header = $"Ban {target.Username}";
                    banMenuItem.Foreground = System.Windows.Media.Brushes.Red;
                    banMenuItem.Icon = new FontAwesome.WPF.ImageAwesome() { Icon = FontAwesome.WPF.FontAwesomeIcon.Ban };

                    banMenuItem.Click += (object s, RoutedEventArgs e) => App.DiscordWindow.Ban(target, guildTarget);
                    menu.Items.Add(banMenuItem);
                }
            }
            else if (target is IUser)
            {

            }
            else
                return null;

            menu.Items.Add(new Separator());

            MenuItem copyIdMenuItem = new MenuItem();
            copyIdMenuItem.Header = "Copy ID";
            copyIdMenuItem.Icon = new FontAwesome.WPF.ImageAwesome() { Icon = FontAwesome.WPF.FontAwesomeIcon.Clipboard };

            copyIdMenuItem.Click += (object s, RoutedEventArgs e) => Clipboard.SetText(target.Id.ToString());
            menu.Items.Add(copyIdMenuItem);

            return menu;
        }

        public static MediaColor? GetUserColour(IGuildUser user, IGuild guild, bool faded = false)
        {
            if (user != null && App.Config.General.UserColours)
            {

                IEnumerable<IRole> mentionedUserRoles = guild.Roles.Where(r => user.RoleIds.Contains(r.Id));
                IRole mentionedColouredRole = mentionedUserRoles.OrderByDescending(r => r.Position).FirstOrDefault(r => r.Color.RawValue != Discord.Color.Default.RawValue && r.Color.RawValue != 3553598);

                if (mentionedColouredRole != null)
                {
                    if (faded)
                        return MediaColor.FromArgb(64, mentionedColouredRole.Color.R, mentionedColouredRole.Color.G, mentionedColouredRole.Color.B);
                    else
                        return MediaColor.FromRgb(mentionedColouredRole.Color.R, mentionedColouredRole.Color.G, mentionedColouredRole.Color.B);
                }
                else
                    return null;
            }
            else
                return null;
        }


        public static async Task FormatMessage(IGuildUser user, IMessage msg, IMessageChannel channel, RichTextBox rtb, System.Windows.Threading.Dispatcher Dispatcher, string text = null)
        {
            for (int i = 0; i < rtb.Document.Blocks.OfType<Paragraph>().Count(); i++)
            {
                Paragraph p = rtb.Document.Blocks.OfType<Paragraph>().ElementAt(i) as Paragraph;
                TextRange tr = new TextRange(p.ContentStart, p.ContentEnd);
                foreach (KeyValuePair<string, GuildEmote> emote in DiscordWindow.AvailableEmotes)
                {
                    try
                    {
                        while (tr.Text.Contains(emote.Key))
                        {
                            Inline inline = (await Dispatcher.InvokeAsync(() => p.Inlines)).First(inl => new TextRange(inl.ContentStart, inl.ContentEnd).Text.Contains(emote.Key));
                            tr = new TextRange(inline.ContentStart, inline.ContentEnd);
                            while (tr.Text.Contains(emote.Key))
                            {
                                TextPointer tp = inline.ContentStart;
                                while (!tp.GetTextInRun(LogicalDirection.Forward).StartsWith(emote.Key))
                                    tp = await Dispatcher.InvokeAsync(() => tp.GetNextInsertionPosition(LogicalDirection.Forward));
                                tr = await Dispatcher.InvokeAsync(() => new TextRange(tp, tp.GetPositionAtOffset(emote.Key.Length)));
                                await Dispatcher.InvokeAsync(() =>
                                {
                                    tr.Text = "";
                                    System.Windows.Controls.Image img = new System.Windows.Controls.Image();
                                    img.Source = Images.GetImage(emote.Value.Url);
                                    img.Stretch = Stretch.Uniform;
                                    img.Height = 20;
                                    new InlineUIContainer(img, tp);
                                });
                                tr = new TextRange(p.ContentStart, p.ContentEnd);
                            }
                        }
                    }
                    catch { }
                }

                List<ulong> mentionedUserIds = new List<ulong>();
                mentionedUserIds.AddRange(msg.MentionedUserIds);

                if (text != null)
                {
                    string matches = Regex.Replace(text, "[^<>@!]", "");

                    if (ulong.TryParse(matches, out ulong id))
                    {
                        mentionedUserIds.Add(id);
                    }
                }

                foreach (ulong id in msg.MentionedUserIds)
                {
                    SocketGuildUser usr = (SocketGuildUser)App.DiscordWindow.Client.GetChannel(msg.Channel.Id).Users.FirstOrDefault(u => u.Id == id);
                    if (usr != null)
                    {
                        try
                        {
                            for (int j = 0; j < (await Dispatcher.InvokeAsync(() => p.Inlines)).Count; j++)
                            {
                                Inline inline = (await Dispatcher.InvokeAsync(() => p.Inlines)).ElementAt(j);
                                tr = new TextRange(inline.ContentStart, inline.ContentEnd);

                                TextPointer tp = inline.ContentStart;
                                string mention = "";
                                if (tr.Text.Contains(usr.Mention))
                                {
                                    mention = usr.Mention;
                                    while (!tp.GetTextInRun(LogicalDirection.Forward).StartsWith(usr.Mention))
                                        tp = await Dispatcher.InvokeAsync(() => tp.GetNextInsertionPosition(LogicalDirection.Forward));
                                    tr = await Dispatcher.InvokeAsync(() => new TextRange(tp, tp.GetPositionAtOffset(usr.Mention.Length)));
                                    await Dispatcher.InvokeAsync(() => tr.Text = "");
                                }
                                else if (tr.Text.Contains($"<@{usr.Id}>"))
                                {
                                    mention = $"<@{usr.Id}>";
                                    while (!tp.GetTextInRun(LogicalDirection.Forward).StartsWith(mention))
                                        tp = await Dispatcher.InvokeAsync(() => tp.GetNextInsertionPosition(LogicalDirection.Forward));
                                    tr = await Dispatcher.InvokeAsync(() => new TextRange(tp, tp.GetPositionAtOffset($"<@{usr.Id}>".Length)));
                                    await Dispatcher.InvokeAsync(() => tr.Text = "");
                                }

                                if (mention != "")
                                {
                                    await Dispatcher.InvokeAsync(async () =>
                                     {
                                         Run cont = new Run($"@{Tools.Name(usr)}", tp);
                                         cont.FontWeight = FontWeights.SemiBold;
                                         cont.ToolTip = new ToolTip();
                                         (cont.ToolTip as ToolTip).Content = $"@{usr.Username}#{usr.Discriminator} / {usr.Status}";
                                         cont.ContextMenu = await Tools.GetUserContextMenu(usr, rtb);
                                         cont.Style = App.Current.Resources["mentionRun"] as Style;
                                         cont.PreviewMouseRightButtonDown += (s, e) => cont.ContextMenu.IsOpen = true;
                                         cont.PreviewMouseLeftButtonDown += (s, e) => App.DiscordWindow.ShowUserDetails(usr, rtb);

                                         if (App.Config.General.UserColourMentions)
                                         {
                                             MediaColor? fg = Tools.GetUserColour(usr, user.Guild);
                                             if (fg.HasValue)
                                             {
                                                 cont.Foreground = new SolidColorBrush(fg.Value);
                                                 cont.Background = new SolidColorBrush(Tools.GetUserColour(usr, user.Guild).Value);
                                                 cont.Background.Opacity = 0.33;
                                             }
                                             else
                                             {
                                                 SolidColorBrush newBrush = App.ForegroundBrush;
                                                 newBrush.Opacity = 0.33;
                                                 cont.Background = newBrush;
                                             }
                                         }
                                         else
                                         {
                                             SolidColorBrush newBrush = App.ForegroundBrush;
                                             newBrush.Opacity = 0.33;
                                             cont.Background = newBrush;
                                         }
                                     });
                                }
                            }
                        }
                        catch { }
                    }
                }

                List<ulong> mentionedChannelIds = new List<ulong>();
                mentionedChannelIds.AddRange(msg.MentionedChannelIds);

                if (text != null)
                {
                    string matches = Regex.Replace(text, "[^<>#!]", "");

                    if (ulong.TryParse(matches, out ulong id))
                    {
                        mentionedChannelIds.Add(id);
                    }
                }

                foreach (ulong id in mentionedChannelIds)
                {
                    IMessageChannel mentioned = (await (channel as ITextChannel).Guild.GetTextChannelsAsync()).FirstOrDefault(u => u.Id == id);
                    if (mentioned != null)
                        try
                        {
                            for (int j = 0; j < (await Dispatcher.InvokeAsync(() => p.Inlines)).Count; j++)
                            {
                                Inline inline = (await Dispatcher.InvokeAsync(() => p.Inlines)).ElementAt(j);

                                tr = new TextRange(inline.ContentStart, inline.ContentEnd);

                                TextPointer tp = inline.ContentStart;
                                if (tr.Text.Contains($"<#{mentioned.Id}>"))
                                {
                                    while (!tp.GetTextInRun(LogicalDirection.Forward).StartsWith($"<#{mentioned.Id}>"))
                                        tp = await Dispatcher.InvokeAsync(() => tp.GetNextInsertionPosition(LogicalDirection.Forward));
                                    tr = await Dispatcher.InvokeAsync(() => new TextRange(tp, tp.GetPositionAtOffset($"<#{mentioned.Id}>".Length)));

                                    await Dispatcher.InvokeAsync(() =>
                                    {
                                        tr.Text = "";
                                        Run userMention = new Run($"#{mentioned.ToString()}", tp);
                                        userMention.FontWeight = FontWeights.SemiBold;
                                        userMention.Background = App.SecondaryBackgroundBrush;

                                        userMention.PreviewMouseLeftButtonDown += async (object s, MouseButtonEventArgs ev) =>
                                        {
                                            await App.DiscordWindow.Refresh(mentioned);
                                        };

                                    });
                                }
                            }
                        }
                        catch { }
                }

                foreach (ulong id in msg.MentionedRoleIds)
                {
                    IRole mentioned = (channel as ITextChannel).Guild.Roles.FirstOrDefault(u => u.Id == id);
                    if (mentioned != null)
                    {
                        try
                        {
                            for (int j = 0; j < (await Dispatcher.InvokeAsync(() => p.Inlines)).Count; j++)
                            {
                                Inline inline = (await Dispatcher.InvokeAsync(() => p.Inlines)).ElementAt(j);

                                tr = new TextRange(inline.ContentStart, inline.ContentEnd);

                                TextPointer tp = inline.ContentStart;
                                if (tr.Text.Contains($"<@&{mentioned.Id}>"))
                                {
                                    while (!tp.GetTextInRun(LogicalDirection.Forward).StartsWith($"<@&{mentioned.Id}>"))
                                        tp = await Dispatcher.InvokeAsync(() => tp.GetNextInsertionPosition(LogicalDirection.Forward));
                                    tr = await Dispatcher.InvokeAsync(() => new TextRange(tp, tp.GetPositionAtOffset($"<@&{mentioned.Id}>".Length)));

                                    await Dispatcher.InvokeAsync(() =>
                                    {
                                        tr.Text = "";
                                        Run userMention = new Run($"@{mentioned.ToString()}", tp);
                                        userMention.FontWeight = FontWeights.SemiBold;
                                        userMention.Style = App.Current.Resources["mentionRun"] as Style;

                                        userMention.Background = System.Windows.Media.Brushes.Transparent;

                                        if (mentioned.Color.RawValue != Discord.Color.Default.RawValue)
                                        {
                                            userMention.Foreground = new SolidColorBrush(MediaColor.FromRgb(mentioned.Color.R, mentioned.Color.G, mentioned.Color.B));
                                            userMention.Background = new SolidColorBrush(MediaColor.FromRgb(mentioned.Color.R, mentioned.Color.G, mentioned.Color.B));
                                            userMention.Background.Opacity = 0.33;
                                        }
                                    });
                                }
                            }
                        }
                        catch { }
                    }
                }

            }
        }

        public static List<int> AllIndexesOf(this string str, string value)
        {
            if (String.IsNullOrEmpty(value))
                throw new ArgumentException("the string to find may not be empty", "value");
            List<int> indexes = new List<int>();
            for (int index = 0; ; index += value.Length)
            {
                index = str.IndexOf(value, index);
                if (index == -1)
                    return indexes;
                indexes.Add(index);
            }
        }

        public static Tuple<int, int> ScaleProportions(int currentWidth, int currentHeight, int maxWidth, int maxHeight)
        {
            double ratioX = (double)maxWidth / currentWidth;
            double ratioY = (double)maxHeight / currentHeight;
            double ratio = Math.Min(ratioX, ratioY);

            int newWidth = (int)(currentWidth * ratio);
            int newHeight = (int)(currentHeight * ratio);

            return new Tuple<int, int>(newWidth, newHeight);
        }

        static bool invalid = false;

        public static bool IsValidEmail(string strIn)
        {
            invalid = false;
            if (String.IsNullOrEmpty(strIn))
                return false;

            // Use IdnMapping class to convert Unicode domain names.
            try
            {
                strIn = Regex.Replace(strIn, @"(@)(.+)$", DomainMapper,
                                      RegexOptions.None, TimeSpan.FromMilliseconds(200));
            }
            catch (RegexMatchTimeoutException)
            {
                return false;
            }

            if (invalid)
                return false;

            // Return true if strIn is in valid e-mail format.
            try
            {
                return Regex.IsMatch(strIn,
                      @"^(?("")("".+?(?<!\\)""@)|(([0-9a-z]((\.(?!\.))|[-!#\$%&'\*\+/=\?\^`\{\}\|~\w])*)(?<=[0-9a-z])@))" +
                      @"(?(\[)(\[(\d{1,3}\.){3}\d{1,3}\])|(([0-9a-z][-\w]*[0-9a-z]*\.)+[a-z0-9][\-a-z0-9]{0,22}[a-z0-9]))$",
                      RegexOptions.IgnoreCase, TimeSpan.FromMilliseconds(250));
            }
            catch (RegexMatchTimeoutException)
            {
                return false;
            }
        }

        private static string DomainMapper(Match match)
        {
            // IdnMapping class with default property values.
            IdnMapping idn = new IdnMapping();

            string domainName = match.Groups[2].Value;
            try
            {
                domainName = idn.GetAscii(domainName);
            }
            catch (ArgumentException)
            {
                invalid = true;
            }
            return match.Groups[1].Value + domainName;
        }
    }
}

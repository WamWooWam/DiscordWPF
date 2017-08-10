using Discord;
using Discord.Net;
using Discord.Net.Providers.WS4Net;
using Discord.Rest;
using Discord.WebSocket;
using DiscordWPF.Controls;
using DiscordWPF.Data;
using DiscordWPF.Dialogs;
using FontAwesome.WPF;
using Microsoft.WindowsAPICodePack.Taskbar;
using Ookii.Dialogs;
using System;
using System.Collections.Generic;
using System.Collections.Specialized;
using System.IO;
using System.Linq;
using System.Reflection;
using System.Text;
using System.Threading;
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
using System.Windows.Threading;

namespace DiscordWPF
{
    /// <summary>
    /// Interaction logic for DiscordWindow.xaml
    /// </summary>
    public partial class DiscordWindow : Window
    {
        public DiscordSocketClient Client;
        public DiscordRestClient RestClient;
        public static Dictionary<string, GuildEmote> AvailableEmotes = new Dictionary<string, GuildEmote>();

        public static IGuild selectedGuild;
        public static IMessageChannel selectedTextChannel;

        public List<IUserMessage> cache = new List<IUserMessage>();

        List<IUser> typingUsers = new List<IUser>();

        TaskbarManager windowsTaskbar = TaskbarManager.Instance;

        System.Windows.Forms.NotifyIcon icon;

        DateTime lastSent = DateTime.Now;

        string Token;
        public TokenType type = TokenType.User;
        Thread populate;

        bool loaded = false;

        public static ChannelPermissions ChannelPermissions
        {
            get
            {
                if (selectedTextChannel is ITextChannel)
                {
                    return ((selectedTextChannel as ITextChannel).Guild.GetCurrentUserAsync().Result).GetPermissions(selectedTextChannel as ITextChannel);
                }
                else if (selectedTextChannel is IDMChannel)
                    return ChannelPermissions.DM;
                else if (selectedTextChannel is IGroupChannel)
                    return ChannelPermissions.Group;
                else
                    return ChannelPermissions.None;
            }
        }

        public static GuildPermissions? GuildPermissions
        {
            get
            {
                if (selectedTextChannel is ITextChannel)
                {
                    return ((selectedTextChannel as ITextChannel).Guild.GetCurrentUserAsync().Result).GuildPermissions;
                }
                else
                    return null;
            }
        }

        ParallelOptions options = new ParallelOptions()
        {
            MaxDegreeOfParallelism = 8
        };

        public DiscordWindow(string Token)
        {
            InitializeComponent();
            this.Token = Token;

            App.Current.MainWindow = this;

            statusOverlay.Visibility = Visibility.Visible;
            settingsContainer.Children.Add(new SettingsUI());
            CommandManager.RegisterClassCommandBinding(typeof(DiscordWindow), new CommandBinding(ApplicationCommands.Paste, new ExecutedRoutedEventHandler(messageTextBox_PasteAsync), new CanExecuteRoutedEventHandler(messageTextBox_CanPaste)));
            icon = new System.Windows.Forms.NotifyIcon();
            icon.Icon = Properties.Resources.app;
            icon.Visible = true;

            icon.BalloonTipShown += Icon_BalloonTipShown;

            icon.BalloonTipIcon = System.Windows.Forms.ToolTipIcon.Info;
        }


        private async void Window_Loaded(object sender, RoutedEventArgs e)
        {
            Client = new DiscordSocketClient(new DiscordSocketConfig
            {
                WebSocketProvider = WS4NetProvider.Instance,
                AlwaysDownloadUsers = false,
                MessageCacheSize = 0
            });
            RestClient = new DiscordRestClient();

            overlay.Visibility = Visibility.Visible;
            ((Storyboard)this.Resources["Enable"]).Completed += Enable_Completed;
            ((Storyboard)this.Resources["AnimateTypingShow"]).Completed += ScrollOnAnimationComplete;
            ((Storyboard)this.Resources["AnimateShow"]).Completed += ScrollOnAnimationComplete;

            Client.Log += Client_Log;
            Client.LoggedOut += Client_LoggedOut;
            Client.Connected += Client_Connected;
            Client.Disconnected += Client_Disconnected;

            RestClient.Log += Client_Log;
            RestClient.LoggedOut += Client_LoggedOut;

            Client.Ready += Client_Ready;

            Client.MessageReceived += Client_MessageReceived;
            Client.MessageDeleted += Client_MessageDeleted;
            Client.MessageUpdated += Client_MessageUpdated;
            Client.UserIsTyping += Client_UserIsTyping;
            Client.GuildUpdated += Client_GuildUpdated;

            Client.CurrentUserUpdated += Client_CurrentUserUpdated;

            Client.JoinedGuild += Client_JoinedGuild;
            Client.LeftGuild += Client_LeftGuild;

            StatusShowWarning("Connecting...", autoHide: false);

            await RestClient.LoginAsync(TokenType.User, Token);

            await Client.LoginAsync(type, Token);
            await Client.StartAsync();
        }

        private async Task Reset()
        {
            if (populate?.ThreadState == ThreadState.Running)
                populate.Abort();

            selectedGuild = null;
            selectedTextChannel = null;
            await Dispatcher.InvokeAsync(() =>
            {
                channelName.Text = "#channel";
                channelTopic.Text = "Select a channel to begin!";
                Title = $"Welcome - DiscordWPF";

                Icon = Properties.Resources.app.ToImageSource();

                messageViewer.Items.Clear();
                usersList.Items.Clear();
            });
        }

        private async Task RefreshGuilds()
        {

            await Dispatcher.InvokeAsync(() =>
            {
                GuildsList.Items.Clear();

                userName.Text = Client.CurrentUser.Username;
                userDiscrim.Text = $"#{Client.CurrentUser.Discriminator}";
                userProfile.ImageSource = Images.GetImage(Client.CurrentUser.GetAvatarUrl());

                DMGuildViewer dms = new DMGuildViewer(Client.DMChannels);
                GuildsList.Items.Add(dms);
                DMGuildViewer groups = new DMGuildViewer(Client.GroupChannels);
                GuildsList.Items.Add(groups);

                GuildsList.Items.Add(new Separator());
            });

            foreach (IGuild guild in Client.Guilds.OrderBy(g => g.Name))
            {
                await Dispatcher.InvokeAsync(() =>
                {
                    GuildViewer viewer = new GuildViewer(guild);
                    GuildsList.Items.Add(viewer);
                });

            }
        }

        private async Task Client_LeftGuild(SocketGuild arg)
        {
            GuildViewer viewer = GuildsList.Items.OfType<GuildViewer>().FirstOrDefault(g => g.Guild.Id == arg.Id);
            if (viewer != null)
            {
                await Dispatcher.InvokeAsync(() =>
                {
                    GuildsList.Items.Remove(viewer);
                });
            }

            if (selectedGuild.Id == arg.Id)
            {
                await Reset();
            }
        }

        private async Task Client_JoinedGuild(SocketGuild arg)
        {
            await RefreshGuilds();
        }

        private async Task Client_CurrentUserUpdated(ISelfUser arg1, ISelfUser arg2)
        {
            await Dispatcher.InvokeAsync(() =>
            {
                userName.Text = arg2.Username;
                userDiscrim.Text = $"#{arg2.Discriminator}";
                userProfile.ImageSource = Images.GetImage(arg2.GetAvatarUrl());
            });
        }

        private async Task Client_LoggedOut()
        {
            LoginWindow window = new LoginWindow();
            App.Current.MainWindow = window;
            window.Show();
            this.Close();
        }

        private void Enable_Completed(object sender, EventArgs e)
        {
            statusIcon.Spin = false;
            overlay.Visibility = Visibility.Hidden;
        }

        private async Task Client_Ready()
        {
            await Client.DownloadUsersAsync(Client.Guilds);

            foreach (IGuild guild in Client.Guilds.OrderBy(g => g.Name))
            {
                foreach (GuildEmote emote in guild.Emotes)
                {
                    if (!AvailableEmotes.ContainsValue(emote))
                    {
                        string emoteText = $"<:{emote.Name}:{emote.Id}>";
                        AvailableEmotes.Add(emoteText, emote);
                    }
                };
            }
        }

        private async Task Client_Log(LogMessage arg)
        {
            await Task.Run(() => Console.WriteLine(arg.Message ?? arg.Exception.ToString()));
        }

        private async Task Client_Disconnected(Exception arg)
        {
            if (Client.LoginState == LoginState.LoggedOut || Client.LoginState == LoginState.LoggingOut)
            {
                await Dispatcher.InvokeAsync(() =>
                {
                    LoginWindow window = new LoginWindow();
                    App.Current.MainWindow = window;
                    window.Show();
                    this.Close();
                });
            }
            else
                await Dispatcher.InvokeAsync(() =>
                {
                    StatusShowWarning("Disconnected. Attempting to reconnect...", autoHide: false);
                });
        }

        #region Client Event Handlers

        /// <summary>
        /// Task run on client connect. Handles main window initialisation
        /// </summary>
        /// <returns></returns>
        private async Task Client_Connected()
        {
            await RefreshGuilds();

            await Dispatcher.InvokeAsync(() =>
            {
                StatusShowSuccess("Connected!", true, true);
            });
        }

        /// <summary>
        /// Event fired on message recieve/send
        /// </summary>
        /// <param name="arg"></param>
        /// <returns></returns>
        private async Task Client_MessageReceived(IMessage arg)
        {
            typingUsers.RemoveAll(u => u.Id == arg.Author.Id);
            await Dispatcher.InvokeAsync(() =>
            {
                if (typingUsers.Any())
                {
                    typingViewer.Text = string.Join(", ", typingUsers.Select(t => Tools.Name(t))) + " is typing...";
                }
                else
                    ((Storyboard)this.Resources["AnimateTypingHide"]).Begin(typingContainer);
            });
            if (selectedTextChannel != null && arg.Channel.Id == selectedTextChannel.Id)
            {
                MessageViewer viewer = null;
                await Dispatcher.InvokeAsync(() =>
                {
                    bool willScroll = CheckScroll();
                    viewer = new MessageViewer(arg, selectedTextChannel);
                    messageViewer.Items.Add(viewer);
                    messageViewer.Items.RemoveAt(0);

                    if (willScroll)
                        messageViewer.ScrollIntoView(viewer);
                });
                if (viewer != null)
                    await CollapseMessage(viewer);
                cache.Add(arg as IUserMessage);
            }
            else if (await Notify(arg))
            {
                Notification notif = new Notification()
                {
                    ChannelId = arg.Channel.Id,
                    MessageId = arg.Id
                };

                if (arg.Channel is ITextChannel)
                {
                    notif.GuildId = (arg.Channel as ITextChannel).Guild.Id;
                    notif.Type = NotifIcationType.Guild;
                }
                else if (arg.Channel is IDMChannel)
                    notif.Type = NotifIcationType.DM;
                else if (arg.Channel is IGroupChannel)
                    notif.Type = NotifIcationType.Group;

                App.Config.CachedNotifications.Add(notif);
                App.Config.Save();

                if (!await Dispatcher.InvokeAsync(() => IsActive))
                {
                    await Dispatcher.InvokeAsync(async () =>
                    {
                        EventHandler handler;
                        if (arg.Channel is ITextChannel)
                        {
                            ITextChannel chan = arg.Channel as ITextChannel;
                            icon.BalloonTipTitle = $"Mentioned in #{arg.Channel.Name}/{chan.Guild.Name}";
                            icon.BalloonTipText = await Tools.ProcessMessageText(arg, chan.Guild);

                            handler = async (e, a) => await Refresh(arg.Channel as ITextChannel);
                            icon.BalloonTipClicked += handler;
                            icon.BalloonTipClosed += (e, a) => icon.BalloonTipClicked -= handler;
                            icon.ShowBalloonTip(5000);
                        }
                        else
                        {
                            icon.BalloonTipTitle = $"Message from @{arg.Author.Username}";
                            icon.BalloonTipText = await Tools.ProcessMessageText(arg, null);

                            handler = async (e, a) => await Refresh(arg.Channel as IDMChannel);
                            icon.BalloonTipClicked += handler;
                            icon.BalloonTipClosed += (e, a) => icon.BalloonTipClicked -= handler;

                            icon.ShowBalloonTip(5000);
                        }
                    });
                }
                else
                {
                    MouseButtonEventHandler handler = null;
                    if (arg.Channel is ITextChannel)
                    {
                        ITextChannel chan = arg.Channel as ITextChannel;
                        await Dispatcher.InvokeAsync(async () =>
                        {
                            notificationTitle.Text = $"Mentioned in #{arg.Channel.Name}/{chan.Guild.Name}";
                            notificationContent.Text = $"\"{await Tools.ProcessMessageText(arg, chan.Guild)}\"";

                            handler = async (e, a) => await Refresh(chan);
                            notificationContainer.MouseUp += handler;

                            Tools.PlayIMSound();
                            scrollOnAnimationComplete = CheckScroll();
                            ((Storyboard)this.Resources["AnimateShow"]).Begin(notificationText);
                        });
                        await Task.Delay(5000);
                        await Dispatcher.InvokeAsync(() =>
                        {
                            notificationContainer.MouseUp -= handler;
                            ((Storyboard)this.Resources["AnimateHide"]).Begin(notificationText);
                        });
                    }
                    else
                    {
                        await Dispatcher.InvokeAsync(async () =>
                        {
                            notificationTitle.Text = ($"Message from @{arg.Author.Username}");
                            notificationContent.Text = $"\"{await Tools.ProcessMessageText(arg, null)}\"";

                            handler = async (e, a) => await Refresh(arg.Channel as IDMChannel);
                            notificationContainer.MouseUp += handler;

                            Tools.PlayIMSound();
                            scrollOnAnimationComplete = CheckScroll();
                            ((Storyboard)this.Resources["AnimateShow"]).Begin(notificationContainer);
                        });
                        await Task.Delay(5000);
                        await Dispatcher.InvokeAsync(() =>
                        {
                            notificationContainer.MouseUp -= handler;
                            ((Storyboard)this.Resources["AnimateHide"]).Begin(notificationContainer);
                        });
                    }
                }
            }
        }

        /// <summary>
        /// Event fired when message is edited
        /// </summary>
        /// <param name="arg1"></param>
        /// <param name="arg2"></param>
        /// <param name="arg3"></param>
        /// <returns></returns>
        private async Task Client_MessageUpdated(Cacheable<IMessage, ulong> arg1, IMessage arg2, IMessageChannel arg3)
        {
            if (selectedTextChannel != null && arg3.Id == selectedTextChannel.Id)
            {
                int? index = null;
                IEnumerable<object> items = (await Dispatcher.InvokeAsync(new Func<ItemCollection>(() => messageViewer.Items))).Cast<object>();
                object item = items.FirstOrDefault(o => o is MessageViewer && (o as MessageViewer).IMessage.Id == arg2.Id);
                index = (await Dispatcher.InvokeAsync(new Func<ItemCollection>(() => messageViewer.Items))).IndexOf(item);


                if (index.HasValue)
                {
                    MessageViewer newViewer = null;
                    await Dispatcher.InvokeAsync(() =>
                    {
                        try
                        {
                            messageViewer.Items.RemoveAt(index.Value);
                        }
                        catch { }
                        newViewer = new MessageViewer(arg2, selectedTextChannel);
                        messageViewer.Items.Insert(index.Value, newViewer);
                    });
                    if (newViewer != null)
                        await CollapseMessage(newViewer);
                }
                cache.RemoveAll(m => m.Id == arg1.Id);
                cache.Add(arg2 as IUserMessage);
            }
        }

        /// <summary>
        /// Event fired when message is deleted
        /// </summary>
        /// <param name="arg1"></param>
        /// <param name="arg2"></param>
        /// <returns></returns>
        private async Task Client_MessageDeleted(Cacheable<IMessage, ulong> arg1, ISocketMessageChannel arg2)
        {
            if (selectedTextChannel != null && arg2.Id == selectedTextChannel.Id)
            {
                int index = 0;
                foreach (MessageViewer viewer in await Dispatcher.InvokeAsync(new Func<ItemCollection>(() => messageViewer.Items)))
                {
                    if (viewer.MessageId == arg1.Id)
                    {
                        index = await Dispatcher.InvokeAsync(new Func<int>(() => messageViewer.Items.IndexOf(viewer)));
                    }
                }
                await Dispatcher.InvokeAsync(() =>
                {
                    if (messageViewer.Items.Cast<object>().ElementAtOrDefault(index) != null)
                        messageViewer.Items.RemoveAt(index);
                });
                cache.RemoveAll(m => m.Id == arg1.Id);
            }
        }

        /// <summary>
        /// Event fired on guild update
        /// </summary>
        /// <param name="arg1"></param>
        /// <param name="arg2"></param>
        /// <returns></returns>
        private async Task Client_GuildUpdated(IGuild arg1, IGuild arg2)
        {
            await Task.Run(() =>
            {
                foreach (GuildEmote emote in arg1.Emotes)
                {
                    if (AvailableEmotes.ContainsValue(emote))
                        AvailableEmotes.Remove($"<:{emote.Name}:{emote.Id}>");
                }

                foreach (GuildEmote emote in arg2.Emotes)
                {
                    string emoteText = $"<:{emote.Name}:{emote.Id}>";
                    AvailableEmotes.Add(emoteText, emote);
                }
            });
        }

        bool CheckScroll()
        {
            return (Tools.GetChildOfType<ScrollViewer>(messageViewContainer).VerticalOffset >= Tools.GetChildOfType<ScrollViewer>(messageViewContainer).ScrollableHeight - 35 && Tools.GetChildOfType<ScrollViewer>(messageViewContainer).VerticalOffset <= Tools.GetChildOfType<ScrollViewer>(messageViewContainer).ScrollableHeight + 35);
        }

        bool scrollOnAnimationComplete = false;
        private bool populating;

        /// <summary>
        /// Event fired when external user starts typing
        /// </summary>
        /// <param name="arg1"></param>
        /// <param name="arg2"></param>
        /// <returns></returns>
        private async Task Client_UserIsTyping(IUser arg1, IMessageChannel arg2)
        {
            if (selectedTextChannel != null && arg2.Id == selectedTextChannel.Id)
            {
                if (!typingUsers.Contains(arg1) && arg1.Id != Client.CurrentUser.Id)
                    typingUsers.Add(arg1);

                await Dispatcher.InvokeAsync(() =>
                {
                    if (typingUsers.Any())
                    {
                        scrollOnAnimationComplete = CheckScroll();
                        ((Storyboard)this.Resources["AnimateTypingShow"]).Begin(typingContainer);
                        typingViewer.Text = string.Join(", ", typingUsers.Select(t => Tools.Name(t))) + " is typing...";
                    }
                    else
                        ((Storyboard)this.Resources["AnimateTypingHide"]).Begin(typingContainer);
                });

                new Thread(async () =>
                {
                    await Task.Delay(9000);
                    typingUsers.RemoveAll(u => u.Id == arg1.Id);

                    await Dispatcher.InvokeAsync(() =>
                    {
                        if (typingUsers.Any())
                        {
                            scrollOnAnimationComplete = CheckScroll();
                            ((Storyboard)this.Resources["AnimateTypingShow"]).Begin(typingContainer);
                            typingViewer.Text = string.Join(", ", typingUsers.Select(t => Tools.Name(t))) + " is typing...";
                        }
                        else
                            ((Storyboard)this.Resources["AnimateTypingHide"]).Begin(typingContainer);
                    });
                }).Start();

            }
        }

        private void ScrollOnAnimationComplete(object sender, EventArgs e)
        {
            if (scrollOnAnimationComplete && messageViewer.Items.Count > 0)
                messageViewer.ScrollIntoView(messageViewer.Items[messageViewer.Items.Count - 1]);
        }

        #endregion

        #region Public Methods

        void Enable()
        {
            if (App.Config.General.ReduceAnimations)
            {
                overlay.Opacity = 0;
                overlay.Visibility = Visibility.Hidden;
            }
            else
                ((Storyboard)this.Resources["Enable"]).Begin(overlay);
        }

        void Disable()
        {
            if (App.Config.General.ReduceAnimations)
            {
                overlay.Opacity = 0.5;
                overlay.Visibility = Visibility.Visible;
            }
            else
            {
                if (overlay.Visibility != Visibility.Visible)
                    overlay.Visibility = Visibility.Visible;
                ((Storyboard)this.Resources["Disable"]).Begin(overlay);
            }
        }

        public async void StatusShowSuccess(string text, bool autoHide = true, bool enable = true, bool disable = false, FontAwesomeIcon? icon = FontAwesomeIcon.Check)
        {
            await statusText.Dispatcher.InvokeAsync(() =>
            {
                if (icon == null)
                    statusIcon.Visibility = Visibility.Collapsed;
                else
                {
                    statusIcon.Visibility = Visibility.Visible;
                    statusIcon.Icon = icon.Value;
                    statusIcon.Spin = false;
                }

                statusText.Text = text;

                Storyboard story = CreateStoryboard(30, App.SuccessColour, 500);
                statusContainer.BeginStoryboard(story);

                if (disable)
                {
                    Disable();
                }
                else if (enable)
                    Enable();
            });
            if (autoHide)
            {
                await Task.Delay(2500);
                StatusHide();
            }
        }

        private Storyboard CreateStoryboard(int to, System.Windows.Media.Color toColour, int duration)
        {
            if ((toColour.R * 0.299 + toColour.G * 0.587 + toColour.B * 0.114) > 186)
            {
                statusIcon.Foreground = Brushes.Black;
                statusText.Foreground = Brushes.Black;
            }
            else
            {
                statusIcon.Foreground = Brushes.White;
                statusText.Foreground = Brushes.White;
            }

            Storyboard story = new Storyboard();
            CubicEase ease = new CubicEase();

            DoubleAnimation size = new DoubleAnimation(to, new Duration(App.Config.General.ReduceAnimations ? TimeSpan.FromMilliseconds(0) : TimeSpan.FromMilliseconds(duration)));
            size.EasingFunction = ease;
            Storyboard.SetTargetName(size, statusContainer.Name);
            Storyboard.SetTargetProperty(size, new PropertyPath(Grid.HeightProperty));

            ColorAnimation colour = new ColorAnimation(toColour, new Duration(App.Config.General.ReduceAnimations ? TimeSpan.FromMilliseconds(0) : TimeSpan.FromMilliseconds(duration)));
            colour.EasingFunction = ease;
            Storyboard.SetTargetName(colour, statusContainer.Name);
            Storyboard.SetTargetProperty(colour, new PropertyPath("(Panel.Background).(SolidColorBrush.Color)", statusContainer));

            story.Children.Add(size); story.Children.Add(colour);
            return story;
        }

        public async void StatusHide()
        {
            await statusText.Dispatcher.InvokeAsync(() =>
            {
                Storyboard story = CreateStoryboard(0, (statusContainer.Background as SolidColorBrush).Color, 500);
                statusContainer.BeginStoryboard(story);
                Enable();
            });
        }

        public async void StatusShowWarning(string text, bool disable = true, bool autoHide = true, FontAwesomeIcon? icon = FontAwesomeIcon.Cog)
        {
            await statusText.Dispatcher.InvokeAsync(() =>
            {
                if (icon == null)
                    statusIcon.Visibility = Visibility.Collapsed;
                else
                {
                    statusIcon.Visibility = Visibility.Visible;
                    statusIcon.Icon = icon.Value;

                    statusIcon.Spin = icon.Value == FontAwesomeIcon.Cog;
                }

                statusText.Text = text;

                Storyboard story = CreateStoryboard(30, App.WarningColour, 500);
                statusContainer.BeginStoryboard(story);

                if (disable)
                {
                    Disable();
                }
            });

            if (autoHide)
            {
                await Task.Delay(2500);
                StatusHide();
            }
        }

        public async void StatusShowError(string text, bool disable = true, bool autoHide = true, FontAwesomeIcon? icon = FontAwesomeIcon.Times)
        {
            await statusText.Dispatcher.InvokeAsync(() =>
            {
                if (icon == null)
                    statusIcon.Visibility = Visibility.Collapsed;
                else
                {
                    statusIcon.Visibility = Visibility.Visible;
                    statusIcon.Icon = icon.Value;
                    statusIcon.Spin = false;
                }

                statusText.Text = text;

                Storyboard story = CreateStoryboard(30, App.ErrorColour, 500);
                statusContainer.BeginStoryboard(story);

                if (disable)
                {
                    Disable();
                }
            });
            if (autoHide)
            {
                await Task.Delay(2500);
                StatusHide();
            }
        }

        /// <summary>
        /// Refreshes UI switching to another channel
        /// </summary>
        /// <param name="channel">The channel to switch to</param>
        public async Task Refresh(IMessageChannel channel)
        {
            if (channel is ITextChannel)
                StatusShowWarning($"Loading #{(channel).Name} - {(channel as IGuildChannel).Guild.Name}...", autoHide: false);
            else if (channel is IDMChannel)
                StatusShowWarning($"Loading @{(channel as IDMChannel).Recipient.Username}...", autoHide: false);
            else if (channel is IGroupChannel)
                StatusShowWarning($"Loading @{(channel as IGroupChannel).GetName()}...", autoHide: false);

            if (cgOverlay.Visibility != Visibility.Hidden)
                cgOverlay.Visibility = Visibility.Hidden;

            bool go = (channel is IGroupChannel) || App.Config.NsfwAgreedChannels.Contains(channel.Id) || !channel.IsNsfw;
            if (!(channel is IGroupChannel) && channel.IsNsfw && !go)
            {
                TaskDialog nsfwContent = new TaskDialog();

                nsfwContent.MainIcon = TaskDialogIcon.Warning;
                nsfwContent.WindowTitle = "NSFW content ahead!";
                nsfwContent.MainInstruction = "This channel is marked as NSFW";
                nsfwContent.Content = "This channel has been marked as NSFW! It may contain sensitive content such as pornography or other such lewd shit.\r\nDo you want to continue?";
                nsfwContent.VerificationText = "Don't ask me again.";

                nsfwContent.ButtonStyle = TaskDialogButtonStyle.CommandLinks;

                TaskDialogButton goButton = new TaskDialogButton("Go ahead!\nI'm willing to view NSFW content.\0");
                TaskDialogButton noButton = new TaskDialogButton("No thanks!\nI don't want to see NSFW content right now.\0");

                nsfwContent.Buttons.Add(goButton);
                nsfwContent.Buttons.Add(noButton);

                if (nsfwContent.ShowDialog() == goButton)
                {
                    if (nsfwContent.IsVerificationChecked)
                    {
                        App.Config.NsfwAgreedChannels.Add(channel.Id);
                        App.Config.Save();
                    }
                    go = true;
                }
            }

            if (go)
            {
                loaded = false;

                if (!string.IsNullOrWhiteSpace(messageTextBox.Text))
                {
                    App.Config.PartialMessages.Remove(selectedTextChannel.Id);
                    App.Config.PartialMessages.Add(selectedTextChannel.Id, messageTextBox.Text);
                }

                if (populate?.ThreadState == ThreadState.Running)
                    populate.Abort();

                if (channel is IGuildChannel)
                    selectedGuild = (channel as IGuildChannel).Guild;

                selectedTextChannel = channel;

                if (channel is ITextChannel)
                {
                    channelName.Text = "#" + (channel as ITextChannel).Name;
                    channelTopic.Text = (channel as ITextChannel).Topic;
                    Title = $"#{(channel as ITextChannel).Name} - {selectedGuild.Name} - DiscordWPF";

                    if ((channel as ITextChannel).Guild.IconUrl != null && App.Config.General.GuildImageWindowIcon)
                    {
                        Icon = Images.GetImage((channel as ITextChannel).Guild.IconUrl);
                        windowsTaskbar.SetOverlayIcon(Properties.Resources.app, selectedGuild.Name);
                    }
                    else
                        Icon = Properties.Resources.app.ToImageSource();
                }
                else if (channel is IDMChannel)
                {
                    channelName.Text = "@" + (channel as IDMChannel).Recipient.Username;
                    channelTopic.Text = (channel as IDMChannel).Recipient.Status.ToString();

                    Title = $"@{(channel as IDMChannel).Recipient.Username} - DiscordWPF";

                    if (App.Config.General.GuildImageWindowIcon && (channel as IDMChannel).Recipient.GetAvatarUrl() != null)
                    {
                        Icon = Images.GetImage((channel as IDMChannel).Recipient.GetAvatarUrl());
                        windowsTaskbar.SetOverlayIcon(Properties.Resources.app, selectedGuild?.Name);
                    }
                    else
                        Icon = Properties.Resources.app.ToImageSource();
                }
                else if (channel is IGroupChannel)
                {
                    channelName.Text = "@" + (channel as IGroupChannel).GetName();
                    channelTopic.Text = (await (channel as IGroupChannel).GetUsersAsync().Flatten()).Count() + " users";

                    Title = $"@{(channel as IGroupChannel).GetName()} - DiscordWPF";

                    Icon = Properties.Resources.app.ToImageSource();
                }

                mainContentGrid.IsEnabled = ChannelPermissions.SendMessages;

                if (ChannelPermissions.AttachFiles)
                {
                    uploadFileButton.Visibility = Visibility.Visible;
                }
                else
                {
                    uploadFileButton.Visibility = Visibility.Collapsed;
                }

                if (App.Config.PartialMessages.ContainsKey(channel.Id))
                    messageTextBox.Text = App.Config.PartialMessages[channel.Id];
                else
                    messageTextBox.Clear();

                usersList.Items.Clear();
                messageViewer.Items.Clear();

                await LoadMessageHistory(channel);
            }
            else
            {
                StatusHide();
            }

            App.Config.Save();
        }

        async Task LoadMessageHistory(IMessageChannel channel, ulong offset = 0, Direction dir = Direction.Before)
        {
            try
            {
                IEnumerable<IMessage> history;
                loaded = false;

                MessageViewer top = null;

                if (offset != 0)
                {
                    StatusShowWarning("Loading message history...", autoHide: false);
                    top = (messageViewer.Items[0] as MessageViewer);
                }

                if (offset == 0)
                    history = await channel.GetMessagesAsync(100).Flatten();
                else
                    history = await channel.GetMessagesAsync(offset, dir, 100).Flatten();
                if (history.Any())
                {
                    foreach (IMessage message in history)
                    {
                        await Dispatcher.InvokeAsync(() =>
                        {
                            MessageViewer viewer = new MessageViewer(message, channel);
                            messageViewer.Items.Insert(0, viewer);
                        }, DispatcherPriority.Loaded);
                    }

                    await Dispatcher.InvokeAsync(() =>
                    {
                        if (offset != 0)
                        {
                            messageViewer.ScrollIntoView(top);
                        }
                        else
                        {
                            if (messageViewer.Items.Count > 0)
                                messageViewer.ScrollIntoView(messageViewer.Items[messageViewer.Items.Count - 1]);
                        }

                        StatusHide();

                        loaded = true;
                    });

                    await CollapseMessages();
                }
                else
                    StatusShowSuccess("No more messages!", true, true, false, FontAwesomeIcon.Check);
            }
            catch (Exception ex)
            {
                if (ex is AggregateException)
                {
                    if ((ex as AggregateException).InnerExceptions.Any(e => e is RateLimitedException))
                    {
                        TaskDialog errorDialog = new TaskDialog();
                        errorDialog.MainIcon = TaskDialogIcon.Error;
                        errorDialog.WindowTitle = "Discord is being aids";
                        errorDialog.MainInstruction = "Discord is being aids!";
                        errorDialog.Content = $"You're being ratelimited while trying to load message history. Fucks sake.";
                        errorDialog.Buttons.Add(new TaskDialogButton(ButtonType.Ok));
                        errorDialog.Show();
                    }
                }
            }
        }

        private async Task CollapseMessages()
        {
            IEnumerable<MessageViewer> items = messageViewer.Items.OfType<MessageViewer>();
            foreach (MessageViewer viewer in items)
            {
                await CollapseMessage(viewer);
            }
        }

        private async Task CollapseMessage(MessageViewer viewer)
        {
            try
            {
                int index = messageViewer.Items.IndexOf(viewer);
                if (index != 0)
                {
                    MessageViewer user = (messageViewer.Items[index - 1] as MessageViewer);
                    if (!viewer.IMessage.Author.IsWebhook && viewer.IMessage.Author.Id == user.IMessage.Author.Id)
                    {
                        await Dispatcher.InvokeAsync(() =>
                        {
                            DateTime creation = viewer.IMessage.CreatedAt.LocalDateTime;
                            viewer.imageContainer.Visibility = Visibility.Collapsed;
                            viewer.authorName.Visibility = Visibility.Collapsed;

                            if (viewer.ToolTip == null)
                            {
                                viewer.ToolTip = new ToolTip();
                                (viewer.ToolTip as ToolTip).Content = $"Sent at {creation.ToLongTimeString()} - {creation.ToLongDateString()}";
                            }
                            else
                            {
                                (viewer.ToolTip as ToolTip).Content += $"\r\nSent at {creation.ToLongTimeString()} - {creation.ToLongDateString()}";
                            }
                        }, DispatcherPriority.Background);
                    }

                    await Dispatcher.InvokeAsync(() => viewer.Tag = true);
                }
            }
            catch { }
        }

        /// <summary>
        /// Sends a mesage to the current channel
        /// </summary>
        /// <param name="text">Message to send</param>
        /// <param name="attachment">Any attachments</param>
        private async Task SendMessage(string text, string attachment = null)
        {
            if (text == "!logout")
            {
                App.Config.Token = "";
                App.Config.Save();
                Close();
            }
            else
            {
                if (text.Length <= 2000)
                {
                    text = await ProcessMessageText(text, selectedTextChannel);
                    if (text.Length <= 2000)
                    {
                        if (attachment != null)
                            await selectedTextChannel.SendFileAsync(attachment, text);
                        else
                            await selectedTextChannel.SendMessageAsync(text);

                        if (App.Config.PartialMessages.ContainsKey(selectedTextChannel.Id))
                            App.Config.PartialMessages.Remove(selectedTextChannel.Id);
                        App.Config.Save();
                        messageTextBox.Clear();
                    }
                    else
                    {
                        StatusShowError("Your message is too long! Make sure its less than 2000 characters!", false);
                    }
                }
                else
                {
                    StatusShowError("Your message is too long! Make sure its less than 2000 characters!", false);
                }
            }
        }

        public static async Task<string> ProcessMessageText(string text, IMessageChannel selectedChannel)
        {
            if (selectedChannel is ITextChannel)
            {
                foreach (Emote emote in selectedGuild.Emotes)
                {
                    if (text.Contains($":{emote.Name}:"))
                    {
                        text = text.Replace($":{emote.Name}:", emote.ToString());
                    }
                }

                foreach (ITextChannel channel in (await selectedGuild.GetChannelsAsync()).OfType<ITextChannel>())
                    text = text.Replace($"#{channel.Name}", channel.Mention);
            }

            if (text.Contains("@") && text.Contains("#"))
                foreach (IUser user in await selectedTextChannel.GetUsersAsync().Flatten())
                    text = text.Replace($"@{user.Username}#{user.Discriminator}", user.Mention);

            return text;
        }

        /// <summary>
        /// Deletes a message
        /// </summary>
        /// <param name="message">Message to delete</param>
        public async void DeleteMessage(IMessage message)
        {
            try
            {
                if (App.Config.DeleteAgreed)
                {
                    StatusShowWarning("Deleting message...", false, false);
                    await selectedTextChannel.DeleteMessagesAsync(new List<ulong>() { message.Id });
                    StatusShowSuccess("Message deleted!");
                }
                else
                {
                    StatusShowWarning("Deleting message...");
                    TaskDialog deleteMessageDialog = new TaskDialog();
                    deleteMessageDialog.MainIcon = TaskDialogIcon.Warning;
                    deleteMessageDialog.WindowTitle = "Delete this message?";
                    deleteMessageDialog.MainInstruction = $"Are you sure you want to delete this message?";
                    if (message.Content != null)
                        deleteMessageDialog.Content = $"{message.Author.Username} ({message.CreatedAt.DateTime.ToShortTimeString()} - {message.CreatedAt.DateTime.ToShortDateString()}) \r\n\"{message.Content}\"";

                    TaskDialogButton yes = new TaskDialogButton(ButtonType.Yes);
                    deleteMessageDialog.Buttons.Add(yes);
                    deleteMessageDialog.Buttons.Add(new TaskDialogButton(ButtonType.No));

                    deleteMessageDialog.VerificationText = "Don't ask me again.";

                    if (deleteMessageDialog.ShowDialog() == yes)
                    {
                        App.Config.DeleteAgreed = deleteMessageDialog.IsVerificationChecked;

                        StatusShowWarning("Deleting message...", false, false);
                        await selectedTextChannel.DeleteMessagesAsync(new List<ulong>() { message.Id });
                        StatusShowSuccess("Message deleted!");
                    }
                    else
                    {
                        StatusHide();
                    }
                }
            }
            catch { StatusShowError("Unable to delete message.", false); }
        }

        /// <summary>
        /// Kicks a user from the selected guild
        /// </summary>
        /// <param name="user">The user to kick</param>
        /// <param name="guild">The guild to kick the user from</param>
        public async void Kick(IUser user, IGuild guild)
        {
            TaskDialog kickDialog = new TaskDialog();

            kickDialog.MainIcon = TaskDialogIcon.Warning;
            kickDialog.WindowTitle = $"Kicking @{user.Username}#{user.Discriminator}";
            kickDialog.MainInstruction = $"Are you sure you want to kick @{user.Username}#{user.Discriminator}?";
            kickDialog.Content = "Are you sure you want to kick this person? They will be able to re-join if they find an invite, and it will probably piss some people off!";

            kickDialog.ButtonStyle = TaskDialogButtonStyle.CommandLinks;

            TaskDialogButton goButton = new TaskDialogButton("I'll do it!\nI'm 110% sure I want to kick this person.\0");
            //goButton.ElevationRequired = true;
            TaskDialogButton noButton = new TaskDialogButton("Maybe not.\nOn second thoughts, maybe it's not worth the hastle.\0");
            noButton.Default = true;

            kickDialog.Buttons.Add(goButton);
            kickDialog.Buttons.Add(noButton);
            //kickDialog.Buttons.Add(new TaskDialogButton(ButtonType.Cancel));

            if (kickDialog.ShowDialog() == goButton)
            {
                await (user as IGuildUser).KickAsync($"Kicked through DiscordWPF by @{Client.CurrentUser.Username}#{Client.CurrentUser.Discriminator}");
                StatusShowSuccess("User Kicked!");
            }
        }

        /// <summary>
        /// Bans a user from a guild
        /// </summary>
        /// <param name="user">The user to ban</param>
        /// <param name="guild">The guild to ban from</param>
        public async void Ban(IUser user, IGuild guild)
        {
            TaskDialog banDialog = new TaskDialog();

            banDialog.MainIcon = TaskDialogIcon.Error;
            banDialog.WindowTitle = $"Banning @{user.Username}#{user.Discriminator}";
            banDialog.MainInstruction = $"Are you sure you want to permanently ban @{user.Username}#{user.Discriminator}?";
            banDialog.Content = "Are you absolutely, positively, 110% sure you want to ban this person? They won't be able to re-join and are banned by IP address. It will almost certainly piss some people off!";

            banDialog.ButtonStyle = TaskDialogButtonStyle.CommandLinks;

            TaskDialogButton goButton = new TaskDialogButton("I'll do it!\nI'm 110% sure I want to ban this person.\0");
            //goButton.ElevationRequired = true;
            TaskDialogButton noButton = new TaskDialogButton("Maybe not.\nOn second thoughts, maybe it's not worth the hastle.\0");
            noButton.Default = true;

            banDialog.Buttons.Add(goButton);
            banDialog.Buttons.Add(noButton);
            //kickDialog.Buttons.Add(new TaskDialogButton(ButtonType.Cancel));

            if (banDialog.ShowDialog() == goButton)
            {
                await guild.AddBanAsync(user, reason: $"Banned through DiscordWPF by @{Client.CurrentUser.Username}#{Client.CurrentUser.Discriminator}");
                StatusShowSuccess("User Banned!");
            }
        }

        /// <summary>
        /// Changes the nickname of a specific user
        /// </summary>
        /// <param name="user">The user to change the nickname of</param>
        public async void ChangeNickname(IUser user)
        {
            if (GuildPermissions.HasValue)
            {
                IGuildUser usr = user as IGuildUser;

                string text = usr.Id == Client.CurrentUser.Id ? "your" : Tools.Name(usr) + "'s";
                if ((usr.Id == Client.CurrentUser.Id && GuildPermissions.Value.ChangeNickname) || GuildPermissions.Value.ManageNicknames)
                {
                    Dialogs.InputDialog dialog = new Dialogs.InputDialog();
                    dialog.Owner = this;
                    dialog.MainInstruction = $"Change {text} nickname";
                    dialog.Text = $"Here you can change {text} nickname. Keep in mind, everyone can see it! You probably shouldn't be rude! They're also limited to 32 characters. Keep that in mind.";
                    dialog.Input = Tools.Name(usr);

                    if (dialog.ShowDialog() == true)
                    {
                        if (dialog.Input.Length <= 32)
                        {
                            await usr.ModifyAsync(new Action<GuildUserProperties>((g) =>
                            {
                                if (dialog.Input != Tools.Name(usr))
                                    g.Nickname = dialog.Input;
                            }));
                            StatusShowSuccess("Nickname updated!");
                        }
                        else
                        {
                            StatusShowError("Nickname too long! Make sure its less than 32 characters!");
                        }
                    }
                }
                else
                {
                    StatusShowError($"You dont have the permission to change {text} nickname!");
                }
            }
            else
            {
                StatusShowError("You can only change your nickname in a guild!");
            }
        }

        /// <summary>
        /// Shows user detail popover/dialog/window
        /// </summary>
        /// <param name="user">The user to show details of</param>
        public void ShowUserDetails(IUser user, FrameworkElement baseElement)
        {
            UserInfo info = new UserInfo(user as IGuildUser, baseElement);
            info.Show();
        }

        /// <summary>
        /// Inserts a mention into the current message
        /// </summary>
        /// <param name="user">The user to mention</param>
        public void MentionUser(IUser user)
        {
            messageTextBox.AppendText($"@{user.Username}#{user.Discriminator}");
        }

        /// <summary>
        /// Switches between small and large mode
        /// TODO: Refine
        /// </summary>
        public void GoSmall()
        {
            if (userName.Visibility == Visibility.Collapsed)
                userName.Visibility = Visibility.Visible;
            else
                userName.Visibility = Visibility.Collapsed;

            if (userDiscrim.Visibility == Visibility.Collapsed)
                userDiscrim.Visibility = Visibility.Visible;
            else
                userDiscrim.Visibility = Visibility.Collapsed;

            foreach (DMGuildViewer viewer in GuildsList.Items.OfType<DMGuildViewer>())
            {
                if (viewer.guildName.Visibility == Visibility.Collapsed)
                    viewer.guildName.Visibility = Visibility.Visible;
                else
                    viewer.guildName.Visibility = Visibility.Collapsed;

                if (viewer.guildDescription.Visibility == Visibility.Collapsed)
                    viewer.guildDescription.Visibility = Visibility.Visible;
                else
                    viewer.guildDescription.Visibility = Visibility.Collapsed;
            }

            foreach (GuildViewer viewer in GuildsList.Items.OfType<GuildViewer>())
            {
                if (viewer.guildName.Visibility == Visibility.Collapsed)
                    viewer.guildName.Visibility = Visibility.Visible;
                else
                    viewer.guildName.Visibility = Visibility.Collapsed;

                if (viewer.guildDescription.Visibility == Visibility.Collapsed)
                    viewer.guildDescription.Visibility = Visibility.Visible;
                else
                    viewer.guildDescription.Visibility = Visibility.Collapsed;
            }
        }

        /// <summary>
        /// Populates the user list. Should be run on seperate thread.
        /// </summary>
        private async void Populate()
        {
            populating = true;
            List<IUser> handledUsers = new List<IUser>();
            IEnumerable<IUser> users = (await selectedTextChannel.GetUsersAsync().Flatten());

            foreach (IRole role in selectedGuild.Roles.Where(r => r.IsHoisted).OrderByDescending(r => r.Position).Cast<SocketRole>().Where(r => r.Members.Any(m => m.Status != UserStatus.Offline && m.Status != UserStatus.Invisible)))
            {
                await Dispatcher.InvokeAsync(() =>
                {
                    TextBlock guildLabel = new TextBlock();
                    guildLabel.Padding = new Thickness(5);
                    guildLabel.FontWeight = FontWeights.Bold;
                    guildLabel.Text = role.Name;
                    guildLabel.Focusable = false;
                    guildLabel.FontSize = 14;
                    guildLabel.Foreground = App.ForegroundBrush;

                    usersList.Items.Add(guildLabel);
                }, DispatcherPriority.Input);
                foreach (IGuildUser user in (role as SocketRole).Members.OfType<IGuildUser>().Where(u => u.Status != UserStatus.Invisible && u.Status != UserStatus.Offline).OrderBy(u => Tools.Name(u)))
                {
                    if (!populating)
                        break;
                    if (!handledUsers.Contains(user))
                    {
                        await Dispatcher.InvokeAsync(() =>
                        {
                            usersList.Items.Add(new UserViewer(user));
                        }, DispatcherPriority.Input);
                        handledUsers.Add(user);
                    }
                }
            }

            IEnumerable<IUser> onlineUsers = users.Where(u => !handledUsers.Contains(u) && u.Status != UserStatus.Invisible && u.Status != UserStatus.Offline).OrderBy(u => Tools.Name(u));

            if (onlineUsers.Any())
            {
                await Dispatcher.InvokeAsync(() =>
                {
                    TextBlock onlineLabel = new TextBlock();
                    onlineLabel.Padding = new Thickness(5);
                    onlineLabel.FontWeight = FontWeights.Bold;
                    onlineLabel.Text = "Online";
                    onlineLabel.Focusable = false;
                    onlineLabel.FontSize = 14;
                    onlineLabel.Foreground = App.ForegroundBrush;

                    usersList.Items.Add(onlineLabel);
                }, DispatcherPriority.Input);

                foreach (IUser user in onlineUsers)
                {
                    if (!populating)
                        break;
                    if (!handledUsers.Contains(user))
                    {
                        await Dispatcher.InvokeAsync(() =>
                        {
                            usersList.Items.Add(new UserViewer(user as IGuildUser));
                        }, DispatcherPriority.Input);
                        handledUsers.Add(user);
                    }
                }
            }

            IEnumerable<IUser> offlineUsers = users.Where(u => !handledUsers.Contains(u)).OrderBy(u => Tools.Name(u));

            if (offlineUsers.Any() && offlineUsers.Count() <= 150)
            {
                await Dispatcher.InvokeAsync(() =>
                {
                    TextBlock offlineLabel = new TextBlock();
                    offlineLabel.Padding = new Thickness(5);
                    offlineLabel.FontWeight = FontWeights.Bold;
                    offlineLabel.Text = "Offline";
                    offlineLabel.Focusable = false;
                    offlineLabel.FontSize = 14;

                    offlineLabel.Foreground = App.ForegroundBrush;

                    usersList.Items.Add(offlineLabel);
                }, DispatcherPriority.Input);
                foreach (IGuildUser user in offlineUsers)
                {
                    if (!populating)
                        break;
                    if (!handledUsers.Contains(user))
                    {
                        await Dispatcher.InvokeAsync(() =>
                        {
                            UserViewer view = new UserViewer(user);
                        //view.IsEnabled = false;
                        usersList.Items.Add(view);
                        }, DispatcherPriority.Input);
                        handledUsers.Add(user);
                    }
                }
            }
        }

        #endregion

        private void Icon_BalloonTipShown(object sender, EventArgs e)
        {
            Tools.PlayIMSound();
        }

        private void TextBox_TextChanged(object sender, TextChangedEventArgs e)
        {
            if (selectedTextChannel != null)
            {
                if (App.Config.PartialMessages.ContainsKey(selectedTextChannel.Id))
                {
                    App.Config.PartialMessages.Remove(selectedTextChannel.Id);
                    App.Config.Save();
                }

                if ((DateTime.Now - lastSent).Seconds > 9)
                {
                    lastSent = DateTime.Now;
                    selectedTextChannel.TriggerTypingAsync();
                }
            }
        }

        private async void TextBox_KeyDown(object sender, KeyEventArgs e)
        {
            if (e.Key == Key.Enter && !Keyboard.IsKeyDown(Key.LeftShift))
            {
                string toSend = messageTextBox.Text;
                e.Handled = true;
                await SendMessage(toSend);
            }
            else if (e.Key == Key.Up && string.IsNullOrWhiteSpace(messageTextBox.Text))
            {
                e.Handled = true;
                MessageViewer viewer = messageViewer.Items.OfType<MessageViewer>().Reverse().FirstOrDefault(m => m.UserId == Client.CurrentUser.Id);
                if (viewer != null)
                {
                    viewer.EditMessage();
                }
            }
        }

        private async void Button_Click(object sender, RoutedEventArgs e)
        {
            if (!string.IsNullOrWhiteSpace(messageTextBox.Text))
                await SendMessage(messageTextBox.Text);
        }

        private void TextBlock_MouseUp(object sender, MouseButtonEventArgs e)
        {
            if (headerRow.MaxHeight == 32)
                headerRow.MaxHeight = Double.PositiveInfinity;
            else
                headerRow.MaxHeight = 32;
        }

        private void userListButton_Click(object sender, RoutedEventArgs e)
        {
            if (selectedGuild != null)
                if (userListContainer.RenderSize.Width == 0 && selectedGuild != null)
                {
                    usersList.Items.Clear();
                    if (populate?.ThreadState != ThreadState.Running)
                    {
                        populate = new Thread(() => Populate());
                        populate.Start();
                    }
                    ((Storyboard)this.Resources["AnimateUserListShow"]).Begin(userListContainer);
                }
                else
                {
                    if (populate?.ThreadState == ThreadState.Running)
                    {
                        populating = false;
                        populate.Interrupt();
                        if (!populate.Join(500))
                        {
                            populate.Abort();
                        }
                    }
                    usersList.Items.Clear();
                    ((Storyboard)this.Resources["AnimateUserListHide"]).Begin(userListContainer);
                }
        }

        private async void uploadFileButton_Click(object sender, RoutedEventArgs e)
        {
            Microsoft.WindowsAPICodePack.Dialogs.CommonOpenFileDialog uploadFileDialog = new Microsoft.WindowsAPICodePack.Dialogs.CommonOpenFileDialog();
            uploadFileDialog.Filters.Add(new Microsoft.WindowsAPICodePack.Dialogs.CommonFileDialogFilter("All files", "*.*"));
            uploadFileDialog.Title = "Choose a file to upload";
            if (uploadFileDialog.ShowDialog() == Microsoft.WindowsAPICodePack.Dialogs.CommonFileDialogResult.Ok)
            {
                await UploadFile(uploadFileDialog.FileName);
            }
        }

        public async Task UploadFile(string path)
        {
            FileInfo info = new FileInfo(path);
            if (info.Length < 8000000)
            {
                await Dispatcher.InvokeAsync(async () =>
                {
                    Dialogs.InputDialog dialog = new Dialogs.InputDialog();
                    dialog.Owner = this;
                    dialog.MainInstruction = $"Uploading \"{Path.GetFileName(path)}\"";
                    dialog.Text = $"Enter a comment (optional)";
                    dialog.Input = messageTextBox.Text;

                    if (dialog.ShowDialog() == true)
                    {
                        StatusShowWarning($"Uploading {Path.GetFileName(path)}...", autoHide: false, disable: true);
                        await SendMessage(dialog.Input, path);
                        StatusShowSuccess($"Uploaded {Path.GetFileName(path)}!", autoHide: true, enable: true);
                    }
                    else if (overlay.Visibility == Visibility.Visible)
                    {
                        StatusHide();
                    }
                });
            }
            else
            {
                StatusShowError("Attachment too large! Make sure it's under 8MB!", false);
            }
        }

        private void ScrollViewer_SizeChanged(object sender, SizeChangedEventArgs e)
        {
            usersList.MinHeight = messageViewContainer.RenderSize.Height;
            //settingsContainer.MaxHeight = ActualHeight;
        }

        private void ScrollViewer_PreviewMouseWheel(object sender, MouseWheelEventArgs e)
        {
            ScrollViewer scv = (ScrollViewer)sender;
            scv.ScrollToVerticalOffset(scv.VerticalOffset - e.Delta);
            e.Handled = true;
        }

        private void Ellipse_MouseUp(object sender, MouseButtonEventArgs e)
        {
            if (!SystemParameters.SwapButtons)
            {
                settingsContainer.MaxHeight = this.RenderSize.Height;
                ((Storyboard)this.Resources["AnimateSettingsShow"]).Begin(settingsContainer);
            }
        }

        private void mainWindow_Closing(object sender, System.ComponentModel.CancelEventArgs e) => icon.Dispose();

        private async void ScrollViewer_ScrollChanged(object sender, ScrollChangedEventArgs e)
        {
            ;
            if (loaded && e.ExtentHeight > 0 && e.ExtentWidthChange == 0 && e.ViewportHeightChange == 0 && e.ViewportWidthChange == 0 && e.ExtentHeightChange == 0 && e.VerticalOffset == 0)
            {
                try
                {
                    await LoadMessageHistory(selectedTextChannel, (messageViewer.Items[0] as MessageViewer).IMessage.Id);
                }
                catch { }
            }
        }


        private void messageTextBox_CanPaste(object sender, CanExecuteRoutedEventArgs e)
        {
            if ((Clipboard.ContainsText() && ChannelPermissions.SendMessages) || ((Clipboard.ContainsFileDropList() || Clipboard.ContainsImage()) && ChannelPermissions.AttachFiles))
            {
                e.CanExecute = true;
            }
        }

        private async void messageTextBox_PasteAsync(object sender, ExecutedRoutedEventArgs e)
        {
            e.Handled = true;
            if (Clipboard.ContainsText())
            {
                messageTextBox.Text += Clipboard.GetText();
            }
            else if (ChannelPermissions.AttachFiles)
            {
                if (Clipboard.ContainsImage())
                {
                    BitmapSource bmpSource = Clipboard.GetImage();
                    string filePath = Path.Combine(Path.GetTempPath(), Path.GetFileNameWithoutExtension(Path.GetRandomFileName()) + ".png");
                    using (FileStream file = File.OpenWrite(filePath))
                    {
                        BitmapEncoder encoder = new PngBitmapEncoder();
                        encoder.Frames.Add(BitmapFrame.Create(bmpSource));
                        encoder.Save(file);
                    }

                    await UploadFile(filePath);
                }
                else if (Clipboard.ContainsFileDropList())
                {
                    StringCollection files = Clipboard.GetFileDropList();
                    foreach (string file in files)
                    {
                        await UploadFile(file);
                    }

                    return;
                }
            }
            else
            {
                StatusShowError("You can't attach files in this channel!", false, true);
            }
        }

        private async void mainWindow_Drop(object sender, DragEventArgs e)
        {
            if (selectedTextChannel != null)
            {
                if (ChannelPermissions.SendMessages)
                {
                    if (e.Data.GetDataPresent(DataFormats.Text) || e.Data.GetDataPresent(DataFormats.UnicodeText))
                    {
                        messageTextBox.AppendText((string)e.Data.GetData(DataFormats.UnicodeText, true));
                    }
                    else if (e.Data.GetDataPresent(DataFormats.FileDrop) && ChannelPermissions.AttachFiles)
                    {
                        string[] files = (string[])e.Data.GetData(DataFormats.FileDrop, true);
                        foreach (string file in files)
                        {
                            await UploadFile(file);
                        }

                        return;
                    }
                }
            }
            if (overlay.Visibility == Visibility.Visible)
            {
                StatusHide();
                e.Effects = DragDropEffects.None;
            }
        }

        private void mainWindow_DragEnter(object sender, DragEventArgs e)
        {
            if (selectedTextChannel != null)
            {
                if (ChannelPermissions.SendMessages)
                {
                    if (e.Data.GetDataPresent(DataFormats.Text) || e.Data.GetDataPresent(DataFormats.UnicodeText))
                    {
                        StatusShowSuccess("Drop to insert text!", autoHide: false, enable: false, disable: true, icon: FontAwesomeIcon.Paste);
                    }
                    else if (e.Data.GetDataPresent(DataFormats.FileDrop) && ChannelPermissions.AttachFiles)
                    {
                        StatusShowSuccess("Drop to attach files! Hold shift to upload directly.", autoHide: false, enable: false, disable: true, icon: FontAwesomeIcon.Upload);
                    }
                    else
                    {
                        StatusShowError("You can't attach files in this channel!", autoHide: false);
                        e.Effects = DragDropEffects.None;
                    }
                }
                else
                {
                    StatusShowError("You can't send messages in this channel!", autoHide: false);
                    e.Effects = DragDropEffects.None;
                }
            }
            else
            {
                StatusShowError("Select a channel before attaching files!", autoHide: false);
                e.Effects = DragDropEffects.None;
            }
        }

        private void mainWindow_DragLeave(object sender, DragEventArgs e)
        {
            if (overlay.Visibility == Visibility.Visible)
            {
                StatusHide();
                e.Effects = DragDropEffects.None;
            }
        }

        private void mainWindow_MouseUp(object sender, MouseButtonEventArgs e)
        {
            if (!pinnedMessageButton.IsMouseOver && !pinnedMessages.IsMouseOver)
            {
                pinnedMessageButton.IsChecked = false;
            }
            if (!notificationButton.IsMouseOver && !notificationContent.IsMouseOver)
            {
                notificationButton.IsChecked = false;
            }
        }

        private async void notificationButton_Checked(object sender, RoutedEventArgs e)
        {
            await PopulateNotifications();
        }

        public static async Task<bool> Notify(IMessage msg)
        {
            if (!msg.Channel.IsMuted())
            {
                if (msg.MentionedUserIds.Any(m => m == App.CurrentUser.Id))
                    return true;

                if (selectedTextChannel is IGuildChannel)
                {
                    IGuildUser usr = await selectedGuild.GetCurrentUserAsync();
                    if (msg.MentionedRoleIds.Any(r => (usr.RoleIds.Contains(r))))
                        return true;
                }

            }

            return false;
        }

        private async Task PopulateNotifications()
        {
            if (App.Config.CachedNotifications.Any())
            {
                await Dispatcher.InvokeAsync(() =>
                {
                    notificationViewer.Children.Clear();
                    notificationViewerContainer.Visibility = Visibility.Collapsed;
                    loadingNotifications.Visibility = Visibility.Visible;
                    noNotifications.Visibility = Visibility.Collapsed;
                });

                IEnumerable<IGrouping<ulong, Notification>> guildSplit = await Task.Run(() =>
                App.Config.CachedNotifications
                    .Where(n => n.Type == NotifIcationType.Guild)
                    .GroupBy(n => n.GuildId.Value));
                IEnumerable<IGrouping<ulong, Notification>> otherSplit = await Task.Run(() => App.Config.CachedNotifications
                    .Where(n => n.Type != NotifIcationType.Guild)
                    .GroupBy(n => n.ChannelId));

                List<IGrouping<ulong, Notification>> notificationGroups = await Task.Run(() => guildSplit.Concat(otherSplit).ToList());

                foreach (IGrouping<ulong, Notification> notificationGroup in notificationGroups)
                {
                    List<IMessage> groupMessages = new List<IMessage>();
                    IGuild guild = null;
                    IRestMessageChannel channel = null;
                    IUser author = null;
                    NotifIcationType? type = null;
                    foreach (Notification notification in notificationGroup)
                    {
                        IMessage message = null;
                        channel = (await RestClient.GetChannelAsync(notification.ChannelId) as IRestMessageChannel);
                        if (channel != null)
                        {
                            type = notification.Type;
                            IEnumerable<RestMessage> messages = (await channel.GetMessagesAsync(notification.MessageId, Direction.Around, 1).Flatten());
                            message = messages.First();
                            author = message.Author;

                            if (message != null)
                                groupMessages.Add(message);
                        }
                        await Task.Delay(50); // Fuck ratelimiting
                    }
                    if (groupMessages.Any())
                    {
                        NotificationGroupModel model = new NotificationGroupModel()
                        {
                            Channel = channel,
                            Guild = guild,
                            Type = type.Value,
                            Messages = groupMessages,
                            User = author
                        };
                        await Dispatcher.InvokeAsync(() =>
                        {
                            NotificationGroup group = new NotificationGroup(model);
                            notificationViewer.Children.Add(group);
                        });
                    }
                }

                await Dispatcher.InvokeAsync(() =>
                {
                    notificationViewerContainer.Visibility = Visibility.Visible;
                    loadingNotifications.Visibility = Visibility.Collapsed;
                    noNotifications.Visibility = Visibility.Collapsed;
                });
            }
            else
            {
                await Dispatcher.InvokeAsync(() =>
                {
                    notificationViewerContainer.Visibility = Visibility.Collapsed;
                    loadingNotifications.Visibility = Visibility.Collapsed;
                    noNotifications.Visibility = Visibility.Visible;
                });
            }
        }
    }
}

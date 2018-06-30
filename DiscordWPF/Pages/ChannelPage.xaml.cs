using DiscordWPF.Controls;
using DiscordWPF.Windows;
using DSharpPlus;
using DSharpPlus.Entities;
using DSharpPlus.EventArgs;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Text;
using System.Threading;
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
using System.Windows.Threading;
using WamWooWam.Core;
using WamWooWam.Wpf;

using static DiscordWPF.Constants;

namespace DiscordWPF.Pages
{
    /// <summary>
    /// Interaction logic for ChannelPage.xaml
    /// </summary>
    public partial class ChannelPage : Page
    {
        public static readonly DependencyProperty ChannelProperty =
            DependencyProperty.Register("Channel", typeof(DiscordChannel), typeof(ChannelPage), new PropertyMetadata(null, ChannelChanged));

        public DiscordChannel Channel { get => GetValue(ChannelProperty) as DiscordChannel; set => SetValue(ChannelProperty, value); }

        private SemaphoreSlim _loading = new SemaphoreSlim(1, 1);
        private CancellationTokenSource _cancellationToken = new CancellationTokenSource();
        private ulong _current;

        public ChannelPage()
        {
            InitializeComponent();
        }

        private static async void ChannelChanged(DependencyObject d, DependencyPropertyChangedEventArgs e)
        {
            var page = d as ChannelPage;
            if (e.NewValue is DiscordChannel channel)
            {
                if (page._current != channel.Id)
                {
                    page._current = channel.Id;
                    await page.Update(channel);
                }
            }
        }

        private async Task Update(DiscordChannel channel)
        {
            _cancellationToken.Cancel();
            _cancellationToken = new CancellationTokenSource();

            try
            {
                await _loading.WaitAsync(_cancellationToken.Token);

                header.Inlines.Clear();

                uploadButton.Visibility = Visibility.Visible;
                sendButton.Visibility = Visibility.Visible;
                messageTextBox.IsEnabled = true;
                messageTextBox.Text = "";

                if (channel is DiscordDmChannel dm)
                {
                    if (dm.Type == ChannelType.Private)
                    {
                        header.Inlines.Add(new Run("@"));
                        header.Inlines.Add(GetHeaderRun(dm.Recipient.Username));
                        header.Inlines.Add(new Run($"#{dm.Recipient.Discriminator}"));

                        userListButton.Visibility = Visibility.Collapsed;

                        Title = $"@{dm.Recipient.Username}#{dm.Recipient.Discriminator}";
                        placeholderText.Text = "Message " + Title;
                    }
                    else
                    {
                        var str = dm.Name ?? Strings.NaturalJoin(dm.Recipients.Select(r => r.Username));
                        header.Inlines.Add(GetHeaderRun(str));

                        Title = str;
                        placeholderText.Text = "Message " + str;
                    }
                }
                else
                {
                    header.Inlines.Add(new Run("#"));
                    header.Inlines.Add(GetHeaderRun(channel.Name));
                    Title = $"#{channel.Name}";

                    placeholderText.Text = "Message " + Title;

                    if (channel.Guild != null)
                    {
                        Title += $" - {channel.Guild.Name}";

                        var perms = channel.PermissionsFor(channel.Guild.CurrentMember);
                        if (!perms.HasPermission(Permissions.SendMessages))
                        {
                            messageTextBox.Text = "This channel is read-only.";
                            messageTextBox.IsEnabled = false;
                            uploadButton.Visibility = Visibility.Collapsed;
                            sendButton.Visibility = Visibility.Collapsed;
                        }

                        if (!perms.HasPermission(Permissions.AttachFiles))
                        {
                            uploadButton.Visibility = Visibility.Collapsed;
                        }
                    }
                }

                messageTextBox.Focus();

                foreach (var viewer in messagesPanel.Children.OfType<MessageViewer>().ToArray())
                {
                    messagesPanel.Children.Remove(viewer);
                }

                var messages = await channel.GetMessagesAsync(50);
                foreach (var group in messages.Reverse().Split(10))
                {
                    await Dispatcher.InvokeAsync(() =>
                    {
                        foreach(var message in group)
                        {
                            var viewer = MessageViewerFactory.GetViewerForMessage(message);

                            if (viewer == null)
                            {
                                viewer = new MessageViewer() { Message = message };
                                Debug.WriteLine("MessageViewer was null!!");
                            }

                            messagesPanel.Children.Add(viewer);
                        }
                    });
                }

                _loading.Release();

                await Dispatcher.InvokeAsync(() => messagesScroll.ScrollToEnd(), DispatcherPriority.ApplicationIdle);
            }
            catch (Exception ex)
            {
                if (!(ex is OperationCanceledException))
                {
                    App.Telemetry.TrackException(ex);
                }
            }
        }

        private void Page_Loaded(object sender, RoutedEventArgs e)
        {
            App.Discord.MessageCreated += Discord_MessageCreated;
            App.Discord.MessageDeleted += Discord_MessageDeleted;

            foreach (var item in messagesPanel.Children.OfType<MessageViewer>())
            {
                item.MiniMode = false;
            }

            messagesPanel.Margin = new Thickness(0, 10, 0, 10);

            miniCanvas.Visibility = Visibility.Hidden;
            popoutButton.Visibility = Visibility.Visible;
            closeButton.Visibility = Visibility.Collapsed;

            userListButton.Visibility = Channel.Type == ChannelType.Private ? Visibility.Collapsed : Visibility.Visible;
            pinsButton.Visibility = Visibility.Visible;
            searchButton.Visibility = Visibility.Visible;

            snapCheck.IsEnabled = false;

            var wind = this.FindVisualParent<ChannelWindow>();
            if (wind != null)
            {
                miniCanvas.Visibility = Visibility.Visible;
                popoutButton.Visibility = Visibility.Collapsed;

                if (wind.IsMiniMode)
                {
                    foreach (var item in messagesPanel.Children.OfType<MessageViewer>())
                    {
                        item.MiniMode = true;
                    }

                    closeButton.Visibility = Visibility.Visible;
                    searchButton.Visibility = Visibility.Collapsed;
                    pinsButton.Visibility = Visibility.Collapsed;
                    userListButton.Visibility = Visibility.Collapsed;
                    snapCheck.IsEnabled = true;
                }
            }
        }

        private async Task Discord_MessageDeleted(MessageDeleteEventArgs e)
        {
            await Dispatcher.InvokeAsync(() =>
            {
                if (e.Channel.Id == Channel.Id)
                {
                    var message = messagesPanel.Children.OfType<MessageViewer>().FirstOrDefault(m => m.Message.Id == e.Message.Id);
                    if (message != null)
                        messagesPanel.Children.Remove(message);
                }
            });
        }

        private void Page_Unloaded(object sender, RoutedEventArgs e)
        {
            App.Discord.MessageCreated -= Discord_MessageCreated;
        }

        private async Task Discord_MessageCreated(MessageCreateEventArgs e)
        {
            MessageViewer msgv = null;

            await Dispatcher.InvokeAsync(() =>
            {
                if (e.Channel.Id == Channel.Id)
                {
                    var currentMessages = messagesPanel.Children.OfType<MessageViewer>().ToArray();
                    if (!currentMessages.Any(m => m.Message.Id == e.Message.Id))
                    {
                        if (currentMessages.Length == 50)
                        {
                            msgv = currentMessages[0];
                            messagesPanel.Children.Remove(msgv);
                            msgv.Message = e.Message;
                            messagesPanel.Children.Add(msgv);
                        }
                        else
                        {
                            msgv = MessageViewerFactory.GetViewerForMessage(e.Message);
                            messagesPanel.Children.Add(msgv);
                        }
                    }
                }
            });

            if (msgv != null)
                await Dispatcher.InvokeAsync(() => msgv.OnLoaded(), DispatcherPriority.Background);
        }

        private void messageTextBox_TextChanged(object sender, TextChangedEventArgs e)
        {

        }

        private async void sendButton_Click(object sender, RoutedEventArgs e)
        {
            await SendMessageAsync();
        }

        private async void messageTextBox_PreviewKeyDown(object sender, KeyEventArgs e)
        {
            if (e.Key == Key.Return && !Keyboard.IsKeyDown(Key.LeftShift))
            {
                e.Handled = true;
                await SendMessageAsync();
            }
        }

        private async Task SendMessageAsync()
        {
            if (!string.IsNullOrWhiteSpace(messageTextBox.Text))
            {
                string str = messageTextBox.Text;
                messageTextBox.Text = "";

                var message = await Channel.SendMessageAsync(str);
            }
        }

        private void CommandBinding_Executed(object sender, ExecutedRoutedEventArgs e)
        {
            Process.Start(e.Parameter.ToString());
        }

        #region Popouts
        private void popoutButton_Click(object sender, RoutedEventArgs e)
        {
            Tools.OpenInNewWindow(this);
        }

        ChannelWindow _oldParent;

        private void miniCheck_Checked(object sender, RoutedEventArgs e)
        {
            _oldParent = this.FindVisualParent<ChannelWindow>();
            if (_oldParent != null)
            {
                _oldParent.Closed += (o, ev) => _oldParent = null;
                _oldParent.Frame.Navigate(null);
                _oldParent.Hide();
            }

            var wind = new ChannelWindow(Channel)
            {
                Width = 420,
                Height = 280,
                ResizeMode = ResizeMode.NoResize,
                WindowStyle = WindowStyle.None,
                AllowsTransparency = true,
                Topmost = true,
                IsMiniMode = true,
                IsSnapping = Settings.GetSetting(MINI_MODE_SNAP_ENABLED, true)
            };

            wind.Frame.Navigate(this);
            wind.Show();

            var binding = new Binding() { Source = wind, Path = new PropertyPath(ChannelWindow.IsSnappingProperty), Mode = BindingMode.TwoWay };
            snapCheck.SetBinding(MenuItem.IsCheckedProperty, binding);
        }

        private void miniCanvas_MouseDown(object sender, MouseButtonEventArgs e)
        {
            if (e.ChangedButton == MouseButton.Left)
            {
                this.FindVisualParent<ChannelWindow>()?.DragMove();
            }
        }

        private void miniCanvas_MouseUp(object sender, MouseButtonEventArgs e)
        {
            if (e.ChangedButton == MouseButton.Left)
            {
                this.FindVisualParent<ChannelWindow>()?.FinishDrag();
            }
        }

        private void CloseAndRestore()
        {
            var parent = this.FindVisualParent<ChannelWindow>();
            if (parent != null && parent.IsMiniMode)
            {
                parent.Frame.Navigate(null);
                parent.Close();
            }

            if (_oldParent != null)
            {
                _oldParent.Frame.Navigate(this);
                _oldParent.Show();

                _oldParent = null;
            }
        }

        private void miniCheck_Unchecked(object sender, RoutedEventArgs e) => CloseAndRestore();
        private void close_Click(object sender, RoutedEventArgs e) => CloseAndRestore();
        #endregion

        private Run GetHeaderRun(string str) => new Run(str) { Foreground = FindResource("SystemBaseHighBrush") as Brush, FontWeight = FontWeights.Bold };
    }
}

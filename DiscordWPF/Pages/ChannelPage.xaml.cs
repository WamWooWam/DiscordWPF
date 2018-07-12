using DiscordWPF.Controls;
using DiscordWPF.Windows;
using DSharpPlus;
using DSharpPlus.Entities;
using DSharpPlus.EventArgs;
using Microsoft.WindowsAPICodePack.Dialogs;
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
using System.IO;
using System.Windows.Threading;
using WamWooWam.Core;
using WamWooWam.Wpf;

using static DiscordWPF.Constants;
using DiscordWPF.Dialogs;
using Microsoft.WindowsAPICodePack.Shell;
using Microsoft.WindowsAPICodePack.Shell.PropertySystem;
using DiscordWPF.Abstractions;

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
            messageTextBox.AddHandler(CommandManager.CanExecuteEvent, new RoutedEventHandler(messageTextBox_CanPaste), true);
            messageTextBox.AddHandler(CommandManager.ExecutedEvent, new RoutedEventHandler(messageTextBox_OnPaste), true);
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
                        foreach (var message in group)
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
            {
                await Dispatcher.InvokeAsync(() => msgv.OnLoaded(), DispatcherPriority.Background);
                await Dispatcher.InvokeAsync(() => messagesScroll.ScrollToEnd(), DispatcherPriority.ApplicationIdle);
            }
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

        #region Uploads

        /// <summary>
        /// Allows <see cref="messageTextBox"/> to accept all paste formats, so I can handle it instead.
        /// </summary>
        private void messageTextBox_CanPaste(object sender, RoutedEventArgs e)
        {
            if (e is CanExecuteRoutedEventArgs canExecute && canExecute.Command == ApplicationCommands.Paste)
            {
                e.Handled = true;
                canExecute.CanExecute = true;
                canExecute.ContinueRouting = false;
            }
        }

        /// <summary>
        /// Handles the click of <see cref="uploadButton"/>. 
        /// Spawns a file dialog to choose a file to upload.
        /// </summary>
        private async void uploadButton_Click(object sender, RoutedEventArgs e)
        {
            using (var dialog = new CommonOpenFileDialog())
            {
                if (dialog.ShowDialog() == CommonFileDialogResult.Ok)
                {
                    await UploadFromFileAsync(dialog.FileName);
                }
            }
        }

        /// <summary>
        /// Handles pasting within <see cref="messageTextBox"/>.
        /// </summary>
        private async void messageTextBox_OnPaste(object sender, RoutedEventArgs e)
        {
            if (e is ExecutedRoutedEventArgs executed && executed.Command == ApplicationCommands.Paste)
            {
                var data = Clipboard.GetDataObject();
                if (data.GetDataPresent(DataFormats.Bitmap) && data.GetData(DataFormats.Bitmap) is BitmapSource bitmap)
                {
                    executed.Handled = true;
                    await UploadFromImageSourceAsync(bitmap);
                }
                else if (data.GetDataPresent(DataFormats.FileDrop) && data.GetData(DataFormats.FileDrop) is string[] files)
                {
                    executed.Handled = true;
                    foreach (var file in files)
                    {
                        await UploadFromFileAsync(file);
                    }
                }

            }
        }

        /// <summary>
        /// The master upload method, all other methods that upload files call this.
        /// </summary>
        /// <param name="caption">The caption for the attachment.</param>
        /// <param name="filename">The attachment file name. Doesn't need to be a real file name.</param>
        /// <param name="stream">The stream with the file to upload</param>
        private async Task UploadStreamAsync(string caption, string filename, Stream stream)
        {
            uploadProgress.IsIndeterminate = true;
            uploadProgress.Visibility = Visibility.Visible;

            var progress = new Progress<double?>(d =>
            {
                if (d.HasValue)
                {
                    uploadProgress.IsIndeterminate = false;
                    uploadProgress.Value = d.Value;
                }
                else if (!uploadProgress.IsIndeterminate)
                {
                    uploadProgress.IsIndeterminate = true;
                }
            });

            await Channel.SendFileWithProgressAsync(caption, stream, filename, progress);

            uploadProgress.Visibility = Visibility.Collapsed;
        }

        /// <summary>
        /// Abstracts uploading from a file.
        /// </summary>
        /// <param name="file">The path of the file to upload.</param>
        private async Task UploadFromFileAsync(string file)
        {
            try
            {
                if (File.Exists(file))
                {
                    using (var shell = ShellFile.FromFilePath(file))
                    {
                        var size = new FileInfo(file).Length;
                        if (size < (App.Discord.CurrentUser.HasNitro ? 50 * 1024 * 1024 : 8 * 1024 * 1024))
                        {
                            var dialog = new UploadFileDialog(shell.Thumbnail.LargeBitmapSource, shell.GetDisplayName(DisplayNameType.Default)) { Owner = Window.GetWindow(this) };
                            if (dialog.ShowDialog() == true)
                            {
                                using (var stream = File.OpenRead(file))
                                {
                                    await UploadStreamAsync(dialog.Caption, Path.GetFileName(file), stream);
                                }
                            }
                        }
                        else
                        {
                            App.ShowErrorDialog(
                                "Unable to send file!",
                                "File too large!",
                                $"Sorry! That file's too large. I'll need something under {Files.SizeSuffix(App.Discord.CurrentUser.HasNitro ? 50 * 1024 * 1024 : 8 * 1024 * 1024)} please!");
                        }
                    }
                }
            }
            catch (Exception ex)
            {
                App.Telemetry.TrackException(ex);
                App.ShowErrorDialog(
                    "Unable to send file!",
                    "An error occured sending a file!",
                    $"Sorry! Something went wrong uploading that file! Please wait a few seconds and try again.");
            }
        }

        /// <summary>
        /// Abstracts uploading from a <see cref="BitmapSource"/>
        /// </summary>
        /// <param name="bitmap">The BitmapSource to upload.</param>
        /// <returns></returns>
        private async Task UploadFromImageSourceAsync(BitmapSource bitmap)
        {
            try
            {
                bitmap.Freeze();

                var dialog = new UploadFileDialog(bitmap, "image.png") { Owner = Window.GetWindow(this) };

                if (dialog.ShowDialog() == true)
                {
                    uploadProgress.IsIndeterminate = true;
                    uploadProgress.Visibility = Visibility.Visible;

                    using (var stream = new MemoryStream())
                    {
                        await Task.Run(() => Tools.EncodeToPng(bitmap, stream));
                        stream.Seek(0, SeekOrigin.Begin);

                        await UploadStreamAsync(dialog.Caption, Path.ChangeExtension(Path.GetRandomFileName(), ".png"), stream);
                    }
                }
            }
            catch (Exception ex)
            {
                App.Telemetry.TrackException(ex);
                App.ShowErrorDialog(
                    "Unable to send image!",
                    "An error occured sending an image!",
                    $"Sorry! Something went wrong uploading that image! Please wait a few seconds and try again.");
            }
        }

        #endregion

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

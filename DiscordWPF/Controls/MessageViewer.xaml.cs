using Discord;
using Discord.WebSocket;
using DiscordWPF.Data;
using Markdown.Xaml;
using Microsoft.WindowsAPICodePack.Dialogs;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Net.Http;
using System.Text;
using System.Text.RegularExpressions;
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

namespace DiscordWPF.Controls
{
    /// <summary>
    /// Interaction logic for GuildViewer.xaml
    /// </summary>
    public partial class MessageViewer : ListViewItem
    {
        public ulong MessageId;
        public ulong UserId;

        public IUserMessage Message;
        public IMessage IMessage;
        public IMessageChannel Channel;

        public bool HasAttachments;
        public bool IsImage;
        public Run attachmentInline;

        public bool Done = false;

        List<Run> secondaryRuns = new List<Run>();

        public MessageViewer(IMessage message, IMessageChannel channel)
        {
            if (message != null && channel != null)
            {
                InitializeComponent();
                messageBody.ContextMenu = null;

                IMessage = message;
                Channel = channel;

                MessageId = message.Id;
                UserId = message.Author.Id;

                authorName.ToolTip = new ToolTip();
                (authorName.ToolTip as ToolTip).Content = $"@{message.Author.Username}#{message.Author.Discriminator} ({message.Author.Id})";

                if (message.Author.Game != null)
                    (authorName.ToolTip as ToolTip).Content += $"\r\nPlaying {message.Author.Game}";

                if (message.Content != null && !string.IsNullOrWhiteSpace(message.Content))
                {
                    if (App.Config.General.FormatText)
                        messageBody.Document = (FlowDocument)(Resources["TextToFlowDocumentConverter"] as TextToFlowDocumentConverter).Convert(message.Content.Replace(Environment.NewLine, Environment.NewLine + Environment.NewLine), typeof(FlowDocument), null, null);
                    else
                        messageBody.AppendText(message.Content);
                    messageEditBody.Text = message.Content;
                }
                else
                    messageBody.Visibility = Visibility.Collapsed;

                authorName.Text = IMessage.Author.Username;

                if (message.EditedTimestamp != null)
                {
                    Run run = new Run()
                    {
                        Text = " (edited)",
                        FontSize = 8,
                        Foreground = App.SecondaryForegroundBrush
                    };
                    secondaryRuns.Add(run);
                    if (messageBody.Document.Blocks.FirstBlock is Paragraph)
                        (messageBody.Document.Blocks.FirstBlock as Paragraph).Inlines.Add(run);
                }

                ChangeTheme();
            }
        }

        public bool IsRichTextBoxEmpty(RichTextBox rtb)
        {
            if (rtb.Document.Blocks.Count == 0) return true;
            TextPointer startPointer = rtb.Document.ContentStart.GetNextInsertionPosition(LogicalDirection.Forward);
            TextPointer endPointer = rtb.Document.ContentEnd.GetNextInsertionPosition(LogicalDirection.Backward);
            return startPointer.CompareTo(endPointer) == 0;
        }

        private async void Item_Click(object sender, RoutedEventArgs e)
        {

        }

        private async void copyMesage_Click(object sender, RoutedEventArgs e)
        {
            if (Message != null && Message.Channel is ITextChannel)
                Clipboard.SetText(await Tools.ProcessMessageText(Message, (Message.Channel as ITextChannel).Guild));
            else
                Clipboard.SetText(await Tools.ProcessMessageText(IMessage, null));
        }

        private void editMessage_Click(object sender, RoutedEventArgs e)
        {
            EditMessage();
        }

        public void EditMessage()
        {
            if (Message != null)
            {
                messageBody.Visibility = Visibility.Collapsed;
                messageEditBody.Visibility = Visibility.Visible;
                messageEditBody.Focus();
                messageEditBody.CaretIndex = messageEditBody.Text.Length;
            }
            else
            {
                App.DiscordWindow.StatusShowError("Unable to edit this message!", false);
            }
        }

        private void deleteMessage_Click(object sender, RoutedEventArgs e)
        {
            App.DiscordWindow.DeleteMessage(IMessage);
        }

        private void copyMessageId_Click(object sender, RoutedEventArgs e)
        {
            Clipboard.SetText(MessageId.ToString());
        }

        private async void copyMessageId_Loaded(object sender, RoutedEventArgs e)
        {
            messageBody.Foreground = (Parent as Control).Foreground;
            string url = IMessage.Author.GetAvatarUrl();
            if (url != null)
                await Dispatcher.InvokeAsync(() => authorImage.ImageSource = new BitmapImage(new Uri(url)));
            else
                await Dispatcher.InvokeAsync(() => authorImage.ImageSource = App.Current.Resources["StockPFP"] as BitmapImage);
        }

        private void ListViewItem_MouseUp(object sender, MouseButtonEventArgs e)
        {

        }

        private async void authorName_Loaded(object sender, RoutedEventArgs eventshit)
        {
            DateTime creation = IMessage.CreatedAt.LocalDateTime;
            authorName.ContextMenu = await Tools.GetUserContextMenu(IMessage.Author as IUser, authorName);
            if (Channel is ITextChannel)
            {
                try
                {
                    if (App.Config.General.UserColourMentions)
                    {
                        System.Windows.Media.Color? fg = await Task.Factory.StartNew(() => Tools.GetUserColour((IMessage.Author as IGuildUser), (Channel as ITextChannel).Guild));
                        if (fg.HasValue)
                        {
                            await Dispatcher.InvokeAsync(() => authorName.Foreground = new SolidColorBrush(fg.Value));
                        }
                    }

                    RichTextBox rtb = await Dispatcher.InvokeAsync(() => messageBody);

                    await Tools.FormatMessage((IMessage.Author as IGuildUser), IMessage, Channel, rtb, Dispatcher);

                    if (IMessage.Author is IUser)
                        await Dispatcher.InvokeAsync(() => authorName.Text = Tools.Name(IMessage.Author as IUser));
                }
                catch { }
            }

            await Dispatcher.InvokeAsync(() =>
            {
                Run run = new Run()
                {
                    Text = $" at {creation.ToLongTimeString()} - {creation.ToLongDateString()}",
                    FontSize = 10,
                    Foreground = App.SecondaryForegroundBrush
                };
                secondaryRuns.Add(run);
                authorName.Inlines.Add(run);
            });

            if (IMessage.Author.Id == App.DiscordWindow.Client.CurrentUser.Id || (DiscordWindow.GuildPermissions?.ManageMessages == true))
            {
                if (IMessage.Author.Id == App.DiscordWindow.Client.CurrentUser.Id)
                    await Dispatcher.InvokeAsync(() =>
                    {
                        editMessage.IsEnabled = true;
                        deleteMessage.IsEnabled = true;
                    });
                else
                    await Dispatcher.InvokeAsync(() =>
                    {
                        editMessage.IsEnabled = false;
                        deleteMessage.IsEnabled = true;
                    });
            }
            else
            {
                await Dispatcher.InvokeAsync(() =>
                {
                    editMessage.IsEnabled = false;
                    deleteMessage.IsEnabled = false;
                });
            }

            if (IMessage.Embeds.Any())
            {
                foreach (IEmbed embed in IMessage.Embeds)
                {
                    EmbedViewer attachment = new EmbedViewer(IMessage, embed as Embed);

                    await Dispatcher.InvokeAsync(() =>
                    {
                        if (embed.Type == EmbedType.Rich)
                            messageBody.Visibility = Visibility.Collapsed;

                        mainGrid.RowDefinitions.Add(new RowDefinition());
                        Grid.SetRow(attachment, mainGrid.RowDefinitions.Count - 1);
                        attachment.Visibility = Visibility.Visible;
                        attachment.HorizontalAlignment = HorizontalAlignment.Left;

                        mainGrid.Children.Add(attachment);
                    });
                }
            }

            if (IMessage.Attachments.Any())
            {
                foreach (IAttachment attach in IMessage.Attachments)
                {
                    await Dispatcher.InvokeAsync(() =>
                    {
                        EmbedViewer attachment = new EmbedViewer(attach as Attachment);
                        mainGrid.RowDefinitions.Add(new RowDefinition());
                        Grid.SetRow(attachment, mainGrid.RowDefinitions.Count - 1);
                        attachment.Visibility = Visibility.Visible;
                        attachment.HorizontalAlignment = HorizontalAlignment.Left;

                        mainGrid.Children.Add(attachment);
                    });
                }
            }

            if (IMessage is IUserMessage)
                Message = IMessage as IUserMessage;
        }

        private async void pinMessage_Click(object sender, RoutedEventArgs e)
        {
            if (Message?.IsPinned == false)
            {
                await Message.PinAsync();
            }
        }

        public void ChangeTheme()
        {
            foreach (Run run in secondaryRuns)
            {
                run.Foreground = App.SecondaryForegroundBrush;
            }

            messageBody.FontFamily = App.Font;
            messageEditBody.FontFamily = App.Font;
        }

        private void messageEditBody_TextChanged(object sender, TextChangedEventArgs e)
        {

        }

        private async void messageEditBody_KeyDown(object sender, KeyEventArgs e)
        {
            if (e.Key == Key.Enter && !Keyboard.IsKeyDown(Key.LeftShift))
            {
                string toSend = await DiscordWindow.ProcessMessageText(messageEditBody.Text, Message.Channel);
                e.Handled = true;
                if (toSend.Length < 2000)
                {
                    await Message.ModifyAsync(new Action<MessageProperties>((p) => p.Content = toSend));
                    App.DiscordWindow.StatusShowSuccess("Message edited!");
                    messageBody.Visibility = Visibility.Visible;
                    messageEditBody.Visibility = Visibility.Collapsed;
                    App.DiscordWindow.messageTextBox.Focus();
                }
                else
                {
                    App.DiscordWindow.StatusShowError("Your message is too long! Make sure its less than 2000 characters!", false);
                }
            }
            else if (e.Key == Key.Escape)
            {
                e.Handled = true;
                messageBody.Visibility = Visibility.Visible;
                messageEditBody.Visibility = Visibility.Collapsed;
                messageEditBody.Text = Message.Content;
                App.DiscordWindow.messageTextBox.Focus();
            }
        }
    }
}


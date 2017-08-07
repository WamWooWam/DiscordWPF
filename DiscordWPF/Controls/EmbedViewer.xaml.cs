using Discord;
using Discord.WebSocket;
using DiscordWPF.Controls.SubControls;
using DiscordWPF.Data;
using Markdown.Xaml;
using Microsoft.WindowsAPICodePack.Dialogs;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Net;
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
using XamlAnimatedGif;

namespace DiscordWPF.Controls
{
    /// <summary>
    /// Interaction logic for EmbedViewer.xaml
    /// </summary>
    public partial class EmbedViewer : UserControl
    {
        public EmbedViewer(Attachment attach)
        {
            InitializeComponent();

            footerGrid.Visibility = Visibility.Collapsed;

            if (attach.Width.HasValue && attach.Height.HasValue)
            {
                description.Visibility = Visibility.Collapsed;
                titleText.Text = $"{System.IO.Path.GetFileName(attach.Url)} ({attach.Width}x{attach.Height})";
                titleText.Margin = new Thickness(0, 0, 0, 5);

                System.Windows.Controls.Image image = AddImageAttachment(attach.Url, attach.Width, attach.Height);
            }
            else if (System.IO.Path.GetExtension(attach.Url) == ".mp4")
            {
                titleText.Text = System.IO.Path.GetFileName(attach.Url);

                description.Visibility = Visibility.Collapsed;

                SubControls.MediaPlayer media = new SubControls.MediaPlayer(attach.Url, attach.ProxyUrl);
                media.HorizontalAlignment = HorizontalAlignment.Left;
                media.VerticalAlignment = VerticalAlignment.Top;
                media.ContextMenu = GetAttachmentContextMenu(attach.Url);
                media.Visibility = Visibility.Collapsed;
                mainContent.Children.Add(media);

                MouseDown += (o, e) =>
                {
                    if (e.LeftButton == MouseButtonState.Pressed && !media.IsMouseOver)
                    {
                        if (media.Visibility == Visibility.Collapsed)
                            media.Visibility = Visibility.Visible;
                        else
                        {
                            media.Visibility = Visibility.Collapsed;
                        }
                    }
                };
            }
            else
            {
                titleText.Text = System.IO.Path.GetFileName(attach.Url);
                description.AppendText(Tools.SizeSuffix(attach.Size, 2));
                MouseUp += async (o, e) =>
                {
                    await Tools.SaveAttachment(attach.Url);
                };
            }
        }

        private System.Windows.Controls.Image AddImageAttachment(string url, int? width, int? height)
        {
            System.Windows.Controls.Image image = new System.Windows.Controls.Image();
            image.HorizontalAlignment = HorizontalAlignment.Left;
            image.VerticalAlignment = VerticalAlignment.Top;
            if (width.HasValue)
                image.MaxWidth = width.Value;
            if (height.HasValue)
                image.MaxHeight = height.Value;
            image.Stretch = Stretch.Uniform;

            if (System.IO.Path.GetExtension(url) == ".gif")
            {
                AnimationBehavior.SetSourceUri(image, new Uri(url));

                image.MouseEnter += (o, e) =>
                {
                    AnimationBehavior.GetAnimator(image).Play();
                };

                image.MouseLeave += (o, e) =>
                {
                    AnimationBehavior.GetAnimator(image).Pause();
                };
            }
            else
            {
                image.Source = new BitmapImage(new Uri(url));
            }
            image.ContextMenu = GetAttachmentContextMenu(url);

            image.Visibility = Visibility.Collapsed;
            mainContent.Children.Add(image);

            MouseDown += (o, e) =>
            {
                if (e.LeftButton == MouseButtonState.Pressed)
                {

                    if (image.Visibility == Visibility.Collapsed)
                        image.Visibility = Visibility.Visible;
                    else
                    {
                        image.Visibility = Visibility.Collapsed;
                        try
                        {
                            Animator anim = AnimationBehavior.GetAnimator(image);
                            if (anim != null)
                                anim.Rewind();
                        }
                        catch { }
                    }
                }
            };

            return image;
        }

        public EmbedViewer(IMessage msg, Embed embed)
        {
            InitializeComponent();

            titleText.Text = embed.Title;
            Margin = new Thickness(10, 10, 0, 0);
            if (embed.Author.HasValue)
            {
                try
                {
                    if (string.IsNullOrEmpty(titleText.Text))
                        titleText.Text = embed.Author.Value.Name;
                    if (embed.Author.Value.IconUrl != null)
                        titleImage.Source = new BitmapImage(new Uri(embed.Author.Value.IconUrl));
                }
                catch { }
            }

            if(embed.Color.HasValue)
            {
                BorderThickness = new Thickness(2.5, 0, 0, 0);
                BorderBrush = new SolidColorBrush(System.Windows.Media.Color.FromRgb(embed.Color.Value.R, embed.Color.Value.G, embed.Color.Value.B));
            }

            if (!string.IsNullOrEmpty(embed.Description))
            {
                description.Document = (FlowDocument)(Resources["TextToFlowDocumentConverter"] as TextToFlowDocumentConverter).Convert(embed.Description.Replace("\n", "\n\n"), typeof(FlowDocument), null, null);
                if (msg != null && msg.Author is IGuildUser)
                    Tools.FormatMessage(msg.Author as IGuildUser, msg, msg.Channel as ITextChannel, description, Dispatcher);
            }
            else
            {
                description.Visibility = Visibility.Collapsed;
            }

            if (embed.Thumbnail.HasValue)
            {
                thumbnailImage.Source = new BitmapImage(new Uri(embed.Thumbnail.Value.Url));
            }
            else
            {
                thumbnailImage.Visibility = Visibility.Collapsed;
            }

            if (embed.Image.HasValue)
            {
                System.Windows.Controls.Image image = AddImageAttachment(embed.Image.Value.Url, embed.Image.Value.Width, embed.Image.Value.Height);
            }

            foreach (EmbedField field in embed.Fields)
            {
                FieldViewer viewer = new FieldViewer(msg, field);
                mainContent.Children.Add(viewer);
            }

            if (embed.Type == EmbedType.Link)
            {

            }
            else if (embed.Type == EmbedType.Video)
            {

            }
            else if (embed.Type == EmbedType.Image)
            {
                description.Visibility = Visibility.Collapsed;
                thumbnailImage.Visibility = Visibility.Collapsed;

                if (embed.Image.HasValue)
                {
                    titleText.Text = System.IO.Path.GetFileName(embed.Image.Value.Url);
                    System.Windows.Controls.Image image = AddImageAttachment(embed.Image.Value.Url, embed.Image.Value.Width, embed.Image.Value.Height);
                }
                else if (embed.Thumbnail.HasValue)
                {
                    titleText.Text = System.IO.Path.GetFileName(string.IsNullOrEmpty(embed.Thumbnail.Value.Url) ? embed.Thumbnail.Value.ProxyUrl : embed.Thumbnail.Value.Url);
                    System.Windows.Controls.Image image = AddImageAttachment(string.IsNullOrEmpty(embed.Thumbnail.Value.Url) ? embed.Thumbnail.Value.ProxyUrl : embed.Thumbnail.Value.Url, embed.Thumbnail.Value.Width, embed.Thumbnail.Value.Height);
                }
            }

            if (embed.Footer.HasValue)
            {
                footerGrid.Visibility = Visibility.Visible;
                footerText.Text = embed.Footer.Value.Text;
                if (embed.Footer.Value.IconUrl != null)
                    footerImage.Source = new BitmapImage(new Uri(embed.Footer.Value.IconUrl));
                else
                    footerImage.Visibility = Visibility.Collapsed;
            }
            else
                footerGrid.Visibility = Visibility.Collapsed;
        }

        ContextMenu GetAttachmentContextMenu(string url)
        {
            ContextMenu menu = new ContextMenu();
            MenuItem item = new MenuItem();

            item.Header = $"Save {System.IO.Path.GetFileName(url)} as...";

            item.Click += async (object o, RoutedEventArgs ev) =>
            {
                await Tools.SaveAttachment(url);
            };

            menu.Items.Add(item);

            return menu;
        }       
    }
}

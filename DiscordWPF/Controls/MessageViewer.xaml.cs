using DSharpPlus.Entities;
using Markdig;
using MWpf = Markdig.Wpf;
using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Linq;
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
using WamWooWam.Core;
using WamWooWam.Wpf;
using Markdig.Extensions.EmphasisExtras;
using Markdig.Parsers;

namespace DiscordWPF.Controls
{
    /// <summary>
    /// Interaction logic for MessageViewer.xaml
    /// </summary>
    public partial class MessageViewer : UserControl
    {
        private bool _internalIsLoaded = false;

        private MarkdownPipeline _pipeline = CreateMarkdownPipeline();

        public static readonly DependencyProperty MessageProperty =
            DependencyProperty.Register(nameof(Message), typeof(DiscordMessage), typeof(MessageViewer), new PropertyMetadata(null, OnPropertyChanged));

        public DiscordMessage Message { get => (DiscordMessage)GetValue(MessageProperty); set => SetValue(MessageProperty, value); }

        public bool MiniMode { get; internal set; }

        public MessageViewer()
        {
            InitializeComponent();
        }

        private static void OnPropertyChanged(DependencyObject d, DependencyPropertyChangedEventArgs e)
        {
            if (d is MessageViewer viewer && e.NewValue is DiscordMessage message)
            {
                if (e.OldValue is DiscordMessage old)
                {
                    old.PropertyChanged -= viewer.Message_PropertyChanged;
                    old.Author.PropertyChanged -= viewer.Author_PropertyChanged;
                }

                message.PropertyChanged += viewer.Message_PropertyChanged;
                message.Author.PropertyChanged += viewer.Author_PropertyChanged;

                if (viewer._pipeline.Extensions.FirstOrDefault(ex => ex is DiscordMarkdownExtension) is DiscordMarkdownExtension ext)
                {
                    ext.Channel = message.Channel;
                    ext.Guild = message.Channel.Guild;
                }

                viewer.DataContext = message;
                viewer.flowDoc.Document = MWpf.Markdown.ToFlowDocument(message.Content, viewer._pipeline);
                viewer.userName.Text = (message.Author is DiscordMember m) ? m.DisplayName : message.Author.Username;

                viewer._internalIsLoaded = false;
            }
        }

        private static MarkdownPipeline CreateMarkdownPipeline()
        {
            var builder = new MarkdownPipelineBuilder()
                .UseEmphasisExtras(EmphasisExtraOptions.Strikethrough)
                .UseSoftlineBreakAsHardlineBreak()
                .Use<DiscordMarkdownExtension>()
                .UseAutoLinks()
                .DisableHtml();

            builder.BlockParsers.RemoveAll(p => p is QuoteBlockParser);

            return builder.Build();
        }

        private void Author_PropertyChanged(object sender, PropertyChangedEventArgs e)
        {
            if (sender is DiscordMember m && e.PropertyName == nameof(m.DisplayName))
            {
                userName.Text = m.DisplayName;
            }
            else if (sender is DiscordUser u && e.PropertyName == nameof(u.Username))
            {
                userName.Text = u.Username;
            }
        }

        private void Message_PropertyChanged(object sender, PropertyChangedEventArgs e)
        {
            if (e.PropertyName == nameof(Message.Content))
            {
                flowDoc.Document = MWpf.Markdown.ToFlowDocument(Message.Content, _pipeline);
            }
        }

        internal void OnLoaded()
        {
            if (!_internalIsLoaded)
            {
                _internalIsLoaded = true;

                SetDefaultThickness();

                userProfilePicture.Visibility = Visibility.Visible;
                userName.Visibility = Visibility.Visible;

                var panel = this.FindVisualParent<StackPanel>();
                var index = panel.Children.IndexOf(this);

                if (index > 0)
                {
                    var other = panel.Children.OfType<MessageViewer>().ElementAtOrDefault(index - 1);
                    if (other != null)
                    {
                        if (other.Message.Author.Id == Message.Author.Id && other.Message.Timestamp.Hour == Message.Timestamp.Hour)
                        {
                            userProfilePicture.Visibility = Visibility.Collapsed;
                            userName.Visibility = Visibility.Collapsed;
                            Margin = new Thickness(46, 3, 10, 0);
                        }
                    }
                }

                var viewer = this.FindVisualParent<ScrollViewer>();
                if (viewer != null)
                    viewer.ScrollToVerticalOffset((viewer.VerticalOffset + ActualHeight).Clamp(0, viewer.ScrollableHeight));
            }
        }

        private void SetDefaultThickness()
        {
            if (MiniMode)
            {
                Margin = new Thickness(10, 10, 10, 0);
            }
            else
            {
                Margin = new Thickness(10, 20, 10, 0);
            }
        }

        private void UserControl_Loaded(object sender, RoutedEventArgs e)
        {
            OnLoaded();
        }

        private void UserControl_Unloaded(object sender, RoutedEventArgs e)
        {
            MessageViewerFactory.ViewerQueue.Enqueue(this);
        }

        private void UserControl_SizeChanged(object sender, SizeChangedEventArgs e)
        {
            var viewer = this.FindVisualParent<ScrollViewer>();
            if (viewer != null && (e.NewSize.Height - e.PreviousSize.Height) > 0)
                viewer.ScrollToVerticalOffset(((viewer.VerticalOffset - e.PreviousSize.Height) + e.NewSize.Height).Clamp(0, viewer.ScrollableHeight));
        }

        private void CommandBinding_Executed(object sender, ExecutedRoutedEventArgs e)
        {
            MessageBox.Show(e.Parameter.ToString());
        }
    }
}

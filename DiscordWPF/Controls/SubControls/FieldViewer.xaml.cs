using Discord;
using Discord.WebSocket;
using DiscordWPF.Data;
using Markdown.Xaml;
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
using System.Windows.Media;
using System.Windows.Media.Imaging;
using System.Windows.Navigation;
using System.Windows.Shapes;

namespace DiscordWPF.Controls
{
    /// <summary>
    /// Interaction logic for FieldViewer.xaml
    /// </summary>
    public partial class FieldViewer : UserControl
    {
        IMessage message;
        EmbedField embField;

        public FieldViewer(IMessage msg, EmbedField field)
        {
            InitializeComponent();

            message = msg;
            embField = field;

            if (field.Name != null)
                Title.Text = field.Name;
            else
                Title.Visibility = Visibility.Hidden;

            if (field.Value != null)
                Content.Document = (FlowDocument)(App.Current.Resources["TextToFlowDocumentConverter"] as TextToFlowDocumentConverter).Convert(field.Value, typeof(FlowDocument), null, null);
            else
                Content.Visibility = Visibility.Hidden;          
            
        }

        private async void UserControl_Loaded(object sender, RoutedEventArgs e)
        {
            if (message != null && message.Author is IGuildUser)
                await Tools.FormatMessage(message.Author as IGuildUser, message, message.Channel as ITextChannel, Content, Dispatcher, embField.Value);
        }
    }
}

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
        public FieldViewer(IMessage msg, EmbedField field)
        {
            InitializeComponent();

            if (field.Name != null)
                Title.Text = field.Name;
            else
                Title.Visibility = Visibility.Hidden;

            if (field.Value != null)
                Content.Document = (FlowDocument)(Resources["TextToFlowDocumentConverter"] as TextToFlowDocumentConverter).Convert(field.Value, typeof(FlowDocument), null, null);
            else
                Content.Visibility = Visibility.Hidden;
           
            if (msg != null && msg.Author is IGuildUser)
                Tools.FormatMessage(msg.Author as IGuildUser, msg, msg.Channel as ITextChannel, Content, Dispatcher, field.Value);
        }
    }
}

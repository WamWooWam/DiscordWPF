using Discord;
using Discord.WebSocket;
using DiscordWPF.Data;
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
    /// Interaction logic for NotificationGroup.xaml
    /// </summary>
    public partial class NotificationGroup : UserControl
    {
        public NotificationGroup(NotificationGroupModel model)
        {
            InitializeComponent();

            if (model.Type == NotifIcationType.Guild)
            {
                sourceTitle.Text = model.Guild.Name;
                if (model.Guild.IconUrl != null)
                    sourceImage.ImageSource = new BitmapImage(new Uri(model.Guild.IconUrl));
            }

            foreach (IMessage msg in model.Messages)
            {
                MessageViewer viewer = new MessageViewer(msg, model.Channel);
                messageViewer.Items.Add(viewer);
            }
        }
    }
}

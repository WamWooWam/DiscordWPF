using Discord;
using DiscordWPF.Data;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Controls.Primitives;
using System.Windows.Data;
using System.Windows.Documents;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using System.Windows.Navigation;
using System.Windows.Shapes;

namespace DiscordWPF.Controls.SubControls
{
    /// <summary>
    /// Interaction logic for ReactionViewer.xaml
    /// </summary>
    public partial class ReactionViewer : ToggleButton
    {
        public ReactionViewer(KeyValuePair<IEmote, ReactionMetadata> reaction)
        {
            InitializeComponent();

            if(reaction.Key is Emoji)
            {
                emoteText.Text = (reaction.Key as Emoji).Name;
            }
            else if (reaction.Key is Emote)
            {
                emoteImage.Source = Images.GetImage((reaction.Key as Emote).Url);
            }

            emoteCount.Text = reaction.Value.ReactionCount.ToString();

            this.IsChecked = reaction.Value.IsMe;
        }
    }
}

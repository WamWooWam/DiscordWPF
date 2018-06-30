using DiscordWPF.Converters;
using DSharpPlus;
using DSharpPlus.Entities;
using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
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
using System.Windows.Threading;

namespace DiscordWPF.Pages
{
    /// <summary>
    /// Interaction logic for FriendsPage.xaml
    /// </summary>
    public partial class FriendsPage : Page
    {
        private ObservableCollection<DiscordRelationship> _relationships;

        public FriendsPage()
        {
            InitializeComponent();
        }

        private void Page_Loaded(object sender, RoutedEventArgs e)
        {
            if (App.Discord != null)
            {
                _relationships = new ObservableCollection<DiscordRelationship>(App.Discord.Relationships);

                var csv = Resources["csv"] as CollectionViewSource;
                csv.Source = _relationships;
                csv.SortDescriptions.Add(new SortDescription("User.Username", ListSortDirection.Ascending));
                csv.GroupDescriptions.Add(new PropertyGroupDescription("RelationshipType", new RelationshipNameConverter()));

                App.Discord.RelationshipAdded += Discord_RelationshipAdded;
                App.Discord.RelationshipRemoved += Discord_RelationshipRemoved;
            }
        }

        private async Task Discord_RelationshipAdded(DSharpPlus.EventArgs.RelationshipEventArgs e)
        {
            var rel = _relationships.FirstOrDefault(r => r.User.Id == e.Relationship.User.Id);
            if (rel == null)
            {
                await Dispatcher.InvokeAsync(() => _relationships.Add(rel));
            }
        }

        private async Task Discord_RelationshipRemoved(DSharpPlus.EventArgs.RelationshipEventArgs e)
        {
            var rel = _relationships.FirstOrDefault(r => r.User.Id == e.Relationship.User.Id);
            if (rel != null)
            {
                await Dispatcher.InvokeAsync(() => _relationships.Remove(rel));
            }
        }
    }
}

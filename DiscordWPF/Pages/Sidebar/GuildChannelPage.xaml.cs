using DiscordWPF.Converters;
using DiscordWPF.Windows;
using DSharpPlus;
using DSharpPlus.Entities;
using DSharpPlus.EventArgs;
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
using WamWooWam.Wpf;

namespace DiscordWPF.Pages.Sidebar
{
    /// <summary>
    /// Interaction logic for GuildChannelPage.xaml
    /// </summary>
    public partial class GuildChannelPage : Page
    {
        DiscordGuild guild;
        DiscordMember currentMember;
        ObservableCollection<DiscordChannel> channels;

        public GuildChannelPage()
        {
            InitializeComponent();

            var converter = new CategoryNameConverter();
            var source = Resources["channelsSource"] as CollectionViewSource;
            source.Filter += Source_Filter;

            source.GroupDescriptions.Add(new PropertyGroupDescription() { Converter = converter });

            source.SortDescriptions.Add(new SortDescription("Type", ListSortDirection.Ascending));
            source.SortDescriptions.Add(new SortDescription("Position", ListSortDirection.Ascending));
            source.SortDescriptions.Add(new SortDescription("Name", ListSortDirection.Ascending));
        }

        internal void ChangeSelection(DiscordChannel channel)
        {
            channelsList.SelectionChanged -= channelsList_SelectionChanged;

            using ((Resources["channelsSource"] as CollectionViewSource).DeferRefresh())
            {
                channelsList.SelectedItem = channel;
            }

            channelsList.SelectionChanged += channelsList_SelectionChanged;
        }

        private void Page_Loaded(object sender, RoutedEventArgs e)
        {
            App.Discord.ChannelCreated += Discord_ChannelCreated;
            App.Discord.ChannelDeleted += Discord_ChannelDeleted;
            App.Discord.ChannelUpdated += Discord_ChannelUpdated;
        }

        private void Page_Unloaded(object sender, RoutedEventArgs e)
        {
            App.Discord.ChannelCreated -= Discord_ChannelCreated;
            App.Discord.ChannelDeleted -= Discord_ChannelDeleted;
            App.Discord.ChannelUpdated -= Discord_ChannelUpdated;
        }

        private async Task Discord_ChannelCreated(ChannelCreateEventArgs e)
        {
            if (guild != null && e.Guild != null && guild.Id == e.Guild.Id && !channels.Any(c => c.Id == e.Channel.Id))
            {
                await Dispatcher.InvokeAsync(() => channels?.Add(e.Channel));
            }
        }

        private async Task Discord_ChannelDeleted(ChannelDeleteEventArgs e)
        {
            if (guild != null && e.Guild != null && guild.Id == e.Guild.Id && channels.Any(c => c.Id == e.Channel.Id))
            {
                await Dispatcher.InvokeAsync(() => channels?.Remove(e.Channel));
            }
        }

        private async Task Discord_ChannelUpdated(ChannelUpdateEventArgs e)
        {
            if (guild != null && e.Guild != null && guild.Id == e.Guild.Id)
            {
                await Dispatcher.InvokeAsync(() =>
                {
                    var source = Resources["channelsSource"] as CollectionViewSource;
                    source.View.Refresh();
                });
            }
        }

        private void Page_DataContextChanged(object sender, DependencyPropertyChangedEventArgs e)
        {
            if (e.NewValue is DiscordGuild g)
            {
                guild = g;
                channels = new ObservableCollection<DiscordChannel>(g.Channels);
                currentMember = g.CurrentMember;

                var source = Resources["channelsSource"] as CollectionViewSource;
                using (source.DeferRefresh())
                {
                    var description = source.GroupDescriptions.First();
                    description.GroupNames.Clear();

                    var cs = g.Channels.Where(c => c.Type == ChannelType.Category);
                    if (cs.Any())
                    {
                        foreach (var channel in cs.OrderBy(c => c.Position))
                        {
                            if (channel.Children.Any(c => c.PermissionsFor(currentMember).HasPermission(Permissions.AccessChannels)))
                            {
                                description.GroupNames.Add(channel.Name);
                            }
                        }
                    }

                    source.Source = channels;
                }
            }
        }

        private void Source_Filter(object sender, FilterEventArgs e)
        {
            if (e.Item is DiscordChannel c)
            {
                if (c.Type == ChannelType.Category)
                {
                    e.Accepted = false;
                    return;
                }
                else
                {
                    var permissions = c.PermissionsFor(currentMember);
                    if (!permissions.HasPermission(Permissions.AccessChannels))
                    {
                        e.Accepted = false;
                    }
                    else
                    {
                        e.Accepted = true;
                    }
                }

                return;
            }

            e.Accepted = false;
        }

        private void channelsList_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            var channel = e.AddedItems.OfType<DiscordChannel>().FirstOrDefault();
            if (channel != null && channel.Type == ChannelType.Text)
            {
                var page = this.FindVisualParent<DiscordPage>();
                if (page != null)
                {
                    Tools.NavigateToChannel(channel, page.Frame);
                }
                else
                {
                    var wind = this.FindVisualParent<GuildWindow>();
                    Tools.NavigateToChannel(channel, wind?.mainFrame);
                }
            }
        }      
    }
}

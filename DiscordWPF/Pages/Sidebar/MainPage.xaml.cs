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
using System.Windows.Threading;
using WamWooWam.Wpf;

namespace DiscordWPF.Pages.Sidebar
{
    /// <summary>
    /// Interaction logic for MainPage.xaml
    /// </summary>
    public partial class MainPage : Page
    {
        private ObservableCollection<DiscordGuild> _guilds;
        private ObservableCollection<DiscordDmChannel> _dms;
        private ObservableCollection<DiscordDmChannel> _groups;        
        private GuildChannelPage _guildChannelPage;

        public MainPage()
        {
            InitializeComponent();
            _guildChannelPage = new GuildChannelPage();
        }

        private async void Page_Loaded(object sender, RoutedEventArgs e)
        {
            var dmsSource = Resources["DMsCollection"] as CollectionViewSource;
            dmsSource.SortDescriptions.Add(new SortDescription("ReadState.LastMessageId", ListSortDirection.Descending));

            _guilds = new ObservableCollection<DiscordGuild>(App.Discord.Guilds.Values);
            _dms = new ObservableCollection<DiscordDmChannel>(App.Discord.PrivateChannels.Where(c => c.Type == ChannelType.Private));
            _groups = new ObservableCollection<DiscordDmChannel>(App.Discord.PrivateChannels.Where(c => c.Type == ChannelType.Group));

            await Dispatcher.InvokeAsync(() =>
            {
                guildsList.ItemsSource = _guilds;
                guildsList.SelectionChanged += OnSelectionChanged;
            }, DispatcherPriority.DataBind);

            await Dispatcher.InvokeAsync(() =>
            {
                dmsSource.Source = _dms;
                dmsList.SelectionChanged += OnSelectionChanged;
            }, DispatcherPriority.DataBind);

            await Dispatcher.InvokeAsync(() =>
            {
                groupsList.ItemsSource = _groups;
                groupsList.SelectionChanged += OnSelectionChanged;
            }, DispatcherPriority.DataBind);

            App.Discord.GuildCreated += Discord_GuildCreated;
            App.Discord.GuildDeleted += Discord_GuildDeleted;
            App.Discord.GuildAvailable += Discord_GuildAvailable;
            App.Discord.GuildUnavailable += Discord_GuildUnavailable;

            App.Discord.DmChannelCreated += Discord_DmChannelCreated;
            App.Discord.DmChannelDeleted += Discord_DmChannelDeleted;
        }

        private void OnSelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            var guild = e.AddedItems.OfType<DiscordGuild>().FirstOrDefault();
            if (guild != null)
            {
                _guildChannelPage.DataContext = guild;
                this.FindVisualParent<Frame>().Navigate(_guildChannelPage);
            }
            else
            {
                var dm = e.AddedItems.OfType<DiscordDmChannel>().FirstOrDefault();
                var page = this.FindVisualParent<DiscordPage>();
                if (dm != null && page != null)
                {
                    Tools.NavigateToChannel(dm, page.Frame);
                }
            }

            (sender as ListView).SelectedItem = null;
        }

        private async Task Discord_GuildCreated(GuildCreateEventArgs e)
        {
            if (!_guilds.Any(g => g.Id == e.Guild.Id))
            {
                await Dispatcher.InvokeAsync(() => _guilds.Add(e.Guild));
            }
        }

        private async Task Discord_GuildAvailable(GuildCreateEventArgs e)
        {
            if (!_guilds.Any(g => g.Id == e.Guild.Id))
            {
                await Dispatcher.InvokeAsync(() => _guilds.Add(e.Guild));
            }
        }

        private async Task Discord_GuildDeleted(GuildDeleteEventArgs e)
        {
            if (_guilds.Any(g => g.Id == e.Guild.Id))
            {
                await Dispatcher.InvokeAsync(() => _guilds.Remove(e.Guild));
            }
        }

        private async Task Discord_GuildUnavailable(GuildDeleteEventArgs e)
        {
            if (_guilds.Any(g => g.Id == e.Guild.Id))
            {
                await Dispatcher.InvokeAsync(() => _guilds.Remove(e.Guild));
            }
        }

        private async Task Discord_DmChannelCreated(DmChannelCreateEventArgs e)
        {
            if (e.Channel.Type == ChannelType.Group)
            {
                await AddToCollectionAsync(e.Channel, _groups);
            }
            else
            {
                await AddToCollectionAsync(e.Channel, _dms);
            }
        }

        private async Task Discord_DmChannelDeleted(DmChannelDeleteEventArgs e)
        {
            if (e.Channel.Type == ChannelType.Group)
            {
                await RemoveFromCollectionAsync(e.Channel, _groups);
            }
            else
            {
                await RemoveFromCollectionAsync(e.Channel, _dms);
            }
        }

        private async Task AddToCollectionAsync(DiscordDmChannel channel, ObservableCollection<DiscordDmChannel> collection)
        {
            if (!collection.Any(c => c.Id == channel.Id))
            {
                await Dispatcher.InvokeAsync(() => collection.Add(channel));
            }
        }

        private async Task RemoveFromCollectionAsync(DiscordDmChannel channel, ObservableCollection<DiscordDmChannel> collection)
        {
            if (collection.Any(c => c.Id == channel.Id))
            {
                await Dispatcher.InvokeAsync(() => collection.Remove(channel));
            }
        }

        private void guildsList_PreviewMouseRightButtonDown(object sender, MouseButtonEventArgs e)
        {
            e.Handled = true;
            var grid = guildsList.FirstVisualChild<Grid>(g => g.IsMouseOver && g.Tag is DiscordGuild);
            if (grid != null)
            {
                if (grid.Tag is DiscordGuild dg && (grid.ContextMenu == null || (grid.ContextMenu.Tag as DiscordGuild) != dg))
                {
                    ContextMenu menu = Tools.GetContextMenuForGuild(dg, new ImageBrush(new BitmapImage(new Uri(dg.IconUrl))));
                    grid.ContextMenu = menu;
                }

                grid.ContextMenu.IsOpen = true;
            }
        }
    }
}

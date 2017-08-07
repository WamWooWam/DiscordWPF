using DiscordWPF.Data;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Net;
using System.Net.Http;
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
using System.Windows.Threading;

namespace DiscordWPF.Controls.SubControls
{
    /// <summary>
    /// Interaction logic for MediaPlayer.xaml
    /// </summary>
    public partial class MediaPlayer : UserControl
    {
        bool playing = false;
        string Url = "";
        DispatcherTimer timer = new DispatcherTimer();

        public MediaPlayer(string url, string thumbnailUrl)
        {
            InitializeComponent();

            if (url == null)
                throw new ArgumentException("url cannot be null!", "url");
            if (!Uri.IsWellFormedUriString(url, UriKind.Absolute))
                throw new ArgumentException("url must be a valid URI!", "url");

            Loaded += MediaPlayer_Loaded;

            Url = url;
            thumbnail.Source = new BitmapImage(new Uri(thumbnailUrl));

            media.IsMuted = true;
            media.BufferingStarted += Media_BufferingStarted;
            media.MediaFailed += Media_MediaFailed;
            media.SourceUpdated += Media_SourceUpdated;
            media.MediaEnded += Media_MediaEnded;
            
            timer.Interval = TimeSpan.FromMilliseconds(33);
            timer.Tick += timer_Tick;
            //timer.Start();
        }

        private void Media_MediaEnded(object sender, RoutedEventArgs e)
        {
            media.Position = new TimeSpan(0);
            media.Pause();
            timer.Stop();
            playPause.Icon = FontAwesome.WPF.FontAwesomeIcon.Play;
        }

        private void Media_SourceUpdated(object sender, DataTransferEventArgs e)
        {

        }

        private async void MediaPlayer_Loaded(object sender, RoutedEventArgs e)
        {
            try
            {
                using (HttpClient client = new HttpClient())
                {
                    string path = Path.Combine(Path.GetTempPath(), Path.GetFileNameWithoutExtension(Path.GetTempFileName()) + Path.GetExtension(Url));
                    using (FileStream file = File.OpenWrite(path))
                    {
                        Stream str = await client.GetStreamAsync(Url);
                        await str.CopyToAsync(file);
                    }

                    await Dispatcher.InvokeAsync(() => media.Source = new Uri(path));
                }
            }
            catch (Exception ex)
            {
                mediaPlayer.Visibility = Visibility.Collapsed;
                label1.Content += ex.Message;
                mediaError.Visibility = Visibility.Visible;
            }
        }

        private void Media_BufferingStarted(object sender, RoutedEventArgs e)
        {

        }

        private void Media_MediaFailed(object sender, ExceptionRoutedEventArgs e)
        {
            mediaPlayer.Visibility = Visibility.Collapsed;
            label1.Content += e.ErrorException.Message;
            mediaError.Visibility = Visibility.Visible;
        }

        void timer_Tick(object sender, EventArgs e)
        {
            if (media.Source != null)
            {
                if (media.NaturalDuration.HasTimeSpan)
                {
                    playBar.Maximum = media.NaturalDuration.TimeSpan.Ticks;
                    playBar.Value = media.Position.Ticks;
                    elapsedTime.Text = media.Position.ToString(@"mm\:ss");
                    remainingTime.Text = "-" + (media.NaturalDuration.TimeSpan - media.Position).ToString(@"mm\:ss");
                }
            }
        }

        private void playBar_MouseMove(object sender, MouseEventArgs e)
        {
            if (e.LeftButton == MouseButtonState.Pressed)
            {
                SetPlayPosition(sender as ProgressBar, e);
            }
        }

        private void SetPlayPosition(object sender, MouseEventArgs e)
        {
            if (media.NaturalDuration.HasTimeSpan)
            {
                Point pos = e.GetPosition(sender as ProgressBar);
                double percentage = pos.X / (sender as ProgressBar).ActualWidth;
                media.Position = TimeSpan.FromMilliseconds(media.NaturalDuration.TimeSpan.TotalMilliseconds * percentage);
            }
        }

        private async void Hyperlink_Click(object sender, RoutedEventArgs e)
        {
            await Tools.SaveAttachment(Url);
        }

        private void Button_Click(object sender, RoutedEventArgs e)
        {
            if (thumbnail.Visibility == Visibility.Visible)
            {
                media.Visibility = Visibility.Visible;
                thumbnail.Visibility = Visibility.Collapsed;
            }

            if (playing)
            {
                timer.Stop();
                media.Pause();
                playPause.Icon = FontAwesome.WPF.FontAwesomeIcon.Play;
            }
            else
            {
                timer.Start();
                media.Play();
                playPause.Icon = FontAwesome.WPF.FontAwesomeIcon.Pause;
            }

            playing = !playing;
        }

        private void muteUnmute_MouseDown(object sender, MouseButtonEventArgs e)
        {

        }

        private void muteUnmute_MouseUp(object sender, MouseButtonEventArgs e)
        {
            media.IsMuted = !media.IsMuted;

            if (!media.IsMuted)
                mutedIcon.Visibility = Visibility.Hidden;
            else
                mutedIcon.Visibility = Visibility.Visible;
        }

        private async void media_PreviewMouseRightButtonUp(object sender, MouseButtonEventArgs e)
        {
            await Tools.SaveAttachment(Url);
        }
    }
}

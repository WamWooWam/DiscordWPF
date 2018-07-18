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
using System.Windows.Shapes;
using Windows.Media.Core;
using Windows.Media.Editing;
using Windows.Media.Transcoding;
using Windows.Storage;

namespace DiscordWPF.Windows
{
    /// <summary>
    /// Interaction logic for VideoTrimWindow.xaml
    /// </summary>
    public partial class VideoTrimWindow : Window
    {
        string _filePath;
        MediaComposition _composition;
        MediaStreamSource _mediaStreamSource;

        public VideoTrimWindow(string file)
        {
            InitializeComponent();
            _filePath = file;
            _composition = new MediaComposition();
        }

        private async void Window_Loaded(object sender, RoutedEventArgs e)
        {
            var file = await StorageFile.GetFileFromPathAsync(_filePath);
            var clip = await MediaClip.CreateFromFileAsync(file);

            _composition.Clips.Add(clip);
        }
    }
}

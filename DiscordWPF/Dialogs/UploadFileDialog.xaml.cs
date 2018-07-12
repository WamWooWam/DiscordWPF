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

namespace DiscordWPF.Dialogs
{
    /// <summary>
    /// Interaction logic for UploadFileDialog.xaml
    /// </summary>
    public partial class UploadFileDialog : Window
    {
        public string Caption
        {
            get => commentTextBox.Text;
            set => commentTextBox.Text = value;
        }

        public UploadFileDialog(ImageSource source, string name)
        {
            InitializeComponent();

            var title = $"Uploading \"{name}\"...";

            Title = title;
            titleTextBox.Text = title;
            thumbnailImage.Source = source;
        }

        private void Window_Loaded(object sender, RoutedEventArgs e)
        {

        }

        private void Cancel_Clicked(object sender, RoutedEventArgs e)
        {
            DialogResult = false;
            Close();
        }

        private void Upload_Clicked(object sender, RoutedEventArgs e)
        {
            DialogResult = true;
            Close();
        }
    }
}

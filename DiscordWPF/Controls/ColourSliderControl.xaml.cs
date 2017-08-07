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
    /// Interaction logic for ColourSliderControl.xaml
    /// </summary>
    public partial class ColourSliderControl : UserControl
    {
        public Color ResultColour;

        public ColourSliderControl()
        {
            InitializeComponent();
            ResultColour = Color.FromRgb(0, 0, 0);
        }

        public void Base(Color colour)
        {
            ResultColour = colour;

            rSlider.Value = colour.R;
            gSlider.Value = colour.G;
            bSlider.Value = colour.B;
        }

        private void rSlider_ValueChanged(object sender, RoutedPropertyChangedEventArgs<double> e)
        {
            ResultColour = Color.FromRgb((byte)rSlider.Value, ResultColour.G, ResultColour.B);
            rLabel.Content = (byte)rSlider.Value;

            preview.Background = new SolidColorBrush(ResultColour);

            if (ColourChanged != null)
                ColourChanged.Invoke(this, null);
        }

        private void gSlider_ValueChanged(object sender, RoutedPropertyChangedEventArgs<double> e)
        {
            ResultColour = Color.FromRgb(ResultColour.R, (byte)gSlider.Value, ResultColour.B);
            gLabel.Content = (byte)gSlider.Value;

            preview.Background = new SolidColorBrush(ResultColour);

            if (ColourChanged != null)
                ColourChanged.Invoke(this, null);
        }

        private void bSlider_ValueChanged(object sender, RoutedPropertyChangedEventArgs<double> e)
        {
            ResultColour = Color.FromRgb(ResultColour.R, ResultColour.G, (byte)bSlider.Value);
            bLabel.Content = (byte)bSlider.Value;

            preview.Background = new SolidColorBrush(ResultColour);

            if (ColourChanged != null)
                ColourChanged.Invoke(this, null);
        }

        public event EventHandler ColourChanged;
    }
}

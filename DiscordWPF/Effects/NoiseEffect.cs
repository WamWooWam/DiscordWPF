using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Media;
using System.Windows.Media.Effects;
using System.Windows.Media.Imaging;

namespace DiscordWPF.Effects
{
    public class NoiseEffect : ShaderEffect
    {
        public static readonly DependencyProperty InputProperty =
            RegisterPixelShaderSamplerProperty("Input", typeof(NoiseEffect), 0);

        public static readonly DependencyProperty RandomInputProperty =
            RegisterPixelShaderSamplerProperty("RandomInput", typeof(NoiseEffect), 1);

        public static readonly DependencyProperty RatioProperty = DependencyProperty.Register("Ratio", typeof(double),
            typeof(NoiseEffect), new UIPropertyMetadata(0.5d, PixelShaderConstantCallback(0)));

        public NoiseEffect()
        {
            var pixelShader = new PixelShader { UriSource = new Uri("/DiscordWPF;component/Shaders/Noise.ps", UriKind.Relative) };
            PixelShader = pixelShader;
            var bitmap = new BitmapImage();
            bitmap.BeginInit();
            bitmap.UriSource = new Uri("pack://application:,,,/DiscordWPF;component/Assets/Noise.png");
            bitmap.EndInit();
            RandomInput =
                new ImageBrush(bitmap)
                {
                    TileMode = TileMode.Tile,
                    Viewport = new Rect(0, 0, 800, 600),
                    ViewportUnits = BrushMappingMode.Absolute
                };

            UpdateShaderValue(InputProperty);
            UpdateShaderValue(RandomInputProperty);
            UpdateShaderValue(RatioProperty);
        }

        public Brush Input
        {
            get => ((Brush)(GetValue(InputProperty)));
            set => SetValue(InputProperty, value);
        }

        /// <summary>The second input texture.</summary>
        public Brush RandomInput
        {
            get => ((Brush)(GetValue(RandomInputProperty)));
            set => SetValue(RandomInputProperty, value);
        }

        public double Ratio
        {
            get => ((double)(GetValue(RatioProperty)));
            set => SetValue(RatioProperty, value);
        }
    }
}

using DSharpPlus;
using DSharpPlus.Entities;
using System;
using System.Collections.Generic;
using System.Globalization;
using System.IO;
using System.Linq;
using System.Net;
using System.Text;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Data;
using System.Windows.Media;
using System.Windows.Media.Imaging;

namespace DiscordWPF.Converters
{
    class GroupIconConverter : IValueConverter
    {
        private static Dictionary<ulong, DrawingVisual> cache = new Dictionary<ulong, DrawingVisual>();

        public object Convert(object value, Type targetType, object parameter, CultureInfo culture)
        {
            if (value is DiscordDmChannel chan)
            {
                if (chan.IconUrl != null)
                {
                    return new ImageBrush(new BitmapImage(new Uri(chan.IconUrl)));
                }

                DrawingVisual visual = null;

                if(!cache.TryGetValue(chan.Id, out visual))
                {
                    visual = new DrawingVisual();
                    var context = visual.RenderOpen();

                    var pairs = chan.Recipients.Select((v, i) => new { v, i })
                        .GroupBy(x => x.i / 2, x => x.v)
                        .ToArray();

                    int width = 0;

                    for (int i = 0; i < pairs.Length; i++)
                    {
                        var group = pairs[i];
                        if (group.Count() == 2)
                        {
                            var one = group.ElementAt(0);
                            var two = group.ElementAt(1);

                            var oneSource = new BitmapImage(new Uri(one.NonAnimatedAvatarUrl));
                            var twoSource = new BitmapImage(new Uri(two.NonAnimatedAvatarUrl));

                            context.DrawImage(oneSource, new Rect(i * 32, 0, 32, 32));
                            context.DrawImage(twoSource, new Rect(i * 32, 32, 32, 32));
                            width += 32;
                        }
                        else
                        {
                            var user = group.FirstOrDefault();
                            var source = new BitmapImage(new Uri(user.GetAvatarUrl(ImageFormat.Png, 32))) { SourceRect = new Int32Rect(0, 0, 32, 64) };

                            context.DrawImage(source, new Rect(1 * 32, 0, 64, 64));
                            width += 64;
                        }
                    }

                    context.Close();
                    cache[chan.Id] = visual;
                }

                return new VisualBrush(visual) { Stretch = Stretch.UniformToFill };
            }

            return value;
        }

        public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture) => throw new NotImplementedException();
    }
}

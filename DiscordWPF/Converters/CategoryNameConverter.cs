using DSharpPlus;
using DSharpPlus.Entities;
using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Data;

namespace DiscordWPF.Converters
{
    class CategoryNameConverter : IValueConverter
    {
        public object Convert(object value, Type targetType, object parameter, CultureInfo culture)
        {
            if (value is DiscordChannel c)
            {
                if (c.Parent != null)
                {
                    return c.Parent.Name;
                }
                else
                {
                    switch (c.Type)
                    {
                        case ChannelType.Text:
                            return "Text Channels";
                        case ChannelType.Voice:
                            return "Voice Channels";
                    }
                }
            }

            return null;
        }

        public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture) => throw new NotImplementedException();
    }
}

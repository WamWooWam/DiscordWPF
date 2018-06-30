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
    class ChannelIconConverter : IValueConverter
    {
        public object Convert(object value, Type targetType, object parameter, CultureInfo culture)
        {
            if(value is DiscordChannel c)
            {
                if(c.Type == ChannelType.Text)
                {
                    return "\xE8BD";
                }

                if (c.Type == ChannelType.Voice)
                {
                    return "\xE767";
                }
            }

            return null;
        }

        public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture) => throw new NotImplementedException();
    }
}

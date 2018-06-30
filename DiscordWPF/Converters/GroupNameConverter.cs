using DSharpPlus.Entities;
using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Data;
using WamWooWam.Core;

namespace DiscordWPF.Converters
{
    class GroupNameConverter : IValueConverter
    {
        public object Convert(object value, Type targetType, object parameter, CultureInfo culture)
        {
            if(value is DiscordDmChannel chan)
            {
                if (!string.IsNullOrWhiteSpace(chan.Name))
                {
                    return chan.Name;
                }
                else
                {
                    return Strings.NaturalJoin(chan.Recipients.Select(c => c.Username));
                }
            }

            return value;
        }

        public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture) => throw new NotImplementedException();
    }
}

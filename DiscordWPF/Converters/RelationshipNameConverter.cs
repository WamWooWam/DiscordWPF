using DSharpPlus;
using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Data;

namespace DiscordWPF.Converters
{
    class RelationshipNameConverter : IValueConverter
    {
        public object Convert(object value, Type targetType, object parameter, CultureInfo culture)
        {
            if(value is DiscordRelationshipType t)
            {
                switch (t)
                {
                    case DiscordRelationshipType.Unknown:
                        return "Unknown";
                    case DiscordRelationshipType.Friend:
                        return "Friends";
                    case DiscordRelationshipType.Blocked:
                        return "Blocked";
                    case DiscordRelationshipType.IncomingRequest:
                    case DiscordRelationshipType.OutgoingRequest:
                        return "Requests";
                    default:
                        break;
                }
            }

            return "Unknown";
        }

        public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture) => throw new NotImplementedException();
    }
}

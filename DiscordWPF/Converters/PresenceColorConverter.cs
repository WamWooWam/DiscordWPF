using DSharpPlus.Entities;
using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Data;
using System.Windows.Media;

namespace DiscordWPF.Converters
{
    class PresenceColourConverter : IValueConverter
    {
        static Brush _online = new SolidColorBrush(Color.FromArgb(255, 0x43, 0xb5, 0x81));

        public object Convert(object value, Type targetType, object parameter, CultureInfo language)
        {
            var v = (DiscordPresence)value;
            if (v?.Activity?.ActivityType == ActivityType.Streaming)
            {
                return Brushes.Purple;
            }

            switch (v?.Status ?? UserStatus.Offline)
            {
                case UserStatus.Offline:
                    return Brushes.Gray;
                case UserStatus.Online:
                    return _online;
                case UserStatus.Idle:
                    return Brushes.Orange;
                case UserStatus.DoNotDisturb:
                    return Brushes.Red;
                case UserStatus.Invisible:
                    return Brushes.Gray;
                default:
                    return Brushes.Black;
            }
        }

        public object ConvertBack(object value, Type targetType, object parameter, CultureInfo language)
        {
            throw new NotImplementedException();
        }
    }

    class PresenceTextConverter : IValueConverter
    {
        public object Convert(object value, Type targetType, object parameter, CultureInfo language)
        {
            if (value is DiscordPresence v)
            {
                if(v.Activity != null && v.Activity.Name != null)
                {
                    switch (v.Activity.ActivityType)
                    {
                        case ActivityType.Playing:
                            return "Playing";
                        case ActivityType.Streaming:
                            return "Streaming";
                        case ActivityType.ListeningTo:
                            return "Listening to";
                        case ActivityType.Watching:
                            return "Watching";
                        default:
                            break;
                    }
                }

                switch (v.Status)
                {
                    case UserStatus.Online:
                        return "Online";
                    case UserStatus.Idle:
                        return "Idle";
                    case UserStatus.DoNotDisturb:
                        return "Do not disturb";
                    case UserStatus.Offline:
                    case UserStatus.Invisible:
                        return "Offline";
                    default:
                        break;
                }
            }

            return "Offline";
        }

        public object ConvertBack(object value, Type targetType, object parameter, CultureInfo language)
        {
            throw new NotImplementedException();
        }
    }
}

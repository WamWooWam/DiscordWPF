using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace DiscordWPF
{
    public static class Constants
    {

#if DEBUG
        public const string MAIN_URL = "http://localhost:8000/";
#else
        public const string MAIN_URL = "https://dwpf.wankerr.com/";
#endif

        public const string APP_USER_MODEL_ID = "com.wankerr.DiscordWPF";
        public const string APP_USER_MODEL_TOAST_ACTIVATOR_CLSID = "A9BBB496-A5F7-4D5C-8268-3F29C02DAFE1";

        public const string UPDATE_BASE_URL = MAIN_URL + "update/";

        public const string OS_WARNING_DISMISSED = "OSWarningDismissed";

        public const string ALLOW_MULTIPLE_CHANNEL_WINDOWS = "MultipleChannelWindows";
        public const string ALLOW_MULTIPLE_GUILD_WINDOWS = "MultipleGuildWindows";
        public const string ALLOW_MULTIPLE_CHANNEL_PAGES = "MultipleChannelPages";

        public const string NO_CACHE_CONTEXT_MENUS = "NoCacheContextMenus";

        public const string USE_LIGHT_THEME = "LightTheme";
        public const string USE_DISCORD_ACCENT_COLOUR = "DiscordAccent";
        public const string CUSTOM_ACCENT_COLOUR = "CustomAccent";

        public const string MINI_MODE_POSITIONS = "MiniModePositions";
        public const string MINI_MODE_SNAP_ENABLED = "MiniModeSnap";
    }
}

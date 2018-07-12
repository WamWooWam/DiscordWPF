using System;
using System.Collections.Generic;
using System.Configuration;
using System.Data;
using System.IO;
using System.Linq;
using System.Net.Http;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Media;
using Textamina.Jsonite;
using WamWooWam.Wpf;
using static DiscordWPF.Constants;

namespace DiscordWPF.Updater
{
    /// <summary>
    /// Interaction logic for App.xaml
    /// </summary>
    public partial class App : Application
    {
        public static HttpClient Client = new HttpClient();

        private void Application_Startup(object sender, StartupEventArgs e)
        {
            Client.BaseAddress = new Uri(UPDATE_BASE_URL);

            bool? light = null;
            Color? accent = null;
            try
            {
                var file = Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData), "DiscordWPF", "settings.json");
                if (File.Exists(file))
                {
                    using (var fileText = File.OpenText(file))
                    {
                        var obj = (JsonObject)Json.Deserialize(fileText);
                        object val;


                        if (obj.TryGetValue(USE_LIGHT_THEME, out val) && bool.TryParse(val.ToString(), out var l))
                        {
                            light = l;
                        }
                        if (obj.TryGetValue(CUSTOM_ACCENT_COLOUR, out val) && ColorConverter.ConvertFromString(val.ToString()) is Color col)
                        {
                            accent = col;
                        }

                        bool useDiscordAccent = accent != null && Misc.IsWindows7;

                        if (useDiscordAccent || (obj.TryGetValue(USE_DISCORD_ACCENT_COLOUR, out val) && bool.TryParse(val.ToString(), out var a) && a))
                        {
                            accent = Color.FromArgb(0xFF, 0x72, 0x89, 0xDA);
                        }
                    }
                }
            }
            catch { }

            ThemeConfiguration configuration = new ThemeConfiguration(light, accent) { NoLoad = true };
            Themes.SetTheme(configuration);
        }
    }
}

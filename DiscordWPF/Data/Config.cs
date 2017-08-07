using Newtonsoft.Json;
using Newtonsoft.Json.Converters;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Media;
using WamWooWam.Core;

namespace DiscordWPF.Data
{
    public class Config
    {
        public Config()
        {
            InstanceID = Strings.RandomString(12);

            DeleteAgreed = false;

            NsfwAgreedChannels = new List<ulong>();
            MutedChannels = new List<ulong>();
            EveryoneSurpressedGuilds = new List<ulong>();

            CachedNotifications = new List<Notification>();

            PartialMessages = new Dictionary<ulong, string>();

            General = new General();
            Personalisation = new Personalisation();
            Sounds = new Sounds();

        }

        public virtual string InstanceID { get; set; }

        public string Token { get; set; }
        public bool DeleteAgreed { get; set; }

        public List<ulong> NsfwAgreedChannels { get; set; }
        public List<ulong> MutedChannels { get; set; }
        public List<ulong> EveryoneSurpressedGuilds { get; set; }

        public List<Notification> CachedNotifications { get; set; }

        public Dictionary<ulong, string> PartialMessages { get; set; }

        public General General { get; set; }
        public Personalisation Personalisation { get; set; }
        public Sounds Sounds { get; set; }

        public void Save()
        {
            try
            {
                File.WriteAllText(Path.Combine(App.CurrentDirectory, "config.json"),
                            JsonConvert.SerializeObject(this, Formatting.Indented));
            }
            catch { }
        }
    }

    public class General
    {
        public General()
        {
            Nicknames = true;
            UserColourMentions = true;
            FormatText = true;
            GuildImageWindowIcon = true;
        }

        public bool Nicknames { get; set; }

        public bool UserColours { get; set; }
        public bool UserColourTitles { get; set; }
        public bool UserColourMentions { get; set; }

        public bool FormatText { get; set; }

        public bool ReduceAnimations { get; set; }
        public bool GuildImageWindowIcon { get; set; }
    }

    public class Personalisation
    {
        public Personalisation()
        {
            UserThemes = new List<Theme>();

            SelectedTheme = 0;
        }

        [JsonIgnore]
        public IReadOnlyList<Theme> Themes
        {
            get
            {
                List<Theme> themes = new List<Theme>();

                Theme light = new Theme()
                {
                    ThemeName = "Light",
                    ReadOnly = true,
                    Position = 0,
                    Font = (App.Current.Resources["DefaultFont"] as FontFamily),
                    SecondaryForeground = Color.FromRgb(106, 106, 106),
                    Foreground = (App.Current.Resources["LightForegroundBrush"] as SolidColorBrush).Color,
                    Background = (App.Current.Resources["LightBackgroundBrush"] as SolidColorBrush).Color,
                    SecondaryBackground = (App.Current.Resources["LightSecondaryBrush"] as SolidColorBrush).Color,
                    SelectedBackground = (App.Current.Resources["LightSelectedBackgroundBrush"] as SolidColorBrush).Color
                };
                themes.Add(light);

                Theme dark = new Theme()
                {
                    ThemeName = "Dark",
                    ReadOnly = true,
                    Position = 1,
                    Font = (App.Current.Resources["DefaultFont"] as FontFamily),
                    SecondaryForeground = Color.FromRgb(106, 106, 106),
                    Foreground = (App.Current.Resources["DarkForegroundBrush"] as SolidColorBrush).Color,
                    Background = (App.Current.Resources["DarkBackgroundBrush"] as SolidColorBrush).Color,
                    SecondaryBackground = (App.Current.Resources["DarkSecondaryBrush"] as SolidColorBrush).Color,
                    SelectedBackground = (App.Current.Resources["DarkSelectedBackgroundBrush"] as SolidColorBrush).Color
                };
                themes.Add(dark);

                themes.AddRange(UserThemes);

                return themes.OrderBy(t => t.Position).ToList();
            }
        }

        public List<Theme> UserThemes { get; set; }
        public int SelectedTheme { get; set; }
    }

    public class Sounds
    {
        public Sounds()
        {
            Enabled = true;
            MessageRecievedSound = "";
        }

        public bool Enabled { get; set; }
        public string MessageRecievedSound { get; set; }
    }

    public class Theme
    {
        public Theme()
        {
            Success = Colors.LimeGreen;
            Warning = Colors.Orange;
            Error = Colors.Red;
        }

        public string ThemeName { get; set; }
        public bool ReadOnly { get; set; }
        public int Position { get; set; }

        public Color Foreground { get; set; }
        public Color SecondaryForeground { get; set; }
        public Color Background { get; set; }
        public Color SecondaryBackground { get; set; }
        public Color SelectedBackground { get; set; }

        public Color? Success { get; set; }
        public Color? Warning { get; set; }
        public Color? Error { get; set; }

        public FontFamily Font { get; set; }
    }
}

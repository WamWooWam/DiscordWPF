using System;
using System.Collections.Generic;
using System.Configuration;
using System.Data;
using System.Linq;
using System.Threading.Tasks;
using System.Windows;
using WamWooWam.Wpf;

namespace DiscordWPF.Backend.Config
{
    /// <summary>
    /// Interaction logic for App.xaml
    /// </summary>
    public partial class App : Application
    {
        protected override void OnStartup(StartupEventArgs e)
        {
            Themes.SetTheme();
        }

        private void Application_Startup(object sender, StartupEventArgs e)
        {
        }
    }
}

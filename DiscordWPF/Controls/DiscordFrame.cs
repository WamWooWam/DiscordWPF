using DiscordWPF.Windows;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;
using System.Windows.Navigation;

namespace DiscordWPF.Controls
{
    class DiscordFrame : Frame
    {
        public DiscordFrame()
        {
            Navigating += DiscordFrame_Navigating;
        }

        private void DiscordFrame_Navigating(object sender, NavigatingCancelEventArgs e)
        {
            if(e.WebRequest != null)
            {
                e.Cancel = true;
            }
        }
    }
}

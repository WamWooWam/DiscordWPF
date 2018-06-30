#if !WIN10
using DSharpPlus.Entities;
using Microsoft.Win32;
using System;
using System.Collections.Generic;
using System.Drawing;
using System.IO;
using System.Linq;
using System.Media;
using System.Text;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Forms;
using System.Windows.Interop;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using WamWooWam.Core;

namespace DiscordWPF.Abstractions
{
    class Win32Abstractions : IAbstractions, IDisposable
    {
        private static NotifyIcon _icon;

        public Win32Abstractions()
        {
            _icon = new NotifyIcon
            {
                Icon = Properties.Resources.TrayIcon,
                Visible = true
            };
        }
        
        public string GetToken(string key)
        {
            if (Settings.TryGetSetting("Token_" + key, out string token))
            {
                return token;
            }
            else
            {
                return null;
            }
        }

        public void SetToken(string key, string token)
        {
            Settings.SetSetting("Token_" + key, token);
        }

        public void ShowInfoNotification(string title, string content)
        {
            _icon.BalloonTipTitle = title;
            _icon.BalloonTipText = content;
            _icon.BalloonTipIcon = ToolTipIcon.Info;
            _icon.ShowBalloonTip(5_000);
        }

        public void ShowNotification(DiscordMessage message)
        {
            string content = Tools.GetMessageContent(message);

            if (string.IsNullOrWhiteSpace(content))
            {
                var attach = message.Attachments.FirstOrDefault(a => a.Height != 0);
                if (attach != null)
                {
                    content = $"Uploaded {Path.GetFileName(attach.ProxyUrl)}";
                }
            }

            _icon.BalloonTipTitle = Tools.GetMessageTitle(message);
            _icon.BalloonTipText = !string.IsNullOrWhiteSpace(content) ? content : "Message";
            _icon.BalloonTipIcon = ToolTipIcon.Info;
            _icon.ShowBalloonTip(5_000);

            PlayIMSound();
        }

        private static void PlayIMSound()
        {
            try
            {
                //if (App.Config.Sounds.MessageRecievedSound == "")
                //{
                RegistryKey key = Registry.CurrentUser.OpenSubKey(@"AppEvents\Schemes\Apps\.Default\Notification.IM\.Current");
                using (SoundPlayer snd = new SoundPlayer(key.GetValue(null) as string))
                {
                    snd.Play();
                }
                //}
                //else
                //{
                //    SoundPlayer snd = new SoundPlayer(App.Config.Sounds.MessageRecievedSound);
                //    snd.Play();
                //}
            }
            catch
            {
                SystemSounds.Asterisk.Play();
            }
        }

        public void Dispose()
        {
            _icon.Visible = false;
            _icon.Dispose();
        }
    }
}
#endif
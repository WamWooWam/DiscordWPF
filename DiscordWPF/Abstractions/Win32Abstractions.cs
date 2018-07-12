using DSharpPlus.Entities;
using Microsoft.Win32;
using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Drawing;
using System.Drawing.Imaging;
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
using WamWooWam.Wpf;

namespace DiscordWPF.Abstractions
{
    class Win32Abstractions : IAbstractions, IDisposable
    {
        private static TrayIcon _icon;
        private static HwndSource _source;
        private static ConcurrentDictionary<ulong, Icon> _userIconCache;

        public Win32Abstractions()
        {
            _userIconCache = new ConcurrentDictionary<ulong, Icon>();
            _icon = new TrayIcon
            {
                Icon = Properties.Resources.TrayIcon,
                Enabled = true,
                Guid = Guid.Parse(Constants.APP_USER_MODEL_TOAST_ACTIVATOR_CLSID),
                UseLargeIcons = true,
                TrimLongText = true,
                OwnerForm = new Form()
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
            EnsureWinProc();
            _icon.ShowBalloonTip(title, content, NotifyIconIcons.Info);
        }

        public async Task ShowNotificationAsync(DiscordMessage message)
        {
            try
            {
                EnsureWinProc();

                var content = Tools.GetMessageContent(message);

                if (string.IsNullOrWhiteSpace(content))
                {
                    var attach = message.Attachments.FirstOrDefault(a => a.Height != 0);
                    if (attach != null)
                    {
                        content = $"Uploaded {Path.GetFileName(attach.ProxyUrl)}";
                    }
                }

                if (!_userIconCache.TryGetValue(message.Author.Id, out var icon))
                {
                    icon = Icon.FromHandle((await CreateBitmapForUserAsync(message.Author)).GetHicon());
                    _userIconCache[message.Author.Id] = icon;
                }

                _icon.ShowBalloonTip(Tools.GetMessageTitle(message), !string.IsNullOrWhiteSpace(content) ? content : "Message", icon, NotifyIconIcons.User);

                PlayIMSound();
            }
            catch (Exception ex)
            {
                System.Windows.MessageBox.Show(ex.ToString());
            }
        }

        public void RetractNotification(ulong id) { /* not possible in win32 */ }

        public Task SendFileWithProgressAsync(DiscordChannel channel, string message, Stream file, string fileName, IProgress<double?> progress)
        {
            progress.Report(null);
            return channel.SendFileAsync(file, fileName, message, false);
        }

        private void EnsureWinProc()
        {
            if (_source == null)
            {
                App.Current.Dispatcher.Invoke(() =>
                {
                    _source = HwndSource.FromHwnd(new WindowInteropHelper(App.Current.MainWindow).Handle);
                    _source.AddHook(new HwndSourceHook(WndProc));
                    return true; // this needs to run synchronously 
                });
            }
        }

        private static async Task<Bitmap> CreateBitmapForUserAsync(DiscordUser user)
        {
            var output = new Bitmap(64, 64, System.Drawing.Imaging.PixelFormat.Format32bppArgb);
            using (var input = (Bitmap)Image.FromFile(await Shared.GetImagePathAsync(new Uri(user.GetAvatarUrl(DSharpPlus.ImageFormat.Png, 64)))))
            {
                var mask = Properties.Resources.WinformsMask;

                var rect = new Rectangle(0, 0, 64, 64);
                var bitsMask = mask.LockBits(rect, ImageLockMode.ReadOnly, System.Drawing.Imaging.PixelFormat.Format32bppArgb);
                var bitsInput = input.LockBits(rect, ImageLockMode.ReadOnly, System.Drawing.Imaging.PixelFormat.Format32bppArgb);
                var bitsOutput = output.LockBits(rect, ImageLockMode.WriteOnly, System.Drawing.Imaging.PixelFormat.Format32bppArgb);

                unsafe
                {
                    for (int y = 0; y < input.Height; y++)
                    {
                        byte* ptrMask = (byte*)bitsMask.Scan0 + y * bitsMask.Stride;
                        byte* ptrInput = (byte*)bitsInput.Scan0 + y * bitsInput.Stride;
                        byte* ptrOutput = (byte*)bitsOutput.Scan0 + y * bitsOutput.Stride;
                        for (int x = 0; x < input.Width; x++)
                        {
                            ptrOutput[4 * x] = ptrInput[4 * x];                         // red
                            ptrOutput[4 * x + 1] = ptrInput[4 * x + 1];                 // green
                            ptrOutput[4 * x + 2] = ptrInput[4 * x + 2];                 // blue
                            ptrOutput[4 * x + 3] = (byte)(~(uint)ptrMask[4 * x + 2]);   // alpha
                        }
                    }
                }

                mask.UnlockBits(bitsMask);
                input.UnlockBits(bitsInput);
                output.UnlockBits(bitsOutput);
            }

            return output;
        }

        /// <summary>
        /// Because WinForms is :ok_hand:
        /// </summary>
        private IntPtr WndProc(IntPtr hwnd, int msg, IntPtr wParam, IntPtr lParam, ref bool handled)
        {
            var message = Message.Create(hwnd, msg, wParam, lParam);

            if (_icon.WndProc(ref message))
            {
                handled = true;
                return message.Result;
            }

            return IntPtr.Zero;
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
            _icon.Enabled = false;
            _icon.Dispose();
        }

    }
}
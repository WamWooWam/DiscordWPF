#if WIN10
using System;
using System.Collections.Generic;
using System.Linq;
using System.Runtime.InteropServices;
using System.Text;
using System.Threading.Tasks;
using DiscordWPF.Specifics.UWP;
using DSharpPlus;
using DSharpPlus.Entities;
using Microsoft.Toolkit.Uwp.Notifications;
using Windows.Data.Xml.Dom;
using Windows.Security.Credentials;
using Windows.UI.Notifications;

namespace DiscordWPF.Abstractions
{
    class UwpAbstractions : IAbstractions
    {
        private static Lazy<PasswordVault> _passwordStore = new Lazy<PasswordVault>(() => new PasswordVault());
        private static Lazy<ToastNotifier> _toastNotifier = new Lazy<ToastNotifier>(() => DesktopNotificationManagerCompat.CreateToastNotifier());

        public string GetToken(string key)
        {
            try
            {
                var token = _passwordStore.Value.FindAllByResource("token").FirstOrDefault(u => u.UserName == key);

                if (token != null)
                {
                    token.RetrievePassword();
                    return token.Password;
                }
            }
            catch
            {
                return null;
            }

            return null;
        }

        public void SetToken(string key, string token)
        {
            if (token == null)
            {
                try
                {
                    var t = _passwordStore.Value.FindAllByResource("token").FirstOrDefault(u => u.UserName == key);

                    if (t != null)
                    {
                        _passwordStore.Value.Remove(t);
                    }
                }
                catch { }
            }
            else
            {
                _passwordStore.Value.Add(new PasswordCredential("token", key, token));
            }
        }

        public void ShowInfoNotification(string title, string content)
        {
            var toastBinding = new ToastBindingGeneric()
            {
                Children =
                {
                    new AdaptiveText() { Text = title, HintStyle = AdaptiveTextStyle.Title },
                    new AdaptiveText() { Text = content }
                }
            };

            var toastContent = new ToastContent()
            {
                DisplayTimestamp = DateTime.Now,
                Visual = new ToastVisual() { BindingGeneric = toastBinding }
            };

            var str = toastContent.GetContent();

            var doc = new XmlDocument();
            doc.LoadXml(str);
            
            _toastNotifier.Value.Show(new ToastNotification(doc));
        }

        public void ShowNotification(DiscordMessage message)
        {
            string messageText = Tools.GetMessageContent(message);
            string title = Tools.GetMessageTitle(message);

            var toastBinding = new ToastBindingGeneric()
            {
                Children = { new AdaptiveText() { Text = title, HintStyle = AdaptiveTextStyle.Title }, new AdaptiveText() { Text = messageText } }
            };

            if (message.Author.GetAvatarUrl(ImageFormat.Png, 256) != null)
            {
                toastBinding.AppLogoOverride = new ToastGenericAppLogo() { Source = message.Author.GetAvatarUrl(ImageFormat.Png, 256), HintCrop = ToastGenericAppLogoCrop.Circle };
            }

            var attach = message.Attachments.FirstOrDefault(a => a.Height != 0);
            if (attach != null)
            {
                toastBinding.HeroImage = new ToastGenericHeroImage { Source = attach.ProxyUrl };
            }
            else
            {
                var embed = message.Embeds.FirstOrDefault(em => em.Thumbnail.ProxyUrl != null || em.Image.ProxyUrl != null);
                if (embed != null)
                    toastBinding.HeroImage = new ToastGenericHeroImage { Source = embed.Thumbnail?.ProxyUrl?.ToString() ?? embed.Image?.ProxyUrl?.ToString() };
            }

            var visual = new ToastVisual() { BindingGeneric = toastBinding };

            var replyString = message.Channel is DiscordDmChannel ? $"Reply to @{message.Author.Username}..." : $"Message #{message.Channel.Name}...";

            var actions = new ToastActionsCustom()
            {
                Inputs = { new ToastTextBox("tbReply") { PlaceholderContent = replyString } },
                Buttons = { new ToastButton("Reply", "") { ActivationType = ToastActivationType.Background, TextBoxId = "tbReply" } }
            };

            var toastContent = new ToastContent()
            {
                DisplayTimestamp = message.Timestamp.DateTime,
                Visual = visual,
                Actions = actions
            };

            var str = toastContent.GetContent();

            var doc = new XmlDocument();
            doc.LoadXml(str);

            App.Current.Dispatcher.Invoke(() => _toastNotifier.Value.Show(new ToastNotification(doc)));
        }       
    }
}
#endif
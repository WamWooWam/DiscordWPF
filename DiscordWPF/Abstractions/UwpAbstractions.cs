
using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Net;
using System.Runtime.InteropServices;
using System.Text;
using System.Threading.Tasks;
using DiscordWPF.Specifics.UWP;
using DSharpPlus;
using DSharpPlus.Entities;
using Microsoft.Toolkit.Uwp.Notifications;
using WamWooWam.Core;
using WamWooWam.Wpf;
using Windows.Data.Xml.Dom;
using Windows.Foundation;
using Windows.Foundation.Collections;
using Windows.Media;
using Windows.Media.Capture;
using Windows.Media.Core;
using Windows.Media.MediaProperties;
using Windows.Media.Transcoding;
using Windows.Security.Credentials;
using Windows.Storage;
using Windows.Storage.Streams;
using Windows.UI.Notifications;
using Windows.Web.Http;

namespace DiscordWPF.Abstractions
{
    class UwpAbstractions : IAbstractions
    {
        private static Lazy<PasswordVault> _passwordStore = new Lazy<PasswordVault>(() => new PasswordVault());
        private static Lazy<ToastNotifier> _toastNotifier = new Lazy<ToastNotifier>(() => ToastNotificationManager.CreateToastNotifier(Constants.APP_USER_MODEL_ID));
        private static ConcurrentDictionary<ulong, ToastNotification> _sentNotifications = new ConcurrentDictionary<ulong, ToastNotification>();

        public UwpAbstractions()
        {
            DesktopNotificationManagerCompat.RegisterAumidAndComServer<UWPNotificationActivator>(Constants.APP_USER_MODEL_ID);
        }

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
            if (string.IsNullOrWhiteSpace(token))
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
            var doc = Misc.IsWindows10 ? GetWindows10InfoToast(title, content) : GetWindows8InfoToast(title, content);

            _toastNotifier.Value.Show(new ToastNotification(doc));
        }

        public async Task ShowNotificationAsync(DiscordMessage message)
        {
            string title = Tools.GetMessageTitle(message);
            string messageText = Tools.GetMessageContent(message);
            ToastNotification notif = null;

            if (Misc.IsWindows10)
            {
                notif = await GetWindows10ToastAsync(message, title, messageText);
            }
            else if (Misc.IsWindows8)
            {
                notif = await GetWindows8ToastAsync(message, title, messageText);
            }

            if (notif != null)
            {
                App.Current.Dispatcher.Invoke(() => _toastNotifier.Value.Show(notif));
            }
        }

        public void RetractNotification(ulong id)
        {
            if (_sentNotifications.TryGetValue(id, out var notif))
            {
                _toastNotifier.Value.Hide(notif);
            }
        }

        public async Task SendFileWithProgressAsync(DiscordChannel channel, string message, Stream file, string fileName, IProgress<double?> progress)
        {
            using (var client = new HttpClient())
            {
                HttpRequestMessage httpRequestMessage
                    = new HttpRequestMessage(HttpMethod.Post, new Uri("https://discordapp.com/api/v7" + string.Format("/channels/{0}/messages", channel.Id)));
                httpRequestMessage.Headers.Add("Authorization", Utilities.GetFormattedToken(channel.Discord));

                var cont = new HttpMultipartFormDataContent();

                if (!string.IsNullOrWhiteSpace(message))
                {
                    cont.Add(new HttpStringContent(message), "content");
                }

                cont.Add(new HttpStreamContent(file.AsInputStream()), "file", fileName);

                httpRequestMessage.Content = cont;

                var send = client.SendRequestAsync(httpRequestMessage);
                send.Progress += new AsyncOperationProgressHandler<HttpResponseMessage, HttpProgress>((o, e) =>
                {
                    progress.Report((e.BytesSent / (double)e.TotalBytesToSend) * 100);
                });

                await send;
            }
        }

        #region Media

        public async Task<string> GetFileMimeAsync(string path)
        {
            var file = await StorageFile.GetFileFromPathAsync(path);
            return file.ContentType;
        }

        public async Task<bool> TryTranscodeAudioAsync(string file, Stream stream, IProgress<double?> progress)
        {
            try
            {
                var storageFile = await StorageFile.GetFileFromPathAsync(file);

                return await TryTranscodeMediaAsync(storageFile, stream, MediaEncodingProfile.CreateMp3(AudioEncodingQuality.Medium), progress);
            }
            catch { }

            return false;
        }

        public async Task<bool> TryTranscodeVideoAsync(string file, Stream stream, IProgress<double?> progress)
        {
            try
            {
                var storageFile = await StorageFile.GetFileFromPathAsync(file);
                var props = await storageFile.Properties.GetVideoPropertiesAsync();
                
                var width = (int)props.Width;
                var height = (int)props.Height;
                Drawing.ScaleProportions(ref width, ref height, 854, 480);

                var profile = MediaEncodingProfile.CreateMp4(VideoEncodingQuality.HD720p);

                profile.Video.Width = (uint)(Math.Round(width / 2.0) * 2);
                profile.Video.Height = (uint)(Math.Round(height / 2.0) * 2);
                profile.Video.Bitrate = 1_500_000;

                return await TryTranscodeMediaAsync(storageFile, stream, profile, progress);
            }
            catch { }

            return false;
        }

        public Task<bool> TryTranscodeImageAsync(string file, Stream stream, IProgress<double?> progress)
        {
            return Task.FromResult(false);
        }

        private static async Task<bool> TryTranscodeMediaAsync(StorageFile file, Stream stream, MediaEncodingProfile profile, IProgress<double?> progress)
        {
            using (var fileStream = await file.OpenReadAsync())
            {
                var transcoder = new MediaTranscoder();
                var prep = await transcoder.PrepareStreamTranscodeAsync(fileStream, stream.AsRandomAccessStream(), profile);

                if (prep.CanTranscode)
                {
                    var task = prep.TranscodeAsync();
                    task.Progress = new AsyncActionProgressHandler<double>((a, p) => progress.Report(p));
                    await task;

                    return true;
                }
            }

            return false;
        }

        #endregion

        #region Helpers

        private XmlDocument GetWindows10InfoToast(string title, string content)
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
            return doc;
        }

        private XmlDocument GetWindows8InfoToast(string title, string content)
        {
            var xml = ToastNotificationManager.GetTemplateContent(ToastTemplateType.ToastText02);

            var text = xml.GetElementsByTagName("text");
            text[0].AppendChild(xml.CreateTextNode(title));
            text[1].AppendChild(xml.CreateTextNode(content));

            return xml;
        }

        private async Task<ToastNotification> GetWindows8ToastAsync(DiscordMessage message, string title, string messageText)
        {
            var xml = ToastNotificationManager.GetTemplateContent(ToastTemplateType.ToastImageAndText02);

            var text = xml.GetElementsByTagName("text");
            text[0].AppendChild(xml.CreateTextNode(title));
            text[1].AppendChild(xml.CreateTextNode(messageText));

            var image = xml.GetElementsByTagName("image");
            image[0].Attributes.GetNamedItem("src").NodeValue = await Shared.GetImagePathAsync(new Uri(message.Author.GetAvatarUrl(ImageFormat.Png, 128)));

            //var audio = xml.CreateElement("audio");
            //audio.Attributes.GetNamedItem("src").NodeValue = "ms-winsoundevent:Notification.IM";
            //audio.Attributes.GetNamedItem("loop").NodeValue = "false";

            //xml.GetElementsByTagName("toast")[0].AppendChild(audio);

            return new ToastNotification(xml);
        }

        private async Task<ToastNotification> GetWindows10ToastAsync(DiscordMessage message, string title, string messageText)
        {
            ToastActionsCustom actions = null;
            var toastBinding = new ToastBindingGeneric()
            {
                Children = { new AdaptiveText() { Text = title, HintStyle = AdaptiveTextStyle.Title }, new AdaptiveText() { Text = messageText } }
            };

            var animated = message.Author.AvatarHash?.StartsWith("a_") ?? false;
            var url = animated ? message.Author.GetAvatarUrl(ImageFormat.Gif, 128) : message.Author.GetAvatarUrl(ImageFormat.Png, 128);
            if (url != null)
            {
                toastBinding.AppLogoOverride = new ToastGenericAppLogo() { Source = await Shared.GetImagePathAsync(new Uri(url)), HintCrop = ToastGenericAppLogoCrop.Circle };
            }

            var attach = message.Attachments.FirstOrDefault(a => a.Height != 0);
            if (attach != null)
            {
                toastBinding.HeroImage = new ToastGenericHeroImage { Source = await Shared.GetImagePathAsync(new Uri(attach.ProxyUrl + "?format=jpeg")) };
            }
            else
            {
                var embed = message.Embeds.FirstOrDefault(em => em.Thumbnail.ProxyUrl != null || em.Image.ProxyUrl != null);
                if (embed != null)
                    toastBinding.HeroImage = new ToastGenericHeroImage { Source = await Shared.GetImagePathAsync(embed.Thumbnail?.ProxyUrl ?? embed.Image?.ProxyUrl) };
            }

#if DEBUG
            var replyString = message.Channel is DiscordDmChannel ? $"Reply to @{message.Author.Username}..." : $"Message #{message.Channel.Name}...";
            actions = new ToastActionsCustom()
            {
                Inputs = { new ToastTextBox("tbReply") { PlaceholderContent = replyString } },
                Buttons = { new ToastButton("Reply", "") { ActivationType = ToastActivationType.Background, TextBoxId = "tbReply" } }
            };
#endif

            var visual = new ToastVisual() { BindingGeneric = toastBinding };

            var toastContent = new ToastContent()
            {
                DisplayTimestamp = message.Timestamp.DateTime,
                Visual = visual,
#if DEBUG
                Actions = actions
#endif
            };

            var str = toastContent.GetContent();

            var doc = new XmlDocument();
            doc.LoadXml(str);

            var notif = new ToastNotification(doc);
            _sentNotifications[message.Id] = notif;

            return notif;
        }

        #endregion
    }
}
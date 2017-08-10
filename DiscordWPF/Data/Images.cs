using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Net.Http;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using System.Windows.Media.Imaging;
using System.Windows.Threading;
using System.Net.Cache;

namespace DiscordWPF.Data
{
    public static class Images
    {
        public static BitmapImage GetImage(string url)
        {
            BitmapImage image = new BitmapImage();
            image.BeginInit();
            image.UriSource = new Uri(url);
            image.CacheOption = BitmapCacheOption.OnDemand;
            image.UriCachePolicy = new RequestCachePolicy(RequestCacheLevel.CacheIfAvailable);
            image.DownloadCompleted += Image_DownloadCompleted;
            Console.WriteLine($"Downloading {Path.GetFileName(url)}...");
            image.EndInit();

            return image;
        }

        private static void Image_DownloadCompleted(object sender, EventArgs e)
        {
            Console.WriteLine($"Downloaded {Path.GetFileName((sender as BitmapImage).UriSource.ToString())}!");
        }
    }

    public class ImageStore
    {
        public ImageStore(Discord.EmbedImage embed)
        {
            Width = embed.Width;
            Height = embed.Height;

            string url = embed.ProxyUrl;

            double ratioX = (double)640 / embed.Width.Value;
            double ratioY = (double)480 / embed.Height.Value;
            double ratio = Math.Min(ratioX, ratioY);

            int newWidth = (int)(embed.Width * ratio);
            int newHeight = (int)(embed.Height * ratio);

            url += $"?width={newWidth}&height={newHeight}";

            Url = new Uri(url);
        }

        public ImageStore(Discord.EmbedThumbnail embed)
        {
            Width = embed.Width;
            Height = embed.Height;

            string url = embed.ProxyUrl;

            double ratioX = (double)640 / embed.Width.Value;
            double ratioY = (double)480 / embed.Height.Value;
            double ratio = Math.Min(ratioX, ratioY);

            int newWidth = (int)(embed.Width * ratio);
            int newHeight = (int)(embed.Height * ratio);

            url += $"?width={newWidth}&height={newHeight}";

            Url = new Uri(url);
        }

        public ImageStore(Discord.IAttachment attachment)
        {
            Width = attachment.Width;
            Height = attachment.Height;

            string url = attachment.ProxyUrl;

            double ratioX = (double)640 / attachment.Width.Value;
            double ratioY = (double)480 / attachment.Height.Value;
            double ratio = Math.Min(ratioX, ratioY);

            int newWidth = (int)(attachment.Width * ratio);
            int newHeight = (int)(attachment.Height * ratio);

            url += $"?width={newWidth}&height={newHeight}";

            Url = new Uri(url);
        }

        public int? Width { get; set; }
        public int? Height { get; set; }

        public Uri Url { get; set; }
    }

   
}

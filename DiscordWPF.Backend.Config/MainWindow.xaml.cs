using DiscordWPF.Shared;
using Microsoft.WindowsAPICodePack.Dialogs;
using Newtonsoft.Json;
using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.IO;
using System.IO.Compression;
using System.Linq;
using System.Reflection;
using System.Security.Cryptography;
using System.Text;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Data;
using System.Windows.Documents;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using System.Windows.Navigation;

namespace DiscordWPF.Backend.Config
{
    /// <summary>
    /// Interaction logic for MainWindow.xaml
    /// </summary>
    public partial class MainWindow : Window
    {
        public MainWindow()
        {
            InitializeComponent();
        }

        private void Window_Loaded(object sender, RoutedEventArgs e)
        {
            CommonOpenFileDialog open = new CommonOpenFileDialog() { EnsureFileExists = true, Title = "Open main executable" };
            open.Filters.Add(new CommonFileDialogFilter("Main Executable", ".exe"));

            if (open.ShowDialog() == CommonFileDialogResult.Ok)
            {
                exeTextBox.Text = open.FileName;
            }

            open.Filters.Clear();

            open.Title = "Open output folder";
            open.IsFolderPicker = true;
            if (open.ShowDialog() == CommonFileDialogResult.Ok)
            {
                pathTextBox.Text = open.FileName;
            }
        }

        private void GenerateUpdatePackage(string exePath, string dataPath, string description)
        {
            var hashData = new ConcurrentDictionary<string, byte[]>();

            var ass = Assembly.LoadFrom(exePath);
            var assName = ass.GetName();

            var details = new UpdateDetails { NewVersion = assName.Version.ToString(), NewVersionDetails = description };

            var folder = Path.GetDirectoryName(exePath);
            var folders = Directory.GetDirectories(folder, "*", SearchOption.AllDirectories);

            var subDir = Path.Combine(dataPath, assName.Version.ToString());
            Directory.CreateDirectory(subDir);
            var filesDir = Path.Combine(subDir, "data");
            Directory.CreateDirectory(filesDir);

            foreach (var f in folders)
            {
                string rawName = f.Remove(0, folder.Length + 1);
                string newPath = Path.Combine(filesDir, rawName);
                Directory.CreateDirectory(newPath);
            }

            Parallel.ForEach(Directory.GetFiles(folder, "*", SearchOption.AllDirectories), f =>
            {
                var rawName = f.Remove(0, folder.Length).Trim('\\', '/');

                using (var stream = File.OpenRead(f))
                using (var hash = SHA256.Create())
                using (var outStream = File.Create(Path.Combine(filesDir, rawName + ".gz")))
                using (var compress = new GZipStream(outStream, CompressionMode.Compress))
                {
                    hashData[rawName] = hash.ComputeHash(stream);
                    stream.Seek(0, SeekOrigin.Begin);
                    stream.CopyTo(compress);
                    compress.Flush();
                }
            });

            details.Files = hashData.OrderBy(k => k.Key).ToDictionary(k => k.Key, v => v.Value);
            details.ReleaseDate = DateTimeOffset.Now;

            File.WriteAllText(Path.Combine(subDir, assName.Version.ToString() + ".json"), JsonConvert.SerializeObject(details, Formatting.Indented));

            var versionsFile = Path.Combine(dataPath, "versions.json");
            Dictionary<string, UpdateDetails> versions;

            if (!File.Exists(versionsFile))
            {
                versions = new Dictionary<string, UpdateDetails> { [assName.Version.ToString()] = details };
            }
            else
            {
                versions = JsonConvert.DeserializeObject<Dictionary<string, UpdateDetails>>(File.ReadAllText(versionsFile));
                versions[assName.Version.ToString()] = details;
            }

            File.WriteAllText(versionsFile, JsonConvert.SerializeObject(versions));
        }

        private void Button_Click(object sender, RoutedEventArgs e)
        {
            GenerateUpdatePackage(exeTextBox.Text, pathTextBox.Text, descriptionTextBox.Text);
        }
    }
}

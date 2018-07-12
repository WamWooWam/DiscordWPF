using Microsoft.Win32;
using Microsoft.WindowsAPICodePack.Dialogs;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Data;
using System.Windows.Documents;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Media.Animation;
using System.Windows.Media.Imaging;
using System.Windows.Navigation;
using System.Windows.Threading;
using Textamina.Jsonite;
using WamWooWam.Wpf;

namespace DiscordWPF.Updater
{
    /// <summary>
    /// Interaction logic for MainWindow.xaml
    /// </summary>
    public partial class MainWindow : Window
    {
        public MainWindow()
        {
            InitializeComponent();
            loadingControl.Text = "Initialising...";
        }

        private void Window_MouseDown(object sender, MouseButtonEventArgs e)
        {
            if (e.ChangedButton == MouseButton.Left)
                DragMove();
        }

        private async void Window_Loaded(object sender, RoutedEventArgs e)
        {
            await loadingControl.ChangeStatusAsync("Initialising...");

            var rawArgs = Environment.GetCommandLineArgs().Skip(1).ToArray();

            if (rawArgs.Any())
            {
                try
                {
                    Dictionary<string, string> args = new Dictionary<string, string>();
                    foreach (string str in rawArgs)
                    {
                        string arg = str.TrimStart('-');
                        args.Add(arg.Substring(0, arg.IndexOf('=')), arg.Substring(arg.IndexOf('=') + 1));
                    }

                    if (args.TryGetValue("type", out var type))
                    {
                        if (type == "update")
                        {
                            await SetupUpdateAsync(args);
                            return;
                        }
                    }
                }
                catch { }
            }

            await SetupInstallerAsync();
        }

        private async Task SetupUpdateAsync(Dictionary<string, string> args)
        {
            errorTitle.Text = "Update failed";

            if (!args.TryGetValue("to", out var toVersion))
                toVersion = "latest";

            if (args.TryGetValue("pid", out var spid) && int.TryParse(spid, out var pid))
            {
                try
                {
                    var p = await Task.Run(() => Process.GetProcessById(pid));

                    await loadingControl.ChangeStatusAsync($"Waiting for {p.ProcessName} to exit... (Id: {pid})");
                    await Task.Run(() => p.WaitForExit());
                }
                catch { }
            }

            await loadingControl.ChangeStatusAsync("Preparing to update...");
            await Update.DoUpdateAsync(toVersion);
        }

        private async Task SetupInstallerAsync()
        {
            try
            {
                errorTitle.Text = "Installation failed";

                await Dispatcher.InvokeAsync(() =>
                {
                    installDirectoryTextBox.Text = Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData), "DiscordWPF");
                });

                await Dispatcher.InvokeAsync(() => (Resources["showInstallControls"] as Storyboard).Begin(), DispatcherPriority.ApplicationIdle);
            }
            catch (Exception ex)
            {
                string error = $"Failed to fetch channel info. {ex.Message}";
                ShowError(error);
            }
        }

        internal void ShowError(string error)
        {
            errorText.Text = error;
            (Resources["showError"] as Storyboard).Begin();
        }

        private async void startInstallButton_Click(object sender, RoutedEventArgs e)
        {
            DirectoryInfo info = null;
            try
            {
                info = new DirectoryInfo(installDirectoryTextBox.Text);
            }
            catch (ArgumentException) { }
            catch (PathTooLongException) { }
            catch (NotSupportedException) { }

            if (!(info is null))
            {
                if (!info.Exists || !info.EnumerateFileSystemInfos().Any() || info.FullName == Directory.GetCurrentDirectory())
                {
                    (Resources["hideInstallControls"] as Storyboard).Begin();

                    if (await Install.DoInstallAsync(installDirectoryTextBox.Text, true, true, this))
                    {
                        (Resources["showInstallComplete"] as Storyboard).Begin();
                    }
                }
                else
                {
                    MessageBox.Show(
                        "The installation folder must be empty!",
                        "Invalid folder path.",
                        MessageBoxButton.OK,
                        MessageBoxImage.Error);
                }
            }
            else
            {
                MessageBox.Show(
                    "That's not a real folder name god damnit! It doesn't have to exist, it just has to be valid.",
                    "Invalid folder path.",
                    MessageBoxButton.OK,
                    MessageBoxImage.Error);
            }
        }

        private void Button_Click(object sender, RoutedEventArgs e)
        {
            Close();
        }

        private void launchButton_Click(object sender, RoutedEventArgs e)
        {
            Close();
        }

        private void selectDirectoryButton_Click(object sender, RoutedEventArgs e)
        {
            using (var dialog = new CommonOpenFileDialog())
            {
                dialog.IsFolderPicker = true;
                dialog.InitialDirectory = installDirectoryTextBox.Text;

                if(dialog.ShowDialog() == CommonFileDialogResult.Ok)
                {
                    installDirectoryTextBox.Text = dialog.FileName;
                }
            }
        }
    }
}

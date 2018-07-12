using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

using System.IO;
using DiscordWPF.Controls;
using Textamina.Jsonite;
using System.IO.Compression;
using Microsoft.WindowsAPICodePack.Shell;
using Microsoft.WindowsAPICodePack.Shell.PropertySystem;
using System.Runtime.InteropServices;
using System.Reflection;

namespace DiscordWPF.Updater
{
    class Install
    {
        public static async Task<bool> DoInstallAsync(string installDir, bool startMenu, bool desktop, MainWindow window)
        {
            var platform = Environment.Is64BitOperatingSystem ? "x64" : "x86";
            var control = window.loadingControl;
            await control.ChangeStatusAsync("Preparing to install...");

            await Task.Delay(1000); // Wait for the animation to play out before installing.

            try
            {
                if (!Directory.Exists(installDir))
                    Directory.CreateDirectory(installDir);

                var details = await App.Client.GetStringAsync("versions.json");
                var versions = ((JsonArray)Json.Deserialize(details))
                    .Cast<JsonObject>();

                var latest = versions
                    .OrderByDescending(v => Version.Parse(v["version"] as string))
                    .FirstOrDefault(v => v["platform"] as string == platform);

                if (latest != null)
                {
                    var version = latest["version"]?.ToString();
                    var obj = (JsonArray)latest["files"];

                    for (int i = 0; i < obj.Count; i++)
                    {
                        var file = obj.ElementAt(i) as JsonObject;
                        try
                        {
                            await control.ChangeStatusAsync("Installing...", i, obj.Count);

                            var path = Path.Combine(installDir, file["name"] as string);

                            if (!Directory.Exists(Path.GetDirectoryName(path)))
                                Directory.CreateDirectory(Path.GetDirectoryName(path));

                            var url = Shared.GetUrlForFile(platform, versions, version, file);

                            using (var destStr = File.Create(path))
                            using (var remoteStr = await App.Client.GetStreamAsync(url))
                            using (var gzip = new GZipStream(remoteStr, CompressionMode.Decompress))
                            {
                                await gzip.CopyToAsync(destStr);
                            }
                        }
                        catch (Exception ex)
                        {
                            await window.Dispatcher.InvokeAsync(() => window.ShowError($"Failed to fetch {file["name"]}. {ex.Message}"));
                            break;
                        }
                    }

                    File.Copy(Assembly.GetExecutingAssembly().Location, Path.Combine(installDir, "DiscordWPF.Updater.exe"));

                    if (startMenu || desktop)
                    {
                        await control.ChangeStatusAsync("Creating shortcuts....");
                    }

                    if (startMenu)
                    {
                        CreateStartShortcut(installDir);
                    }

                    return true;
                }
                else
                {
                    await window.Dispatcher.InvokeAsync(() => window.ShowError($"Failed to fetch details for version \"latest\" with channel \"{platform}\"."));
                }
            }
            catch (Exception ex)
            {
                await window.Dispatcher.InvokeAsync(() => window.ShowError($"Catostrophic installation failure. {ex.Message}"));
            }

            return false;
        }
        
        internal static void CreateStartShortcut(string installDir)
        {
            var path = Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.StartMenu), "DiscordWPF");
            var shortcutPath = Path.Combine(path, "DiscordWPF.lnk");
            Directory.CreateDirectory(path);

            var shell = new IWshRuntimeLibrary.WshShell();
            var shortcut = shell.CreateShortcut(shortcutPath) as IWshRuntimeLibrary.WshShortcut;
            shortcut.TargetPath = Path.Combine(installDir, "DiscordWPF.exe"); // TODO: details["main_exe"]
            shortcut.WorkingDirectory = installDir;
            shortcut.Description = "Launch DiscordWPF";
            shortcut.Save();

            Marshal.ReleaseComObject(shortcut);
            Marshal.ReleaseComObject(shell);

            using (var link = (ShellFile)ShellObject.FromParsingName(shortcutPath))
            using (var writer = link.Properties.GetPropertyWriter())
            {
                writer.WriteProperty(SystemProperties.System.AppUserModel.ID, Constants.APP_USER_MODEL_ID);
                //writer.WriteProperty("System.AppUserModel.ToastActivatorCLSID", Constants.APP_USER_MODEL_TOAST_ACTIVATOR_CLSID);
                writer.Close();
            }
        }
    }
}

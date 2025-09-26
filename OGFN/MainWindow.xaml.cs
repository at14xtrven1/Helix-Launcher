using Newtonsoft.Json;
using Newtonsoft.Json.Linq;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Net;
using System.Runtime.InteropServices;
using System.Text;
using System.Windows;
using System.Windows.Forms;
using System.Windows.Navigation;
using WpfApp6.Services;
using WpfApp6.Services.Launch;
using MessageBox = System.Windows.MessageBox;

namespace Helix
{
    public class ConfigFile
    {
        public List<string> FolderPaths { get; set; } = new List<string>();
    }

    public partial class MainWindow : Window
    {
        private readonly string configIniPath = Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "config.ini");

        public MainWindow(string username)
        {
            InitializeComponent();
            EnsureIniExists();
        }

        private void EnsureIniExists()
        {
            if (!File.Exists(configIniPath))
            {
 
                var iniContent = new StringBuilder();
                iniContent.AppendLine("[Auth]");
                iniContent.AppendLine("Path=NONE");
                File.WriteAllText(configIniPath, iniContent.ToString());
            }
        }

        private void Hyperlink_RequestNavigate(object sender, RequestNavigateEventArgs e)
        {
            Process.Start(new ProcessStartInfo
            {
                FileName = e.Uri.AbsoluteUri,
                UseShellExecute = true
            });
            e.Handled = true;
        }

        private void AddBuildButton_Click(object sender, RoutedEventArgs e)
        {
            var dialog = new FolderBrowserDialog
            {
                Description = "Select Fortnite 24.20 Build Folder",
                ShowNewFolderButton = false
            };

            dialog.ShowDialog();

            string selectedPath = dialog.SelectedPath;

            if (!selectedPath.Contains("24.20"))
            {
                MessageBox.Show("You can only use and play 24.20 / Chapter 4 Season 2 right now!", "Invalid Folder", MessageBoxButton.OK, MessageBoxImage.Warning);
                return;
            }
 
            IniFile.WriteValue("Auth", "Path", selectedPath, configIniPath);

            MessageBox.Show("Build folder saved to config.ini", "Success", MessageBoxButton.OK, MessageBoxImage.Information);
        }

        private void PlayButton_Click(object sender, RoutedEventArgs e)
        {
            try
            {
                string path = UpdateINI.ReadValue("Auth", "Path");

                if (string.IsNullOrEmpty(path) || path == "NONE" || !Directory.Exists(path))
                {
                    MessageBox.Show("The configured Fortnite path is invalid or missing.", "Error", MessageBoxButton.OK, MessageBoxImage.Error);
                    return;
                }

                string exePath = Path.Combine(path, "FortniteGame", "Binaries", "Win64", "FortniteClient-Win64-Shipping.exe");
                if (!File.Exists(exePath))
                {
                    MessageBox.Show("Fortnite executable not found at the specified path.", "Error", MessageBoxButton.OK, MessageBoxImage.Error);
                    return;
                }
 

                string dllPath = Path.Combine(path, "Engine", "Binaries", "ThirdParty", "NVIDIA", "NVaftermath", "Win64", "GFSDK_Aftermath_Lib.x64.dll");
                string dllFolder = Path.GetDirectoryName(dllPath);

                if (!Directory.Exists(dllFolder))
                {
                    Directory.CreateDirectory(dllFolder);
                }

                if (!File.Exists(dllPath))
                {
                    try
                    {
                        System.Net.ServicePointManager.SecurityProtocol = System.Net.SecurityProtocolType.Tls12;
                        using (WebClient client = new WebClient())
                        {
                            client.DownloadFile(
                                "Your DLL Here",
                                dllPath);
                        }
                    }
                    catch (Exception ex)
                    {
                        MessageBox.Show($"Failed to download the required DLL:\n{ex.Message}", "Download Error", MessageBoxButton.OK, MessageBoxImage.Error);
                        return;
                    }
                }

                PSBasics.Start(path, "-epicapp=Fortnite -epicenv=Prod -epiclocale=en-us -epicportal -noeac -fromfl=be -fltoken=h1cdhchd10150221h130eB56 -skippatchcheck", null, null);
                FakeAC.Start(path, "FortniteClient-Win64-Shipping_BE.exe", "-epicapp=Fortnite -epicenv=Prod -epiclocale=en-us -epicportal -noeac -fromfl=be -fltoken=h1cdhchd10150221h130eB56 -skippatchcheck", "r");
                FakeAC.Start(path, "FortniteLauncher.exe", "-epicapp=Fortnite -epicenv=Prod -epiclocale=en-us -epicportal -noeac -fromfl=be -fltoken=h1cdhchd10150221h130eB56 -skippatchcheck", "dsf");

                PSBasics._FortniteProcess?.WaitForExit();

                try
                {
                    FakeAC._FNLauncherProcess?.Close();
                    FakeAC._FNAntiCheatProcess?.Close();
                }
                catch (Exception ex)
                {
                    MessageBox.Show($"Error closing processes: {ex.Message}", "Error", MessageBoxButton.OK, MessageBoxImage.Error);
                }
            }
            catch (Exception ex)
            {
                MessageBox.Show($"An unexpected error occurred:\n{ex.Message}", "Error", MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }

        internal class UpdateINI
        {
            [DllImport("kernel32.dll", CharSet = CharSet.Auto)]
            private static extern int GetPrivateProfileString(
                string section, string key, string defaultValue,
                StringBuilder retVal, int size, string filePath);

            private static readonly string configFilePath = Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "config.ini");

            internal static string ReadValue(string section, string key)
            {
                if (!File.Exists(configFilePath))
                    return "NONE";

                var retVal = new StringBuilder(255);
                GetPrivateProfileString(section, key, "NONE", retVal, retVal.Capacity, configFilePath);
                return retVal.ToString();
            }
        }

        public static class IniFile
        {
            [DllImport("kernel32.dll", CharSet = CharSet.Auto)]
            private static extern long WritePrivateProfileString(
                string section, string key, string val, string filePath);

            public static void WriteValue(string section, string key, string value, string filePath)
            {
                WritePrivateProfileString(section, key, value, filePath);
            }
        }
    }
}


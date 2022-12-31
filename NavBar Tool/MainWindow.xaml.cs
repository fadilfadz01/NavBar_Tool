using Microsoft.Win32;
using ProcessPrivileges;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Globalization;
using System.IO;
using System.Linq;
using System.Management;
using System.Reflection;
using System.Runtime.InteropServices;
using System.Security.Principal;
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
using System.Windows.Shapes;
using Path = System.IO.Path;

namespace NavBar_Tool
{
    /// <summary>
    /// Interaction logic for MainWindow.xaml
    /// </summary>
    public partial class MainWindow : Window
    {
        [DllImport("kernel32.dll", SetLastError = true)]
        static extern int UpdateResource(IntPtr hUpdate, uint lpType, string lpName, ushort wLanguage, byte[] lpData, uint cbData);

        [DllImport("kernel32.dll", SetLastError = true)]
        static extern IntPtr BeginUpdateResource(string pFileName, [MarshalAs(UnmanagedType.Bool)]bool bDeleteExistingResources);

        [DllImport("kernel32.dll", SetLastError = true)]
        static extern bool EndUpdateResource(IntPtr hUpdate, bool fDiscard);

        [DllImport("advapi32.dll", SetLastError = true)]
        static extern int RegLoadKey(IntPtr hKey, string lpSubKey, string lpFile);

        [DllImport("advapi32.dll", SetLastError = true)]
        static extern Int32 RegUnLoadKey(IntPtr hKey, string lpSubKey);

        public string[] Drive = new string[2];
        string[] PhysicalDrive = new string[2];
        string[] VolumeLabel = new string[2];
        bool result = false;
        OpenFileDialog openFileDialog;
        string backFile = null;
        string searchFile = null;
        string startFile = null;
        string resourceFile;
        string res;
        string[] imgFiles;
        string[] buttons = { "Back", "Search", "Start" };
        bool flag = false;

        public MainWindow()
        {
            InitializeComponent();
            ResizeMode = ResizeMode.CanMinimize;
            getPriviledge();
            _ = StartProg();
            //To-Do: detect mounted drives, reset image on reset click, button sorting order
        }

        private async Task StartProg()
        {
            /*if (!AdminRelauncher())
            {
                Application.Current.Shutdown();
            }*/
            if (Directory.Exists(@"C:\ProgramData\NavBar Tool\.rsrc"))
            {
                Directory.Delete(@"C:\ProgramData\NavBar Tool\.rsrc", true);
            }
            Directory.CreateDirectory(@"C:\ProgramData\NavBar Tool");
            await GetDevice();
            /*Drive[0] = "E:";
            DriveCombo.Items.Add("MainOS (E:)");
            DeviceBlock.Text = $"Mass Storage Mode connected: ";
            DriveCombo.SelectedIndex = 0;
            DriveCombo.Visibility = Visibility.Visible;*/
        }

        private async Task GetDevice()
        {
            await Task.Run(() =>
            {
                try
                {
                    while (!result)
                    {
                        int i = 0;
                        foreach (ManagementObject logical in new ManagementObjectSearcher("select * from Win32_LogicalDisk").Get())
                        {
                            string Label = string.Empty;
                            foreach (ManagementObject partition in logical.GetRelated("Win32_DiskPartition"))
                            {
                                foreach (ManagementObject drive in partition.GetRelated("Win32_DiskDrive"))
                                {
                                    if (drive["PNPDeviceID"].ToString().Contains("VEN_QUALCOMM&PROD_MMC_STORAGE") || drive["PNPDeviceID"].ToString().Contains("VEN_MSFT&PROD_PHONE_MMC_STOR"))
                                    {
                                        Label = logical["VolumeName"] == null ? "" : logical["VolumeName"].ToString();
                                        if ((Drive[i] == null) || string.Equals(Label, "MainOS", StringComparison.CurrentCultureIgnoreCase))
                                        {
                                            Drive[i] = logical["Name"].ToString();
                                            PhysicalDrive[i] = drive["DeviceID"].ToString();
                                            VolumeLabel[i] = Label;
                                            Dispatcher.Invoke(() =>
                                            {
                                                DriveCombo.Items.Add($"{VolumeLabel[i]} ({Drive[i]})");
                                            });
                                            i++;
                                        }
                                        if (string.Equals(Label, "MainOS", StringComparison.CurrentCultureIgnoreCase))
                                        {
                                            result = true;
                                            break;
                                        }
                                    }
                                }
                                if (string.Equals(Label, "MainOS", StringComparison.CurrentCultureIgnoreCase))
                                {
                                    result = true;
                                    break;
                                }
                            }
                        }
                        /*foreach(ManagementObject logical in new ManagementObjectSearcher(@"Select * From Win32_USBHub").Get())
                        {
                            MessageBox.Show(logical["Name"].ToString());
                        }*/
                    }
                    Dispatcher.Invoke(() =>
                    {
                        DeviceBlock.Text = $"Mass Storage Mode connected: ";
                        DriveCombo.SelectedIndex = 0;
                        DriveCombo.Visibility = Visibility.Visible;
                        //new IsUsbDisconnected();
                    });
                }
                catch { }
            });
        }

        private void GetAssets()
        {
            if (Directory.GetFiles($@"{Drive[DriveCombo.SelectedIndex]}\Windows\System32", "UIXMobileAssets*.dll")[0] != null)
            {
                resourceFile = Path.GetFileName(Directory.GetFiles($@"{Drive[DriveCombo.SelectedIndex]}\Windows\System32", "UIXMobileAssets*.dll")[0]);
                res = Path.GetFileNameWithoutExtension(resourceFile).Substring(15);
            }
            else
            {
                MessageBox.Show("Missing the resource file.", "Error", MessageBoxButton.OK, MessageBoxImage.Error);
                return;
            }
            GetResourceFile("7z.exe", 1);
            Process getAssets = new Process();
            getAssets.StartInfo.FileName = @"C:\ProgramData\NavBar Tool\7z.exe";
            getAssets.StartInfo.Arguments = $@"e ""{Drive[DriveCombo.SelectedIndex]}\Windows\System32\{resourceFile}"" -spf -y -o""C:\ProgramData\NavBar Tool"" STEERINGWHEEL.BACK.*.PNG STEERINGWHEEL.SEARCH.*.PNG STEERINGWHEEL.START.*.PNG -r";
            getAssets.StartInfo.UseShellExecute = false;
            getAssets.StartInfo.CreateNoWindow = true;
            getAssets.Start();
            getAssets.WaitForExit();
            imgFiles = Directory.GetFiles(@"C:\ProgramData\NavBar Tool\.rsrc\RCDATA", "STEERINGWHEEL.*.PNG");
            Image backImg = new Image();
            backImg.Source = new BitmapImage(new Uri(imgFiles[0]));
            ButtonOne.Content = backImg;
            Image searchImg = new Image();
            searchImg.Source = new BitmapImage(new Uri(imgFiles[1]));
            ButtonThree.Content = searchImg;
            Image startImg = new Image();
            startImg.Source = new BitmapImage(new Uri(imgFiles[2]));
            ButtonTwo.Content = startImg;
        }

        private void GetRegestry()
        {
            if (string.Join(" ", Registry.LocalMachine.GetSubKeyNames()).Contains("LSOFTWARE"))
            {
                RegUnLoadKey(new IntPtr(unchecked((int)0x80000002)), "LSOFTWARE");
            }
            RegLoadKey(new IntPtr(unchecked((int)0x80000002)), "LSOFTWARE", $@"{Drive[DriveCombo.SelectedIndex]}\Windows\System32\Config\SOFTWARE");
            if (Registry.GetValue($@"HKEY_LOCAL_MACHINE\LSOFTWARE\Microsoft\Shell\NavigationBar", "SoftwareModeEnabled", null) == null || Convert.ToInt32(Registry.GetValue($@"HKEY_LOCAL_MACHINE\LSOFTWARE\Microsoft\Shell\NavigationBar", "SoftwareModeEnabled", null))  == 0)
            {
                Registry.SetValue($@"HKEY_LOCAL_MACHINE\LSOFTWARE\Microsoft\Shell\NavigationBar", "SoftwareModeEnabled", 0, RegistryValueKind.DWord);
                VirtualCombo.SelectedIndex = 0;
            }
            else
            {
                VirtualCombo.SelectedIndex = 1;
            }
            string defaultValues = string.Empty;
            for (int i = 0; i <= 2; i++)
            {
                defaultValues += Registry.GetValue($@"HKEY_LOCAL_MACHINE\LSOFTWARE\Microsoft\Shell\NavigationBar\{res}\{buttons[i]}", "Margins", null).ToString() + "\n";
                string[] margins = Registry.GetValue($@"HKEY_LOCAL_MACHINE\LSOFTWARE\Microsoft\Shell\NavigationBar\{res}\{buttons[i]}", "Margins", null).ToString().Split(',');
                if (i == 0 && margins[1] == "1")
                {
                    ComboOne.SelectedIndex = 1;
                }
                else if(i == 0 && margins[1] == "0")
                {
                    ComboOne.SelectedIndex = 0;
                }
                if (i == 1 && margins[1] == "1")
                {
                    ComboThree.SelectedIndex = 1;
                }
                else if (i == 1 && margins[1] == "0")
                {
                    ComboThree.SelectedIndex = 0;
                }
                if (i == 2 && margins[1] == "1")
                {
                    ComboTwo.SelectedIndex = 1;
                }
                else if (i == 2 && margins[1] == "0")
                {
                    ComboTwo.SelectedIndex = 0;
                }
            }
            if (!File.Exists($@"{Drive[DriveCombo.SelectedIndex]}\Windows\System32\NavBar_Tool.dat"))
            {
                File.AppendAllText($@"{Drive[DriveCombo.SelectedIndex]}\Windows\System32\NavBar_Tool.dat", defaultValues);
                File.AppendAllText($@"{Drive[DriveCombo.SelectedIndex]}\Windows\System32\NavBar_Tool.dat", Registry.GetValue($@"HKEY_LOCAL_MACHINE\LSOFTWARE\Microsoft\Shell\NavigationBar\{res}\Start", "Width", string.Empty).ToString());
            }
            RegUnLoadKey(new IntPtr(unchecked((int)0x80000002)), "LSOFTWARE");
            VirtualCombo.IsEnabled = true;
            ButtonOne.IsEnabled = true;
            ButtonTwo.IsEnabled = true;
            ButtonThree.IsEnabled = true;
            ComboOne.IsEnabled = true;
            ComboThree.IsEnabled = true;
            ComboTwo.IsEnabled = true;
            flag = true;
        }

        private void DriveCombo_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            GetAssets();
            GetRegestry();
            if (File.Exists($@"{Drive[DriveCombo.SelectedIndex]}\Windows\System32\{resourceFile}.bak") && File.Exists($@"{Drive[DriveCombo.SelectedIndex]}\Windows\System32\NavigationBar.reg.bak"))
            {
                ResetBtn.IsEnabled = true;
            }
        }

        private void ButtonsCombo_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            if (flag)
            {
                ApplyBtn.IsEnabled = true;
            }
        }

        private void ImgBtn_Click(object sender, RoutedEventArgs e)
        {
            var btn = sender as Button;
            Image img = (Image)btn.Content;
            openFileDialog = new OpenFileDialog();
            openFileDialog.Filter = "Image file (*.png)|*.png";
            if (openFileDialog.ShowDialog() == true)
            {
                var oldImg = BitmapFrame.Create(new Uri(img.Source.ToString()), BitmapCreateOptions.DelayCreation, BitmapCacheOption.None);
                var newImg = BitmapFrame.Create(new Uri(openFileDialog.FileName), BitmapCreateOptions.DelayCreation, BitmapCacheOption.None);
                if (Math.Round(Convert.ToDouble(oldImg.PixelWidth) / Convert.ToDouble(oldImg.PixelHeight), 3) != Math.Round(Convert.ToDouble(newImg.PixelWidth) / Convert.ToDouble(newImg.PixelHeight), 3))
                {
                    if (oldImg.Width == oldImg.Height)
                    {
                        MessageBox.Show($"Please select an image has 1:1 (square) dimension.", "Error", MessageBoxButton.OK, MessageBoxImage.Error);
                    }
                    else
                    {
                        MessageBox.Show($"Please select an image has {oldImg.Width}x{oldImg.Height} pixels.", "Error", MessageBoxButton.OK, MessageBoxImage.Error);
                    }
                    return;
                }
                var image = new Image();
                image.Source = new BitmapImage(new Uri(openFileDialog.FileName));
                btn.Content = image;
                ApplyBtn.IsEnabled = true;
            }
            else
            {
                return;
            }
            if (Path.GetFileName(img.Source.ToString()) == Path.GetFileName(imgFiles[0].ToString()))
            {
                backFile = openFileDialog.FileName;
            }
            else if (Path.GetFileName(img.Source.ToString()) == Path.GetFileName(imgFiles[1].ToString()))
            {
                searchFile = openFileDialog.FileName;
            }
            else if (Path.GetFileName(img.Source.ToString()) == Path.GetFileName(imgFiles[2].ToString()))
            {
                startFile = openFileDialog.FileName;
            }
        }

        private void ApplyBtn_Click(object sender, RoutedEventArgs e)
        {
            ApplyBtn.IsEnabled = false;
            if (!File.Exists($@"{Drive[DriveCombo.SelectedIndex]}\Windows\System32\{resourceFile}.bak"))
            {
                File.Copy($@"{Drive[DriveCombo.SelectedIndex]}\Windows\System32\{resourceFile}", $@"{Drive[DriveCombo.SelectedIndex]}\Windows\System32\{resourceFile}.bak", false);
            }
            File.Copy($@"{Drive[DriveCombo.SelectedIndex]}\Windows\System32\Config\SOFTWARE", $@"{Drive[DriveCombo.SelectedIndex]}\Windows\System32\Config\SOFTWARE.bak", true);
            ResetBtn.IsEnabled = true;
            IntPtr handleExe = BeginUpdateResource($@"{Drive[DriveCombo.SelectedIndex]}\Windows\System32\{resourceFile}", false);
            if (handleExe == null)
            {
                MessageBox.Show("Failed to read the resource file.", "Error", MessageBoxButton.OK, MessageBoxImage.Error);
                return;
            }
            if (backFile != null)
            {
                byte[] data = File.ReadAllBytes(backFile);
                UpdateResource(handleExe, 10, Path.GetFileName(imgFiles[0].ToString()), 1033, data, (uint)data.Length);
            }
            if (searchFile != null)
            {
                byte[] data = File.ReadAllBytes(searchFile);
                UpdateResource(handleExe, 10, Path.GetFileName(imgFiles[1].ToString()), 1033, data, (uint)data.Length);
            }
            if (startFile != null)
            {
                byte[] data = File.ReadAllBytes(startFile);
                UpdateResource(handleExe, 10, Path.GetFileName(imgFiles[2].ToString()), 1033, data, (uint)data.Length);
            }
            if (!EndUpdateResource(handleExe, false))
            {
                MessageBox.Show("Failed to write the image.", "Error", MessageBoxButton.OK, MessageBoxImage.Error);
                return;
            }
            RegLoadKey(new IntPtr(unchecked((int)0x80000002)), "LSOFTWARE", $@"{Drive[DriveCombo.SelectedIndex]}\Windows\System32\Config\SOFTWARE");
            if (!File.Exists($@"{Drive[DriveCombo.SelectedIndex]}\Windows\System32\NavigationBar.reg.bak"))
            {
                Process getReg = new Process();
                getReg.StartInfo.FileName = "Reg.exe";
                getReg.StartInfo.Arguments = $@"EXPORT HKEY_LOCAL_MACHINE\LSOFTWARE\Microsoft\Shell\NavigationBar {Drive[DriveCombo.SelectedIndex]}\Windows\System32\NavigationBar.reg.bak";
                getReg.StartInfo.UseShellExecute = false;
                getReg.StartInfo.CreateNoWindow = true;
                getReg.Start();
                getReg.WaitForExit();
            }
            Registry.SetValue($@"HKEY_LOCAL_MACHINE\LSOFTWARE\Microsoft\Shell\NavigationBar", "SoftwareModeEnabled", VirtualCombo.SelectedIndex);
            string[] defaultValues = File.ReadAllLines($@"{Drive[DriveCombo.SelectedIndex]}\Windows\System32\NavBar_Tool.dat");
            string[] backMargins = defaultValues[0].Split(',');
            string[] searchMargins = defaultValues[1].Split(',');
            string[] startMargins = defaultValues[2].Split(',');
            if(ComboOne.SelectedIndex == 0 && ComboTwo.SelectedIndex == 0 && ComboThree.SelectedIndex == 0)
            {
                Registry.SetValue($@"HKEY_LOCAL_MACHINE\LSOFTWARE\Microsoft\Shell\NavigationBar\{res}\{buttons[0]}", "Margins", $"{backMargins[0]},0,0,0");
                Registry.SetValue($@"HKEY_LOCAL_MACHINE\LSOFTWARE\Microsoft\Shell\NavigationBar\{res}\{buttons[1]}", "Margins", $"{searchMargins[0]},0,0,0");
                Registry.SetValue($@"HKEY_LOCAL_MACHINE\LSOFTWARE\Microsoft\Shell\NavigationBar\{res}\{buttons[2]}", "Margins", $"{startMargins[0]},0,0,0");
            }
            else if (ComboOne.SelectedIndex == 1 && ComboTwo.SelectedIndex == 0 && ComboThree.SelectedIndex == 0)
            {
                string newStartMargin = (Convert.ToInt32(startMargins[0]) + (Convert.ToInt32(startMargins[0]) / 2)).ToString();
                string newSearchMargin = (Convert.ToInt32(searchMargins[0]) + (Convert.ToInt32(startMargins[0]) / 2)).ToString();
                Registry.SetValue($@"HKEY_LOCAL_MACHINE\LSOFTWARE\Microsoft\Shell\NavigationBar\{res}\{buttons[0]}", "Margins", $"{backMargins[0]},1,0,0");
                Registry.SetValue($@"HKEY_LOCAL_MACHINE\LSOFTWARE\Microsoft\Shell\NavigationBar\{res}\{buttons[1]}", "Margins", $"{newSearchMargin},0,0,0");
                Registry.SetValue($@"HKEY_LOCAL_MACHINE\LSOFTWARE\Microsoft\Shell\NavigationBar\{res}\{buttons[2]}", "Margins", $"{newStartMargin},0,0,0");
            }
            else if (ComboOne.SelectedIndex == 0 && ComboTwo.SelectedIndex == 1 && ComboThree.SelectedIndex == 0)
            {
                string newBackMargin = (Convert.ToInt32(backMargins[0]) + (Convert.ToInt32(startMargins[0]) / 2)).ToString();
                string newSearchMargin = (Convert.ToInt32(searchMargins[0]) + (Convert.ToInt32(startMargins[0]) / 2)).ToString();
                Registry.SetValue($@"HKEY_LOCAL_MACHINE\LSOFTWARE\Microsoft\Shell\NavigationBar\{res}\{buttons[0]}", "Margins", $"{newBackMargin},0,0,0");
                Registry.SetValue($@"HKEY_LOCAL_MACHINE\LSOFTWARE\Microsoft\Shell\NavigationBar\{res}\{buttons[1]}", "Margins", $"{newSearchMargin},0,0,0");
                Registry.SetValue($@"HKEY_LOCAL_MACHINE\LSOFTWARE\Microsoft\Shell\NavigationBar\{res}\{buttons[2]}", "Margins", $"{startMargins[0]},1,0,0");
            }
            else if (ComboOne.SelectedIndex == 0 && ComboTwo.SelectedIndex == 0 && ComboThree.SelectedIndex == 1)
            {
                string newBackMargin = (Convert.ToInt32(backMargins[0]) + (Convert.ToInt32(searchMargins[0]) / 2)).ToString();
                string newStartMargin = (Convert.ToInt32(startMargins[0]) + (Convert.ToInt32(searchMargins[0]) / 2)).ToString();
                Registry.SetValue($@"HKEY_LOCAL_MACHINE\LSOFTWARE\Microsoft\Shell\NavigationBar\{res}\{buttons[0]}", "Margins", $"{newBackMargin},0,0,0");
                Registry.SetValue($@"HKEY_LOCAL_MACHINE\LSOFTWARE\Microsoft\Shell\NavigationBar\{res}\{buttons[1]}", "Margins", $"{searchMargins[0]},1,0,0");
                Registry.SetValue($@"HKEY_LOCAL_MACHINE\LSOFTWARE\Microsoft\Shell\NavigationBar\{res}\{buttons[2]}", "Margins", $"{newStartMargin},0,0,0");
            }
            else if (ComboOne.SelectedIndex == 1 && ComboTwo.SelectedIndex == 1 && ComboThree.SelectedIndex == 0)
            {
                string newSearchMargin = (Convert.ToInt32(backMargins[0]) + Convert.ToInt32(startMargins[0]) + Convert.ToInt32(defaultValues[3])).ToString();
                Registry.SetValue($@"HKEY_LOCAL_MACHINE\LSOFTWARE\Microsoft\Shell\NavigationBar\{res}\{buttons[0]}", "Margins", $"{backMargins[0]},1,0,0");
                Registry.SetValue($@"HKEY_LOCAL_MACHINE\LSOFTWARE\Microsoft\Shell\NavigationBar\{res}\{buttons[1]}", "Margins", $"{newSearchMargin},0,0,0");
                Registry.SetValue($@"HKEY_LOCAL_MACHINE\LSOFTWARE\Microsoft\Shell\NavigationBar\{res}\{buttons[2]}", "Margins", $"{startMargins[0]},1,0,0");
            }
            else if (ComboOne.SelectedIndex == 1 && ComboTwo.SelectedIndex == 0 && ComboThree.SelectedIndex == 1)
            {
                string newStarthMargin = (Convert.ToInt32(backMargins[0]) + Convert.ToInt32(startMargins[0]) + Convert.ToInt32(defaultValues[3])).ToString();
                Registry.SetValue($@"HKEY_LOCAL_MACHINE\LSOFTWARE\Microsoft\Shell\NavigationBar\{res}\{buttons[0]}", "Margins", $"{backMargins[0]},1,0,0");
                Registry.SetValue($@"HKEY_LOCAL_MACHINE\LSOFTWARE\Microsoft\Shell\NavigationBar\{res}\{buttons[1]}", "Margins", $"{searchMargins[0]},1,0,0");
                Registry.SetValue($@"HKEY_LOCAL_MACHINE\LSOFTWARE\Microsoft\Shell\NavigationBar\{res}\{buttons[2]}", "Margins", $"{newStarthMargin},0,0,0");
            }
            else if (ComboOne.SelectedIndex == 0 && ComboTwo.SelectedIndex == 1 && ComboThree.SelectedIndex == 1)
            {
                string newBackMargin = (Convert.ToInt32(backMargins[0]) + Convert.ToInt32(startMargins[0]) + Convert.ToInt32(defaultValues[3])).ToString();
                Registry.SetValue($@"HKEY_LOCAL_MACHINE\LSOFTWARE\Microsoft\Shell\NavigationBar\{res}\{buttons[0]}", "Margins", $"{newBackMargin},0,0,0");
                Registry.SetValue($@"HKEY_LOCAL_MACHINE\LSOFTWARE\Microsoft\Shell\NavigationBar\{res}\{buttons[1]}", "Margins", $"{searchMargins[0]},1,0,0");
                Registry.SetValue($@"HKEY_LOCAL_MACHINE\LSOFTWARE\Microsoft\Shell\NavigationBar\{res}\{buttons[2]}", "Margins", $"{startMargins[0]},1,0,0");
            }
            else if (ComboOne.SelectedIndex == 1 && ComboTwo.SelectedIndex == 1 && ComboThree.SelectedIndex == 1)
            {
                Registry.SetValue($@"HKEY_LOCAL_MACHINE\LSOFTWARE\Microsoft\Shell\NavigationBar\{res}\{buttons[0]}", "Margins", $"{backMargins[0]},1,0,0");
                Registry.SetValue($@"HKEY_LOCAL_MACHINE\LSOFTWARE\Microsoft\Shell\NavigationBar\{res}\{buttons[1]}", "Margins", $"{searchMargins[0]},1,0,0");
                Registry.SetValue($@"HKEY_LOCAL_MACHINE\LSOFTWARE\Microsoft\Shell\NavigationBar\{res}\{buttons[2]}", "Margins", $"{startMargins[0]},1,0,0");
            }
            RegUnLoadKey(new IntPtr(unchecked((int)0x80000002)), "LSOFTWARE");
        }

        private void ResetBtn_Click(object sender, RoutedEventArgs e)
        {
            ResetBtn.IsEnabled = false;
            //ApplyBtn.IsEnabled = false;
            File.Copy($@"{Drive[DriveCombo.SelectedIndex]}\Windows\System32\{resourceFile}.bak", $@"{Drive[DriveCombo.SelectedIndex]}\Windows\System32\{resourceFile}", true);
            RegLoadKey(new IntPtr(unchecked((int)0x80000002)), "LSOFTWARE", $@"{Drive[DriveCombo.SelectedIndex]}\Windows\System32\Config\SOFTWARE");
            Process getReg = new Process();
            getReg.StartInfo.FileName = "Reg.exe";
            getReg.StartInfo.Arguments = $@"IMPORT {Drive[DriveCombo.SelectedIndex]}\Windows\System32\NavigationBar.reg.bak";
            getReg.StartInfo.UseShellExecute = false;
            getReg.StartInfo.CreateNoWindow = true;
            getReg.Start();
            getReg.WaitForExit();
            RegUnLoadKey(new IntPtr(unchecked((int)0x80000002)), "LSOFTWARE");
            /*flag = false;
            GetAssets();
            GetRegestry();*/
            Process.Start(Application.ResourceAssembly.Location);
            Application.Current.Shutdown();
        }

        public static void getPriviledge()
        {
            try
            {
                using (AccessTokenHandle accessTokenHandle =
                    Process.GetCurrentProcess().GetAccessTokenHandle(
                        TokenAccessRights.AdjustPrivileges | TokenAccessRights.Query))
                {
                    AdjustPrivilegeResult backupResult = accessTokenHandle.EnablePrivilege(Privilege.Backup);
                    AdjustPrivilegeResult restoreResult = accessTokenHandle.EnablePrivilege(Privilege.Restore);
                }
            }
            catch (Exception ex) { }
        }

        public static string GetResourceFile(string resourceName, int dump)
        {
            var embeddedResource = Assembly.GetExecutingAssembly().GetManifestResourceNames().Where(s => s.Contains(resourceName)).ToArray();
            if (!string.IsNullOrWhiteSpace(embeddedResource[0]))
            {
                using (var stream = Assembly.GetExecutingAssembly().GetManifestResourceStream(embeddedResource[0]))
                {
                    if (dump == 0)
                    {
                        using (StreamReader reader = new StreamReader(stream))
                        {
                            string result = reader.ReadToEnd();
                            return result;
                        }
                    }
                    else
                    {
                        var data = new byte[stream.Length];
                        stream.Read(data, 0, data.Length);
                        File.WriteAllBytes($@"C:\ProgramData\NavBar Tool\{resourceName}", data);
                    }
                }
            }
            return null;
        }

        private void AboutBtn_Click(object sender, RoutedEventArgs e)
        {
            MessageBox.Show($"NavBar Tool\nVersion {FileVersionInfo.GetVersionInfo(Assembly.GetExecutingAssembly().Location).FileVersion}\nDeveloped by Fadil Fadz (fadilfadz01)\n\nCopyright © 2022\n\nhttps://github.com/fadilfadz01/NavBar_Tool", "About");
        }

        /*private bool AdminRelauncher()
        {
            if (!IsRunAsAdmin())
            {
                ProcessStartInfo proc = new ProcessStartInfo();
                proc.UseShellExecute = true;
                proc.WorkingDirectory = Environment.CurrentDirectory;
                proc.FileName = Assembly.GetEntryAssembly().CodeBase;

                proc.Verb = "runas";

                try
                {
                    Process.Start(proc);
                    Application.Current.Shutdown();
                }
                catch (Exception ex)
                {
                    return false;
                }
            }
            return true;
        }

        private bool IsRunAsAdmin()
        {
            try
            {
                WindowsIdentity id = WindowsIdentity.GetCurrent();
                WindowsPrincipal principal = new WindowsPrincipal(id);
                return principal.IsInRole(WindowsBuiltInRole.Administrator);
            }
            catch (Exception)
            {
                return false;
            }
        }*/
    }

    /*public class IsUsbDisconnected
    {
        public IsUsbDisconnected()
        {
            Check();
        }

        public async void Check()
        {
            while (Directory.Exists("F:"))
            {
                await Task.Delay(200);
            }
            MainWindow mainWindow = new MainWindow();
            mainWindow.DeviceBlock.Text = "Waiting for device to connect into Mass Storage Mode...";
            VirtualCombo.IsEnabled = false;
            ComboOne.IsEnabled = false;
            ComboThree.IsEnabled = false;
            ComboTwo.IsEnabled = false;
            ComboOne.SelectedIndex = -1;
            ComboTwo.SelectedIndex = -1;
            ComboThree.SelectedIndex = -1;
            ButtonOne.IsEnabled = false;
            ButtonTwo.IsEnabled = false;
            ButtonThree.IsEnabled = false;
            ButtonOne.Content = null;
            ButtonTwo.Content = null;
            ButtonThree.Content = null;
        }
    }*/
}

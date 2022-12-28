using Microsoft.Win32;
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
        static extern int UpdateResource(IntPtr hUpdate, uint lpType, string lpName, ushort wLanguage, /*IntPtr*/byte[] lpData, uint cbData);

        [DllImport("kernel32.dll", SetLastError = true)]
        static extern IntPtr BeginUpdateResource(string pFileName, [MarshalAs(UnmanagedType.Bool)]bool bDeleteExistingResources);

        [DllImport("kernel32.dll", SetLastError = true)]
        static extern bool EndUpdateResource(IntPtr hUpdate, bool fDiscard);

        string Drive;
        string PhysicalDrive;
        string VolumeLabel;
        bool result = false;
        OpenFileDialog openFileDialog;
        string backFile = null;
        string searchFile = null;
        string startFile = null;
        string resourceFile;
        string[] imgFiles;

        public MainWindow()
        {
            InitializeComponent();
            if (!AdminRelauncher())
            {
                Application.Current.Shutdown();
            }
            _ = StartProg();
        }

        private async Task StartProg()
        {
            Directory.CreateDirectory(@"C:\ProgramData\NavBar Tool\Backups");
            //await GetDevice();
            Drive = @"C:\Users\Fadil Fadz\Desktop";
            DeviceBlock.Text = $@"Mass Storage Mode connected: MainOS (E:)";
            GetAssets();
        }

        private async Task GetDevice()
        {
            await Task.Run(() =>
            {
                try
                {
                    while (!result)
                    {
                        foreach (ManagementObject logical in new ManagementObjectSearcher("select * from Win32_LogicalDisk").Get())
                        {
                            System.Diagnostics.Debug.Print(logical["Name"].ToString());

                            string Label = "";
                            foreach (ManagementObject partition in logical.GetRelated("Win32_DiskPartition"))
                            {
                                foreach (ManagementObject drive in partition.GetRelated("Win32_DiskDrive"))
                                {
                                    if (drive["PNPDeviceID"].ToString().Contains("VEN_QUALCOMM&PROD_MMC_STORAGE") || drive["PNPDeviceID"].ToString().Contains("VEN_MSFT&PROD_PHONE_MMC_STOR"))
                                    {
                                        Label = logical["VolumeName"] == null ? "" : logical["VolumeName"].ToString();
                                        if ((Drive == null) || string.Equals(Label, "MainOS", StringComparison.CurrentCultureIgnoreCase)) // Always prefer the MainOS drive-mapping
                                        {
                                            Drive = logical["Name"].ToString();
                                            PhysicalDrive = drive["DeviceID"].ToString();
                                            VolumeLabel = Label;
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
                    }
                    Dispatcher.Invoke(() =>
                    {
                        DeviceBlock.Text = $"Mass Storage Mode connected: {VolumeLabel} ({Drive})";
                    });
                }
                catch { }
            });
        }

        private void GetAssets()
        {
            if (Directory.GetFiles($@"{Drive}\Windows\System32", "UIXMobileAssets*.dll")[0] != null)
            {
                resourceFile = Path.GetFileName(Directory.GetFiles($@"{Drive}\Windows\System32", "UIXMobileAssets*.dll")[0]);
            }
            else
            {
                MessageBox.Show("Missing the resource file.", "Error", MessageBoxButton.OK, MessageBoxImage.Error);
                return;
            }
            if (File.Exists($@"C:\ProgramData\NavBar Tool\Backups\{resourceFile}"))
            {
                RestoreBtn.IsEnabled = true;
            }
            GetResourceFile("7z.exe", 1);
            Process getAssets = new Process();
            getAssets.StartInfo.FileName = @"C:\ProgramData\NavBar Tool\7z.exe";
            getAssets.StartInfo.Arguments = $@"e ""{Drive}\Windows\System32\{resourceFile}"" -spf -y -o""C:\ProgramData\NavBar Tool"" STEERINGWHEEL.BACK.*.PNG STEERINGWHEEL.SEARCH.*.PNG STEERINGWHEEL.START.*.PNG -r";
            getAssets.StartInfo.UseShellExecute = false;
            getAssets.StartInfo.CreateNoWindow = true;
            getAssets.Start();
            getAssets.WaitForExit();
            imgFiles = Directory.GetFiles(@"C:\ProgramData\NavBar Tool\.rsrc\RCDATA", "STEERINGWHEEL.*.PNG");
            Image backImg = new Image();
            backImg.Source = new BitmapImage(new Uri(imgFiles[0]));
            BackBtn.Content = backImg;
            BackBtn.IsEnabled = true;
            Image searchImg = new Image();
            searchImg.Source = new BitmapImage(new Uri(imgFiles[1]));
            SearchBtn.Content = searchImg;
            SearchBtn.IsEnabled = true;
            Image startImg = new Image();
            startImg.Source = new BitmapImage(new Uri(imgFiles[2]));
            StartBtn.Content = startImg;
            StartBtn.IsEnabled = true;
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
                if (Math.Round(Convert.ToDouble(newImg.PixelWidth) / Convert.ToDouble(newImg.PixelHeight), 3) != Math.Round(Convert.ToDouble(oldImg.PixelWidth) / Convert.ToDouble(oldImg.PixelHeight), 3))
                {
                    MessageBox.Show($"Please select an image has {oldImg.Width}x{oldImg.Height} pixels.", "Error", MessageBoxButton.OK, MessageBoxImage.Error);
                    return;
                }
                var image = new Image();
                image.Source = new BitmapImage(new Uri(openFileDialog.FileName));
                btn.Content = image;
                ApplyBtn.IsEnabled = true;
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
            if (!File.Exists($@"C:\ProgramData\NavBar Tool\Backups\{resourceFile}"))
            {
                File.Copy($@"{Drive}\Windows\System32\{resourceFile}", $@"C:\ProgramData\NavBar Tool\Backups\{resourceFile}", true);
            }
            RestoreBtn.IsEnabled = true;
            IntPtr handleExe = BeginUpdateResource($@"{Drive}\Windows\System32\{resourceFile}", false);
            if (handleExe == null)
            {
                MessageBox.Show("Failed to read the resource file.", "Error", MessageBoxButton.OK, MessageBoxImage.Error);
                return;
            }
            /*CultureInfo currentCulture = CultureInfo.CurrentCulture;
            int pid = ((ushort)currentCulture.LCID) & 0x3ff;
            int sid = ((ushort)currentCulture.LCID) >> 10;
            ushort languageID = (ushort)((((ushort)pid) << 10) | ((ushort)sid));*/
            if (backFile != null)
            {
                byte[] data = File.ReadAllBytes(backFile);
                UpdateResource(handleExe, 10, Path.GetFileName(imgFiles[0].ToString()), /*languageID*/1033, data, (uint)data.Length);
            }
            if (searchFile != null)
            {
                byte[] data = File.ReadAllBytes(searchFile);
                UpdateResource(handleExe, 10, Path.GetFileName(imgFiles[1].ToString()), /*languageID*/1033, data, (uint)data.Length);
            }
            if (startFile != null)
            {
                byte[] data = File.ReadAllBytes(startFile);
                UpdateResource(handleExe, 10, Path.GetFileName(imgFiles[2].ToString()), /*languageID*/1033, data, (uint)data.Length);
            }
            //GCHandle iconHandle = GCHandle.Alloc(data, GCHandleType.Pinned);
            if (!EndUpdateResource(handleExe, false))
            {
                MessageBox.Show("Failed to write the image.", "Error", MessageBoxButton.OK, MessageBoxImage.Error);
                return;
            }
            ApplyBtn.IsEnabled = false;
        }

        private void RestoreBtn_Click(object sender, RoutedEventArgs e)
        {
            RestoreBtn.IsEnabled = false;
            File.Copy($@"C:\ProgramData\NavBar Tool\Backups\{resourceFile}", $@"{Drive}\Windows\System32\{resourceFile}", true);
            //MessageBox.Show("Successfully restored the default resource.", "Done", MessageBoxButton.OK, MessageBoxImage.Exclamation);
        }

        private bool AdminRelauncher()
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
    }
}

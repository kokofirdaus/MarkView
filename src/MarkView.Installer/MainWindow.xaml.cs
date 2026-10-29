using System;
using System.Diagnostics;
using System.IO;
using System.Reflection;
using System.Runtime.InteropServices;
using System.Threading.Tasks;
using System.Windows;
using Microsoft.Win32;

namespace MarkView.Installer
{
    public partial class MainWindow : Window
    {
        [DllImport("dwmapi.dll")]
        private static extern int DwmSetWindowAttribute(IntPtr hwnd, int attr, ref int attrValue, int attrSize);

        private readonly string _installDir;
        private readonly string _targetExePath;
        private bool _isFinished = false;

        public MainWindow()
        {
            InitializeComponent();

            SourceInitialized += (s, e) =>
            {
                var helper = new System.Windows.Interop.WindowInteropHelper(this);
                if (helper.Handle != IntPtr.Zero)
                {
                    int darkMode = 1;
                    DwmSetWindowAttribute(helper.Handle, 20, ref darkMode, sizeof(int));
                    DwmSetWindowAttribute(helper.Handle, 19, ref darkMode, sizeof(int));
                }
            };

            _installDir = Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData), "Programs", "MarkView");
            _targetExePath = Path.Combine(_installDir, "MarkView.exe");
            TxtPath.Text = _installDir;
        }

        private async void BtnInstall_Click(object sender, RoutedEventArgs e)
        {
            if (_isFinished)
            {
                if (ChkLaunchAfter.IsChecked == true && File.Exists(_targetExePath))
                {
                    Process.Start(new ProcessStartInfo(_targetExePath) { UseShellExecute = true });
                }
                Close();
                return;
            }

            PnlConfig.Visibility = Visibility.Collapsed;
            PnlProgress.Visibility = Visibility.Visible;
            BtnInstall.IsEnabled = false;
            BtnCancel.IsEnabled = false;

            bool success = await Task.Run(() => PerformInstallation());

            BtnInstall.IsEnabled = true;
            PrgBar.Visibility = Visibility.Collapsed;

            if (success)
            {
                TxtStatus.Text = "🎉 MarkView berhasil terpasang di komputer Anda!";
                ChkLaunchAfter.Visibility = Visibility.Visible;
                BtnInstall.Content = "Selesai";
                _isFinished = true;
            }
            else
            {
                TxtStatus.Text = "❌ Terjadi kegagalan saat memasang MarkView.";
                BtnCancel.IsEnabled = true;
            }
        }

        private bool PerformInstallation()
        {
            try
            {
                // 1. Create target directory
                Directory.CreateDirectory(_installDir);

                // 2. Extract embedded payload binary
                var asm = Assembly.GetExecutingAssembly();
                string[] resourceNames = asm.GetManifestResourceNames();
                
                string? matchedName = null;
                foreach (var name in resourceNames)
                {
                    if (name.EndsWith(".bin", StringComparison.OrdinalIgnoreCase) || name.EndsWith("MarkView.exe", StringComparison.OrdinalIgnoreCase))
                    {
                        matchedName = name;
                        break;
                    }
                }

                if (string.IsNullOrEmpty(matchedName))
                {
                    throw new FileNotFoundException("Payload MarkView tidak ditemukan di dalam paket installer.");
                }

                using (var stream = asm.GetManifestResourceStream(matchedName))
                {
                    if (stream == null) throw new InvalidOperationException("Gagal membaca stream resource.");
                    using (var fs = new FileStream(_targetExePath, FileMode.Create, FileAccess.Write, FileShare.None))
                    {
                        stream.CopyTo(fs);
                    }
                }

                // 3. Create Shortcuts
                bool createDesktop = false;
                bool createStartMenu = false;
                bool registerAssociations = false;

                Dispatcher.Invoke(() =>
                {
                    createDesktop = ChkDesktopShortcut.IsChecked == true;
                    createStartMenu = ChkStartMenuShortcut.IsChecked == true;
                    registerAssociations = ChkFileAssociation.IsChecked == true;
                });

                if (createDesktop || createStartMenu)
                {
                    Type? shellType = Type.GetTypeFromProgID("WScript.Shell");
                    if (shellType != null)
                    {
                        dynamic? shell = Activator.CreateInstance(shellType);
                        if (shell != null)
                        {
                            if (createDesktop)
                            {
                                string desktop = Environment.GetFolderPath(Environment.SpecialFolder.Desktop);
                                dynamic shortcut = shell.CreateShortcut(Path.Combine(desktop, "MarkView.lnk"));
                                shortcut.TargetPath = _targetExePath;
                                shortcut.WorkingDirectory = _installDir;
                                shortcut.Description = "MarkView - Modern Windows Markdown Viewer & Editor";
                                shortcut.Save();
                            }

                            if (createStartMenu)
                            {
                                string startMenu = Environment.GetFolderPath(Environment.SpecialFolder.Programs);
                                dynamic shortcut = shell.CreateShortcut(Path.Combine(startMenu, "MarkView.lnk"));
                                shortcut.TargetPath = _targetExePath;
                                shortcut.WorkingDirectory = _installDir;
                                shortcut.Description = "MarkView - Modern Windows Markdown Viewer & Editor";
                                shortcut.Save();
                            }
                        }
                    }
                }

                // 4. Register File Association (.md, .markdown)
                if (registerAssociations)
                {
                    RegisterFileAssociation(".md", "MarkView.Document", "Markdown Document", _targetExePath);
                    RegisterFileAssociation(".markdown", "MarkView.Document", "Markdown Document", _targetExePath);
                }

                return true;
            }
            catch (Exception ex)
            {
                Dispatcher.Invoke(() =>
                {
                    MessageBox.Show($"Error instalasi: {ex.Message}", "Gagal Pasang", MessageBoxButton.OK, MessageBoxImage.Error);
                });
                return false;
            }
        }

        private static void RegisterFileAssociation(string extension, string progId, string description, string appPath)
        {
            try
            {
                using var keyProgId = Registry.CurrentUser.CreateSubKey($@"Software\Classes\{progId}");
                keyProgId?.SetValue("", description);

                using var keyIcon = Registry.CurrentUser.CreateSubKey($@"Software\Classes\{progId}\DefaultIcon");
                keyIcon?.SetValue("", $"{appPath},0");

                using var keyCommand = Registry.CurrentUser.CreateSubKey($@"Software\Classes\{progId}\shell\open\command");
                keyCommand?.SetValue("", $"\"{appPath}\" \"%1\"");

                using var keyExt = Registry.CurrentUser.CreateSubKey($@"Software\Classes\{extension}");
                keyExt?.SetValue("", progId);
            }
            catch
            {
                // Skip if registry write permission is restricted
            }
        }

        private void BtnCancel_Click(object sender, RoutedEventArgs e)
        {
            Close();
        }
    }
}

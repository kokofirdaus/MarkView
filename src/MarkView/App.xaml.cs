using System;
using System.IO;
using System.Windows;
using System.Windows.Threading;
using MarkView.Services;
using MarkView.ViewModels;
using MarkView.Views;

namespace MarkView
{
    public partial class App : Application
    {
        protected override void OnStartup(StartupEventArgs e)
        {
            base.OnStartup(e);

            // Setup global crash handler for debugging stability
            AppDomain.CurrentDomain.UnhandledException += (s, args) =>
            {
                LogCrash(args.ExceptionObject as Exception);
            };

            DispatcherUnhandledException += (s, args) =>
            {
                LogCrash(args.Exception);
                MessageBox.Show($"Terjadi kesalahan sistem yang tidak terduga:\n\n{args.Exception?.Message}\n\nDetail disimpan di crash log.", 
                    "MarkView Error", MessageBoxButton.OK, MessageBoxImage.Error);
                args.Handled = true;
            };

            try
            {
                var fileService = new FileService();
                var settingsService = new SettingsService();
                var themeService = new ThemeService();

                var mainViewModel = new MainViewModel(fileService, settingsService, themeService);

                // Handle CLI arguments (file association / double click from Windows Explorer)
                if (e.Args.Length > 0 && File.Exists(e.Args[0]))
                {
                    mainViewModel.OpenFileFromPath(e.Args[0]);
                }

                var mainWindow = new MainWindow(mainViewModel);
                mainWindow.Show();
            }
            catch (Exception ex)
            {
                LogCrash(ex);
                MessageBox.Show($"Gagal memulai MarkView:\n\n{ex.Message}\n\n{ex.StackTrace}", 
                    "MarkView Startup Failure", MessageBoxButton.OK, MessageBoxImage.Error);
                Shutdown(-1);
            }
        }

        private static void LogCrash(Exception? ex)
        {
            if (ex == null) return;
            try
            {
                string dir = Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData), "MarkView");
                Directory.CreateDirectory(dir);
                string crashFile = Path.Combine(dir, "crash.log");
                string content = $"[{DateTime.Now:yyyy-MM-dd HH:mm:ss}] Unhandled Exception:\n{ex}\n\n";
                File.AppendAllText(crashFile, content);
            }
            catch
            {
                // Fallback: silent if disk write fails
            }
        }
    }
}

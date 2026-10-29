using System;
using System.ComponentModel;
using System.IO;
using System.Windows;
using MarkView.Helpers;
using MarkView.ViewModels;

namespace MarkView.Views
{
    public partial class MainWindow : Window
    {
        private readonly MainViewModel _viewModel;

        public MainWindow(MainViewModel viewModel)
        {
            InitializeComponent();
            _viewModel = viewModel;
            DataContext = _viewModel;

            SourceInitialized += (s, e) => WindowHelper.ApplyWindowTheme(this, _viewModel.CurrentTheme);
            _viewModel.PropertyChanged += (s, e) =>
            {
                if (e.PropertyName == nameof(_viewModel.CurrentTheme))
                {
                    WindowHelper.ApplyWindowTheme(this, _viewModel.CurrentTheme);
                }
            };

            // Restore maximized state properly if set
            Loaded += (s, e) =>
            {
                if (_viewModel.IsMaximized)
                {
                    WindowState = WindowState.Maximized;
                }
                WindowHelper.ApplyWindowTheme(this, _viewModel.CurrentTheme);
            };
        }

        private void MenuItemExit_Click(object sender, RoutedEventArgs e)
        {
            Close();
        }

        private void Window_Closing(object sender, CancelEventArgs e)
        {
            if (!_viewModel.PromptSaveIfModified())
            {
                e.Cancel = true;
                return;
            }

            // Persist current window state & preferences accurately
            if (WindowState == WindowState.Maximized)
            {
                _viewModel.IsMaximized = true;
                _viewModel.WindowWidth = RestoreBounds.Width;
                _viewModel.WindowHeight = RestoreBounds.Height;
                _viewModel.WindowLeft = RestoreBounds.Left;
                _viewModel.WindowTop = RestoreBounds.Top;
            }
            else
            {
                _viewModel.IsMaximized = false;
                _viewModel.WindowWidth = Width;
                _viewModel.WindowHeight = Height;
                _viewModel.WindowLeft = Left;
                _viewModel.WindowTop = Top;
            }

            _viewModel.SaveSettings();
        }

        private void Window_DragOver(object sender, DragEventArgs e)
        {
            if (e.Data.GetDataPresent(DataFormats.FileDrop))
            {
                var files = (string[]?)e.Data.GetData(DataFormats.FileDrop);
                if (files != null && files.Length > 0)
                {
                    string ext = Path.GetExtension(files[0]).ToLowerInvariant();
                    if (ext == ".md" || ext == ".txt" || ext == ".markdown")
                    {
                        e.Effects = DragDropEffects.Copy;
                        e.Handled = true;
                        return;
                    }
                }
            }

            e.Effects = DragDropEffects.None;
            e.Handled = true;
        }

        private void Window_Drop(object sender, DragEventArgs e)
        {
            if (e.Data.GetDataPresent(DataFormats.FileDrop))
            {
                var files = (string[]?)e.Data.GetData(DataFormats.FileDrop);
                if (files != null && files.Length > 0)
                {
                    string filePath = files[0];
                    string ext = Path.GetExtension(filePath).ToLowerInvariant();
                    if (ext == ".md" || ext == ".txt" || ext == ".markdown")
                    {
                        _viewModel.ExecuteOpenPath(filePath);
                    }
                    else
                    {
                        MessageBox.Show("MarkView hanya mendukung pembukaan file berekstensi .md atau .txt", "Format Tidak Didukung", MessageBoxButton.OK, MessageBoxImage.Warning);
                    }
                }
            }
        }
    }
}

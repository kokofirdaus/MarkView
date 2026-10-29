using System;
using System.Collections.ObjectModel;
using System.IO;
using System.Text.RegularExpressions;
using System.Windows;
using System.Windows.Input;
using System.Windows.Threading;
using MarkView.Commands;
using MarkView.Enums;
using MarkView.Models;
using MarkView.Services.Interfaces;
using MarkView.Views;

namespace MarkView.ViewModels
{
    public class MainViewModel : ViewModelBase
    {
        private static readonly Regex WordRegex = new(@"[\p{L}\p{N}]+", RegexOptions.Compiled);

        private readonly IFileService _fileService;
        private readonly ISettingsService _settingsService;
        private readonly IThemeService _themeService;
        private readonly DispatcherTimer _debounceTimer;

        private string? _currentFilePath;
        private bool _hasFile;
        private bool _isModified;
        private string _fileEncoding = "UTF-8";
        private string _rawMarkdown = string.Empty;
        private string _previewMarkdown = string.Empty;

        private ViewMode _currentViewMode = ViewMode.Split;
        private GridLength _editorColumnWidth = new(1, GridUnitType.Star);
        private GridLength _splitterColumnWidth = new(5);
        private GridLength _previewColumnWidth = new(1, GridUnitType.Star);
        private double _editorFontSize = 14.0;
        private bool _wordWrap = true;
        private bool _showLineNumbers = true;

        private int _caretLine = 1;
        private int _caretColumn = 1;
        private int _wordCount = 0;

        private bool _isFindBarVisible = false;
        private string _findQuery = string.Empty;

        private double _windowWidth = 1200.0;
        private double _windowHeight = 800.0;
        private double _windowLeft = 100.0;
        private double _windowTop = 100.0;
        private bool _isMaximized = false;

        public event Action? RequestFind;

        public MainViewModel(IFileService fileService, ISettingsService settingsService, IThemeService themeService)
        {
            _fileService = fileService;
            _settingsService = settingsService;
            _themeService = themeService;

            RecentFiles = new ObservableCollection<RecentFile>();

            // Setup 300ms Debounce Timer for Preview Rendering
            _debounceTimer = new DispatcherTimer
            {
                Interval = TimeSpan.FromMilliseconds(300)
            };
            _debounceTimer.Tick += (s, e) =>
            {
                _debounceTimer.Stop();
                PreviewMarkdown = RawMarkdown;
                UpdateWordCount();
            };

            // Commands Initialization
            NewFileCommand = new RelayCommand(ExecuteNewFile);
            OpenFileCommand = new RelayCommand(ExecuteOpenFile);
            OpenRecentFileCommand = new RelayCommand(param =>
            {
                if (param is string path)
                {
                    ExecuteOpenPath(path);
                }
            });
            SaveFileCommand = new RelayCommand(ExecuteSaveFile, () => HasFile);
            SaveAsFileCommand = new RelayCommand(ExecuteSaveAsFile, () => HasFile);
            ExportPdfCommand = new RelayCommand(ExecuteExportPdf, () => HasFile);
            PrintCommand = new RelayCommand(ExecuteExportPdf, () => HasFile);

            ToggleThemeCommand = new RelayCommand(ExecuteToggleTheme);
            SetViewModeCommand = new RelayCommand(param =>
            {
                if (param is string modeStr && Enum.TryParse<ViewMode>(modeStr, true, out var mode))
                {
                    CurrentViewMode = mode;
                    SaveSettings();
                }
                else if (param is ViewMode vMode)
                {
                    CurrentViewMode = vMode;
                    SaveSettings();
                }
            });
            ToggleFindBarCommand = new RelayCommand(ExecuteToggleFindBar);
            ToggleWordWrapCommand = new RelayCommand(() =>
            {
                WordWrap = !WordWrap;
                SaveSettings();
            });
            ToggleLineNumbersCommand = new RelayCommand(() =>
            {
                ShowLineNumbers = !ShowLineNumbers;
                SaveSettings();
            });
            ZoomInCommand = new RelayCommand(() =>
            {
                if (EditorFontSize < 28.0)
                {
                    EditorFontSize += 1.0;
                    SaveSettings();
                }
            });
            ZoomOutCommand = new RelayCommand(() =>
            {
                if (EditorFontSize > 9.0)
                {
                    EditorFontSize -= 1.0;
                    SaveSettings();
                }
            });
            ResetZoomCommand = new RelayCommand(() =>
            {
                EditorFontSize = 14.0;
                SaveSettings();
            });
            ShowAboutCommand = new RelayCommand(ExecuteShowAbout);

            // Load and apply saved settings
            LoadSettings();
        }

        #region Properties

        public string? CurrentFilePath
        {
            get => _currentFilePath;
            set
            {
                if (SetField(ref _currentFilePath, value))
                {
                    OnPropertyChanged(nameof(CurrentFileName));
                    OnPropertyChanged(nameof(WindowTitle));
                }
            }
        }

        public string CurrentFileName => string.IsNullOrEmpty(CurrentFilePath) ? "Tanpa Judul.md" : Path.GetFileName(CurrentFilePath);

        public string WindowTitle => HasFile 
            ? $"{(IsModified ? "* " : "")}{CurrentFileName} — MarkView" 
            : "MarkView";

        public bool HasFile
        {
            get => _hasFile;
            set
            {
                if (SetField(ref _hasFile, value))
                {
                    OnPropertyChanged(nameof(WindowTitle));
                    OnPropertyChanged(nameof(CurrentSaveStatus));
                }
            }
        }

        public bool IsModified
        {
            get => _isModified;
            set
            {
                if (SetField(ref _isModified, value))
                {
                    OnPropertyChanged(nameof(CurrentSaveStatus));
                    OnPropertyChanged(nameof(WindowTitle));
                }
            }
        }

        public string FileEncoding
        {
            get => _fileEncoding;
            set => SetField(ref _fileEncoding, value);
        }

        public SaveStatus CurrentSaveStatus
        {
            get
            {
                if (!HasFile || string.IsNullOrEmpty(CurrentFilePath)) return SaveStatus.NewFile;
                return IsModified ? SaveStatus.Modified : SaveStatus.Saved;
            }
        }

        public string RawMarkdown
        {
            get => _rawMarkdown;
            set
            {
                if (SetField(ref _rawMarkdown, value))
                {
                    if (HasFile)
                    {
                        IsModified = true;
                    }
                    _debounceTimer.Stop();
                    _debounceTimer.Start();
                }
            }
        }

        public string PreviewMarkdown
        {
            get => _previewMarkdown;
            set => SetField(ref _previewMarkdown, value);
        }

        public string MarkdownStyleName => CurrentTheme == AppTheme.Dark ? "GithubLikeDark" : "GithubLike";

        public ViewMode CurrentViewMode
        {
            get => _currentViewMode;
            set
            {
                if (SetField(ref _currentViewMode, value))
                {
                    UpdateColumnWidths();
                }
            }
        }

        public Visibility EditorVisibility => CurrentViewMode == ViewMode.PreviewOnly ? Visibility.Collapsed : Visibility.Visible;

        public Visibility PreviewVisibility => CurrentViewMode == ViewMode.EditorOnly ? Visibility.Collapsed : Visibility.Visible;

        public Visibility SplitterVisibility => CurrentViewMode == ViewMode.Split ? Visibility.Visible : Visibility.Collapsed;

        public bool IsEditorOnly => CurrentViewMode == ViewMode.EditorOnly;
        public bool IsSplit => CurrentViewMode == ViewMode.Split;
        public bool IsPreviewOnly => CurrentViewMode == ViewMode.PreviewOnly;

        public GridLength SplitterColumnWidth
        {
            get => _splitterColumnWidth;
            set => SetField(ref _splitterColumnWidth, value);
        }

        public GridLength EditorColumnWidth
        {
            get => _editorColumnWidth;
            set => SetField(ref _editorColumnWidth, value);
        }

        public GridLength PreviewColumnWidth
        {
            get => _previewColumnWidth;
            set => SetField(ref _previewColumnWidth, value);
        }

        public double EditorFontSize
        {
            get => _editorFontSize;
            set => SetField(ref _editorFontSize, value);
        }

        public bool WordWrap
        {
            get => _wordWrap;
            set => SetField(ref _wordWrap, value);
        }

        public bool ShowLineNumbers
        {
            get => _showLineNumbers;
            set => SetField(ref _showLineNumbers, value);
        }

        public AppTheme CurrentTheme => _themeService.CurrentTheme;

        public int CaretLine
        {
            get => _caretLine;
            set
            {
                if (SetField(ref _caretLine, value))
                {
                    OnPropertyChanged(nameof(StatusCursorPositionText));
                }
            }
        }

        public int CaretColumn
        {
            get => _caretColumn;
            set
            {
                if (SetField(ref _caretColumn, value))
                {
                    OnPropertyChanged(nameof(StatusCursorPositionText));
                }
            }
        }

        public int WordCount
        {
            get => _wordCount;
            set
            {
                if (SetField(ref _wordCount, value))
                {
                    OnPropertyChanged(nameof(StatusWordCountText));
                }
            }
        }

        public string StatusWordCountText => $"{WordCount} kata";

        public string StatusCursorPositionText => $"Ln {CaretLine}, Col {CaretColumn}";

        public bool IsFindBarVisible
        {
            get => _isFindBarVisible;
            set => SetField(ref _isFindBarVisible, value);
        }

        public string FindQuery
        {
            get => _findQuery;
            set => SetField(ref _findQuery, value);
        }

        public double WindowWidth
        {
            get => _windowWidth;
            set => SetField(ref _windowWidth, value);
        }

        public double WindowHeight
        {
            get => _windowHeight;
            set => SetField(ref _windowHeight, value);
        }

        public double WindowLeft
        {
            get => _windowLeft;
            set => SetField(ref _windowLeft, value);
        }

        public double WindowTop
        {
            get => _windowTop;
            set => SetField(ref _windowTop, value);
        }

        public bool IsMaximized
        {
            get => _isMaximized;
            set => SetField(ref _isMaximized, value);
        }

        public ObservableCollection<RecentFile> RecentFiles { get; }

        #endregion

        #region Commands

        public ICommand NewFileCommand { get; }
        public ICommand OpenFileCommand { get; }
        public ICommand OpenRecentFileCommand { get; }
        public ICommand SaveFileCommand { get; }
        public ICommand SaveAsFileCommand { get; }
        public ICommand ExportPdfCommand { get; }
        public ICommand PrintCommand { get; }
        public ICommand ToggleThemeCommand { get; }
        public ICommand SetViewModeCommand { get; }
        public ICommand ToggleFindBarCommand { get; }
        public ICommand ToggleWordWrapCommand { get; }
        public ICommand ToggleLineNumbersCommand { get; }
        public ICommand ZoomInCommand { get; }
        public ICommand ZoomOutCommand { get; }
        public ICommand ResetZoomCommand { get; }
        public ICommand ShowAboutCommand { get; }

        #endregion

        #region Logic & Methods

        private void UpdateColumnWidths()
        {
            switch (CurrentViewMode)
            {
                case ViewMode.EditorOnly:
                    EditorColumnWidth = new GridLength(1, GridUnitType.Star);
                    SplitterColumnWidth = new GridLength(0, GridUnitType.Pixel);
                    PreviewColumnWidth = new GridLength(0, GridUnitType.Pixel);
                    break;
                case ViewMode.PreviewOnly:
                    EditorColumnWidth = new GridLength(0, GridUnitType.Pixel);
                    SplitterColumnWidth = new GridLength(0, GridUnitType.Pixel);
                    PreviewColumnWidth = new GridLength(1, GridUnitType.Star);
                    break;
                case ViewMode.Split:
                default:
                    EditorColumnWidth = new GridLength(1, GridUnitType.Star);
                    SplitterColumnWidth = new GridLength(5, GridUnitType.Pixel);
                    PreviewColumnWidth = new GridLength(1, GridUnitType.Star);
                    break;
            }

            OnPropertyChanged(nameof(EditorVisibility));
            OnPropertyChanged(nameof(PreviewVisibility));
            OnPropertyChanged(nameof(SplitterVisibility));
            OnPropertyChanged(nameof(IsEditorOnly));
            OnPropertyChanged(nameof(IsSplit));
            OnPropertyChanged(nameof(IsPreviewOnly));
        }

        private void UpdateWordCount()
        {
            if (string.IsNullOrWhiteSpace(RawMarkdown))
            {
                WordCount = 0;
                return;
            }
            WordCount = WordRegex.Matches(RawMarkdown).Count;
        }

        public bool PromptSaveIfModified()
        {
            if (!IsModified || !HasFile) return true;

            var result = MessageBox.Show(
                $"File '{CurrentFileName}' telah diubah.\nApakah Anda ingin menyimpan perubahan terlebih dahulu?",
                "Konfirmasi Penyimpanan",
                MessageBoxButton.YesNoCancel,
                MessageBoxImage.Question);

            switch (result)
            {
                case MessageBoxResult.Yes:
                    ExecuteSaveFile();
                    return !IsModified; // true jika berhasil simpan
                case MessageBoxResult.No:
                    return true; // abaikan perubahan
                case MessageBoxResult.Cancel:
                default:
                    return false; // batalkan aksi
            }
        }

        public void ExecuteNewFile()
        {
            if (!PromptSaveIfModified()) return;

            _debounceTimer.Stop();
            CurrentFilePath = null;
            _rawMarkdown = string.Empty;
            OnPropertyChanged(nameof(RawMarkdown));
            PreviewMarkdown = string.Empty;
            FileEncoding = "UTF-8";
            IsModified = false;
            HasFile = true;
            UpdateWordCount();
        }

        public void ExecuteOpenFile()
        {
            if (!PromptSaveIfModified()) return;

            string? initialDir = null;
            if (!string.IsNullOrEmpty(CurrentFilePath))
            {
                initialDir = Path.GetDirectoryName(CurrentFilePath);
            }

            string? selectedPath = _fileService.ShowOpenFileDialog(initialDir);
            if (!string.IsNullOrEmpty(selectedPath))
            {
                OpenFileFromPath(selectedPath);
            }
        }

        public void ExecuteOpenPath(string filePath)
        {
            if (!PromptSaveIfModified()) return;
            OpenFileFromPath(filePath);
        }

        public void OpenFileFromPath(string filePath)
        {
            try
            {
                var (content, encoding) = _fileService.ReadAllTextWithEncoding(filePath);
                CurrentFilePath = filePath;
                FileEncoding = encoding;
                
                // Stop debounce timer and apply immediately
                _debounceTimer.Stop();
                _rawMarkdown = content;
                OnPropertyChanged(nameof(RawMarkdown));
                PreviewMarkdown = content;
                
                IsModified = false;
                HasFile = true;
                UpdateWordCount();

                AddToRecentFiles(filePath);
                SaveSettings();
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Gagal membuka file:\n{ex.Message}", "Error Membuka File", MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }

        public void ExecuteSaveFile()
        {
            if (string.IsNullOrEmpty(CurrentFilePath))
            {
                ExecuteSaveAsFile();
                return;
            }

            try
            {
                _fileService.WriteAllText(CurrentFilePath, RawMarkdown);
                IsModified = false;
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Gagal menyimpan file:\n{ex.Message}", "Error Menyimpan", MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }

        public void ExecuteSaveAsFile()
        {
            try
            {
                string? initialDir = !string.IsNullOrEmpty(CurrentFilePath) ? Path.GetDirectoryName(CurrentFilePath) : null;
                string? newPath = _fileService.ShowSaveFileDialog(CurrentFileName, initialDir);
                if (!string.IsNullOrEmpty(newPath))
                {
                    _fileService.WriteAllText(newPath, RawMarkdown);
                    CurrentFilePath = newPath;
                    IsModified = false;
                    AddToRecentFiles(newPath);
                    SaveSettings();
                }
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Gagal menyimpan file:\n{ex.Message}", "Error Menyimpan", MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }

        public void ExecuteExportPdf()
        {
            if (!HasFile) return;

            var dialog = new PageSetupDialog(RawMarkdown, CurrentFileName, CurrentTheme)
            {
                Owner = Application.Current.MainWindow
            };
            dialog.ShowDialog();
        }

        public void ExecuteToggleTheme()
        {
            _themeService.ToggleTheme();
            OnPropertyChanged(nameof(CurrentTheme));
            OnPropertyChanged(nameof(MarkdownStyleName));
            SaveSettings();
        }

        public void ExecuteToggleFindBar()
        {
            IsFindBarVisible = !IsFindBarVisible;
            if (IsFindBarVisible)
            {
                RequestFind?.Invoke();
            }
        }

        public void ExecuteShowAbout()
        {
            MessageBox.Show(
                "MarkView — Markdown Viewer & Editor\n\n" +
                "Versi: 1.0.0\n" +
                "Arsitektur: C# 12 · .NET 8 WPF\n" +
                "Renderer: MdXaml + Markdig (100% Native DirectX / GDI+)\n" +
                "Editor: AvalonEdit\n" +
                "Fitur: Split View, Print & PDF Export, Dark/Light Themes\n" +
                "Zero WebView · Ultra Resource-Efficient\n\n" +
                "Dibuat untuk performa tinggi dan tampilan bersih.",
                "Tentang MarkView",
                MessageBoxButton.OK,
                MessageBoxImage.Information);
        }

        private void AddToRecentFiles(string filePath)
        {
            for (int i = RecentFiles.Count - 1; i >= 0; i--)
            {
                if (string.Equals(RecentFiles[i].FilePath, filePath, StringComparison.OrdinalIgnoreCase))
                {
                    RecentFiles.RemoveAt(i);
                }
            }

            RecentFiles.Insert(0, new RecentFile
            {
                FilePath = filePath,
                LastOpened = DateTime.Now
            });

            while (RecentFiles.Count > 10)
            {
                RecentFiles.RemoveAt(RecentFiles.Count - 1);
            }
        }

        private void LoadSettings()
        {
            var settings = _settingsService.LoadSettings();

            _themeService.ApplyTheme(settings.Theme);
            CurrentViewMode = settings.ViewMode;
            EditorFontSize = settings.FontSize;
            WordWrap = settings.WordWrap;
            ShowLineNumbers = settings.ShowLineNumbers;

            WindowWidth = settings.WindowWidth;
            WindowHeight = settings.WindowHeight;
            WindowLeft = settings.WindowLeft;
            WindowTop = settings.WindowTop;
            IsMaximized = settings.IsMaximized;

            RecentFiles.Clear();
            if (settings.RecentFiles != null)
            {
                foreach (var rf in settings.RecentFiles)
                {
                    if (File.Exists(rf.FilePath))
                    {
                        RecentFiles.Add(rf);
                    }
                }
            }

            UpdateColumnWidths();
            OnPropertyChanged(nameof(MarkdownStyleName));
        }

        public void SaveSettings()
        {
            var settings = new AppSettings
            {
                Theme = _themeService.CurrentTheme,
                ViewMode = CurrentViewMode,
                FontSize = EditorFontSize,
                WordWrap = WordWrap,
                ShowLineNumbers = ShowLineNumbers,
                WindowWidth = WindowWidth,
                WindowHeight = WindowHeight,
                WindowLeft = WindowLeft,
                WindowTop = WindowTop,
                IsMaximized = IsMaximized,
                RecentFiles = new System.Collections.Generic.List<RecentFile>(RecentFiles)
            };

            _settingsService.SaveSettings(settings);
        }

        #endregion
    }
}

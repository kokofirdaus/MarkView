using System.ComponentModel;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Media;
using ICSharpCode.AvalonEdit.Search;
using MarkView.Enums;
using MarkView.ViewModels;

namespace MarkView.Views
{
    public partial class EditorPanel : UserControl
    {
        private bool _isInternalUpdate = false;
        private SearchPanel? _searchPanel;
        private MainViewModel? _subscribedVm;

        public EditorPanel()
        {
            InitializeComponent();

            // Install AvalonEdit built-in search panel
            _searchPanel = SearchPanel.Install(Editor.TextArea);

            // Track caret position
            Editor.TextArea.Caret.PositionChanged += (s, e) =>
            {
                if (DataContext is MainViewModel vm)
                {
                    vm.CaretLine = Editor.TextArea.Caret.Line;
                    vm.CaretColumn = Editor.TextArea.Caret.Column;
                }
            };

            // User typing in editor -> update ViewModel
            Editor.TextChanged += (s, e) =>
            {
                if (_isInternalUpdate) return;
                if (DataContext is MainViewModel vm)
                {
                    _isInternalUpdate = true;
                    vm.RawMarkdown = Editor.Text;
                    _isInternalUpdate = false;
                }
            };

            // ViewModel DataContext listener with clean leak-free lifecycle
            DataContextChanged += OnDataContextChanged;
            Unloaded += OnUnloaded;
        }

        private void OnDataContextChanged(object sender, DependencyPropertyChangedEventArgs e)
        {
            // Unsubscribe from previous ViewModel to prevent memory leak
            if (_subscribedVm != null)
            {
                _subscribedVm.PropertyChanged -= OnViewModelPropertyChanged;
                _subscribedVm.RequestFind -= OnRequestFind;
                _subscribedVm = null;
            }

            if (DataContext is MainViewModel vm)
            {
                _subscribedVm = vm;
                vm.PropertyChanged += OnViewModelPropertyChanged;
                vm.RequestFind += OnRequestFind;

                // Sync initial text immediately upon attaching
                if (!_isInternalUpdate && Editor.Text != vm.RawMarkdown)
                {
                    _isInternalUpdate = true;
                    Editor.Text = vm.RawMarkdown ?? string.Empty;
                    _isInternalUpdate = false;
                }

                UpdateThemeStyling(vm);
            }
        }

        private void OnViewModelPropertyChanged(object? sender, PropertyChangedEventArgs args)
        {
            if (args.PropertyName == nameof(MainViewModel.RawMarkdown))
            {
                if (_isInternalUpdate) return;
                if (DataContext is MainViewModel vm)
                {
                    _isInternalUpdate = true;
                    if (Editor.Text != vm.RawMarkdown)
                    {
                        Editor.Text = vm.RawMarkdown ?? string.Empty;
                    }
                    _isInternalUpdate = false;
                }
            }
            else if (args.PropertyName == nameof(MainViewModel.CurrentTheme))
            {
                if (DataContext is MainViewModel vm)
                {
                    UpdateThemeStyling(vm);
                }
            }
        }

        private void UpdateThemeStyling(MainViewModel vm)
        {
            bool isDark = vm.CurrentTheme == AppTheme.Dark;
            Editor.TextArea.Caret.CaretBrush = isDark ? Brushes.White : Brushes.Black;
            Editor.TextArea.SelectionBrush = isDark 
                ? new SolidColorBrush(Color.FromArgb(120, 14, 99, 156)) 
                : new SolidColorBrush(Color.FromArgb(120, 0, 102, 204));
        }

        private void OnRequestFind()
        {
            _searchPanel?.Open();
            Editor.Focus();
        }

        private void OnUnloaded(object sender, RoutedEventArgs e)
        {
            if (_subscribedVm != null)
            {
                _subscribedVm.PropertyChanged -= OnViewModelPropertyChanged;
                _subscribedVm.RequestFind -= OnRequestFind;
                _subscribedVm = null;
            }
        }
    }
}

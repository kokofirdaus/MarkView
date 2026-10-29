using System;
using System.Diagnostics;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Documents;
using System.Windows.Navigation;

namespace MarkView.Views
{
    public partial class PreviewPanel : UserControl
    {
        public PreviewPanel()
        {
            InitializeComponent();

            // Handle markdown hyperlink clicks safely
            AddHandler(Hyperlink.RequestNavigateEvent, new RequestNavigateEventHandler(OnHyperlinkRequestNavigate));

            Loaded += (s, e) =>
            {
                if (DataContext is ViewModels.MainViewModel vm)
                {
                    vm.PropertyChanged += (sender, args) =>
                    {
                        if (args.PropertyName == nameof(vm.CurrentTheme))
                        {
                            // Force re-apply markdown style on theme switch so MdXaml updates immediately
                            if (TryFindResource("MarkdownDocumentStyle") is Style style)
                            {
                                MarkdownViewer.MarkdownStyle = null;
                                MarkdownViewer.MarkdownStyle = style;
                            }
                        }
                    };
                }
            };
        }

        private static void OnHyperlinkRequestNavigate(object sender, RequestNavigateEventArgs e)
        {
            if (e.Uri != null && (e.Uri.Scheme == Uri.UriSchemeHttp || e.Uri.Scheme == Uri.UriSchemeHttps))
            {
                try
                {
                    Process.Start(new ProcessStartInfo(e.Uri.AbsoluteUri) { UseShellExecute = true });
                    e.Handled = true;
                }
                catch
                {
                    // Fail silently if browser cannot be launched
                }
            }
        }
    }
}

using System;
using System.IO;
using System.Linq;
using System.Printing;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Documents;
using MarkView.Enums;
using MarkView.Helpers;
using MarkView.Services;
using Microsoft.Win32;

namespace MarkView.Views
{
    public partial class PageSetupDialog : Window
    {
        private readonly string _markdownContent;
        private readonly string _documentTitle;
        private readonly AppTheme _theme;

        public PageSetupDialog(string markdownContent, string documentTitle, AppTheme theme = AppTheme.Dark)
        {
            InitializeComponent();
            _markdownContent = markdownContent;
            _documentTitle = documentTitle;
            _theme = theme;

            SourceInitialized += (s, e) => WindowHelper.ApplyWindowTheme(this, _theme);
            Loaded += (s, e) => WindowHelper.ApplyWindowTheme(this, _theme);

            UpdateSummary();
        }

        private PrintOptions GetCurrentOptions()
        {
            var options = new PrintOptions
            {
                Size = (PageSize)CmbPageSize.SelectedIndex,
                IsLandscape = RbLandscape.IsChecked == true,
                Margin = (PageMargin)CmbMargin.SelectedIndex,
                CleanPrintTheme = ChkCleanTheme.IsChecked == true,
                IncludeHeader = ChkHeader.IsChecked == true
            };
            return options;
        }

        private void OnConfigChanged(object sender, RoutedEventArgs e)
        {
            if (TxtSummary != null)
            {
                UpdateSummary();
            }
        }

        private void UpdateSummary()
        {
            string sizeName = CmbPageSize.SelectedIndex switch
            {
                1 => "Letter",
                2 => "Legal",
                _ => "A4"
            };
            string orientation = RbLandscape.IsChecked == true ? "Landscape" : "Portrait";
            string marginName = CmbMargin.SelectedIndex switch
            {
                1 => "Sempit (12.7mm)",
                2 => "Lebar (38.1mm)",
                _ => "Normal (25.4mm)"
            };

            TxtSummary.Text = $"Ringkasan: {sizeName} · {orientation} · Margin {marginName}";
        }

        private void BtnExportPdf_Click(object sender, RoutedEventArgs e)
        {
            try
            {
                var options = GetCurrentOptions();
                var doc = PrintHelper.CreatePrintDocument(_markdownContent, _documentTitle, options);

                var printDialog = new PrintDialog();
                printDialog.PrintTicket.PageOrientation = options.IsLandscape ? PageOrientation.Landscape : PageOrientation.Portrait;
                printDialog.PrintTicket.PageMediaSize = new PageMediaSize(
                    options.Size == PageSize.A4 ? PageMediaSizeName.ISOA4 : PageMediaSizeName.NorthAmericaLetter);

                // Look for PDF virtual printer
                var printServer = new LocalPrintServer();
                var pdfQueue = printServer.GetPrintQueues().FirstOrDefault(q => q.Name.Contains("PDF", StringComparison.OrdinalIgnoreCase));
                if (pdfQueue != null)
                {
                    printDialog.PrintQueue = pdfQueue;
                }

                // Show print dialog to confirm or let user choose save location
                if (printDialog.ShowDialog() == true)
                {
                    var paginator = ((IDocumentPaginatorSource)doc).DocumentPaginator;
                    printDialog.PrintDocument(paginator, _documentTitle);
                    MessageBox.Show("Dokumen berhasil dikirim ke printer/PDF.", "Cetak Selesai", MessageBoxButton.OK, MessageBoxImage.Information);
                    DialogResult = true;
                    Close();
                }
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Terjadi kesalahan saat mengekspor ke PDF:\n{ex.Message}", "Gagal Cetak/Ekspor", MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }

        private void BtnPrint_Click(object sender, RoutedEventArgs e)
        {
            try
            {
                var options = GetCurrentOptions();
                var doc = PrintHelper.CreatePrintDocument(_markdownContent, _documentTitle, options);

                var printDialog = new PrintDialog();
                printDialog.PrintTicket.PageOrientation = options.IsLandscape ? PageOrientation.Landscape : PageOrientation.Portrait;
                printDialog.PrintTicket.PageMediaSize = new PageMediaSize(
                    options.Size == PageSize.A4 ? PageMediaSizeName.ISOA4 : PageMediaSizeName.NorthAmericaLetter);

                if (printDialog.ShowDialog() == true)
                {
                    var paginator = ((IDocumentPaginatorSource)doc).DocumentPaginator;
                    printDialog.PrintDocument(paginator, _documentTitle);
                    DialogResult = true;
                    Close();
                }
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Terjadi kesalahan saat mencetak:\n{ex.Message}", "Gagal Cetak", MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }

        private void BtnCancel_Click(object sender, RoutedEventArgs e)
        {
            DialogResult = false;
            Close();
        }
    }
}

using System;
using System.IO;
using System.Printing;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Documents;
using System.Windows.Media;
using MdXaml;

namespace MarkView.Services
{
    public enum PageSize
    {
        A4,
        Letter,
        Legal
    }

    public enum PageMargin
    {
        Normal,
        Narrow,
        Wide
    }

    public class PrintOptions
    {
        public PageSize Size { get; set; } = PageSize.A4;
        public bool IsLandscape { get; set; } = false;
        public PageMargin Margin { get; set; } = PageMargin.Normal;
        public bool CleanPrintTheme { get; set; } = true;
        public bool IncludeHeader { get; set; } = true;
    }

    public static class PrintHelper
    {
        public static FlowDocument CreatePrintDocument(string markdown, string title, PrintOptions options)
        {
            var engine = new Markdown();

            FlowDocument doc = engine.Transform(markdown ?? string.Empty);

            // Standard 96 DPI: 1 inch = 96 units, 1 mm = 96 / 25.4 units
            const double mmToUnits = 96.0 / 25.4;

            double width, height;
            switch (options.Size)
            {
                case PageSize.Letter:
                    width = 8.5 * 96;
                    height = 11.0 * 96;
                    break;
                case PageSize.Legal:
                    width = 8.5 * 96;
                    height = 14.0 * 96;
                    break;
                case PageSize.A4:
                default:
                    width = 210 * mmToUnits;
                    height = 297 * mmToUnits;
                    break;
            }

            if (options.IsLandscape)
            {
                (width, height) = (height, width);
            }

            double marginVal = options.Margin switch
            {
                PageMargin.Narrow => 12.7 * mmToUnits,
                PageMargin.Wide => 38.1 * mmToUnits,
                _ => 25.4 * mmToUnits
            };

            doc.PageWidth = width;
            doc.PageHeight = height;
            doc.PagePadding = new Thickness(marginVal);
            doc.ColumnWidth = width; // Single column page layout

            if (options.CleanPrintTheme)
            {
                doc.Background = Brushes.White;
                doc.Foreground = Brushes.Black;
            }

            if (options.IncludeHeader && !string.IsNullOrWhiteSpace(title))
            {
                var headerBlock = new Paragraph(new Run(title))
                {
                    FontSize = 10,
                    Foreground = Brushes.Gray,
                    TextAlignment = TextAlignment.Right,
                    Margin = new Thickness(0, 0, 0, 15)
                };
                doc.Blocks.InsertBefore(doc.Blocks.FirstBlock, headerBlock);
            }

            return doc;
        }
    }
}

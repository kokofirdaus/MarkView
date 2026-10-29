using System;
using System.Globalization;
using System.Windows;
using System.Windows.Data;
using MarkView.Enums;

namespace MarkView.Converters
{
    public class ViewModeToVisibilityConverter : IValueConverter
    {
        public object Convert(object? value, Type targetType, object? parameter, CultureInfo culture)
        {
            if (value is ViewMode currentMode && parameter is string targetModeStr)
            {
                if (Enum.TryParse<ViewMode>(targetModeStr, true, out var targetMode))
                {
                    if (currentMode == targetMode)
                        return Visibility.Visible;
                }
            }
            return Visibility.Collapsed;
        }

        public object ConvertBack(object? value, Type targetType, object? parameter, CultureInfo culture)
        {
            throw new NotImplementedException();
        }
    }
}

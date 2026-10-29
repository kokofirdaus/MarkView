using System;
using System.Linq;
using System.Windows;
using MarkView.Enums;
using MarkView.Services.Interfaces;

namespace MarkView.Services
{
    public class ThemeService : IThemeService
    {
        public AppTheme CurrentTheme { get; private set; } = AppTheme.Light;

        public void ApplyTheme(AppTheme theme)
        {
            CurrentTheme = theme;
            string themeUri = theme == AppTheme.Dark ? "Themes/Dark.xaml" : "Themes/Light.xaml";

            var newDictionary = new ResourceDictionary
            {
                Source = new Uri(themeUri, UriKind.Relative)
            };

            var appResources = Application.Current.Resources;
            var mergedDicts = appResources.MergedDictionaries;

            // Remove existing theme dictionaries (Dark.xaml or Light.xaml), preserving Styles.xaml
            var existingThemeDicts = mergedDicts
                .Where(d => d.Source != null && (d.Source.OriginalString.EndsWith("Dark.xaml", StringComparison.OrdinalIgnoreCase) || d.Source.OriginalString.EndsWith("Light.xaml", StringComparison.OrdinalIgnoreCase)))
                .ToList();

            foreach (var d in existingThemeDicts)
            {
                mergedDicts.Remove(d);
            }

            mergedDicts.Add(newDictionary);
        }

        public void ToggleTheme()
        {
            ApplyTheme(CurrentTheme == AppTheme.Light ? AppTheme.Dark : AppTheme.Light);
        }
    }
}

using System;
using System.IO;
using System.Text.Json;
using System.Windows;
using MarkView.Models;
using MarkView.Services.Interfaces;

namespace MarkView.Services
{
    public class SettingsService : ISettingsService
    {
        private readonly string _settingsFilePath;
        private static readonly JsonSerializerOptions JsonOptions = new()
        {
            WriteIndented = true
        };

        public SettingsService()
        {
            string appData = Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData);
            string folder = Path.Combine(appData, "MarkView");
            _settingsFilePath = Path.Combine(folder, "settings.json");
        }

        public AppSettings LoadSettings()
        {
            try
            {
                if (File.Exists(_settingsFilePath))
                {
                    string json = File.ReadAllText(_settingsFilePath);
                    var settings = JsonSerializer.Deserialize<AppSettings>(json, JsonOptions);
                    if (settings != null)
                    {
                        ValidateSettings(settings);
                        return settings;
                    }
                }
            }
            catch
            {
                // Silently fallback if corrupted, backup corrupt file
                try
                {
                    if (File.Exists(_settingsFilePath))
                    {
                        File.Copy(_settingsFilePath, _settingsFilePath + ".bak", true);
                    }
                }
                catch { }
            }

            var defaultSettings = new AppSettings();
            ValidateSettings(defaultSettings);
            return defaultSettings;
        }

        public void SaveSettings(AppSettings settings)
        {
            try
            {
                string? folder = Path.GetDirectoryName(_settingsFilePath);
                if (!string.IsNullOrEmpty(folder) && !Directory.Exists(folder))
                {
                    Directory.CreateDirectory(folder);
                }

                string json = JsonSerializer.Serialize(settings, JsonOptions);
                File.WriteAllText(_settingsFilePath, json);
            }
            catch
            {
                // Ignore background persistence errors
            }
        }

        private static void ValidateSettings(AppSettings settings)
        {
            // Ensure minimum reasonable dimensions
            if (settings.WindowWidth < 600) settings.WindowWidth = 1200;
            if (settings.WindowHeight < 400) settings.WindowHeight = 800;

            // Ensure font size is within safe bounds
            if (settings.FontSize < 9.0 || settings.FontSize > 36.0) settings.FontSize = 14.0;

            // Ensure window opens within visible virtual screen bounds
            double virtualLeft = SystemParameters.VirtualScreenLeft;
            double virtualTop = SystemParameters.VirtualScreenTop;
            double virtualWidth = SystemParameters.VirtualScreenWidth;
            double virtualHeight = SystemParameters.VirtualScreenHeight;

            if (settings.WindowLeft < virtualLeft || 
                settings.WindowLeft > (virtualLeft + virtualWidth - 100) ||
                settings.WindowTop < virtualTop || 
                settings.WindowTop > (virtualTop + virtualHeight - 100))
            {
                // Center on primary screen if coordinates are invalid or off-screen
                settings.WindowLeft = Math.Max(0, (SystemParameters.PrimaryScreenWidth - settings.WindowWidth) / 2);
                settings.WindowTop = Math.Max(0, (SystemParameters.PrimaryScreenHeight - settings.WindowHeight) / 2);
            }
        }
    }
}

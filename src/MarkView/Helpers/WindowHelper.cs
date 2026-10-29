using System;
using System.Runtime.InteropServices;
using System.Windows;
using System.Windows.Interop;
using MarkView.Enums;

namespace MarkView.Helpers
{
    public static class WindowHelper
    {
        [DllImport("dwmapi.dll")]
        private static extern int DwmSetWindowAttribute(IntPtr hwnd, int attr, ref int attrValue, int attrSize);

        public static void ApplyWindowTheme(Window window, AppTheme theme)
        {
            if (window == null) return;
            try
            {
                var helper = new WindowInteropHelper(window);
                if (helper.Handle != IntPtr.Zero)
                {
                    int darkMode = theme == AppTheme.Dark ? 1 : 0;
                    // DWMWA_USE_IMMERSIVE_DARK_MODE = 20 (Windows 10 19041+ / Windows 11)
                    DwmSetWindowAttribute(helper.Handle, 20, ref darkMode, sizeof(int));
                    // DWMWA_USE_IMMERSIVE_DARK_MODE_BEFORE_20H1 = 19 (Windows 10 1809-1909)
                    DwmSetWindowAttribute(helper.Handle, 19, ref darkMode, sizeof(int));
                }
            }
            catch
            {
                // Fallback for older OS versions
            }
        }
    }
}

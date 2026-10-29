using System.Collections.Generic;
using MarkView.Enums;

namespace MarkView.Models
{
    public class AppSettings
    {
        public AppTheme Theme { get; set; } = AppTheme.Light;
        public ViewMode ViewMode { get; set; } = ViewMode.Split;
        public double FontSize { get; set; } = 14.0;
        public bool WordWrap { get; set; } = true;
        public bool ShowLineNumbers { get; set; } = true;
        public double EditorColumnWeight { get; set; } = 1.0;
        public double PreviewColumnWeight { get; set; } = 1.0;
        
        // Window bounds & state
        public double WindowWidth { get; set; } = 1200.0;
        public double WindowHeight { get; set; } = 800.0;
        public double WindowLeft { get; set; } = 100.0;
        public double WindowTop { get; set; } = 100.0;
        public bool IsMaximized { get; set; } = false;

        public List<RecentFile> RecentFiles { get; set; } = new List<RecentFile>();
    }
}

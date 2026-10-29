using System;
using System.IO;
using System.Text.Json.Serialization;

namespace MarkView.Models
{
    public class RecentFile
    {
        public string FilePath { get; set; } = string.Empty;
        public DateTime LastOpened { get; set; } = DateTime.Now;

        [JsonIgnore]
        public string FileName => string.IsNullOrEmpty(FilePath) ? string.Empty : Path.GetFileName(FilePath);
    }
}

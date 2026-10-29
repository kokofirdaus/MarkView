using System;
using System.IO;
using System.Text;
using Microsoft.Win32;
using MarkView.Services.Interfaces;

namespace MarkView.Services
{
    public class FileService : IFileService
    {
        private const string FileDialogFilter = "Markdown Files (*.md)|*.md|Text Files (*.txt)|*.txt|All Files (*.*)|*.*";

        public string? ShowOpenFileDialog(string? initialDirectory = null)
        {
            var dialog = new OpenFileDialog
            {
                Title = "Buka File Markdown",
                Filter = FileDialogFilter,
                FilterIndex = 1,
                Multiselect = false
            };

            if (!string.IsNullOrEmpty(initialDirectory) && Directory.Exists(initialDirectory))
            {
                dialog.InitialDirectory = initialDirectory;
            }
            else
            {
                dialog.InitialDirectory = Environment.GetFolderPath(Environment.SpecialFolder.MyDocuments);
            }

            return dialog.ShowDialog() == true ? dialog.FileName : null;
        }

        public string? ShowSaveFileDialog(string defaultFileName, string? initialDirectory = null)
        {
            var dialog = new SaveFileDialog
            {
                Title = "Simpan File Markdown",
                Filter = FileDialogFilter,
                FilterIndex = 1,
                DefaultExt = ".md",
                FileName = defaultFileName
            };

            if (!string.IsNullOrEmpty(initialDirectory) && Directory.Exists(initialDirectory))
            {
                dialog.InitialDirectory = initialDirectory;
            }
            else
            {
                dialog.InitialDirectory = Environment.GetFolderPath(Environment.SpecialFolder.MyDocuments);
            }

            return dialog.ShowDialog() == true ? dialog.FileName : null;
        }

        public (string Content, string EncodingName) ReadAllTextWithEncoding(string filePath)
        {
            if (!File.Exists(filePath))
            {
                throw new FileNotFoundException("File tidak ditemukan.", filePath);
            }

            // Use StreamReader with detectEncodingFromByteOrderMarks for atomic, safe encoding detection
            using var stream = new FileStream(filePath, FileMode.Open, FileAccess.Read, FileShare.ReadWrite);
            using var reader = new StreamReader(stream, Encoding.UTF8, detectEncodingFromByteOrderMarks: true);
            string content = reader.ReadToEnd();
            string encodingName = reader.CurrentEncoding.WebName.ToUpperInvariant();

            return (content, encodingName);
        }

        public void WriteAllText(string filePath, string content)
        {
            var dir = Path.GetDirectoryName(filePath);
            if (!string.IsNullOrEmpty(dir) && !Directory.Exists(dir))
            {
                Directory.CreateDirectory(dir);
            }

            // UTF-8 without BOM as modern cross-platform standard
            File.WriteAllText(filePath, content, new UTF8Encoding(false));
        }
    }
}

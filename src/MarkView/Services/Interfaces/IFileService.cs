namespace MarkView.Services.Interfaces
{
    public interface IFileService
    {
        string? ShowOpenFileDialog(string? initialDirectory = null);
        string? ShowSaveFileDialog(string defaultFileName, string? initialDirectory = null);
        (string Content, string EncodingName) ReadAllTextWithEncoding(string filePath);
        void WriteAllText(string filePath, string content);
    }
}

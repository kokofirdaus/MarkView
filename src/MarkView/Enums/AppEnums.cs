namespace MarkView.Enums
{
    public enum ViewMode
    {
        EditorOnly = 0,
        Split = 1,
        PreviewOnly = 2
    }

    public enum AppTheme
    {
        Light = 0,
        Dark = 1
    }

    public enum SaveStatus
    {
        NewFile,     // "📄 File baru"
        Saved,       // "✅ Tersimpan"
        Modified     // "✏️ Diubah"
    }
}

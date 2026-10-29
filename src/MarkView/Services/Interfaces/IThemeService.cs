using MarkView.Enums;

namespace MarkView.Services.Interfaces
{
    public interface IThemeService
    {
        AppTheme CurrentTheme { get; }
        void ApplyTheme(AppTheme theme);
        void ToggleTheme();
    }
}

namespace journalApp.Services
{
    public interface IThemeService
    {
        Task<string> GetTheme();
        Task SetTheme(string theme);
        Task ToggleTheme();
    }
}
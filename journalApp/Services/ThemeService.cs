using System.Threading.Tasks;
using Microsoft.Maui.Storage;

namespace journalApp.Services
{
    public class ThemeService : IThemeService
    {
        private const string THEME_KEY = "app_theme";

        public Task<string> GetTheme()
        {
            var theme = Preferences.Default.Get(THEME_KEY, "light");
            return Task.FromResult(theme);
        }

        public Task SetTheme(string theme)
        {
            Preferences.Default.Set(THEME_KEY, theme);
            return Task.CompletedTask;
        }

        public async Task ToggleTheme()
        {
            var current = await GetTheme();
            var next = current == "light" ? "dark" : "light";
            await SetTheme(next);
        }
    }
}
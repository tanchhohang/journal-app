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
            Console.WriteLine($"=== Theme set to: {theme}");
            return Task.CompletedTask;
        }

        public async Task ToggleTheme()
        {
            var currentTheme = await GetTheme();
            var newTheme = currentTheme == "light" ? "dark" : "light";
            await SetTheme(newTheme);
        }
    }
}
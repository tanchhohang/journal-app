using Microsoft.Extensions.Logging;
using journalApp.Services;
using Radzen;

namespace journalApp;

public static class MauiProgram
{
    public static MauiApp CreateMauiApp()
    {
        var builder = MauiApp.CreateBuilder();
        builder
            .UseMauiApp<App>()
            .ConfigureFonts(fonts =>
            {
                fonts.AddFont("OpenSans-Regular.ttf", "OpenSansRegular");
            });

        builder.Services.AddMauiBlazorWebView();

#if DEBUG
        builder.Services.AddBlazorWebViewDeveloperTools();
        builder.Logging.AddDebug();
#endif

        builder.Services.AddSingleton<IEntryService, EntryService>();
        builder.Services.AddSingleton<ISecurityService, SecurityService>();
        builder.Services.AddSingleton<IThemeService, journalApp.Services.ThemeService>();
        builder.Services.AddSingleton<IStreakService, StreakService>();

        
        builder.Services.AddRadzenComponents();

        return builder.Build();
    }
}
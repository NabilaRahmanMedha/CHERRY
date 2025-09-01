using CHERRY.Services;
using CHERRY.Views;
using Microsoft.Extensions.Logging;

namespace CHERRY
{
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
                    fonts.AddFont("OpenSans-Semibold.ttf", "OpenSansSemibold");
                });

            // Register services
            builder.Services.AddSingleton<DatabaseService>();
            builder.Services.AddSingleton<UserService>();
            builder.Services.AddSingleton(sp => new HttpClient { BaseAddress = new Uri("https://localhost:5001/") });
            builder.Services.AddSingleton<AuthService>();

            // Register pages
            builder.Services.AddTransient<LoginPage>();
            builder.Services.AddTransient<RegistrationPage>();
            builder.Services.AddTransient<ProfilePage>();
            builder.Services.AddTransient<EditProfilePage>();
            builder.Services.AddTransient<CyclePage>();
            builder.Services.AddTransient<CalendarPage>();
            builder.Services.AddTransient<ReportsPage>();

            // Register AppShell
            builder.Services.AddSingleton<AppShell>();

#if DEBUG
            builder.Logging.AddDebug();
#endif

            return builder.Build();
        }
    }
}
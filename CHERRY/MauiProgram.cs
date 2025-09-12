using CHERRY.Services;
using CHERRY.Views;
using Microsoft.Extensions.Logging;
using Microsoft.Maui;
using Microsoft.Maui.Controls.Hosting;
using Microsoft.Maui.Hosting;
using Microcharts.Maui;
using Microsoft.Extensions.DependencyInjection;

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
                })
            .UseMicrocharts();

            // Register services
            builder.Services.AddSingleton<DatabaseService>();
            builder.Services.AddSingleton<UserService>();
            builder.Services.AddSingleton(sp => new HttpClient { BaseAddress = new Uri("http://10.0.2.2:5000/") });
            builder.Services.AddSingleton<AuthService>();
            builder.Services.AddSingleton<GeminiService>(sp =>
            {
                var http = new HttpClient();
                var svc = new GeminiService(http);
                // Configure with provided API key
                svc.Configure("AIzaSyAXlrEtKZxWbr7hoCGmd-EYvoXh0D9u7vw");
                return svc;
            });
            builder.Services.AddSingleton<ProfileApiService>();
            builder.Services.AddSingleton<CycleApiService>();
            builder.Services.AddHttpClient<NearbyPlacesService>();
            // Google Places fallback
            var googleApiKey = Environment.GetEnvironmentVariable("GOOGLE_PLACES_API_KEY") ?? string.Empty;
            builder.Services.AddSingleton(new GooglePlacesOptions { ApiKey = googleApiKey });
            builder.Services.AddHttpClient<GooglePlacesService>();

            // Notifications
#if ANDROID
            builder.Services.AddSingleton<INotificationService, CHERRY.Platforms.Android.Services.AndroidNotificationService>();
#else
            builder.Services.AddSingleton<INotificationService, NoopNotificationService>();
#endif

            // Register pages
            builder.Services.AddTransient<LoginPage>();
            builder.Services.AddTransient<RegistrationPage>();
            builder.Services.AddTransient<ProfilePage>();
            builder.Services.AddTransient<EditProfilePage>();
            builder.Services.AddTransient<CyclePage>();
            builder.Services.AddTransient<CalendarPage>();
            builder.Services.AddTransient<ReportsPage>();
            builder.Services.AddTransient<EmergencyPage>();
            builder.Services.AddTransient<IntroPage>();

            // Register AppShell
            builder.Services.AddSingleton<AppShell>();

#if DEBUG
            builder.Logging.AddDebug();
#endif

            return builder.Build();
        }
    }
}
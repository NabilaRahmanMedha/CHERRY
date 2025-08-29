using Microsoft.Extensions.Logging;
using CHERRY.Services;

namespace CHERRY
{
    public static class MauiProgram
    {
        public static MauiApp CreateMauiApp()
        {
            var builder = MauiApp.CreateBuilder();

            builder
                .UseMauiApp<App>();

            // Setup DB path
            string dbPath = Path.Combine(FileSystem.AppDataDirectory, "periodtracker.db3");
            builder.Services.AddSingleton(new DatabaseService(dbPath));

            return builder.Build();
        }
    }
}

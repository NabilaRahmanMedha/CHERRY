using CHERRY.Services;
using CHERRY.Views;
using Microsoft.Maui.Controls;

namespace CHERRY
{
    public partial class App : Application
    {
        private readonly DatabaseService _db;

        public App(DatabaseService db)
        {
            InitializeComponent(); // Ensure this method is defined in App.xaml
            _db = db;

            // Set the main page with navigation
            MainPage = new NavigationPage(new LoginPage(_db));
        }

        protected override Window CreateWindow(IActivationState? activationState)
        {
            var window = base.CreateWindow(activationState);

            // Optional: Set window size for desktop
            window.Width = 400;
            window.Height = 600;

            return window;
        }
    }
}
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
            InitializeComponent();
            _db = db;
            // Removed MainPage assignment to avoid CS0618
        }

        protected override Window CreateWindow(IActivationState? activationState)
        {
            return new Window(new NavigationPage(new LoginPage(_db)));
        }
    }
}

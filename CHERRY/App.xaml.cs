using CHERRY.Services;
using CHERRY.Views;
using Microsoft.Maui.Controls;

namespace CHERRY
{
    public partial class App : Microsoft.Maui.Controls.Application
    {
        private readonly AuthService _auth;

        /*public App(AuthService auth)
        {
            InitializeComponent(); // Ensure this method is defined in App.xaml
            _auth = auth;

            // Set the main page with navigation
            Microsoft.Maui.Controls.Application.Current.MainPage = new NavigationPage(new LoginPage(_auth));
        }*/

        /*public App()
        {
            InitializeComponent(); // Ensure this method is defined in App.xaml
           // _auth = auth;

            // Set the main page with navigation
            Microsoft.Maui.Controls.Application.Current.MainPage = new AppShell();
        }*/

        public App(AuthService auth)
        {
            _auth = auth;
            InitializeComponent(); // Ensure this method is defined in App.xaml

            Microsoft.Maui.Controls.Application.Current.MainPage = new NavigationPage(new IntroPage(_auth));
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
using CHERRY.Services;
using Microsoft.Maui.Controls;

namespace CHERRY.Views
{
    public partial class LoginPage : ContentPage
    {
        private readonly AuthService _auth;

        public LoginPage(AuthService auth)
        {
            InitializeComponent();
            _auth = auth;
        }

        private async void OnLoginClicked(object sender, EventArgs e)
        {
            var email = EmailEntry.Text;
            var password = PasswordEntry.Text;

            var ok = await _auth.LoginAsync(email, password);

            if (ok)
            {
                await DisplayAlert("Welcome", $"Hello {email}", "OK");
                Application.Current.Windows[0].Page = new AppShell();
            }
            else
            {
                MessageLabel.Text = "Invalid login. Try again.";
            }
        }

        private async void OnRegisterNavigateClicked(object sender, EventArgs e)
        {
            await Navigation.PushAsync(new RegistrationPage(_auth));
        }
    }
}
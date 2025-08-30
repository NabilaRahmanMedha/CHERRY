using CHERRY.Services;
using Microsoft.Maui.Controls;

namespace CHERRY.Views
{
    public partial class LoginPage : ContentPage
    {
        private readonly DatabaseService _db;

        public LoginPage(DatabaseService db)
        {
            InitializeComponent(); // This connects with your XAML
            _db = db;
        }

        private async void OnLoginClicked(object sender, EventArgs e)
        {
            var email = EmailEntry.Text;
            var password = PasswordEntry.Text;

            var user = await _db.LoginUserAsync(email, password);

            if (user != null)
            {
                await DisplayAlert("Welcome", $"Hello {user.Email}", "OK");
                // Navigate to AppShell and pass the required DatabaseService instance
                Application.Current.Windows[0].Page = new AppShell(); // Removed the argument
            }
            else
            {
                MessageLabel.Text = "Invalid login. Try again.";
            }
        }

        private async void OnRegisterNavigateClicked(object sender, EventArgs e)
        {
            await Navigation.PushAsync(new RegistrationPage(_db));
        }
    }
}
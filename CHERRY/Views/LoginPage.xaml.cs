using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using CHERRY.Services;
using Microsoft.Maui.Controls;
using CHERRY.Models;

namespace CHERRY.Views
{

    public partial class LoginPage : ContentPage
    {
        private readonly DatabaseService _db;

        public LoginPage(DatabaseService db)
        {
            InitializeComponent();
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
                // Navigate to Main Dashboard
                await Navigation.PushAsync(new MainPage());
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

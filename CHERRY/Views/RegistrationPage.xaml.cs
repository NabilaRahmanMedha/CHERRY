using CHERRY.Services;
using CHERRY.Models;
using Microsoft.Maui.Controls;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace CHERRY.Views
{
    public partial class RegistrationPage : ContentPage
    {
        private readonly DatabaseService _db;

        public RegistrationPage(DatabaseService db)
        {
            InitializeComponent();
            _db = db;
        }

        private async void OnRegisterClicked(object sender, EventArgs e)
        {
            var email = EmailEntry.Text;
            var password = PasswordEntry.Text;

            if (string.IsNullOrWhiteSpace(email) || string.IsNullOrWhiteSpace(password))
            {
                MessageLabel.Text = "Please enter both email and password.";
                return;
            }

            bool success = await _db.RegisterUserAsync(email, password);
            if (success)
            {
                await DisplayAlert("Success", "Account created!", "OK");
                await Navigation.PushAsync(new LoginPage(_db));
            }
            else
            {
                MessageLabel.Text = "Email already registered.";
            }
        }
    }
}

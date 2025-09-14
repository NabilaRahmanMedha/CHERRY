using Microsoft.Maui.Controls;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.Maui.Devices.Sensors;
using CHERRY.Services;


namespace CHERRY.Views
{
    public partial class MenuFlyoutView : ContentView
    {
        public MenuFlyoutView()
        {
            InitializeComponent();
        }

        private async void OnProfileClicked(object sender, EventArgs e)
        {
            await Shell.Current.GoToAsync("//ProfilePage");
        }

        private async void OnCalendarClicked(object sender, EventArgs e)
        {
            await Shell.Current.GoToAsync("//CalendarPage");
        }

        private async void OnSettingsClicked(object sender, EventArgs e)
        {
            await Shell.Current.GoToAsync("//SettingsPage");
        }

        private async void OnHelpClicked(object sender, EventArgs e)
        {
            await Shell.Current.GoToAsync("//help");
        }

        private async void OnAboutClicked(object sender, EventArgs e)
        {
            await Shell.Current.GoToAsync(nameof(AboutCherryPage));
        }

        private async void OnLogoutClicked(object sender, EventArgs e)
        {
            bool answer = await Application.Current.MainPage.DisplayAlert(
                "Logout",
                "Are you sure you want to logout?",
                "Yes", "No");

            if (answer)
            {
                await Shell.Current.GoToAsync("//LoginPage");
            }
        }
    }
}
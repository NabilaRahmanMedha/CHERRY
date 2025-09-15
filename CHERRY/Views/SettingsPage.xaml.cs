using Microsoft.Maui.Controls;
using Microsoft.Maui.Storage;
using System;
using CHERRY.Services;

namespace CHERRY.Views
{
    public partial class SettingsPage : ContentPage
    {
        private readonly AuthService _auth;
        private readonly ProfileApiService _profileApi;

        private const string PrefPrefix = "settings";
        private const string PeriodKey = "period_reminders";
        private const string OvulationKey = "ovulation_alerts";
        private const string TipsKey = "daily_tips";
        private const string ChatbotKey = "chatbot_notifications";
        private const string ThemeKey = "theme"; // 0=Light,1=Dark,2=System
        private const string UnitsKey = "units"; // 0=Days,1=Weeks

        public SettingsPage()
        {
            InitializeComponent();
            _auth = ServiceHelper.GetService<AuthService>();
            _profileApi = ServiceHelper.GetService<ProfileApiService>();
        }

        protected override async void OnAppearing()
        {
            base.OnAppearing();

            var scoped = await GetUserScopeAsync();
            PeriodSwitch.IsToggled = Preferences.Get(Scope(scoped, PeriodKey), false);
            OvulationSwitch.IsToggled = Preferences.Get(Scope(scoped, OvulationKey), false);
            DailyTipsSwitch.IsToggled = Preferences.Get(Scope(scoped, TipsKey), true);
            ChatbotSwitch.IsToggled = Preferences.Get(Scope(scoped, ChatbotKey), true);
            ThemePicker.SelectedIndex = Preferences.Get(Scope(scoped, ThemeKey), 2);
            UnitsPicker.SelectedIndex = Preferences.Get(Scope(scoped, UnitsKey), 0);
        }

        // Logout button click
        private async void OnLogoutClicked(object sender, EventArgs e)
        {
            bool answer = await DisplayAlert("Log Out", "Are you sure you want to log out?", "Yes", "No");
            if (answer)
            {
                await _auth.LogoutAsync();
                await Shell.Current.GoToAsync("//LoginPage");
            }
        }

        private async void OnEditProfileClicked(object sender, EventArgs e)
        {
            await Shell.Current.GoToAsync(nameof(EditProfilePage));
        }

        private async void OnChangePasswordClicked(object sender, EventArgs e)
        {
            await DisplayAlert("Info", "Change password is not implemented yet.", "OK");
        }

        private async void OnDeleteAccountClicked(object sender, EventArgs e)
        {
            bool confirm = await DisplayAlert("Delete Account", "This will permanently delete your account. Continue?", "Delete", "Cancel");
            if (!confirm) return;
            var ok = await _profileApi.DeleteProfileAsync();
            if (ok)
            {
                await _auth.LogoutAsync();
                await Shell.Current.GoToAsync("//LoginPage");
            }
            else
            {
                await DisplayAlert("Error", "Could not delete the account.", "OK");
            }
        }

        private async void OnFaqClicked(object sender, EventArgs e)
        {
            await DisplayAlert("Help", "FAQ coming soon.", "OK");
        }

        private async void OnContactSupportClicked(object sender, EventArgs e)
        {
            await DisplayAlert("Support", "Contact us at support@cherry.app", "OK");
        }

        private async void OnTermsPrivacyClicked(object sender, EventArgs e) =>
            await DisplayAlert("Terms & Privacy", "See our website for details.", "OK");

        private void OnAboutCherryClicked(object sender, EventArgs e)
        {
            // TODO: Navigate to About Cherry page
        }

        // Toggle handlers (Notifications / Privacy & Security)
        private async void OnPeriodRemindersToggled(object sender, ToggledEventArgs e)
        {
            var scoped = await GetUserScopeAsync();
            Preferences.Set(Scope(scoped, PeriodKey), e.Value);
        }

        private async void OnOvulationAlertsToggled(object sender, ToggledEventArgs e)
        {
            var scoped = await GetUserScopeAsync();
            Preferences.Set(Scope(scoped, OvulationKey), e.Value);
        }

        private async void OnDailyTipsToggled(object sender, ToggledEventArgs e)
        {
            var scoped = await GetUserScopeAsync();
            Preferences.Set(Scope(scoped, TipsKey), e.Value);
        }

        private async void OnChatbotNotificationsToggled(object sender, ToggledEventArgs e)
        {
            var scoped = await GetUserScopeAsync();
            Preferences.Set(Scope(scoped, ChatbotKey), e.Value);
        }

        private void OnBiometricLoginToggled(object sender, ToggledEventArgs e)
        {
            // TODO: Handle API call to enable/disable biometric login
        }

        private void OnAppLockToggled(object sender, ToggledEventArgs e)
        {
            // TODO: Handle API call to enable/disable app lock
        }

        private void OnDataSharingToggled(object sender, ToggledEventArgs e)
        {
            // TODO: Handle API call to enable/disable data sharing
        }

        private async void OnThemeChanged(object sender, EventArgs e)
        {
            var scoped = await GetUserScopeAsync();
            var index = Math.Max(0, ThemePicker.SelectedIndex);
            Preferences.Set(Scope(scoped, ThemeKey), index);
            App.Current.UserAppTheme = index == 0 ? AppTheme.Light : index == 1 ? AppTheme.Dark : AppTheme.Unspecified;
        }

        private async void OnUnitsChanged(object sender, EventArgs e)
        {
            var scoped = await GetUserScopeAsync();
            var index = Math.Max(0, UnitsPicker.SelectedIndex);
            Preferences.Set(Scope(scoped, UnitsKey), index);
        }

        private async Task<string> GetUserScopeAsync()
        {
            var email = await _auth.GetEmailAsync();
            return string.IsNullOrWhiteSpace(email) ? PrefPrefix : $"{PrefPrefix}:{email}";
        }

        private static string Scope(string scope, string key) => $"{scope}:{key}";
    }
}

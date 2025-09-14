using Microsoft.Maui.Controls;
using System;

namespace CHERRY.Views
{
    public partial class SettingsPage : ContentPage
    {
        public SettingsPage()
        {
            InitializeComponent();
        }

        // Logout button click
        private async void OnLogoutClicked(object sender, EventArgs e)
        {
            // TODO: Add API or navigation logic for logout
            bool answer = await DisplayAlert("Log Out", "Are you sure you want to log out?", "Yes", "No");
            if (answer)
            {
                // Placeholder for logout logic
            }
        }

        // Other placeholders for future API integration if needed
        private void OnEditProfileClicked(object sender, EventArgs e)
        {
            // TODO: Navigate to Edit Profile page
        }

        private void OnChangePasswordClicked(object sender, EventArgs e)
        {
            // TODO: Navigate to Change Password page
        }

        private void OnDeleteAccountClicked(object sender, EventArgs e)
        {
            // TODO: Add delete account logic
        }

        private void OnFaqClicked(object sender, EventArgs e)
        {
            // TODO: Navigate to FAQ/Help page
        }

        private void OnContactSupportClicked(object sender, EventArgs e)
        {
            // TODO: Navigate to Contact Support page
        }

        private void OnTermsPrivacyClicked(object sender, EventArgs e)
        {
            // TODO: Show Terms & Privacy
        }

        private void OnAboutCherryClicked(object sender, EventArgs e)
        {
            // TODO: Navigate to About Cherry page
        }

        // Toggle handlers (Notifications / Privacy & Security)
        private void OnPeriodRemindersToggled(object sender, ToggledEventArgs e)
        {
            // TODO: Handle API call to save user preference
        }

        private void OnOvulationAlertsToggled(object sender, ToggledEventArgs e)
        {
            // TODO: Handle API call to save user preference
        }

        private void OnDailyTipsToggled(object sender, ToggledEventArgs e)
        {
            // TODO: Handle API call to save user preference
        }

        private void OnChatbotNotificationsToggled(object sender, ToggledEventArgs e)
        {
            // TODO: Handle API call to save user preference
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
    }
}

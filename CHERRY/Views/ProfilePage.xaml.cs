using CHERRY.Services;
using Microsoft.Maui.Controls;
using Microsoft.Maui.Media;
using System;
using System.IO;
using System.Threading.Tasks;

namespace CHERRY.Views
{
    public partial class ProfilePage : ContentPage
    {
        private readonly ProfileApiService _profileApi;
        private readonly AuthService _auth;

        public ProfilePage(DatabaseService db)
        {
            InitializeComponent();
            _profileApi = ServiceHelper.GetService<ProfileApiService>();
            _auth = ServiceHelper.GetService<AuthService>();
            LoadUserData();
        }

        protected override void OnAppearing()
        {
            base.OnAppearing();
            LoadUserData();
        }

        private async void LoadUserData()
        {
            try
            {
                EmailLabel.Text = await _auth.GetEmailAsync();
                var profile = await _profileApi.GetProfileAsync();
                if (profile != null)
                {
                    NicknameLabel.Text = string.IsNullOrEmpty(profile.Nickname) ? "Set Nickname" : profile.Nickname;
                    PeriodLengthLabel.Text = profile.PeriodLength > 0 ? $"{profile.PeriodLength} days" : "Not set";
                    CycleLengthLabel.Text = profile.CycleLength > 0 ? $"{profile.CycleLength} days" : "Not set";
                    if (!string.IsNullOrWhiteSpace(profile.ProfileImageUrl))
                    {
                        var baseUri = ServiceHelper.GetService<HttpClient>().BaseAddress!;
                        var relative = profile.ProfileImageUrl.StartsWith("/") ? profile.ProfileImageUrl.Substring(1) : profile.ProfileImageUrl;
                        var absolute = new Uri(baseUri, relative);
                        ProfileImage.Source = ImageSource.FromUri(absolute);
                    }
                }
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Error loading user data: {ex.Message}");
            }
        }

        private async void OnEditProfileClicked(object sender, EventArgs e)
        {
            try
            {
                // Navigate without passing email; edit page will load via API
                await Shell.Current.GoToAsync($"{nameof(EditProfilePage)}");
            }
            catch (Exception ex)
            {
                await DisplayAlert("Error", $"Failed to navigate: {ex.Message}", "OK");
            }
        }

        private async void OnEditPhotoClicked(object sender, EventArgs e)
        {
            try
            {
                var result = await FilePicker.PickAsync(new PickOptions
                {
                    PickerTitle = "Select Profile Picture",
                    FileTypes = FilePickerFileType.Images
                });

                if (result != null)
                {
                    using var stream = await result.OpenReadAsync();
                    var uploadedUrl = await _profileApi.UploadProfileImageAsync(stream, result.FileName, result.ContentType);
                    if (!string.IsNullOrWhiteSpace(uploadedUrl))
                    {
                        ProfileImage.Source = ImageSource.FromUri(new Uri(uploadedUrl));
                    }
                }
            }
            catch (Exception ex)
            {
                await DisplayAlert("Error", "Failed to select image: " + ex.Message, "OK");
            }
        }

        // Add other missing event handlers that might be in your XAML
        private async void OnGraphReportClicked(object sender, EventArgs e)
        {
            await DisplayAlert("My Goals", "Feature coming soon!", "OK");
        }

        private async void OnReferAFriendClicked(object sender, EventArgs e)
        {
            await DisplayAlert("Subscription", "Feature coming soon!", "OK");
        }

        private async void OnAboutCherryClicked(object sender, EventArgs e)
        {
            await DisplayAlert("Health Report", "Feature coming soon!", "OK");
        }

        private async void OnCycleOvulationClicked(object sender, EventArgs e)
        {
            await DisplayAlert("Terms and Conditions", "Feature coming soon!", "OK");
        }

        private async void OnDeleteProfileClicked(object sender, EventArgs e)
        {
            bool answer = await DisplayAlert("Confirm", "Are you sure you want to delete your profile? This action cannot be undone.", "Yes", "No");
            if (answer)
            {
                var success = await _profileApi.DeleteProfileAsync();
                if (success)
                {
                    await _auth.LogoutAsync();
                    Microsoft.Maui.Controls.Application.Current.MainPage = new LoginPage(ServiceHelper.GetService<AuthService>());
                }
                else
                {
                    await DisplayAlert("Error", "Failed to delete profile.", "OK");
                }
            }
        }

        private async void OnLogoutClicked(object sender, EventArgs e)
        {
            bool answer = await DisplayAlert("Log Out", "Are you sure you want to log out?", "Yes", "No");
            if (answer)
            {
                await _auth.LogoutAsync();

                // Navigate to login page
                Microsoft.Maui.Controls.Application.Current.MainPage = new LoginPage(ServiceHelper.GetService<AuthService>());
            }
        }
    }
}
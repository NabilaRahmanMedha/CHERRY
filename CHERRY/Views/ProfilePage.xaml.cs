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
        private readonly DatabaseService _db;
        private readonly UserService _userService;

        public ProfilePage(DatabaseService db)
        {
            InitializeComponent();
            _db = db;
            _userService = new UserService();
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
                // Get current user email from preferences
                var userEmail = await SecureStorage.GetAsync("user_email");
                if (!string.IsNullOrEmpty(userEmail))
                {
                    EmailLabel.Text = userEmail;

                    // Load user profile data
                    var user = await _userService.GetUserProfileAsync(userEmail);
                    if (user != null)
                    {
                        NicknameLabel.Text = string.IsNullOrEmpty(user.Nickname) ? "Set Nickname" : user.Nickname;
                        PeriodLengthLabel.Text = user.PeriodLength > 0 ? $"{user.PeriodLength} days" : "Not set";
                        CycleLengthLabel.Text = user.CycleLength > 0 ? $"{user.CycleLength} days" : "Not set";

                        // Load profile image if exists
                        if (!string.IsNullOrEmpty(user.ProfileImagePath) && File.Exists(user.ProfileImagePath))
                        {
                            ProfileImage.Source = ImageSource.FromFile(user.ProfileImagePath);
                        }
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
                var userEmail = await SecureStorage.GetAsync("user_email");
                if (!string.IsNullOrEmpty(userEmail))
                {
                    // Use Shell navigation instead of Navigation.PushAsync
                    await Shell.Current.GoToAsync($"{nameof(EditProfilePage)}?email={userEmail}");
                }
                else
                {
                    await DisplayAlert("Error", "User not logged in", "OK");
                }
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
                    // Save the image to app data
                    var userEmail = await SecureStorage.GetAsync("user_email");
                    var fileName = $"{userEmail}_profile{Path.GetExtension(result.FileName)}";
                    var filePath = Path.Combine(FileSystem.AppDataDirectory, fileName);

                    using (var stream = await result.OpenReadAsync())
                    using (var fileStream = File.OpenWrite(filePath))
                    {
                        await stream.CopyToAsync(fileStream);
                    }

                    // Update profile image
                    ProfileImage.Source = ImageSource.FromFile(filePath);

                    // Save path to database
                    await _userService.UpdateProfileImageAsync(userEmail, filePath);
                }
            }
            catch (Exception ex)
            {
                await DisplayAlert("Error", "Failed to select image: " + ex.Message, "OK");
            }
        }

        // Add other missing event handlers that might be in your XAML
        private async void OnMyGoalsClicked(object sender, EventArgs e)
        {
            await DisplayAlert("My Goals", "Feature coming soon!", "OK");
        }

        private async void OnSubscriptionClicked(object sender, EventArgs e)
        {
            await DisplayAlert("Subscription", "Feature coming soon!", "OK");
        }

        private async void OnHealthReportClicked(object sender, EventArgs e)
        {
            await DisplayAlert("Health Report", "Feature coming soon!", "OK");
        }

        private async void OnTermsClicked(object sender, EventArgs e)
        {
            await DisplayAlert("Terms and Conditions", "Feature coming soon!", "OK");
        }

        private async void OnDeleteProfileClicked(object sender, EventArgs e)
        {
            bool answer = await DisplayAlert("Confirm", "Are you sure you want to delete your profile? This action cannot be undone.", "Yes", "No");
            if (answer)
            {
                var userEmail = await SecureStorage.GetAsync("user_email");
                if (!string.IsNullOrEmpty(userEmail))
                {
                    bool success = await _userService.DeleteUserAsync(userEmail);
                    if (success)
                    {
                        // Clear secure storage
                        SecureStorage.Remove("user_email");

                        // Navigate to login page
                        Application.Current.MainPage = new LoginPage(ServiceHelper.GetService<AuthService>());
                    }
                    else
                    {
                        await DisplayAlert("Error", "Failed to delete profile.", "OK");
                    }
                }
            }
        }

        private async void OnLogoutClicked(object sender, EventArgs e)
        {
            bool answer = await DisplayAlert("Log Out", "Are you sure you want to log out?", "Yes", "No");
            if (answer)
            {
                // Clear secure storage
                SecureStorage.Remove("user_email");

                // Navigate to login page
                Application.Current.MainPage = new LoginPage(ServiceHelper.GetService<AuthService>());
            }
        }
    }
}
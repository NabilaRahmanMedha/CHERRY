using CHERRY.Services;
using Microsoft.Maui.Controls;
using Microsoft.Maui.Media;
using System;
using System.IO;
using System.Threading.Tasks;

namespace CHERRY.Views
{
    public partial class EditProfilePage : ContentPage
    {
        private readonly DatabaseService _db;
        private readonly UserService _userService;
        private string _userEmail;
        private string _profileImagePath;

        public EditProfilePage()
        {
            InitializeComponent();
            _db = new DatabaseService();
            _userService = new UserService();
        }

        protected override async void OnAppearing()
        {
            base.OnAppearing();

            // Get the email parameter from query string
            if (Shell.Current?.CurrentState?.Location != null)
            {
                var query = Shell.Current.CurrentState.Location;
                if (query.Query != null)
                {
                    if (query.Query.Contains("email="))
                    {
                        _userEmail = query.Query.Replace("?email=", "").Trim();
                        await LoadUserData(_userEmail);
                    }
                }
            }

            // Fallback: try to get email from SecureStorage
            if (string.IsNullOrEmpty(_userEmail))
            {
                _userEmail = await SecureStorage.GetAsync("user_email");
                if (!string.IsNullOrEmpty(_userEmail))
                {
                    await LoadUserData(_userEmail);
                }
            }
        }

        private async Task LoadUserData(string email)
        {
            var user = await _userService.GetUserProfileAsync(email);
            if (user != null)
            {
                NicknameEntry.Text = user.Nickname;
                EmailEntry.Text = user.Email;
                PeriodLengthEntry.Text = user.PeriodLength > 0 ? user.PeriodLength.ToString() : "";
                CycleLengthEntry.Text = user.CycleLength > 0 ? user.CycleLength.ToString() : "";

                if (!string.IsNullOrEmpty(user.ProfileImagePath) && File.Exists(user.ProfileImagePath))
                {
                    ProfileImage.Source = ImageSource.FromFile(user.ProfileImagePath);
                    _profileImagePath = user.ProfileImagePath;
                }
            }
        }

        private async void OnChangePhotoClicked(object sender, EventArgs e)
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
                    var fileName = $"{_userEmail}_profile{Path.GetExtension(result.FileName)}";
                    _profileImagePath = Path.Combine(FileSystem.AppDataDirectory, fileName);

                    using (var stream = await result.OpenReadAsync())
                    using (var fileStream = File.OpenWrite(_profileImagePath))
                    {
                        await stream.CopyToAsync(fileStream);
                    }

                    // Update profile image
                    ProfileImage.Source = ImageSource.FromFile(_profileImagePath);
                }
            }
            catch (Exception ex)
            {
                await DisplayAlert("Error", "Failed to select image: " + ex.Message, "OK");
            }
        }

        private async void OnCancelClicked(object sender, EventArgs e)
        {
            await Shell.Current.GoToAsync(".."); // Go back to previous page
        }

        private async void OnSaveClicked(object sender, EventArgs e)
        {
            if (string.IsNullOrEmpty(_userEmail))
            {
                await DisplayAlert("Error", "User not found", "OK");
                return;
            }

            // Validate inputs
            if (string.IsNullOrWhiteSpace(EmailEntry.Text))
            {
                await DisplayAlert("Error", "Email is required", "OK");
                return;
            }

            // Get user
            var user = await _userService.GetUserProfileAsync(_userEmail);
            if (user == null)
            {
                await DisplayAlert("Error", "User not found", "OK");
                return;
            }

            // Update user data
            user.Nickname = NicknameEntry.Text;
            user.Email = EmailEntry.Text;

            if (int.TryParse(PeriodLengthEntry.Text, out int periodLength))
            {
                user.PeriodLength = periodLength;
            }

            if (int.TryParse(CycleLengthEntry.Text, out int cycleLength))
            {
                user.CycleLength = cycleLength;
            }

            if (!string.IsNullOrEmpty(_profileImagePath))
            {
                user.ProfileImagePath = _profileImagePath;
            }

            // Save changes
            bool success = await _userService.UpdateUserProfileAsync(user);
            if (success)
            {
                await DisplayAlert("Success", "Profile updated successfully", "OK");
                await Shell.Current.GoToAsync(".."); // Go back to previous page
            }
            else
            {
                await DisplayAlert("Error", "Failed to update profile", "OK");
            }
        }
    }
}
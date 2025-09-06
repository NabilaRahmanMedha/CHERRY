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
        private readonly ProfileApiService _profileApi;
        private readonly AuthService _auth;
        private ProfileApiService.UserProfileDto _profile = new ProfileApiService.UserProfileDto();
        private Stream _pendingImageStream;
        private string _pendingImageName;

        public EditProfilePage()
        {
            InitializeComponent();
            _profileApi = ServiceHelper.GetService<ProfileApiService>();
            _auth = ServiceHelper.GetService<AuthService>();
        }

        protected override async void OnAppearing()
        {
            base.OnAppearing();

            await LoadUserData();
        }

        private async Task LoadUserData()
        {
            var email = await _auth.GetEmailAsync();
            EmailEntry.Text = email;
            var profile = await _profileApi.GetProfileAsync();
            if (profile == null) return;
            _profile = profile;
            NicknameEntry.Text = _profile.Nickname;
            PeriodLengthEntry.Text = _profile.PeriodLength > 0 ? _profile.PeriodLength.ToString() : "";
            CycleLengthEntry.Text = _profile.CycleLength > 0 ? _profile.CycleLength.ToString() : "";
            if (!string.IsNullOrWhiteSpace(_profile.ProfileImageUrl))
            {
                ProfileImage.Source = ImageSource.FromUri(new Uri(new Uri(ServiceHelper.GetService<HttpClient>().BaseAddress!, ".").ToString().TrimEnd('/') + _profile.ProfileImageUrl));
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
                    _pendingImageStream = await result.OpenReadAsync();
                    _pendingImageName = result.FileName;
                    ProfileImage.Source = ImageSource.FromStream(() => _pendingImageStream);
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
            // Validate inputs
            if (string.IsNullOrWhiteSpace(EmailEntry.Text))
            {
                await DisplayAlert("Error", "Email is required", "OK");
                return;
            }

            if (int.TryParse(PeriodLengthEntry.Text, out int periodLength)) _profile.PeriodLength = periodLength; else _profile.PeriodLength = 0;
            if (int.TryParse(CycleLengthEntry.Text, out int cycleLength)) _profile.CycleLength = cycleLength; else _profile.CycleLength = 0;
            _profile.Nickname = NicknameEntry.Text;

            if (_pendingImageStream != null)
            {
                var url = await _profileApi.UploadProfileImageAsync(_pendingImageStream, _pendingImageName, "image/*");
                if (!string.IsNullOrWhiteSpace(url)) _profile.ProfileImageUrl = url;
            }

            bool success = await _profileApi.UpdateProfileAsync(_profile);
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
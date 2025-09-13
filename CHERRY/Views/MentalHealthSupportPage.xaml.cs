using Microsoft.Maui.Controls;
using Microsoft.Maui.ApplicationModel.Communication; // For PhoneDialer

namespace CHERRY.Views
{
    public partial class MentalHealthSupportPage : ContentPage
    {
        public MentalHealthSupportPage()
        {
            InitializeComponent();
        }

        // Navigate or API call for finding professionals
        private async void OnFindProfessionalsClicked(object sender, EventArgs e)
        {
            await DisplayAlert("Find Professionals", "This will show mental health professionals.", "OK");
        }

        // Navigate or API call for community resources
        private async void OnSupportNetworksClicked(object sender, EventArgs e)
        {
            await DisplayAlert("Support Networks", "This will show community and peer support resources.", "OK");
        }

        // Emergency call handler
        private async void OnEmergencyCallClicked(object sender, EventArgs e)
        {
            bool proceed = await DisplayAlert("Emergency Call",
                "Are you sure you want to call emergency services (999)?", "Call", "Cancel");

            if (proceed)
            {
                try
                {
                    PhoneDialer.Open("999");
                }
                catch (Exception ex)
                {
                    await DisplayAlert("Error", "Could not make the call. Please check your device.", "OK");
                    Console.WriteLine($"Call error: {ex.Message}");
                }
            }
        }

        // Mental health hotline handler
        private async void OnMentalHealthHotlineClicked(object sender, EventArgs e)
        {
            bool proceed = await DisplayAlert("Hotline",
                "Do you want to call the Mental Health Hotline (16263)?", "Call", "Cancel");

            if (proceed)
            {
                try
                {
                    PhoneDialer.Open("16263");
                }
                catch (Exception ex)
                {
                    await DisplayAlert("Error", "Could not make the call. Please check your device.", "OK");
                    Console.WriteLine($"Call error: {ex.Message}");
                }
            }
        }
    }
}

using Microsoft.Maui.Controls;

namespace CHERRY.Views
{
    public partial class SexualHealthPage : ContentPage
    {
        public SexualHealthPage()
        {
            InitializeComponent();
        }

        private async void OnBackClicked(object sender, EventArgs e)
        {
            await Navigation.PopAsync();
        }
    }
}

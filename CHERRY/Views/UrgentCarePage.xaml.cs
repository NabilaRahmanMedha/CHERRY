using Microsoft.Maui.Controls;

namespace CHERRY.Views
{
    public partial class UrgentCarePage : ContentPage
    {
        public UrgentCarePage()
        {
            InitializeComponent();
        }

        private async void OnBackClicked(object sender, EventArgs e)
        {
            await Navigation.PopAsync();
        }
    }
}

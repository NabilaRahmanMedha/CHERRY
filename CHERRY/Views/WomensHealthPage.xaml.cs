using Microsoft.Maui.Controls;

namespace CHERRY.Views;

public partial class WomensHealthPage : ContentPage
{
    public WomensHealthPage()
    {
        InitializeComponent();
    }

    private async void OnBackClicked(object sender, EventArgs e)
    {
        await Navigation.PopAsync();
    }
}

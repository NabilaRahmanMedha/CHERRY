using CHERRY.Services;
using CHERRY.Models;
using Microsoft.Maui.Controls;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
namespace CHERRY.Views;

public partial class CyclePage : ContentPage
{
    public CyclePage()
    {
        InitializeComponent();
    }
    private async void OnLogPeriodClicked(object sender, EventArgs e)
    {
        await DisplayAlert("Log Period", "Here you can log your period details.", "OK");
        // Later: Navigate to a PeriodLogPage
    }

    private async void OnCalendarClicked(object sender, EventArgs e)
    {
        await DisplayAlert("Calendar", "Here you can view your cycle calendar.", "OK");
        // Later: Navigate to CalendarPage
    }

    private async void OnSettingsClicked(object sender, EventArgs e)
    {
        await DisplayAlert("Settings", "Here you can change app settings.", "OK");
        // Later: Navigate to SettingsPage
    }
}

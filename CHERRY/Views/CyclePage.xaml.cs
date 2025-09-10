using CHERRY.Services;
using Microsoft.Maui.Controls;
using Microsoft.Maui.ApplicationModel;
using MauiColor = Microsoft.Maui.Graphics.Color;
#if ANDROID
using Microsoft.Maui.Platform;
using AndroidColor = Android.Graphics.Color;
#endif
using System;
using System.Collections.Generic;
using System.Linq;

namespace CHERRY.Views
{
    public partial class CyclePage : ContentPage
    {
        private CycleService _cycleService = new CycleService();

        public CyclePage()
        {
            InitializeComponent();
            UpdateUI();
        }

        protected override void OnAppearing()
        {
            base.OnAppearing();
            UpdateUI();
        }

        protected override void OnDisappearing()
        {
            base.OnDisappearing();
            // Restore the app's default pink for other pages
            var shell = Shell.Current;
            var defaultPink = Microsoft.Maui.Graphics.Color.FromArgb("#eb3f95");
            try
            {
                if (shell != null)
                {
                    Shell.SetBackgroundColor(shell, defaultPink);
                    Shell.SetForegroundColor(shell, Microsoft.Maui.Graphics.Colors.White);
                    Shell.SetTitleColor(shell, Microsoft.Maui.Graphics.Colors.White);
                }

#if ANDROID
                var window = (Platform.CurrentActivity?.Window);
                if (window != null)
                {
                    var native = AndroidColor.Argb(
                        (int)(defaultPink.Alpha * 255),
                        (int)(defaultPink.Red * 255),
                        (int)(defaultPink.Green * 255),
                        (int)(defaultPink.Blue * 255));
                    window.SetStatusBarColor(native);
                }
#endif
            }
            catch { }
        }

        private void UpdateUI()
        {
            // Set current date
            CurrentDateLabel.Text = DateTime.Now.ToString("dddd, MMM dd");

            var cycleData = _cycleService.GetCycleData();

            if (!cycleData.HasData)
            {
                SetDefaultValues();
                return;
            }

            UpdateCycleStatus(cycleData);
            UpdateRecentHistory(cycleData);
            UpdateStatistics(cycleData);
            UpdateDailyTip(cycleData);
        }

        private void SetDefaultValues()
        {
            CycleDayLabel.Text = "-";
            NextPeriodLabel.Text = "-";
            OvulationLabel.Text = "-";
            FertilityLabel.Text = "-";

            RecentPeriod1Label.Text = "-";
            RecentDuration1Label.Text = "-";
            RecentCycle1Label.Text = "-";

            RecentPeriod2Label.Text = "-";
            RecentDuration2Label.Text = "-";
            RecentCycle2Label.Text = "-";

            AvgCycleLabel.Text = "-";
            AvgPeriodLabel.Text = "-";
            OvulationDayLabel.Text = "-";
            CyclePatternLabel.Text = "-";

            DailyTipLabel.Text = "Track your period to get personalized tips and predictions.";
        }

        private void UpdateCycleStatus(CycleData cycleData)
        {
            CycleDayLabel.Text = cycleData.CurrentCycleDay > 0 ? $"Day {cycleData.CurrentCycleDay}" : "-";

            if (cycleData.DaysUntilNextPeriod > 0)
            {
                NextPeriodLabel.Text = $"in {cycleData.DaysUntilNextPeriod} days";
            }
            else if (cycleData.DaysUntilNextPeriod == 0)
            {
                NextPeriodLabel.Text = "Today";
            }
            else
            {
                NextPeriodLabel.Text = "-";
            }

            if (Math.Abs(cycleData.DaysUntilOvulation) <= 2)
            {
                OvulationLabel.Text = "Now";
            }
            else if (cycleData.DaysUntilOvulation != 0)
            {
                OvulationLabel.Text = $"in {Math.Abs(cycleData.DaysUntilOvulation)} days";
            }
            else
            {
                OvulationLabel.Text = "-";
            }

            FertilityLabel.Text = !string.IsNullOrEmpty(cycleData.FertilityStatus) ?
                cycleData.FertilityStatus : "-";
        }

        private void UpdateRecentHistory(CycleData cycleData)
        {
            var sortedCycles = cycleData.CycleHistory.OrderByDescending(c => c.StartDate).ToList();

            if (sortedCycles.Count > 0)
            {
                RecentPeriod1Label.Text = sortedCycles[0].StartDate.ToString("MMM d, yyyy");
                RecentDuration1Label.Text = $"{sortedCycles[0].Duration} days";

                if (sortedCycles.Count > 1)
                {
                    TimeSpan cycleLength = sortedCycles[0].StartDate - sortedCycles[1].StartDate;
                    RecentCycle1Label.Text = $"{cycleLength.Days} days";
                }
                else
                {
                    RecentCycle1Label.Text = "-";
                }
            }
            else
            {
                RecentPeriod1Label.Text = "-";
                RecentDuration1Label.Text = "-";
                RecentCycle1Label.Text = "-";
            }

            if (sortedCycles.Count > 1)
            {
                RecentPeriod2Label.Text = sortedCycles[1].StartDate.ToString("MMM d, yyyy");
                RecentDuration2Label.Text = $"{sortedCycles[1].Duration} days";

                if (sortedCycles.Count > 2)
                {
                    TimeSpan cycleLength = sortedCycles[1].StartDate - sortedCycles[2].StartDate;
                    RecentCycle2Label.Text = $"{cycleLength.Days} days";
                }
                else
                {
                    RecentCycle2Label.Text = "-";
                }
            }
            else
            {
                RecentPeriod2Label.Text = "-";
                RecentDuration2Label.Text = "-";
                RecentCycle2Label.Text = "-";
            }
        }

        private void UpdateStatistics(CycleData cycleData)
        {
            AvgCycleLabel.Text = cycleData.AverageCycleLength > 0 ?
                cycleData.AverageCycleLength.ToString() : "-";

            // Calculate average period length
            int avgPeriodLength = cycleData.CycleHistory.Any() ?
                (int)cycleData.CycleHistory.Average(c => c.Duration) : 0;
            AvgPeriodLabel.Text = avgPeriodLength > 0 ? avgPeriodLength.ToString() : "-";

            // Set ovulation day
            OvulationDayLabel.Text = cycleData.AverageCycleLength > 0 ?
                (cycleData.AverageCycleLength - 14).ToString() : "-";

            // Determine cycle pattern
            CyclePatternLabel.Text = cycleData.AverageCycleLength > 0 ?
                GetCyclePattern(cycleData.AverageCycleLength) : "-";
        }

        private string GetCyclePattern(int avgCycleLength)
        {
            return (avgCycleLength >= 21 && avgCycleLength <= 35) ? "Regular" : "Irregular";
        }

        private void UpdateDailyTip(CycleData cycleData)
        {
            var (tip, backgroundColor) = _cycleService.GetDailyTip(cycleData);
            DailyTipLabel.Text = tip;
            HeaderBorder.BackgroundColor = backgroundColor;

            // Dynamically update Shell/Nav colors to match content theme
            try
            {
                // Only change the top navigation/status bar tint; leave TabBar colors as defined in XAML/styles
                var shell = Shell.Current;
                if (shell != null)
                {
                    Shell.SetBackgroundColor(shell, backgroundColor);
                    Shell.SetForegroundColor(shell, Microsoft.Maui.Graphics.Colors.White);
                    Shell.SetTitleColor(shell, Microsoft.Maui.Graphics.Colors.White);
                }

#if ANDROID
                // Also update Android status bar color to match prediction
                var window = (Platform.CurrentActivity?.Window);
                if (window != null)
                {
                    // Convert MAUI Color to Android Color manually to avoid missing extension method issues
                    var native = AndroidColor.Argb(
                        (int)(backgroundColor.Alpha * 255),
                        (int)(backgroundColor.Red * 255),
                        (int)(backgroundColor.Green * 255),
                        (int)(backgroundColor.Blue * 255));
                    window.SetStatusBarColor(native);
                }
#endif
            }
            catch { }

            // Send/update persistent notification of prediction
            try
            {
                var notifier = ServiceHelper.GetService<INotificationService>();
                notifier?.ShowOrUpdatePersistent("daily_prediction", "Prediction", tip);
            }
            catch { }
        }

        private async void OnLogPeriodClicked(object sender, EventArgs e)
        {
            await Shell.Current.GoToAsync("//CalendarPage");
        }

        private async void OnCalendarClicked(object sender, EventArgs e)
        {
            await Shell.Current.GoToAsync("//CalendarPage");
        }

        private async void OnProfileClicked(object sender, EventArgs e)
        {
            await Shell.Current.GoToAsync("//ProfilePage");
        }

        private async void OnSettingsClicked(object sender, EventArgs e)
        {
            await DisplayAlert("Settings", "Here you can change app settings.", "OK");
        }

        private async void OnMenuClicked(object sender, EventArgs e)
        {
            await DisplayAlert("Settings", "Here you can change app settings.", "OK");
        }

        private async void OnEmergencyClicked(object sender, EventArgs e)
        {
            await Shell.Current.GoToAsync("//EmergencyPage");
        }
    }
}
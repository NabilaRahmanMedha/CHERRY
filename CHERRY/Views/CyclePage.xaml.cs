using CHERRY.Services;
using Microsoft.Maui.Controls;
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

            AvgCycleLabel.Text = "28";
            AvgPeriodLabel.Text = "5";
            OvulationDayLabel.Text = "14";
            CyclePatternLabel.Text = "Regular";

            DailyTipLabel.Text = "Track your period to get personalized tips and predictions.";
        }

        private void UpdateCycleStatus(CycleData cycleData)
        {
            CycleDayLabel.Text = $"Day {cycleData.CurrentCycleDay}";
            NextPeriodLabel.Text = cycleData.DaysUntilNextPeriod > 0 ? $"in {cycleData.DaysUntilNextPeriod} days" : "Today";
            OvulationLabel.Text = Math.Abs(cycleData.DaysUntilOvulation) <= 2 ? "Now" : $"in {Math.Abs(cycleData.DaysUntilOvulation)} days";
            FertilityLabel.Text = cycleData.FertilityStatus;
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
            }
        }

        private void UpdateStatistics(CycleData cycleData)
        {
            AvgCycleLabel.Text = cycleData.AverageCycleLength.ToString();

            // Calculate average period length
            int avgPeriodLength = (int)cycleData.CycleHistory.Average(c => c.Duration);
            AvgPeriodLabel.Text = avgPeriodLength.ToString();

            // Set ovulation day
            OvulationDayLabel.Text = (cycleData.AverageCycleLength - 14).ToString();

            // Determine cycle pattern
            CyclePatternLabel.Text = GetCyclePattern(cycleData.AverageCycleLength);
        }

        private string GetCyclePattern(int avgCycleLength)
        {
            return (avgCycleLength >= 21 && avgCycleLength <= 35) ? "Regular" : "Irregular";
        }

        private void UpdateDailyTip(CycleData cycleData)
        {
            DailyTipLabel.Text = _cycleService.GetDailyTip(cycleData);
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
    }
}
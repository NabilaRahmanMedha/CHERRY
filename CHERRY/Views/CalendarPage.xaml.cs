using Microsoft.Maui.Controls;
using Microsoft.Maui;
using System;
using System.Collections.Generic;
using System.Linq;

namespace CHERRY.Views
{
    public partial class CalendarPage : ContentPage
    {
        private DateTime _currentDate;
        private List<DateTime> _periodDays = new List<DateTime>();
        private List<DateTime> _predictedPeriodDays = new List<DateTime>();
        private List<DateTime> _ovulationDays = new List<DateTime>();
        private List<Cycle> _cycleHistory = new List<Cycle>();
        private int _averageCycleLength = 28;

        public CalendarPage()
        {
            InitializeComponent();
            _currentDate = DateTime.Today;
            LoadUserData();
            UpdateCalendar();
            UpdateStats();
        }

        protected override void OnAppearing()
        {
            base.OnAppearing();
            LoadUserData();
            UpdateCalendar();
            UpdateStats();
        }

        private void LoadUserData()
        {
            if (Preferences.ContainsKey("CycleHistory"))
            {
                string historyJson = Preferences.Get("CycleHistory", string.Empty);
                if (!string.IsNullOrEmpty(historyJson))
                {
                    _cycleHistory = System.Text.Json.JsonSerializer.Deserialize<List<Cycle>>(historyJson);
                }
            }

            _averageCycleLength = Preferences.Get("AverageCycleLength", 28);

            _periodDays.Clear();
            foreach (var cycle in _cycleHistory)
            {
                for (int i = 0; i < cycle.Duration; i++)
                {
                    _periodDays.Add(cycle.StartDate.AddDays(i));
                }
            }

            CalculatePrediction();
        }

        private void SaveUserData()
        {
            string historyJson = System.Text.Json.JsonSerializer.Serialize(_cycleHistory);
            Preferences.Set("CycleHistory", historyJson);
            Preferences.Set("AverageCycleLength", _averageCycleLength);
        }

        private void CalculatePrediction()
        {
            if (_cycleHistory.Count == 0)
            {
                PredictionLabel.Text = "Mark your period to get predictions";
                _predictedPeriodDays.Clear();
                _ovulationDays.Clear();
                return;
            }

            var lastPeriod = _cycleHistory.OrderByDescending(c => c.StartDate).First();
            DateTime nextPredictedStart = lastPeriod.StartDate.AddDays(_averageCycleLength);

            _predictedPeriodDays.Clear();
            for (int i = 0; i < 5; i++)
            {
                _predictedPeriodDays.Add(nextPredictedStart.AddDays(i));
            }

            DateTime ovulationDate = nextPredictedStart.AddDays(-14);
            _ovulationDays.Clear();
            for (int i = -2; i <= 2; i++)
            {
                _ovulationDays.Add(ovulationDate.AddDays(i));
            }

            PredictionLabel.Text = $"Next period: {nextPredictedStart:MMM d}\nOvulation: {ovulationDate:MMM d}";

            if (_cycleHistory.Count >= 2)
            {
                RecalculateAverageCycleLength();
            }
        }

        private void UpdateStats()
        {
            if (_cycleHistory.Count == 0)
            {
                CycleDayLabel.Text = "-";
                NextPeriodLabel.Text = "-";
                OvulationLabel.Text = "-";
                FertilityLabel.Text = "-";
                return;
            }

            // Calculate current cycle day
            var lastPeriod = _cycleHistory.OrderByDescending(c => c.StartDate).First();
            int daysSinceLastPeriod = (DateTime.Today - lastPeriod.StartDate).Days;
            int currentCycleDay = (daysSinceLastPeriod % _averageCycleLength) + 1;
            CycleDayLabel.Text = currentCycleDay.ToString();

            // Calculate days until next period
            DateTime nextPeriodStart = lastPeriod.StartDate.AddDays(_averageCycleLength);
            int daysUntilNextPeriod = (nextPeriodStart - DateTime.Today).Days;
            NextPeriodLabel.Text = daysUntilNextPeriod > 0 ? $"{daysUntilNextPeriod}d" : "Today";

            // Calculate days until ovulation
            DateTime ovulationDate = nextPeriodStart.AddDays(-14);
            int daysUntilOvulation = (ovulationDate - DateTime.Today).Days;
            OvulationLabel.Text = Math.Abs(daysUntilOvulation) <= 2 ? "Now" : $"{daysUntilOvulation}d";

            // Determine fertility status
            string fertilityStatus = "Low";
            if (daysUntilOvulation >= -2 && daysUntilOvulation <= 2)
            {
                fertilityStatus = "High";
            }
            else if (daysUntilOvulation >= -5 && daysUntilOvulation <= 5)
            {
                fertilityStatus = "Medium";
            }
            FertilityLabel.Text = fertilityStatus;
        }

        private void RecalculateAverageCycleLength()
        {
            int totalDays = 0;
            int cycleCount = 0;

            var sortedCycles = _cycleHistory.OrderBy(c => c.StartDate).ToList();

            for (int i = 1; i < sortedCycles.Count; i++)
            {
                TimeSpan cycleLength = sortedCycles[i].StartDate - sortedCycles[i - 1].StartDate;
                totalDays += cycleLength.Days;
                cycleCount++;
            }

            if (cycleCount > 0)
            {
                _averageCycleLength = totalDays / cycleCount;
            }
        }

        private void UpdateCalendar()
        {
            MonthLabel.Text = _currentDate.ToString("MMMM yyyy").ToUpper();

            CalendarGrid.Children.Clear();
            CalendarGrid.RowDefinitions.Clear();
            CalendarGrid.ColumnDefinitions.Clear();

            for (int i = 0; i < 7; i++)
            {
                CalendarGrid.ColumnDefinitions.Add(new ColumnDefinition());
            }

            CalendarGrid.RowDefinitions.Add(new RowDefinition { Height = GridLength.Auto });

            string[] days = { "MON", "TUE", "WED", "THU", "FRI", "SAT", "SUN" };
            for (int i = 0; i < 7; i++)
            {
                var label = new Label
                {
                    Text = days[i],
                    Style = (Style)Resources["DayHeaderLabelStyle"]
                };
                CalendarGrid.Add(label, i, 0);
            }

            DateTime firstDayOfMonth = new DateTime(_currentDate.Year, _currentDate.Month, 1);
            int daysInMonth = DateTime.DaysInMonth(_currentDate.Year, _currentDate.Month);
            int startPosition = ((int)firstDayOfMonth.DayOfWeek + 6) % 7;

            int row = 1;
            int col = startPosition;

            for (int day = 1; day <= daysInMonth; day++)
            {
                if (row >= CalendarGrid.RowDefinitions.Count)
                {
                    CalendarGrid.RowDefinitions.Add(new RowDefinition { Height = GridLength.Star });
                }

                DateTime currentDay = new DateTime(_currentDate.Year, _currentDate.Month, day);
                bool isPeriodDay = _periodDays.Contains(currentDay.Date);
                bool isPredictedDay = _predictedPeriodDays.Contains(currentDay.Date);
                bool isOvulationDay = _ovulationDays.Contains(currentDay.Date);
                bool isToday = currentDay.Date == DateTime.Today.Date;
                bool isFutureDate = currentDay.Date > DateTime.Today.Date;

                Border dayBorder = new Border
                {
                    Style = (Style)Resources["DayFrameStyle"]
                };

                if (isPeriodDay)
                    dayBorder.BackgroundColor = (Color)Resources["PeriodColor"];
                else if (isPredictedDay)
                    dayBorder.BackgroundColor = (Color)Resources["PredictedPeriodColor"];
                else if (isOvulationDay)
                    dayBorder.BackgroundColor = (Color)Resources["OvulationColor"];
                else if (isFutureDate)
                    dayBorder.BackgroundColor = (Color)Resources["LightCreamColor"];

                if (isToday)
                {
                    dayBorder.Stroke = (Color)Resources["TodayBorderColor"];
                }

                var dayLabel = new Label
                {
                    Text = day.ToString(),
                    Style = (Style)Resources["DayLabelStyle"]
                };

                if (isFutureDate)
                    dayLabel.TextColor = (Color)Resources["DarkGrayTextColor"];
                else if (isPredictedDay)
                    dayLabel.TextColor = (Color)Resources["GrayTextColor"];

                dayBorder.Content = dayLabel;

                if (!isFutureDate)
                {
                    var tapGesture = new TapGestureRecognizer();
                    tapGesture.Tapped += (s, e) => OnDayClicked(currentDay);
                    dayBorder.GestureRecognizers.Add(tapGesture);
                }
                else if (isPredictedDay || isOvulationDay)
                {
                    var tapGesture = new TapGestureRecognizer();
                    tapGesture.Tapped += (s, e) => OnFutureDayTapped(currentDay, isPredictedDay, isOvulationDay);
                    dayBorder.GestureRecognizers.Add(tapGesture);
                }

                CalendarGrid.Add(dayBorder, col, row);
                col++;

                if (col > 6)
                {
                    col = 0;
                    row++;
                }
            }
        }

        private async void OnFutureDayTapped(DateTime date, bool isPredicted, bool isOvulation)
        {
            string message = "";

            if (isPredicted)
                message = $"Predicted period day based on your cycle history.";
            else if (isOvulation)
                message = $"Predicted ovulation day based on your cycle history.";

            await DisplayAlert("Prediction", message, "OK");
        }

        private void OnDayClicked(DateTime date)
        {
            if (date.Date > DateTime.Today.Date)
            {
                DisplayAlert("Invalid Date", "You cannot mark periods for future dates.", "OK");
                return;
            }

            bool isPeriodDay = _periodDays.Contains(date.Date);

            if (!isPeriodDay)
            {
                int daysToAdd = 5;
                for (int i = 0; i < daysToAdd; i++)
                {
                    DateTime periodDay = date.AddDays(i);
                    if (periodDay.Date > DateTime.Today.Date)
                    {
                        daysToAdd = i;
                        break;
                    }

                    if (!_periodDays.Contains(periodDay.Date))
                    {
                        _periodDays.Add(periodDay.Date);
                    }
                }

                _cycleHistory.Add(new Cycle
                {
                    StartDate = date.Date,
                    Duration = daysToAdd
                });

                CalculatePrediction();
            }
            else
            {
                var periodToRemove = _cycleHistory.FirstOrDefault(c =>
                    date.Date >= c.StartDate && date.Date < c.StartDate.AddDays(c.Duration));

                if (periodToRemove != null)
                {
                    for (int i = 0; i < periodToRemove.Duration; i++)
                    {
                        DateTime periodDay = periodToRemove.StartDate.AddDays(i);
                        _periodDays.Remove(periodDay);
                    }

                    _cycleHistory.Remove(periodToRemove);
                    CalculatePrediction();
                }
            }

            SaveUserData();
            UpdateCalendar();
            UpdateStats();
        }

        private void OnPrevMonthClicked(object sender, EventArgs e)
        {
            _currentDate = _currentDate.AddMonths(-1);
            UpdateCalendar();
        }

        private void OnNextMonthClicked(object sender, EventArgs e)
        {
            _currentDate = _currentDate.AddMonths(1);
            UpdateCalendar();
        }

        private async void OnEditClicked(object sender, EventArgs e)
        {
            string action = await DisplayActionSheet("Edit Period", "Cancel", null,
                "Mark Period", "Clear All", "View Cycle Info", "Set Cycle Length");

            if (action == "Mark Period")
            {
                await DisplayAlert("Info", "Click on a day to mark the start of your period. The next 5 days will be marked automatically. You can only mark past or current dates.", "OK");
            }
            else if (action == "Clear All")
            {
                bool confirm = await DisplayAlert("Confirm", "Are you sure you want to clear all period data?", "Yes", "No");
                if (confirm)
                {
                    _periodDays.Clear();
                    _cycleHistory.Clear();
                    _predictedPeriodDays.Clear();
                    _ovulationDays.Clear();
                    PredictionLabel.Text = "Track your cycles to get predictions";
                    SaveUserData();
                    UpdateCalendar();
                    UpdateStats();
                }
            }
            else if (action == "View Cycle Info")
            {
                string info = "Cycle Information:\n\n";

                if (_cycleHistory.Count > 0)
                {
                    var sortedCycles = _cycleHistory.OrderBy(c => c.StartDate).ToList();
                    info += "Your cycle history:\n";

                    foreach (var cycle in sortedCycles)
                    {
                        info += $"{cycle.StartDate:MMM d} ({cycle.Duration} days)\n";
                    }

                    info += $"\nAverage cycle length: {_averageCycleLength} days\n";

                    if (_predictedPeriodDays.Count > 0)
                    {
                        info += $"\nNext period: {_predictedPeriodDays[0]:MMM d}";
                        info += $"\nOvulation: {_ovulationDays[2]:MMM d}";
                    }
                }
                else
                {
                    info += "No cycle data available. Mark your period to get predictions.";
                }

                await DisplayAlert("Cycle Information", info, "OK");
            }
            else if (action == "Set Cycle Length")
            {
                string result = await DisplayPromptAsync("Set Cycle Length",
                    "Enter your average cycle length (days):",
                    keyboard: Keyboard.Numeric,
                    initialValue: _averageCycleLength.ToString());

                if (int.TryParse(result, out int newLength) && newLength > 0)
                {
                    _averageCycleLength = newLength;
                    CalculatePrediction();
                    SaveUserData();
                    UpdateCalendar();
                    UpdateStats();
                }
            }
        }
    }

    public class Cycle
    {
        public DateTime StartDate { get; set; }
        public int Duration { get; set; }
    }
}
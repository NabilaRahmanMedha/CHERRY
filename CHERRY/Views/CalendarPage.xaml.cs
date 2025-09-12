using CHERRY.Services;
using Microsoft.Maui.Controls;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace CHERRY.Views
{
    public partial class CalendarPage : ContentPage
    {
        private DateTime _currentDate;
        private DateTime? _selectedStartDate = null;
        private readonly CycleService _cycleService = new CycleService();
        private readonly CycleApiService _apiService;
        private List<DateTime> _periodDays = new List<DateTime>();
        private List<DateTime> _predictedPeriodDays = new List<DateTime>();
        private List<DateTime> _ovulationDays = new List<DateTime>();

        public CalendarPage()
        {
            InitializeComponent();
            _currentDate = DateTime.Today;
            LoadCalendarData();
            UpdateCalendar();
            UpdateStats();
        }

        public CalendarPage(CycleApiService apiService)
        {
            InitializeComponent();
            _apiService = apiService;
            _currentDate = DateTime.Today;
            _ = SyncFromServer();
            LoadCalendarData();
            UpdateCalendar();
            UpdateStats();
        }

        protected override void OnAppearing()
        {
            base.OnAppearing();
            _ = SyncFromServer();
            LoadCalendarData();
            UpdateCalendar();
            UpdateStats();
        }

        private void LoadCalendarData()
        {
            // Load period days from cycle history
            _periodDays.Clear();
            var cycleHistory = _cycleService.GetCycleHistory();
            foreach (var cycle in cycleHistory)
            {
                for (int i = 0; i < cycle.Duration; i++)
                {
                    _periodDays.Add(cycle.StartDate.AddDays(i));
                }
            }

            // Load predictions
            CalculatePredictions();
        }

        private void CalculatePredictions()
        {
            var cycleData = _cycleService.GetCycleData();

            if (!cycleData.HasData)
            {
                PredictionLabel.Text = "Mark your period to get predictions";
                _predictedPeriodDays.Clear();
                _ovulationDays.Clear();
                return;
            }

            // Get predicted period start dates for multiple cycles
            var predictedPeriodStartDates = _cycleService.GetPredictedPeriodDates(6);
            int averagePeriodLength = _cycleService.GetAveragePeriodLength();

            // Create full period predictions (multiple days for each period)
            _predictedPeriodDays = new List<DateTime>();
            foreach (var periodStartDate in predictedPeriodStartDates)
            {
                for (int i = 0; i < averagePeriodLength; i++)
                {
                    DateTime periodDay = periodStartDate.AddDays(i);
                    if (!_predictedPeriodDays.Contains(periodDay.Date))
                    {
                        _predictedPeriodDays.Add(periodDay.Date);
                    }
                }
            }

            // Get predicted ovulation dates - 9-day window
            _ovulationDays = new List<DateTime>();
            foreach (var periodStartDate in predictedPeriodStartDates)
            {
                DateTime ovulationStart = periodStartDate.AddDays(-17); // 5 days before ovulation + ovulation day
                DateTime ovulationEnd = periodStartDate.AddDays(-9);    // 4 days after ovulation

                for (DateTime date = ovulationStart; date <= ovulationEnd; date = date.AddDays(1))
                {
                    if (!_ovulationDays.Contains(date.Date))
                    {
                        _ovulationDays.Add(date.Date);
                    }
                }
            }

            // Update prediction label with average period length
            if (predictedPeriodStartDates.Count > 0)
            {
                DateTime nextPeriodStart = predictedPeriodStartDates[0];
                DateTime ovulationDate = nextPeriodStart.AddDays(-14);
                int avgCycleLength = _cycleService.GetAverageCycleLength();

                PredictionLabel.Text = $"Next period: {nextPeriodStart:MMM d}-{nextPeriodStart.AddDays(averagePeriodLength - 1):MMM d}\n" +
                                      $"Ovulation window: {ovulationDate.AddDays(-4):MMM d}-{ovulationDate.AddDays(4):MMM d}\n";
            }
        }

        private void CreateDayCell(DateTime date, int column, int row)
        {
            bool isPeriodDay = _periodDays.Contains(date.Date);
            bool isPredictedDay = _predictedPeriodDays.Contains(date.Date);
            bool isOvulationDay = _ovulationDays.Contains(date.Date);
            bool isToday = date.Date == DateTime.Today.Date;
            bool isFutureDate = date.Date > DateTime.Today.Date;
            bool isSelectedStart = _selectedStartDate?.Date == date.Date;

            Border dayBorder = new Border
            {
                Style = (Style)Resources["DayFrameStyle"]
            };

            // Set background color based on day type
            if (isSelectedStart)
            {
                dayBorder.BackgroundColor = (Color)Resources["PeriodColor"];
            }
            else if (isPeriodDay)
            {
                dayBorder.BackgroundColor = (Color)Resources["PeriodColor"];
            }
            else if (isPredictedDay)
            {
                dayBorder.BackgroundColor = (Color)Resources["PredictedPeriodColor"];
            }
            else if (isOvulationDay)
            {
                dayBorder.BackgroundColor = (Color)Resources["OvulationColor"];
            }
            else if (isFutureDate)
            {
                dayBorder.BackgroundColor = (Color)Resources["LightCreamColor"];
            }

            // Add border for today
            if (isToday)
            {
                dayBorder.Stroke = (Color)Resources["TodayBorderColor"];
                dayBorder.StrokeThickness = 2;
            }

            var dayLabel = new Label
            {
                Text = date.Day.ToString(),
                Style = (Style)Resources["DayLabelStyle"]
            };

            // Set text color
            if (isFutureDate || isPredictedDay)
            {
                dayLabel.TextColor = (Color)Resources["DarkGrayTextColor"];
            }
            else if (isSelectedStart)
            {
                dayLabel.TextColor = Colors.White;
            }

            dayBorder.Content = dayLabel;

            // Add tap gesture
            var tapGesture = new TapGestureRecognizer();
            tapGesture.Tapped += (s, e) => OnDayTapped(date, isFutureDate, isPredictedDay, isOvulationDay);
            dayBorder.GestureRecognizers.Add(tapGesture);

            CalendarGrid.Add(dayBorder, column, row);
        }

        private void UpdateStats()
        {
            var cycleData = _cycleService.GetCycleData();

            if (!cycleData.HasData)
            {
                CycleDayLabel.Text = "-";
                NextPeriodLabel.Text = "-";
                OvulationLabel.Text = "-";
                FertilityLabel.Text = "-";
                return;
            }

            // Update the labels with actual data
            CycleDayLabel.Text = cycleData.CurrentCycleDay > 0 ?
                cycleData.CurrentCycleDay.ToString() : "-";

            if (cycleData.DaysUntilNextPeriod > 0)
            {
                NextPeriodLabel.Text = $"{cycleData.DaysUntilNextPeriod}d";
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
                OvulationLabel.Text = $"{Math.Abs(cycleData.DaysUntilOvulation)}d";
            }
            else
            {
                OvulationLabel.Text = "-";
            }

            FertilityLabel.Text = !string.IsNullOrEmpty(cycleData.FertilityStatus) ?
                cycleData.FertilityStatus : "-";
        }

        private void UpdateCalendar()
        {
            MonthLabel.Text = _currentDate.ToString("MMMM yyyy").ToUpper();

            CalendarGrid.Children.Clear();
            CalendarGrid.RowDefinitions.Clear();
            CalendarGrid.ColumnDefinitions.Clear();

            // Add columns for days of week
            for (int i = 0; i < 7; i++)
            {
                CalendarGrid.ColumnDefinitions.Add(new ColumnDefinition());
            }

            // Add header row for day names
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

            // Calculate calendar dates
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
                CreateDayCell(currentDay, col, row);

                col++;
                if (col > 6)
                {
                    col = 0;
                    row++;
                }
            }
        }

        private async void OnDayTapped(DateTime date, bool isFutureDate, bool isPredictedDay, bool isOvulationDay)
        {
            if (isFutureDate)
            {
                if (isPredictedDay)
                {
                    await DisplayAlert("Predicted Period",
                        "This is a predicted period day based on your cycle history.", "OK");
                }
                else if (isOvulationDay)
                {
                    await DisplayAlert("Predicted Ovulation",
                        "This is a predicted ovulation day based on your cycle history.", "OK");
                }
                return;
            }

            // Two-tap period marking logic
            if (_selectedStartDate == null)
            {
                // First tap - select start date
                _selectedStartDate = date;
                await DisplayAlert("Start Date Selected",
                    $"Selected {date:MMM d} as period start. Tap the end date.", "OK");
                UpdateCalendar();
            }
            else
            {
                // Second tap - select end date and save period
                DateTime startDate = _selectedStartDate.Value;
                DateTime endDate = date;

                if (endDate < startDate)
                {
                    await DisplayAlert("Invalid Selection",
                        "End date cannot be before start date.", "OK");
                    _selectedStartDate = null;
                    UpdateCalendar();
                    return;
                }

                int duration = (endDate - startDate).Days + 1;

                // Confirm period entry
                bool confirm = await DisplayAlert("Confirm Period",
                    $"Mark period from {startDate:MMM d} to {endDate:MMM d} ({duration} days)?",
                    "Yes", "No");

                if (confirm)
                {
                    // Check if this period overlaps with existing period
                    var existingPeriod = _cycleService.GetCycleHistory()
                        .FirstOrDefault(c =>
                            (startDate >= c.StartDate && startDate <= c.EndDate) ||
                            (endDate >= c.StartDate && endDate <= c.EndDate));

                    if (existingPeriod != null)
                    {
                        bool overwrite = await DisplayAlert("Overlap Detected",
                            "This period overlaps with an existing period. Overwrite?",
                            "Yes", "No");

                        if (!overwrite)
                        {
                            _selectedStartDate = null;
                            UpdateCalendar();
                            return;
                        }
                    }

                    // Add period locally and send to backend
                    _cycleService.AddPeriodWithDates(startDate, endDate);
                    _ = SaveToServer(startDate, endDate);

                    await DisplayAlert("Period Added",
                        $"Period marked for {duration} days.", "OK");

                    // Reload data and update UI
                    LoadCalendarData();
                    UpdateCalendar();
                    UpdateStats();
                }

                _selectedStartDate = null;
            }
        }

        private async Task SyncFromServer()
        {
            try
            {
                var api = _apiService ?? ServiceHelper.GetService<CycleApiService>();
                var history = await api.GetHistoryAsync();
                if (history.Count == 0) return;

                // Merge server entries into local storage, avoid duplicates
                var local = _cycleService.GetCycleHistory();
                var serverAsLocal = history
                    .Select(h => new Cycle { StartDate = h.StartDate.ToDateTime(TimeOnly.MinValue), Duration = (h.EndDate.ToDateTime(TimeOnly.MinValue) - h.StartDate.ToDateTime(TimeOnly.MinValue)).Days + 1 })
                    .ToList();

                foreach (var s in serverAsLocal)
                {
                    bool exists = local.Any(c => c.StartDate.Date == s.StartDate.Date && c.Duration == s.Duration);
                    if (!exists) local.Add(s);
                }
                _cycleService.SaveCycleHistory(local);
            }
            catch { }
        }

        private async Task SaveToServer(DateTime startDate, DateTime endDate)
        {
            try
            {
                var api = _apiService ?? ServiceHelper.GetService<CycleApiService>();
                await api.CreateAsync(startDate, endDate);
            }
            catch { }
        }

        private void OnPrevMonthClicked(object sender, EventArgs e)
        {
            _currentDate = _currentDate.AddMonths(-1);
            _selectedStartDate = null; // Reset selection when changing months
            UpdateCalendar();
        }

        private void OnNextMonthClicked(object sender, EventArgs e)
        {
            _currentDate = _currentDate.AddMonths(1);
            _selectedStartDate = null; // Reset selection when changing months
            UpdateCalendar();
        }

        private async void OnEditClicked(object sender, EventArgs e)
        {
            string action = await DisplayActionSheet("Calendar Options", "Cancel", null,
                "Mark Period", "Clear All Data", "View Cycle History", "Set Cycle Length", "View Predictions", "Edit Last Period", "Delete Last Period");

            switch (action)
            {
                case "Mark Period":
                    await DisplayAlert("How to Mark Period",
                        "Tap once to select start date, then tap again to select end date.", "OK");
                    break;

                case "Clear All Data":
                    await ClearAllData();
                    break;

                case "View Cycle History":
                    await ViewCycleHistory();
                    break;

                case "Set Cycle Length":
                    await SetCycleLength();
                    break;

                case "View Predictions":
                    await ViewPredictions();
                    break;

                case "Edit Last Period":
                    await EditLastPeriod();
                    break;

                case "Delete Last Period":
                    await DeleteLastPeriod();
                    break;
            }
        }

        private async Task ClearAllData()
        {
            bool confirm = await DisplayAlert("Confirm",
                "Are you sure you want to clear all period data?", "Yes", "No");

            if (confirm)
            {
                _cycleService.SaveCycleHistory(new List<Cycle>());
                _selectedStartDate = null;
                LoadCalendarData();
                UpdateCalendar();
                UpdateStats();
                await DisplayAlert("Cleared", "All period data has been cleared.", "OK");
            }
        }

        private async Task ViewCycleHistory()
        {
            var cycleHistory = _cycleService.GetCycleHistory();

            if (cycleHistory.Count == 0)
            {
                await DisplayAlert("Cycle History", "No period data available.", "OK");
                return;
            }

            string historyText = "Your Cycle History:\n\n";
            foreach (var cycle in cycleHistory.OrderByDescending(c => c.StartDate))
            {
                historyText += $"{cycle.StartDate:MMM d} - {cycle.EndDate:MMM d} ({cycle.Duration} days)\n";
            }

            // Add statistics
            var cycleData = _cycleService.GetCycleData();
            if (cycleData.HasData)
            {
                historyText += $"\nStatistics:\n";
                historyText += $"Average Cycle: {cycleData.AverageCycleLength} days\n";
                historyText += $"Average Period: {cycleData.AveragePeriodLength} days\n";
                historyText += $"Regularity: {_cycleService.GetCycleRegularityScore()}%";
            }

            await DisplayAlert("Cycle History", historyText, "OK");
        }

        private async Task SetCycleLength()
        {
            int currentLength = _cycleService.GetAverageCycleLength();
            string result = await DisplayPromptAsync("Set Cycle Length",
                "Enter your average cycle length (21-35 days is typical):",
                keyboard: Keyboard.Numeric,
                initialValue: currentLength > 0 ? currentLength.ToString() : "",
                maxLength: 2);

            if (int.TryParse(result, out int newLength) && newLength >= 21 && newLength <= 35)
            {
                _cycleService.SaveAverageCycleLength(newLength);
                CalculatePredictions();
                UpdateCalendar();
                UpdateStats();
                await DisplayAlert("Updated", $"Cycle length set to {newLength} days.", "OK");
            }
            else if (!string.IsNullOrEmpty(result))
            {
                await DisplayAlert("Invalid", "Please enter a number between 21 and 35.", "OK");
            }
        }

        private async Task ViewPredictions()
        {
            var predictions = _cycleService.GetPredictedPeriodDates(6);
            var ovulations = _cycleService.GetPredictedOvulationDates(6);

            if (predictions.Count == 0)
            {
                await DisplayAlert("Predictions", "Mark your period to get predictions.", "OK");
                return;
            }

            string predictionText = "Upcoming Predictions:\n\n";

            predictionText += "Next Periods:\n";
            foreach (var date in predictions)
            {
                predictionText += $"{date:MMM d, yyyy}\n";
            }

            predictionText += "\nOvulation Days:\n";
            foreach (var date in ovulations)
            {
                predictionText += $"{date:MMM d, yyyy}\n";
            }

            // Add average information
            var cycleData = _cycleService.GetCycleData();
            if (cycleData.HasData)
            {
                predictionText += $"\nBased on:\n";
                predictionText += $"Average Cycle: {cycleData.AverageCycleLength} days\n";
                predictionText += $"Average Period: {cycleData.AveragePeriodLength} days";
            }

            await DisplayAlert("Predictions", predictionText, "OK");
        }

        private async Task EditLastPeriod()
        {
            var history = _cycleService.GetCycleHistory().OrderByDescending(c => c.StartDate).ToList();
            if (history.Count == 0)
            {
                await DisplayAlert("Edit Period", "No period data available.", "OK");
                return;
            }

            var last = history[0];
            string startStr = await DisplayPromptAsync("Edit Period", "Enter new start date (yyyy-MM-dd)", initialValue: last.StartDate.ToString("yyyy-MM-dd"));
            if (string.IsNullOrWhiteSpace(startStr)) return;
            string endStr = await DisplayPromptAsync("Edit Period", "Enter new end date (yyyy-MM-dd)", initialValue: last.EndDate.ToString("yyyy-MM-dd"));
            if (string.IsNullOrWhiteSpace(endStr)) return;

            if (!DateTime.TryParse(startStr, out var newStart) || !DateTime.TryParse(endStr, out var newEnd))
            {
                await DisplayAlert("Invalid", "Please enter valid dates in yyyy-MM-dd format.", "OK");
                return;
            }
            if (newEnd < newStart)
            {
                await DisplayAlert("Invalid", "End date cannot be before start date.", "OK");
                return;
            }

            // Update locally
            _cycleService.UpdatePeriodEndDate(last.StartDate, newEnd);

            // Update server
            int? id = await GetServerCycleIdAsync(last.StartDate, last.EndDate);
            if (id.HasValue)
            {
                var api = _apiService ?? ServiceHelper.GetService<CycleApiService>();
                await api.UpdateAsync(id.Value, newStart, newEnd);
            }

            LoadCalendarData();
            UpdateCalendar();
            UpdateStats();
            await DisplayAlert("Updated", "Period updated.", "OK");
        }

        private async Task DeleteLastPeriod()
        {
            var history = _cycleService.GetCycleHistory().OrderByDescending(c => c.StartDate).ToList();
            if (history.Count == 0)
            {
                await DisplayAlert("Delete Period", "No period data available.", "OK");
                return;
            }

            var last = history[0];
            bool confirm = await DisplayAlert("Delete Period",
                $"Delete period {last.StartDate:MMM d} - {last.EndDate:MMM d}?", "Yes", "No");
            if (!confirm) return;

            // Remove locally
            var remaining = _cycleService.GetCycleHistory()
                .Where(c => !(c.StartDate == last.StartDate && c.Duration == last.Duration))
                .ToList();
            _cycleService.SaveCycleHistory(remaining);

            // Remove on server
            int? id = await GetServerCycleIdAsync(last.StartDate, last.EndDate);
            if (id.HasValue)
            {
                var api = _apiService ?? ServiceHelper.GetService<CycleApiService>();
                await api.DeleteAsync(id.Value);
            }

            LoadCalendarData();
            UpdateCalendar();
            UpdateStats();
            await DisplayAlert("Deleted", "Last period deleted.", "OK");
        }

        private async Task<int?> GetServerCycleIdAsync(DateTime startDate, DateTime endDate)
        {
            try
            {
                var api = _apiService ?? ServiceHelper.GetService<CycleApiService>();
                var history = await api.GetHistoryAsync();
                var match = history.FirstOrDefault(h =>
                    h.StartDate.ToDateTime(TimeOnly.MinValue).Date == startDate.Date &&
                    h.EndDate.ToDateTime(TimeOnly.MinValue).Date == endDate.Date);
                return match?.Id;
            }
            catch { return null; }
        }

        // Cancel selection if user wants to abort
        private async void OnCancelSelectionClicked(object sender, EventArgs e)
        {
            if (_selectedStartDate != null)
            {
                _selectedStartDate = null;
                UpdateCalendar();
                await DisplayAlert("Selection Cancelled", "Period selection cancelled.", "OK");
            }
        }
    }
}
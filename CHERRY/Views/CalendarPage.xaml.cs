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

        public CalendarPage()
        {
            InitializeComponent();
            _currentDate = DateTime.Today;

            // Load sample data with your actual dates
            LoadSampleData();

            UpdateCalendar();
        }

        private void LoadSampleData()
        {
            // Add your actual period history
            _cycleHistory.Add(new Cycle
            {
                StartDate = new DateTime(2024, 6, 10),  // June 10
                Duration = 5
            });

            _cycleHistory.Add(new Cycle
            {
                StartDate = new DateTime(2024, 7, 8),   // July 8
                Duration = 5
            });

            _cycleHistory.Add(new Cycle
            {
                StartDate = new DateTime(2024, 8, 9),   // August 9
                Duration = 5
            });

            // Calculate predictions based on your actual data
            CalculatePrediction();
        }

        private void CalculatePrediction()
        {
            if (_cycleHistory.Count < 2)  // Need at least 2 cycles to calculate average
            {
                // Not enough data to make a prediction
                return;
            }

            // Calculate average cycle length from your actual history
            int totalDays = 0;
            int cycleCount = 0;

            // Sort cycles by date to ensure correct calculation
            var sortedCycles = _cycleHistory.OrderBy(c => c.StartDate).ToList();

            for (int i = 1; i < sortedCycles.Count; i++)
            {
                TimeSpan cycleLength = sortedCycles[i].StartDate - sortedCycles[i - 1].StartDate;
                totalDays += cycleLength.Days;
                cycleCount++;
            }

            // Calculate average cycle length
            int averageCycleLength = totalDays / cycleCount;

            // Get the last recorded period start date
            DateTime lastPeriodStart = sortedCycles.Last().StartDate;

            // Predict next period start date based on YOUR average cycle length
            DateTime predictedStartDate = lastPeriodStart.AddDays(averageCycleLength);

            // Add predicted period days (typically 5 days)
            _predictedPeriodDays.Clear();
            for (int i = 0; i < 5; i++)
            {
                _predictedPeriodDays.Add(predictedStartDate.AddDays(i));
            }

            // CORRECTED: Calculate ovulation days (typically 14 days before next period starts)
            _ovulationDays.Clear();
            DateTime ovulationDate = predictedStartDate.AddDays(-14); // Ovulation occurs ~14 days before next period

            // Ovulation window is typically 5-6 days but most fertile 2-3 days around ovulation
            // Let's mark 5 days for the fertility window as requested
            for (int i = -2; i <= 2; i++) // 2 days before and 2 days after ovulation
            {
                _ovulationDays.Add(ovulationDate.AddDays(i));
            }

            // Debug output to check calculations
            Console.WriteLine($"Average cycle length: {averageCycleLength} days");
            Console.WriteLine($"Last period: {lastPeriodStart:MMM d}");
            Console.WriteLine($"Predicted next period: {predictedStartDate:MMM d}");
            Console.WriteLine($"Predicted ovulation: {ovulationDate:MMM d}");

            // Show prediction info to user
            PredictionLabel.Text = $"Next period: {predictedStartDate:MMM d}\nOvulation: {ovulationDate:MMM d}";
        }

        private void UpdateCalendar()
        {
            // Update the month/year label
            MonthLabel.Text = _currentDate.ToString("MMMM yyyy").ToUpper();

            // Clear previous calendar content
            CalendarGrid.Children.Clear();
            CalendarGrid.RowDefinitions.Clear();
            CalendarGrid.ColumnDefinitions.Clear();

            // Create column definitions for days of the week
            for (int i = 0; i < 7; i++)
            {
                CalendarGrid.ColumnDefinitions.Add(new ColumnDefinition());
            }

            // Create row definitions for header and weeks
            CalendarGrid.RowDefinitions.Add(new RowDefinition { Height = GridLength.Auto });

            // Add day headers
            string[] days = { "MON", "TUE", "WED", "THU", "FRI", "SAT", "SUN" };
            for (int i = 0; i < 7; i++)
            {
                var label = new Label
                {
                    Text = days[i],
                    FontAttributes = FontAttributes.Bold,
                    HorizontalOptions = LayoutOptions.Center,
                    VerticalOptions = LayoutOptions.Center,
                    TextColor = Color.FromArgb("#666666"),
                    FontSize = 14
                };
                CalendarGrid.Add(label, i, 0);
            }

            // Get first day of month and days in month
            DateTime firstDayOfMonth = new DateTime(_currentDate.Year, _currentDate.Month, 1);
            int daysInMonth = DateTime.DaysInMonth(_currentDate.Year, _currentDate.Month);

            // Calculate starting position (0 = Monday, 1 = Tuesday, etc.)
            int startPosition = ((int)firstDayOfMonth.DayOfWeek + 6) % 7;

            // Add days to calendar
            int row = 1;
            int col = startPosition;

            for (int day = 1; day <= daysInMonth; day++)
            {
                // Add new row if needed
                if (row >= CalendarGrid.RowDefinitions.Count)
                {
                    CalendarGrid.RowDefinitions.Add(new RowDefinition { Height = GridLength.Star });
                }

                DateTime currentDay = new DateTime(_currentDate.Year, _currentDate.Month, day);
                bool isPeriodDay = _periodDays.Contains(currentDay.Date);
                bool isPredictedDay = _predictedPeriodDays.Contains(currentDay.Date);
                bool isOvulationDay = _ovulationDays.Contains(currentDay.Date);
                bool isToday = (day == DateTime.Today.Day && _currentDate.Month == DateTime.Today.Month && _currentDate.Year == DateTime.Today.Year);

                // Create a simple frame for each day
                var dayFrame = new Frame
                {
                    BackgroundColor = isPeriodDay ? Color.FromArgb("#FFB6C1") :
                                      isPredictedDay ? Color.FromArgb("#E0E0E0") :
                                      isOvulationDay ? Color.FromArgb("#C71585") : // Dark pink for ovulation
                                      Colors.Transparent,
                    CornerRadius = 20,
                    WidthRequest = 40,
                    HeightRequest = 40,
                    Padding = 0,
                    HasShadow = false,
                    BorderColor = isToday ? Color.FromArgb("#6A5ACD") : Colors.Transparent
                };

                // Create day label
                var dayLabel = new Label
                {
                    Text = day.ToString(),
                    HorizontalOptions = LayoutOptions.Center,
                    VerticalOptions = LayoutOptions.Center,
                    TextColor = (isPeriodDay || isOvulationDay || isPredictedDay) ? Colors.White : Colors.Black,
                    FontSize = 16,
                    FontAttributes = (isOvulationDay) ? FontAttributes.Bold : FontAttributes.None
                };

                dayFrame.Content = dayLabel;

                // Add tap gesture recognizer
                var tapGesture = new TapGestureRecognizer();
                tapGesture.Tapped += (s, e) => OnDayClicked(currentDay);
                dayFrame.GestureRecognizers.Add(tapGesture);

                // Add to grid
                CalendarGrid.Add(dayFrame, col, row);

                // Move to next column
                col++;

                // Move to next row if at end of week
                if (col > 6)
                {
                    col = 0;
                    row++;
                }
            }
        }

        private void OnDayClicked(DateTime date)
        {
            // Check if this day is already part of a period
            bool isStartOfNewPeriod = true;
            foreach (var periodDay in _periodDays)
            {
                if (periodDay.Date == date.Date.AddDays(-1) || periodDay.Date == date.Date.AddDays(1))
                {
                    isStartOfNewPeriod = false;
                    break;
                }
            }

            if (isStartOfNewPeriod)
            {
                // Add new period (5 days)
                for (int i = 0; i < 5; i++)
                {
                    DateTime periodDay = date.AddDays(i);
                    if (!_periodDays.Contains(periodDay.Date))
                    {
                        _periodDays.Add(periodDay.Date);
                    }
                }

                // Add to cycle history
                _cycleHistory.Add(new Cycle
                {
                    StartDate = date.Date,
                    Duration = 5
                });

                // Recalculate predictions
                CalculatePrediction();
            }
            else
            {
                // Remove period day
                if (_periodDays.Contains(date.Date))
                {
                    _periodDays.Remove(date.Date);

                    // Find and remove from cycle history
                    var cycleToRemove = _cycleHistory.FirstOrDefault(c => c.StartDate.Date == date.Date ||
                        (date.Date > c.StartDate.Date && date.Date <= c.StartDate.AddDays(c.Duration - 1).Date));

                    if (cycleToRemove != null)
                    {
                        _cycleHistory.Remove(cycleToRemove);
                    }

                    // Recalculate predictions
                    CalculatePrediction();
                }
            }

            UpdateCalendar();
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
            // Show edit options
            string action = await DisplayActionSheet("Edit Period", "Cancel", null,
                "Mark Period", "Clear All", "View Cycle Info");

            if (action == "Mark Period")
            {
                await DisplayAlert("Info", "Click on a day to mark the start of your period. The next 5 days will be marked automatically.", "OK");
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
                    UpdateCalendar();
                }
            }
            else if (action == "View Cycle Info")
            {
                string info = "Cycle Information:\n\n";

                if (_cycleHistory.Count > 0)
                {
                    // Sort cycles by date
                    var sortedCycles = _cycleHistory.OrderBy(c => c.StartDate).ToList();

                    info += "Your cycle history:\n";
                    for (int i = 0; i < sortedCycles.Count; i++)
                    {
                        info += $"{sortedCycles[i].StartDate:MMM d}\n";
                    }

                    info += "\n";

                    if (sortedCycles.Count > 1)
                    {
                        int totalDays = 0;
                        for (int i = 1; i < sortedCycles.Count; i++)
                        {
                            int cycleLength = (sortedCycles[i].StartDate - sortedCycles[i - 1].StartDate).Days;
                            totalDays += cycleLength;
                            info += $"Cycle {i}: {cycleLength} days\n";
                        }

                        int avgCycleLength = totalDays / (sortedCycles.Count - 1);
                        info += $"\nAverage cycle: {avgCycleLength} days\n";
                    }

                    if (_predictedPeriodDays.Count > 0 && _ovulationDays.Count > 0)
                    {
                        info += $"\nNext period: {_predictedPeriodDays[0]:MMM d}";
                        info += $"\nOvulation: {_ovulationDays[2]:MMM d}"; // Middle of ovulation window
                    }
                }
                else
                {
                    info += "No cycle data available. Mark your period to get predictions.";
                }

                await DisplayAlert("Cycle Information", info, "OK");
            }
        }
    }

    public class Cycle
    {
        public DateTime StartDate { get; set; }
        public int Duration { get; set; }
    }
}
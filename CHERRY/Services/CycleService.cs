using System;
using System.Collections.Generic;
using System.Linq;
using System.Text.Json;
using Microsoft.Maui.Storage;

namespace CHERRY.Services
{
    public class CycleService
    {
        private const string CycleHistoryKey = "CycleHistory";
        private const string AverageCycleLengthKey = "AverageCycleLength";
        private const string AveragePeriodLengthKey = "AveragePeriodLength";
        private const string LastPeriodDateKey = "LastPeriodDate";
        private const string AuthEmailKey = "auth_email";

        private static string GetUserScopedKey(string baseKey)
        {
            try
            {
                var emailTask = SecureStorage.Default.GetAsync(AuthEmailKey);
                var email = emailTask.GetAwaiter().GetResult();
                if (string.IsNullOrWhiteSpace(email)) email = "guest";
                return $"{baseKey}:{email}";
            }
            catch
            {
                return baseKey;
            }
        }

        public List<Cycle> GetCycleHistory()
        {
            var scopedKey = GetUserScopedKey(CycleHistoryKey);
            if (Preferences.ContainsKey(scopedKey))
            {
                string historyJson = Preferences.Get(scopedKey, string.Empty);
                if (!string.IsNullOrEmpty(historyJson))
                {
                    try
                    {
                        return JsonSerializer.Deserialize<List<Cycle>>(historyJson) ?? new List<Cycle>();
                    }
                    catch
                    {
                        return new List<Cycle>();
                    }
                }
            }
            return new List<Cycle>();
        }

        public void SaveAverageCycleLength(int length)
        {
            Preferences.Set(GetUserScopedKey(AverageCycleLengthKey), length);
        }

        public void SaveCycleHistory(List<Cycle> cycleHistory)
        {
            if (cycleHistory == null)
                cycleHistory = new List<Cycle>();

            string historyJson = JsonSerializer.Serialize(cycleHistory);
            Preferences.Set(GetUserScopedKey(CycleHistoryKey), historyJson);

            // Update averages when saving history
            if (cycleHistory.Count > 0)
            {
                UpdateAverages(cycleHistory);

                // Save last period date
                var lastPeriod = cycleHistory.OrderByDescending(c => c.StartDate).First();
                Preferences.Set(GetUserScopedKey(LastPeriodDateKey), lastPeriod.StartDate.ToString("O")); // ISO 8601 format
            }
        }

        public int GetAverageCycleLength()
        {
            return Preferences.Get(GetUserScopedKey(AverageCycleLengthKey), 28);
        }

        public int GetAveragePeriodLength()
        {
            return Preferences.Get(GetUserScopedKey(AveragePeriodLengthKey), 5);
        }

        public DateTime? GetLastPeriodDate()
        {
            var key = GetUserScopedKey(LastPeriodDateKey);
            if (Preferences.ContainsKey(key))
            {
                string lastPeriodDateString = Preferences.Get(key, string.Empty);
                if (!string.IsNullOrEmpty(lastPeriodDateString))
                {
                    if (DateTime.TryParse(lastPeriodDateString, out DateTime lastPeriodDate))
                    {
                        return lastPeriodDate;
                    }
                }
            }

            // Fallback: get from cycle history
            var cycleHistory = GetCycleHistory();
            if (cycleHistory.Count > 0)
            {
                return cycleHistory.OrderByDescending(c => c.StartDate).First().StartDate;
            }

            return null;
        }

        private void UpdateAverages(List<Cycle> cycleHistory)
        {
            // Calculate average cycle length
            if (cycleHistory.Count > 1)
            {
                var cycleLengths = CalculateCycleLengths(cycleHistory);
                if (cycleLengths.Any())
                {
                    var averageCycleLength = (int)Math.Round(cycleLengths.Average());
                    Preferences.Set(AverageCycleLengthKey, averageCycleLength);
                }
            }

            // Calculate average period length
            if (cycleHistory.Any())
            {
                var averagePeriodLength = (int)Math.Round(cycleHistory.Average(c => c.Duration));
                Preferences.Set(AveragePeriodLengthKey, averagePeriodLength);
            }
        }

        public CycleData GetCycleData()
        {
            var cycleHistory = GetCycleHistory();
            int averageCycleLength = GetAverageCycleLength();
            int averagePeriodLength = GetAveragePeriodLength();
            DateTime? lastPeriodDate = GetLastPeriodDate();

            if (cycleHistory.Count == 0 || !lastPeriodDate.HasValue)
            {
                return new CycleData { HasData = false };
            }

            var lastPeriod = cycleHistory.OrderByDescending(c => c.StartDate).First();
            int daysSinceLastPeriod = (DateTime.Today - lastPeriod.StartDate).Days;
            int currentCycleDay = daysSinceLastPeriod + 1; // Day 1 is first day of period

            DateTime nextPeriodStart = lastPeriod.StartDate.AddDays(averageCycleLength);
            int daysUntilNextPeriod = (nextPeriodStart - DateTime.Today).Days;

            DateTime ovulationDate = nextPeriodStart.AddDays(-14);
            int daysUntilOvulation = (ovulationDate - DateTime.Today).Days;

            string fertilityStatus = GetFertilityStatus(daysUntilOvulation);

            // Check if currently on period
            bool isOnPeriod = IsCurrentlyOnPeriod(lastPeriod);

            return new CycleData
            {
                HasData = true,
                CurrentCycleDay = currentCycleDay,
                DaysUntilNextPeriod = Math.Max(0, daysUntilNextPeriod), // Don't show negative days
                DaysUntilOvulation = daysUntilOvulation,
                FertilityStatus = fertilityStatus,
                LastPeriod = lastPeriod,
                CycleHistory = cycleHistory,
                AverageCycleLength = averageCycleLength,
                AveragePeriodLength = averagePeriodLength,
                LastPeriodDate = lastPeriod.StartDate,
                IsCurrentlyOnPeriod = isOnPeriod,
                PeriodProgress = isOnPeriod ? CalculatePeriodProgress(lastPeriod) : 0
            };
        }

        private bool IsCurrentlyOnPeriod(Cycle lastPeriod)
        {
            DateTime periodEndDate = lastPeriod.StartDate.AddDays(lastPeriod.Duration - 1); // -1 because start date is day 1
            return DateTime.Today >= lastPeriod.StartDate && DateTime.Today <= periodEndDate;
        }

        private double CalculatePeriodProgress(Cycle lastPeriod)
        {
            DateTime periodEndDate = lastPeriod.StartDate.AddDays(lastPeriod.Duration - 1);
            if (DateTime.Today > periodEndDate)
                return 100;

            int totalDays = lastPeriod.Duration;
            int daysPassed = (DateTime.Today - lastPeriod.StartDate).Days + 1; // +1 because start date is day 1

            return (double)daysPassed / totalDays * 100;
        }

        public List<int> CalculateCycleLengths(List<Cycle> cycles)
        {
            var lengths = new List<int>();

            if (cycles == null || cycles.Count < 2)
                return lengths;

            var sortedCycles = cycles.OrderBy(c => c.StartDate).ToList();

            for (int i = 1; i < sortedCycles.Count; i++)
            {
                var length = (sortedCycles[i].StartDate - sortedCycles[i - 1].StartDate).Days;
                lengths.Add(length);
            }

            return lengths;
        }

        private string GetFertilityStatus(int daysUntilOvulation)
        {
            if (daysUntilOvulation >= -2 && daysUntilOvulation <= 2)
                return "High";
            if (daysUntilOvulation >= -5 && daysUntilOvulation <= 5)
                return "Medium";
            return "Low";
        }

        public string GetDailyTip(CycleData cycleData)
        {
            if (!cycleData.HasData)
                return "Track your period to get personalized tips and predictions.";

            if (cycleData.IsCurrentlyOnPeriod)
            {
                int dayOfPeriod = (DateTime.Today - cycleData.LastPeriod.StartDate).Days + 1;
                return GetPeriodDayTip(dayOfPeriod, cycleData.LastPeriod.Duration);
            }
            else if (cycleData.CurrentCycleDay >= (cycleData.AverageCycleLength - 14) &&
                     cycleData.CurrentCycleDay <= (cycleData.AverageCycleLength - 10))
            {
                return "You're in your fertile window. This is the best time to conceive if you're trying to get pregnant.";
            }
            else if (cycleData.CurrentCycleDay >= (cycleData.AverageCycleLength - 5))
            {
                return "Your period is coming soon. You might experience PMS symptoms like bloating or mood changes.";
            }
            else if (cycleData.CurrentCycleDay >= (cycleData.AverageCycleLength - 14) &&
                     cycleData.CurrentCycleDay <= (cycleData.AverageCycleLength - 8))
            {
                return "You're approaching ovulation. Consider tracking your basal body temperature for better predictions.";
            }
            else
            {
                return "You're in the follicular phase. This is a good time for exercise and productivity.";
            }
        }

        private string GetPeriodDayTip(int dayOfPeriod, int totalDuration)
        {
            if (dayOfPeriod == 1)
                return "First day of your period. Make sure to stay hydrated and get plenty of rest.";
            else if (dayOfPeriod == totalDuration)
                return "Last day of your period. You should be feeling better soon!";
            else if (dayOfPeriod <= totalDuration / 2)
                return "You're in the middle of your period. Continue to take it easy and listen to your body.";
            else
                return "Your period is winding down. You might start feeling more energetic soon.";
        }

        // Method to add a new period with start date only
        public void AddPeriod(DateTime startDate, int duration)
        {
            var cycleHistory = GetCycleHistory();
            cycleHistory.Add(new Cycle { StartDate = startDate, Duration = duration });
            SaveCycleHistory(cycleHistory);
        }

        // Method to add a new period with both start and end dates
        public void AddPeriodWithDates(DateTime startDate, DateTime endDate)
        {
            if (endDate < startDate)
                throw new ArgumentException("End date cannot be before start date.");

            int duration = (endDate - startDate).Days + 1; // +1 to include both start and end dates

            var cycleHistory = GetCycleHistory();
            cycleHistory.Add(new Cycle { StartDate = startDate, Duration = duration });
            SaveCycleHistory(cycleHistory);
        }

        // Method to update an existing period's end date
        public void UpdatePeriodEndDate(DateTime startDate, DateTime newEndDate)
        {
            var cycleHistory = GetCycleHistory();
            var period = cycleHistory.FirstOrDefault(c => c.StartDate.Date == startDate.Date);

            if (period != null)
            {
                if (newEndDate < startDate)
                    throw new ArgumentException("End date cannot be before start date.");

                period.Duration = (newEndDate - startDate).Days + 1;
                SaveCycleHistory(cycleHistory);
            }
        }

        // Method to get current period status
        public PeriodStatus GetCurrentPeriodStatus()
        {
            var cycleHistory = GetCycleHistory();
            if (cycleHistory.Count == 0)
                return new PeriodStatus { IsOnPeriod = false };

            var lastPeriod = cycleHistory.OrderByDescending(c => c.StartDate).First();
            DateTime periodEndDate = lastPeriod.StartDate.AddDays(lastPeriod.Duration - 1);

            bool isOnPeriod = DateTime.Today >= lastPeriod.StartDate && DateTime.Today <= periodEndDate;
            int dayOfPeriod = isOnPeriod ? (DateTime.Today - lastPeriod.StartDate).Days + 1 : 0;
            double progress = isOnPeriod ? (double)dayOfPeriod / lastPeriod.Duration * 100 : 0;

            return new PeriodStatus
            {
                IsOnPeriod = isOnPeriod,
                DayOfPeriod = dayOfPeriod,
                TotalDays = lastPeriod.Duration,
                ProgressPercentage = progress,
                StartDate = lastPeriod.StartDate,
                EndDate = periodEndDate
            };
        }

        // Method to get predicted period dates
        public List<DateTime> GetPredictedPeriodDates(int numberOfCycles = 3)
        {
            var lastPeriodDate = GetLastPeriodDate();
            if (!lastPeriodDate.HasValue)
                return new List<DateTime>();

            var averageCycleLength = GetAverageCycleLength();
            var predictions = new List<DateTime>();

            for (int i = 1; i <= numberOfCycles; i++)
            {
                predictions.Add(lastPeriodDate.Value.AddDays(averageCycleLength * i));
            }

            return predictions;
        }

        // Method to get predicted ovulation dates
        public List<DateTime> GetPredictedOvulationDates(int numberOfCycles = 3)
        {
            var periodDates = GetPredictedPeriodDates(numberOfCycles);
            return periodDates.Select(date => date.AddDays(-14)).ToList();
        }

        // Method to get cycle regularity score
        public double GetCycleRegularityScore()
        {
            var cycleHistory = GetCycleHistory();
            if (cycleHistory.Count < 2)
                return 0;

            var cycleLengths = CalculateCycleLengths(cycleHistory);
            int normalCount = cycleLengths.Count(l => l >= 21 && l <= 35);
            return Math.Round((double)normalCount / cycleLengths.Count * 100);
        }

        // Method to get period duration regularity score
        public double GetPeriodDurationRegularityScore()
        {
            var cycleHistory = GetCycleHistory();
            if (cycleHistory.Count == 0)
                return 0;

            int normalCount = cycleHistory.Count(c => c.Duration >= 3 && c.Duration <= 7);
            return Math.Round((double)normalCount / cycleHistory.Count * 100);
        }
    }

    public class CycleData
    {
        public bool HasData { get; set; }
        public int CurrentCycleDay { get; set; }
        public int DaysUntilNextPeriod { get; set; }
        public int DaysUntilOvulation { get; set; }
        public string FertilityStatus { get; set; }
        public Cycle LastPeriod { get; set; }
        public List<Cycle> CycleHistory { get; set; }
        public int AverageCycleLength { get; set; }
        public int AveragePeriodLength { get; set; }
        public DateTime LastPeriodDate { get; set; }
        public bool IsCurrentlyOnPeriod { get; set; }
        public double PeriodProgress { get; set; }
    }

    public class Cycle
    {
        public DateTime StartDate { get; set; }
        public int Duration { get; set; }

        public DateTime EndDate => StartDate.AddDays(Duration - 1);
    }

    public class PeriodStatus
    {
        public bool IsOnPeriod { get; set; }
        public int DayOfPeriod { get; set; }
        public int TotalDays { get; set; }
        public double ProgressPercentage { get; set; }
        public DateTime StartDate { get; set; }
        public DateTime EndDate { get; set; }
    }
}
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text.Json;

namespace CHERRY.Services
{
    public class CycleService
    {
        private const string CycleHistoryKey = "CycleHistory";
        private const string AverageCycleLengthKey = "AverageCycleLength";

        public List<Cycle> GetCycleHistory()
        {
            if (Preferences.ContainsKey(CycleHistoryKey))
            {
                string historyJson = Preferences.Get(CycleHistoryKey, string.Empty);
                if (!string.IsNullOrEmpty(historyJson))
                {
                    return JsonSerializer.Deserialize<List<Cycle>>(historyJson);
                }
            }
            return new List<Cycle>();
        }

        public void SaveCycleHistory(List<Cycle> cycleHistory)
        {
            string historyJson = JsonSerializer.Serialize(cycleHistory);
            Preferences.Set(CycleHistoryKey, historyJson);
        }

        public int GetAverageCycleLength()
        {
            return Preferences.Get(AverageCycleLengthKey, 28);
        }

        public void SaveAverageCycleLength(int length)
        {
            Preferences.Set(AverageCycleLengthKey, length);
        }

        public CycleData GetCycleData()
        {
            var cycleHistory = GetCycleHistory();
            int averageCycleLength = GetAverageCycleLength();

            if (cycleHistory.Count == 0)
            {
                return new CycleData { HasData = false };
            }

            var lastPeriod = cycleHistory.OrderByDescending(c => c.StartDate).First();
            int daysSinceLastPeriod = (DateTime.Today - lastPeriod.StartDate).Days;
            int currentCycleDay = (daysSinceLastPeriod % averageCycleLength) + 1;

            DateTime nextPeriodStart = lastPeriod.StartDate.AddDays(averageCycleLength);
            int daysUntilNextPeriod = (nextPeriodStart - DateTime.Today).Days;

            DateTime ovulationDate = nextPeriodStart.AddDays(-14);
            int daysUntilOvulation = (ovulationDate - DateTime.Today).Days;

            string fertilityStatus = GetFertilityStatus(daysUntilOvulation);

            return new CycleData
            {
                HasData = true,
                CurrentCycleDay = currentCycleDay,
                DaysUntilNextPeriod = daysUntilNextPeriod,
                DaysUntilOvulation = daysUntilOvulation,
                FertilityStatus = fertilityStatus,
                LastPeriod = lastPeriod,
                CycleHistory = cycleHistory,
                AverageCycleLength = averageCycleLength
            };
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

            if (cycleData.CurrentCycleDay <= 5)
            {
                return "You're on your period. Make sure to stay hydrated and get enough rest.";
            }
            else if (cycleData.CurrentCycleDay >= cycleData.AverageCycleLength - 14 &&
                     cycleData.CurrentCycleDay <= cycleData.AverageCycleLength - 10)
            {
                return "You're in your fertile window. This is the best time to conceive if you're trying to get pregnant.";
            }
            else if (cycleData.CurrentCycleDay >= cycleData.AverageCycleLength - 5)
            {
                return "Your period is coming soon. You might experience PMS symptoms like bloating or mood changes.";
            }
            else
            {
                return "You're in the follicular phase. This is a good time for exercise and productivity.";
            }
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
    }

    public class Cycle
    {
        public DateTime StartDate { get; set; }
        public int Duration { get; set; }
    }
}
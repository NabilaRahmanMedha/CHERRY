using CHERRY.Services;
using Microcharts;
using SkiaSharp;
using System.Collections.ObjectModel;

namespace CHERRY.Views
{
    public partial class ReportsPage : ContentPage
    {
        private const int NormalCycleMin = 21;
        private const int NormalCycleMax = 35;
        private const int NormalDurationMin = 3;
        private const int NormalDurationMax = 7;

        private readonly SKColor _normalColor = SKColor.Parse("#4CAF50"); // Green
        private readonly SKColor _shortColor = SKColor.Parse("#FF5252");  // Red (too short)
        private readonly SKColor _longColor = SKColor.Parse("#FF9800");   // Orange (too long)
        private readonly SKColor _textColor = SKColor.Parse("#333333");

        private readonly CycleService _cycleService;
        private ObservableCollection<CycleWithStats> _recentCycles;

        public ReportsPage()
        {
            InitializeComponent();
            _cycleService = new CycleService();
            _recentCycles = new ObservableCollection<CycleWithStats>();
            RecentCyclesCollectionView.ItemsSource = _recentCycles;
        }

        protected override void OnAppearing()
        {
            base.OnAppearing();
            LoadCycleData();
        }

        private void LoadCycleData()
        {
            var cycleHistory = _cycleService.GetCycleHistory();
            var averageCycleLength = _cycleService.GetAverageCycleLength();

            // Update statistics
            AvgCycleLengthLabel.Text = $"{averageCycleLength} days";

            if (cycleHistory.Count > 1)
            {
                var cycleLengths = CalculateCycleLengths(cycleHistory);
                if (cycleLengths.Any())
                {
                    var shortest = cycleLengths.Min();
                    var longest = cycleLengths.Max();

                    ShortestCycleLabel.Text = $"{shortest} days";
                    LongestCycleLabel.Text = $"{longest} days";

                    // Calculate regularity scores
                    var cycleRegularity = CalculateCycleRegularityScore(cycleLengths);
                    var durationRegularity = CalculateDurationRegularityScore(cycleHistory.Select(c => c.Duration).ToList());

                    CycleRegularityLabel.Text = $"{cycleRegularity}%";
                    DurationRegularityLabel.Text = $"{durationRegularity}%";

                    // Update period duration stats
                    var durations = cycleHistory.Select(c => c.Duration).ToList();
                    AvgDurationLabel.Text = $"{durations.Average():0.#} days";
                }
                else
                {
                    SetDefaultStats();
                }
            }
            else if (cycleHistory.Count == 1)
            {
                // Only one cycle - show duration stats only
                var cycle = cycleHistory[0];
                AvgDurationLabel.Text = $"{cycle.Duration} days";
                SetDefaultStats();
            }
            else
            {
                SetDefaultStats();
            }

            // Update recent cycles
            UpdateRecentCycles(cycleHistory);

            // Update both charts
            UpdateCycleLengthChart(cycleHistory);
            UpdatePeriodDurationChart(cycleHistory);
        }

        private void SetDefaultStats()
        {
            ShortestCycleLabel.Text = "-";
            LongestCycleLabel.Text = "-";
            CycleRegularityLabel.Text = "-";
            DurationRegularityLabel.Text = "-";
            AvgDurationLabel.Text = "-";
        }

        private void UpdateRecentCycles(List<Cycle> cycles)
        {
            _recentCycles.Clear();

            if (cycles == null || cycles.Count == 0)
                return;

            // Order cycles by date (newest first)
            var sortedCycles = cycles.OrderByDescending(c => c.StartDate).ToList();

            for (int i = 0; i < sortedCycles.Count; i++)
            {
                int? cycleLength = null;
                string cycleLengthStatusColor = null;
                string durationStatusColor = null;

                // Calculate cycle length and determine status
                if (i < sortedCycles.Count - 1)
                {
                    cycleLength = (sortedCycles[i].StartDate - sortedCycles[i + 1].StartDate).Days;

                    // Determine cycle length status color
                    if (cycleLength < NormalCycleMin)
                        cycleLengthStatusColor = "#FF5252";
                    else if (cycleLength > NormalCycleMax)
                        cycleLengthStatusColor = "#FF9800";
                    else
                        cycleLengthStatusColor = "#4CAF50";
                }

                // Determine duration status color
                var duration = sortedCycles[i].Duration;
                if (duration < NormalDurationMin)
                    durationStatusColor = "#FF5252";
                else if (duration > NormalDurationMax)
                    durationStatusColor = "#FF9800";
                else
                    durationStatusColor = "#4CAF50";

                _recentCycles.Add(new CycleWithStats
                {
                    StartDate = sortedCycles[i].StartDate,
                    Duration = sortedCycles[i].Duration,
                    CycleLength = cycleLength,
                    CycleLengthStatusColor = cycleLengthStatusColor,
                    DurationStatusColor = durationStatusColor
                });
            }
        }

        private List<int> CalculateCycleLengths(List<Cycle> cycles)
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

        private double CalculateCycleRegularityScore(List<int> cycleLengths)
        {
            if (cycleLengths.Count < 2) return 0;

            int normalCount = cycleLengths.Count(l => l >= NormalCycleMin && l <= NormalCycleMax);
            return Math.Round((double)normalCount / cycleLengths.Count * 100);
        }

        private double CalculateDurationRegularityScore(List<int> durations)
        {
            if (durations.Count == 0) return 0;

            int normalCount = durations.Count(d => d >= NormalDurationMin && d <= NormalDurationMax);
            return Math.Round((double)normalCount / durations.Count * 100);
        }

        private void UpdateCycleLengthChart(List<Cycle> cycles)
        {
            if (cycles == null || cycles.Count < 2)
            {
                CycleLengthChart.Chart = null;
                CycleChartEmptyLabel.IsVisible = true;
                CycleStatsLabel.Text = string.Empty;
                return;
            }

            CycleChartEmptyLabel.IsVisible = false;

            // Use only the last 12 intervals to keep the chart readable
            var cycleLengths = CalculateCycleLengths(cycles);
            if (cycleLengths.Count > 12)
                cycleLengths = cycleLengths.Skip(cycleLengths.Count - 12).ToList();
            var entries = new List<ChartEntry>();

            // Calculate statistics for the label
            int normalCount = cycleLengths.Count(l => l >= NormalCycleMin && l <= NormalCycleMax);
            double regularityScore = CalculateCycleRegularityScore(cycleLengths);

            CycleStatsLabel.Text = $"{normalCount}/{cycleLengths.Count} cycles ({regularityScore}%) in normal range";

            for (int i = 0; i < cycleLengths.Count; i++)
            {
                var cycleNumber = i + 1;
                var length = cycleLengths[i];

                // Determine color based on cycle length
                SKColor pointColor;
                if (length < NormalCycleMin)
                {
                    pointColor = _shortColor;
                }
                else if (length > NormalCycleMax)
                {
                    pointColor = _longColor;
                }
                else
                {
                    pointColor = _normalColor;
                }

                var entry = new ChartEntry(length)
                {
                    // Show fewer x labels to reduce clutter
                    Label = (cycleNumber % 2 == 1) ? $"C{cycleNumber}" : string.Empty,
                    ValueLabel = length.ToString(),
                    Color = pointColor,
                    TextColor = _textColor,
                    ValueLabelColor = pointColor
                };

                entries.Add(entry);
            }

            // Adaptive y-axis with padding but within reasonable range
            float minY = cycleLengths.Min();
            float maxY = cycleLengths.Max();
            minY = Math.Max(18, minY - 2);
            maxY = Math.Min(45, maxY + 2);

            var chart = new LineChart
            {
                Entries = entries,
                LabelTextSize = 12,
                Margin = 20,
                BackgroundColor = SKColors.Transparent,
                LineMode = LineMode.Spline,
                PointMode = PointMode.Circle,
                PointSize = 6,
                LineSize = 2,
                LabelOrientation = Orientation.Horizontal,
                ValueLabelOrientation = Orientation.Horizontal,
                LineAreaAlpha = 0,
                MinValue = minY,
                MaxValue = maxY
            };

            CycleLengthChart.Chart = chart;
        }

        private void UpdatePeriodDurationChart(List<Cycle> cycles)
        {
            if (cycles == null || cycles.Count == 0)
            {
                PeriodDurationChart.Chart = null;
                PeriodChartEmptyLabel.IsVisible = true;
                PeriodStatsLabel.Text = string.Empty;
                return;
            }

            PeriodChartEmptyLabel.IsVisible = false;

            // Use only the last 12 periods for readability
            var entries = new List<ChartEntry>();
            var sortedCycles = cycles.OrderBy(c => c.StartDate).ToList();
            if (sortedCycles.Count > 12)
                sortedCycles = sortedCycles.Skip(sortedCycles.Count - 12).ToList();

            // Calculate statistics for the label
            int normalCount = cycles.Count(c => c.Duration >= NormalDurationMin && c.Duration <= NormalDurationMax);
            double regularityScore = CalculateDurationRegularityScore(cycles.Select(c => c.Duration).ToList());

            PeriodStatsLabel.Text = $"{normalCount}/{cycles.Count} periods ({regularityScore}%) in normal range";

            for (int i = 0; i < sortedCycles.Count; i++)
            {
                var cycle = sortedCycles[i];
                var cycleNumber = sortedCycles.Count - i;

                // Determine color based on period duration
                SKColor barColor;
                if (cycle.Duration < NormalDurationMin)
                {
                    barColor = _shortColor;
                }
                else if (cycle.Duration > NormalDurationMax)
                {
                    barColor = _longColor;
                }
                else
                {
                    barColor = _normalColor;
                }

                var entry = new ChartEntry(cycle.Duration)
                {
                    Label = (cycleNumber % 2 == 1) ? $"P{cycleNumber}" : string.Empty,
                    ValueLabel = cycle.Duration.ToString(),
                    Color = barColor,
                    TextColor = _textColor,
                    ValueLabelColor = barColor
                };

                entries.Add(entry);
            }

            // Reverse to show chronological order
            entries.Reverse();

            // Adaptive y-axis within 0–10 days (typical durations)
            float minDur = Math.Max(0, sortedCycles.Min(c => c.Duration) - 1);
            float maxDur = Math.Min(15, sortedCycles.Max(c => c.Duration) + 1);

            var chart = new BarChart
            {
                Entries = entries,
                LabelTextSize = 12,
                Margin = 20,
                BackgroundColor = SKColors.Transparent,
                LabelOrientation = Orientation.Horizontal,
                ValueLabelOrientation = Orientation.Horizontal,
                BarAreaAlpha = 120,
                MinValue = minDur,
                MaxValue = maxDur
            };

            PeriodDurationChart.Chart = chart;
        }
    }

    public class CycleWithStats
    {
        public DateTime StartDate { get; set; }
        public int Duration { get; set; }
        public int? CycleLength { get; set; }
        public string CycleLengthStatusColor { get; set; }
        public string DurationStatusColor { get; set; }

        public string CycleLengthDisplay => CycleLength.HasValue ? $"{CycleLength.Value} days" : "N/A";
    }
}
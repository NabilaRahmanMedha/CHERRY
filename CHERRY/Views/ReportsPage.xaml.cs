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
        private readonly SKColor _normalColor = SKColor.Parse("#4CAF50"); // Green
        private readonly SKColor _shortColor = SKColor.Parse("#FF5252");  // Red (too short)
        private readonly SKColor _longColor = SKColor.Parse("#FF9800");   // Orange (too long)
        private readonly SKColor _textColor = SKColor.Parse("#333333");

        private readonly CycleService _cycleService;
        private ObservableCollection<CycleWithLength> _recentCycles;

        public ReportsPage()
        {
            InitializeComponent();
            _cycleService = new CycleService();
            _recentCycles = new ObservableCollection<CycleWithLength>();
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

                    // Calculate regularity score
                    var regularity = CalculateRegularityScore(cycleLengths);
                    RegularityLabel.Text = $"{regularity}%";
                }
                else
                {
                    ShortestCycleLabel.Text = "-";
                    LongestCycleLabel.Text = "-";
                    RegularityLabel.Text = "-";
                }
            }
            else
            {
                ShortestCycleLabel.Text = "-";
                LongestCycleLabel.Text = "-";
                RegularityLabel.Text = "-";
            }

            // Update recent cycles
            UpdateRecentCycles(cycleHistory);

            // Update chart
            UpdateCycleChart(cycleHistory);
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
                string statusColor = null;

                // Calculate cycle length and determine status
                if (i < sortedCycles.Count - 1)
                {
                    cycleLength = (sortedCycles[i].StartDate - sortedCycles[i + 1].StartDate).Days;

                    // Determine status color
                    if (cycleLength < NormalCycleMin)
                        statusColor = "#FF5252"; // Red
                    else if (cycleLength > NormalCycleMax)
                        statusColor = "#FF9800"; // Orange
                    else
                        statusColor = "#4CAF50"; // Green
                }

                _recentCycles.Add(new CycleWithLength
                {
                    StartDate = sortedCycles[i].StartDate,
                    Duration = sortedCycles[i].Duration,
                    CycleLength = cycleLength,
                    StatusColor = statusColor
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

        private double CalculateRegularityScore(List<int> cycleLengths)
        {
            if (cycleLengths.Count < 2) return 0;

            int normalCount = cycleLengths.Count(l => l >= NormalCycleMin && l <= NormalCycleMax);
            return Math.Round((double)normalCount / cycleLengths.Count * 100);
        }

        private void UpdateCycleChart(List<Cycle> cycles)
        {
            if (cycles == null || cycles.Count < 2)
            {
                CycleLengthChart.Chart = null;
                ChartEmptyLabel.IsVisible = true;
                StatsLabel.Text = string.Empty;
                return;
            }

            ChartEmptyLabel.IsVisible = false;

            var cycleLengths = CalculateCycleLengths(cycles);
            var entries = new List<ChartEntry>();

            // Calculate statistics for the label
            int normalCount = cycleLengths.Count(l => l >= NormalCycleMin && l <= NormalCycleMax);
            int shortCount = cycleLengths.Count(l => l < NormalCycleMin);
            int longCount = cycleLengths.Count(l => l > NormalCycleMax);
            double regularityScore = CalculateRegularityScore(cycleLengths);

            StatsLabel.Text = $"{normalCount}/{cycleLengths.Count} cycles ({regularityScore}%) in normal range";

            for (int i = 0; i < cycleLengths.Count; i++)
            {
                var cycleNumber = i + 1;
                var length = cycleLengths[i];

                // Determine color based on cycle length
                SKColor pointColor;
                if (length < NormalCycleMin)
                {
                    pointColor = _shortColor; // Too short
                }
                else if (length > NormalCycleMax)
                {
                    pointColor = _longColor;  // Too long
                }
                else
                {
                    pointColor = _normalColor; // Normal
                }

                var entry = new ChartEntry(length)
                {
                    Label = $"C{cycleNumber}",
                    ValueLabel = length.ToString(),
                    Color = pointColor,
                    TextColor = _textColor,
                    ValueLabelColor = pointColor
                };

                entries.Add(entry);
            }

            var chart = new LineChart
            {
                Entries = entries,
                LabelTextSize = 12,
                Margin = 20,
                BackgroundColor = SKColors.Transparent,
                LineMode = LineMode.Straight,
                PointMode = PointMode.Circle,
                PointSize = 14,
                LineSize = 3,
                LabelOrientation = Orientation.Horizontal,
                ValueLabelOrientation = Orientation.Horizontal,
                LineAreaAlpha = 10
            };

            CycleLengthChart.Chart = chart;
        }
    }

    public class CycleWithLength
    {
        public DateTime StartDate { get; set; }
        public int Duration { get; set; }
        public int? CycleLength { get; set; }
        public string StatusColor { get; set; }

        public string CycleLengthDisplay => CycleLength.HasValue ? $"{CycleLength.Value} days" : "N/A";
    }
}
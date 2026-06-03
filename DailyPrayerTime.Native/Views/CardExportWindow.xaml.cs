using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Windows;
using DailyPrayerTime.Native.Models;
using DailyPrayerTime.Native.Services;
using DailyPrayerTime.Native.Helpers;

namespace DailyPrayerTime.Native.Views
{
    public partial class CardExportWindow : Window
    {
        private List<PeriodItem> _weeks = new();
        private List<PeriodItem> _months = new();

        public CardExportWindow()
        {
            InitializeComponent();
            GeneratePeriods();
            PopulateComboBox();
        }

        private void GeneratePeriods()
        {
            DateTime today = DateTime.Today;

            // Generate last 10 weeks (Saturday to Friday)
            int diff = (7 + (int)today.DayOfWeek - (int)DayOfWeek.Saturday) % 7;
            DateTime currentWeekStart = today.AddDays(-diff);

            for (int i = 0; i < 10; i++)
            {
                DateTime start = currentWeekStart.AddDays(-i * 7);
                DateTime end = start.AddDays(6);
                string label = i == 0 
                    ? $"This Week ({start:MMM dd} - {end:MMM dd})" 
                    : i == 1 
                        ? $"Last Week ({start:MMM dd} - {end:MMM dd})" 
                        : $"{start:MMM dd} - {end:MMM dd, yyyy}";

                _weeks.Add(new PeriodItem
                {
                    DisplayText = label,
                    StartDate = start,
                    EndDate = end
                });
            }

            // Generate last 12 months
            DateTime currentMonthStart = new DateTime(today.Year, today.Month, 1);
            for (int i = 0; i < 12; i++)
            {
                DateTime start = currentMonthStart.AddMonths(-i);
                DateTime end = start.AddMonths(1).AddDays(-1);
                string label = i == 0
                    ? $"This Month ({start:MMMM yyyy})"
                    : i == 1
                        ? $"Last Month ({start:MMMM yyyy})"
                        : start.ToString("MMMM yyyy");

                _months.Add(new PeriodItem
                {
                    DisplayText = label,
                    StartDate = start,
                    EndDate = end
                });
            }
        }

        private void PopulateComboBox()
        {
            if (PeriodComboBox == null) return;

            PeriodComboBox.Items.Clear();
            if (RadioWeekly.IsChecked == true)
            {
                foreach (var week in _weeks)
                {
                    PeriodComboBox.Items.Add(week.DisplayText);
                }
            }
            else
            {
                foreach (var month in _months)
                {
                    PeriodComboBox.Items.Add(month.DisplayText);
                }
            }
            PeriodComboBox.SelectedIndex = 0;
        }

        private void ReportType_Changed(object sender, RoutedEventArgs e)
        {
            PopulateComboBox();
        }

        private void Cancel_Click(object sender, RoutedEventArgs e)
        {
            this.DialogResult = false;
            this.Close();
        }

        private void Generate_Click(object sender, RoutedEventArgs e)
        {
            if (PeriodComboBox.SelectedIndex < 0) return;

            try
            {
                bool isWeekly = RadioWeekly.IsChecked == true;
                PeriodItem selectedPeriod = isWeekly 
                    ? _weeks[PeriodComboBox.SelectedIndex] 
                    : _months[PeriodComboBox.SelectedIndex];

                DateTime start = selectedPeriod.StartDate;
                DateTime end = selectedPeriod.EndDate;
                int totalDaysInPeriod = (end - start).Days + 1;

                int totalPrayersCompleted = 0;
                int totalDaysTracked = 0;
                int totalAdhkarCompleted = 0;
                int totalNafalCompleted = 0;
                int totalTasbihCount = 0;
                int fastingDays = 0;

                var prayerStats = new Dictionary<string, int>
                {
                    { "Fajr", 0 },
                    { "Dhuhr", 0 },
                    { "Asr", 0 },
                    { "Maghrib", 0 },
                    { "Isha", 0 }
                };

                for (DateTime date = start; date <= end; date = date.AddDays(1))
                {
                    var dayDeeds = TrackerService.Instance.LoadDay(date);
                    bool hasData = dayDeeds.Prayers.Values.Any(p => p.Any(d => d.IsChecked)) || dayDeeds.TotalTasbihCount > 0;
                    if (hasData) totalDaysTracked++;

                    if (dayDeeds.Sawm) fastingDays++;

                    foreach (var prayer in dayDeeds.Prayers)
                    {
                        string prayerKey = prayer.Key == "Jumuah" ? "Dhuhr" : prayer.Key;
                        foreach (var deed in prayer.Value)
                        {
                            if (deed.IsChecked)
                            {
                                if (prayer.Key == "Adhkar")
                                {
                                    totalAdhkarCompleted++;
                                }
                                else if (prayer.Key == "Tahajjud" || prayer.Key == "Duha" || prayer.Key == "Awwabin")
                                {
                                    totalNafalCompleted++;
                                }
                                else if (deed.Type == DeedType.Fard)
                                {
                                    totalPrayersCompleted++;
                                    if (prayerStats.ContainsKey(prayerKey))
                                    {
                                        prayerStats[prayerKey]++;
                                    }
                                }
                            }
                        }
                    }

                    if (dayDeeds.TasbihCounts != null)
                    {
                        foreach (var count in dayDeeds.TasbihCounts.Values)
                            totalTasbihCount += count;
                    }
                }

                double completionRate = (5 * totalDaysInPeriod) > 0
                    ? Math.Round((double)totalPrayersCompleted / (5 * totalDaysInPeriod) * 100, 1)
                    : 0;

                string periodLabel = isWeekly
                    ? $"{start:MMM dd} - {end:MMM dd, yyyy}".ToUpper()
                    : start.ToString("MMMM yyyy").ToUpper();

                string fastingLabel = fastingDays > 0
                    ? isWeekly 
                        ? $"🌙 Fasting: {fastingDays} / 7 Days"
                        : $"🌙 Fasting: {fastingDays} Days"
                    : "🌙 Fasting: Not Tracked";

                string fileDateStr = isWeekly
                    ? start.ToString("yyyy-MM-dd")
                    : start.ToString("yyyy-MM");

                // Generate the card
                string filePath = CardGenerator.GenerateCard(
                    isWeekly,
                    periodLabel,
                    totalPrayersCompleted,
                    totalDaysTracked,
                    completionRate,
                    totalDaysInPeriod,
                    totalAdhkarCompleted,
                    totalNafalCompleted,
                    totalTasbihCount,
                    prayerStats,
                    fastingLabel,
                    fileDateStr
                );

                this.DialogResult = true;
                this.Close();

                var result = MessageBox.Show(
                    $"Card saved to:\n{filePath}\n\nOpen folder to view?",
                    "Card Generated!",
                    MessageBoxButton.YesNo,
                    MessageBoxImage.Information);

                if (result == MessageBoxResult.Yes)
                {
                    System.Diagnostics.Process.Start("explorer.exe", $"/select,\"{filePath}\"");
                }
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Failed to generate card: {ex.Message}", "Error", MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }
    }

    public class PeriodItem
    {
        public string DisplayText { get; set; } = "";
        public DateTime StartDate { get; set; }
        public DateTime EndDate { get; set; }
    }
}

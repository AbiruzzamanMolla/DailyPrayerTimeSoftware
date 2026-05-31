using System;
using System.Collections.Generic;
using System.Linq;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Media;
using DailyPrayerTime.Native.Helpers;
using DailyPrayerTime.Native.Models;
using DailyPrayerTime.Native.Services;

namespace DailyPrayerTime.Native.Views
{
    public partial class CycleView : UserControl
    {
        private CycleService _cycleService;

        public CycleView()
        {
            InitializeComponent();
            _cycleService = CycleService.Instance;
        }

        public void LoadData()
        {
            UpdateStatusCard();
            UpdateStats();
            LoadEntries();
        }

        private void UpdateStatusCard()
        {
            var today = DateTime.Today;
            var info = _cycleService.GetDayInfo(today);

            switch (info.Status)
            {
                case CycleStatus.Hayd:
                    StatusEmoji.Text = "🔴";
                    StatusText.Text = LocalizationManager.Instance.GetString("Cycle_Hayd") ?? "Menstruation (Hayd)";
                    StatusDetail.Text = info.PeriodDayNumber > 0
                        ? string.Format(LocalizationManager.Instance.GetString("Cycle_DayOfPeriod") ?? "Day {0}", info.PeriodDayNumber)
                        : "";
                    StatusPrediction.Text = "";
                    break;
                case CycleStatus.Tuhr:
                    StatusEmoji.Text = "🟢";
                    StatusText.Text = LocalizationManager.Instance.GetString("Cycle_Tuhr") ?? "Pure (Tuhur)";
                    int safeDays = _cycleService.GetDaysUntilNextPeriod();
                    StatusDetail.Text = safeDays >= 0
                        ? string.Format(LocalizationManager.Instance.GetString("Cycle_SafeDays") ?? "{0} safe days", safeDays)
                        : "";
                    StatusPrediction.Text = safeDays >= 0
                        ? string.Format(LocalizationManager.Instance.GetString("Cycle_NextPeriod") ?? "Next in {0} days", safeDays)
                        : "";
                    break;
                case CycleStatus.Istihadah:
                    StatusEmoji.Text = "🟡";
                    StatusText.Text = LocalizationManager.Instance.GetString("Cycle_Istihadah") ?? "Irregular Bleeding";
                    StatusDetail.Text = "Consult a scholar for specific rulings";
                    StatusPrediction.Text = "";
                    break;
                default:
                    StatusEmoji.Text = "⚪";
                    StatusText.Text = LocalizationManager.Instance.GetString("Cycle_Unknown") ?? "No data";
                    StatusDetail.Text = "Tap 'Start Period' to begin tracking";
                    StatusPrediction.Text = "";
                    break;
            }
        }

        private void UpdateStats()
        {
            var meta = _cycleService.Meta;
            if (meta.TotalCycles > 0)
            {
                AvgCycleText.Text = meta.AverageCycleLength.ToString();
                AvgPeriodText.Text = meta.AveragePeriodLength.ToString();
            }

            int daysUntil = _cycleService.GetDaysUntilNextPeriod();
            NextPeriodText.Text = daysUntil >= 0 ? daysUntil.ToString() : "--";
        }

        private void LoadEntries()
        {
            EntriesList.Children.Clear();
            var entries = _cycleService.Entries.OrderByDescending(e => e.StartDate).Take(12).ToList();

            if (entries.Count == 0)
            {
                EntriesList.Children.Add(EmptyText);
                EmptyText.Visibility = Visibility.Visible;
                return;
            }

            EmptyText.Visibility = Visibility.Collapsed;

            foreach (var entry in entries)
            {
                EntriesList.Children.Add(CreateEntryCard(entry));
            }
        }

        private Border CreateEntryCard(CycleEntry entry)
        {
            DateTime start = DateTime.Parse(entry.StartDate);
            string endDate = string.IsNullOrEmpty(entry.EndDate) ? "Present" : DateTime.Parse(entry.EndDate).ToString("MMM dd");
            int periodDays = string.IsNullOrEmpty(entry.EndDate) ? (DateTime.Today - start).Days + 1
                : (DateTime.Parse(entry.EndDate) - start).Days + 1;

            var statusColor = entry.EndDate == null ? "#dc2626" : "#059669";
            var statusText = entry.EndDate == null ? "Ongoing" : $"{periodDays} days";

            var card = new Border
            {
                Background = new SolidColorBrush(Color.FromArgb(0x1A, 0xFF, 0xFF, 0xFF)),
                BorderBrush = new SolidColorBrush(Color.FromArgb(0x2A, 0xFF, 0xFF, 0xFF)),
                BorderThickness = new Thickness(1),
                CornerRadius = new CornerRadius(10),
                Padding = new Thickness(12, 10, 12, 10),
                Margin = new Thickness(0, 0, 0, 6)
            };

            var grid = new Grid();
            grid.ColumnDefinitions.Add(new ColumnDefinition { Width = new GridLength(1, GridUnitType.Star) });
            grid.ColumnDefinitions.Add(new ColumnDefinition { Width = GridLength.Auto });

            var left = new StackPanel();
            left.Children.Add(new TextBlock
            {
                Text = $"{start:MMM dd, yyyy} — {endDate}",
                FontSize = 12,
                FontWeight = FontWeights.SemiBold,
                Foreground = new SolidColorBrush(Colors.White)
            });
            left.Children.Add(new TextBlock
            {
                Text = $"{_cycleService.SelectedMadhab} madhab · {periodDays} days",
                FontSize = 10,
                Foreground = new SolidColorBrush(Color.FromArgb(0x99, 0xFF, 0xFF, 0xFF)),
                Margin = new Thickness(0, 2, 0, 0)
            });

            var deleteBtn = new Button
            {
                Content = "✕",
                FontSize = 10,
                Foreground = new SolidColorBrush(Color.FromArgb(0x99, 0xFF, 0xFF, 0xFF)),
                Background = new SolidColorBrush(Colors.Transparent),
                BorderThickness = new Thickness(0),
                Cursor = System.Windows.Input.Cursors.Hand,
                Tag = entry.StartDate,
                VerticalAlignment = VerticalAlignment.Center,
                Padding = new Thickness(4)
            };
            deleteBtn.Click += DeleteEntry_Click;

            Grid.SetColumn(left, 0);
            Grid.SetColumn(deleteBtn, 1);
            grid.Children.Add(left);
            grid.Children.Add(deleteBtn);

            card.Child = grid;
            return card;
        }

        private void StartPeriod_Click(object sender, RoutedEventArgs e)
        {
            var today = DateTime.Today;
            var existing = _cycleService.Entries.FirstOrDefault(
                e => e.StartDate == today.ToString("yyyy-MM-dd"));

            if (existing != null)
            {
                MessageBox.Show("Already started today.", "", MessageBoxButton.OK, MessageBoxImage.Information);
                return;
            }

            var entry = new CycleEntry
            {
                StartDate = today.ToString("yyyy-MM-dd"),
                Status = CycleStatus.Hayd,
                Madhab = _cycleService.SelectedMadhab
            };

            _cycleService.AddEntry(entry);
            _ = CloudSyncService.Instance.PushCycleEntryAsync(entry.StartDate, entry);
            LoadData();
        }

        private void EndPeriod_Click(object sender, RoutedEventArgs e)
        {
            var ongoing = _cycleService.Entries.FirstOrDefault(
                e => string.IsNullOrEmpty(e.EndDate));

            if (ongoing == null)
            {
                MessageBox.Show("No ongoing period to end.", "", MessageBoxButton.OK, MessageBoxImage.Information);
                return;
            }

            ongoing.EndDate = DateTime.Today.ToString("yyyy-MM-dd");
            ongoing.Status = CycleStatus.Tuhr;
            _cycleService.UpdateEntry(ongoing.StartDate, ongoing);
            _ = CloudSyncService.Instance.PushCycleEntryAsync(ongoing.StartDate, ongoing);
            LoadData();
        }

        private void DeleteEntry_Click(object sender, RoutedEventArgs e)
        {
            if (sender is Button btn && btn.Tag is string startDate)
            {
                var result = MessageBox.Show("Delete this entry?", "Confirm",
                    MessageBoxButton.YesNo, MessageBoxImage.Warning);
                if (result == MessageBoxResult.Yes)
                {
                    _cycleService.DeleteEntry(startDate);
                    _ = FirestoreRestHelper.DeleteDocumentAsync("cycle", startDate);
                    LoadData();
                }
            }
        }

        private void Refresh_Click(object sender, RoutedEventArgs e)
        {
            LoadData();
        }
    }
}

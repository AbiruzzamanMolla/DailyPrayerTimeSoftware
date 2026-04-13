using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Linq;
using System.Windows;
using System.Windows.Controls;
using DailyPrayerTime.Native.Models;
using DailyPrayerTime.Native.Services;

namespace DailyPrayerTime.Native
{
    public partial class TrackerView : System.Windows.Controls.UserControl
    {
        private DailyDeeds _currentDeeds;
        public ObservableCollection<PrayerTrackItem> PrayerItems { get; set; } = new ObservableCollection<PrayerTrackItem>();

        public TrackerView()
        {
            InitializeComponent();
            PrayerList.ItemsSource = PrayerItems;
        }

        public void LoadData()
        {
            _currentDeeds = TrackerService.Instance.LoadDay(DateTime.Today);
            
            // Sync UI
            CurrentDateLabel.Text = DateTime.Today.ToString("dd MMM").ToUpper();
            SawmToggle.IsChecked = _currentDeeds.Sawm;
            SawmIndicator.Visibility = _currentDeeds.Sawm ? Visibility.Visible : Visibility.Collapsed;
            
            UpdateNafalUI();
            RefreshPrayerList();
            UpdateOverallProgress();
            LoadHistory();
        }

        public void UpdateMiniStatus(string name, string countdown)
        {
            MiniPrayerName.Text = name?.ToUpper() ?? "NONE";
            MiniCountdown.Text = countdown ?? "00:00:00";
        }

        private void UpdateNafalUI()
        {
            DuhaCount.Text = GetNafalValue("Duha").ToString();
            AwwabinCount.Text = GetNafalValue("Awwabin").ToString();
            TahajjudCount.Text = GetNafalValue("Tahajjud").ToString();
        }

        private int GetNafalValue(string name)
        {
            if (!_currentDeeds.Prayers.ContainsKey(name)) return 0;
            return _currentDeeds.Prayers[name].FirstOrDefault()?.Value ?? 0;
        }

        private void RefreshPrayerList()
        {
            PrayerItems.Clear();
            string[] sections = { "Fajr", "Dhuhr", "Asr", "Maghrib", "Isha", "Adhkar" };
            
            foreach (var p in sections)
            {
                if (_currentDeeds.Prayers.TryGetValue(p, out var deeds))
                {
                    var item = new PrayerTrackItem
                    {
                        PrayerName = p == "Adhkar" ? "ADHKAR & DUAS" : p.ToUpper(),
                        Deeds = deeds
                    };
                    PrayerItems.Add(item);
                }
            }
        }

        private void UpdateOverallProgress()
        {
            int total = 0;
            int checkedCount = 0;

            foreach (var p in _currentDeeds.Prayers.Values)
            {
                foreach (var d in p)
                {
                    total++;
                    if (d.IsChecked || (d.Type == DeedType.Nafl && d.Value > 0)) checkedCount++;
                }
            }

            if (total == 0) return;
            int percent = (checkedCount * 100) / total;
            OverallProgressPercent.Text = $"{percent}%";
            MainProgressBar.Value = percent;
        }

        private void RakatCheck_Changed(object sender, RoutedEventArgs e)
        {
            TrackerService.Instance.SaveDay(_currentDeeds);
            UpdateOverallProgress();
            LoadHistory();
        }

        private void NafalCount_Click(object sender, RoutedEventArgs e)
        {
            if (sender is System.Windows.Controls.Button btn && btn.Tag is string tag)
            {
                var parts = tag.Split(':');
                string name = parts[0];
                int delta = int.Parse(parts[1]);

                if (!_currentDeeds.Prayers.ContainsKey(name))
                    _currentDeeds.Prayers[name] = new List<DeedEntry> { new DeedEntry { Label = name, Type = DeedType.Nafl } };

                var entry = _currentDeeds.Prayers[name].First();
                entry.Value = Math.Max(0, entry.Value + delta);
                entry.IsChecked = entry.Value > 0;

                UpdateNafalUI();
                TrackerService.Instance.SaveDay(_currentDeeds);
                UpdateOverallProgress();
                LoadHistory();
            }
        }

        private void LoadHistory()
        {
            var today = DateTime.Today;
            var history = new List<HistoryItem>();
            
            for (int i = 0; i < 7; i++)
            {
                var date = today.AddDays(-i);
                var deeds = TrackerService.Instance.LoadDay(date);
                int prog = CalculateProgress(deeds);
                
                history.Add(new HistoryItem 
                { 
                    DateLabel = i == 0 ? "Today" : (i == 1 ? "Yesterday" : date.ToString("MMM dd")),
                    ProgressValue = prog,
                    ProgressText = $"{prog}%"
                });
            }
            
            HistoryList.ItemsSource = history;
        }

        private int CalculateProgress(DailyDeeds deeds)
        {
            int total = 0;
            int checkedCount = 0;
            foreach (var p in deeds.Prayers.Values)
            {
                foreach (var d in p)
                {
                    total++;
                    if (d.IsChecked || (d.Type == DeedType.Nafl && d.Value > 0)) checkedCount++;
                }
            }
            return total == 0 ? 0 : (checkedCount * 100) / total;
        }

        private void Close_Click(object sender, RoutedEventArgs e)
        {
            if (System.Windows.Application.Current.MainWindow is MainWindow mw)
            {
                mw.TrackerToggle_Click(this, null);
            }
        }
    }

    public class HistoryItem
    {
        public string DateLabel { get; set; } = "";
        public double ProgressValue { get; set; } = 0;
        public string ProgressText { get; set; } = "";
    }

    public class PrayerTrackItem
    {
        public string PrayerName { get; set; }
        public List<DeedEntry> Deeds { get; set; }
        public string CompletionText => $"{Deeds.Count(d => d.IsChecked)}/{Deeds.Count} Done";
    }
}

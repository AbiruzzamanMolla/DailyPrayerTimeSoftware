using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Linq;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Media;
using System.Windows.Shapes;
using WColor = System.Windows.Media.Color;
using WColorConverter = System.Windows.Media.ColorConverter;
using DailyPrayerTime.Native.Models;
using DailyPrayerTime.Native.Services;

namespace DailyPrayerTime.Native
{
    public partial class TrackerView : System.Windows.Controls.UserControl
    {
        private DailyDeeds _currentDeeds = null!;
        public ObservableCollection<PrayerTrackItem> PrayerItems { get; set; } = new ObservableCollection<PrayerTrackItem>();

        public TrackerView()
        {
            InitializeComponent();
            PrayerList.ItemsSource = PrayerItems;
            TrackerTabList.SelectedIndex = 0; // Default to Daily
        }

        private HashSet<string> _enabledPrayers = new HashSet<string>();

        public void LoadData(HashSet<string>? enabledPrayers = null, DateTime? specificDate = null)
        {
            DateTime targetDate = specificDate ?? DateTime.Today;
            
            // For past dates, all prayers should be considered "enabled" since the time has already passed.
            if (targetDate.Date < DateTime.Today)
            {
                _enabledPrayers = new HashSet<string> { "Adhkar", "Adhkar_Morning", "Adhkar_Evening", "Ishraq", "Duha", "Awwabin", "Tahajjud", "Fajr", "Dhuhr", "Asr", "Maghrib", "Isha", "Jumuah" };
            }
            else
            {
                _enabledPrayers = enabledPrayers ?? new HashSet<string> { "Adhkar", "Ishraq", "Duha", "Awwabin", "Tahajjud", "Fajr", "Dhuhr", "Asr", "Maghrib", "Isha", "Jumuah" };
            }
            
            _currentDeeds = TrackerService.Instance.LoadDay(targetDate);
            
            // Sync UI
            UpdateDateDisplay(targetDate);
            SawmToggle.IsChecked = _currentDeeds.Sawm;
            SawmIndicator.Visibility = _currentDeeds.Sawm ? Visibility.Visible : Visibility.Collapsed;
            
            UpdateNafalUI();
            RefreshPrayerList();
            UpdateOverallProgress();
            
            // If we are looking at today, show the tab system. If a specific past date, maybe go to Daily tab.
            if (specificDate.HasValue && specificDate != DateTime.Today)
            {
                TrackerTabList.SelectedIndex = 0; // Force Daily
            }
            UpdateViewForTab();
        }

        private void UpdateDateDisplay(DateTime date)
        {
            CurrentDateLabel.Text = date.ToString("dd MMM").ToUpper();
            
            try
            {
                var calendar = new System.Globalization.UmAlQuraCalendar();
                var hijriDate = date.AddDays(SettingsManager.Current.HijriAdjustment);
                int d = calendar.GetDayOfMonth(hijriDate);
                int m = calendar.GetMonth(hijriDate);
                int y = calendar.GetYear(hijriDate);
                
                string[] months = { "", "Muharram", "Safar", "Rabi' al-awwal", "Rabi' al-thani", "Jumada al-ula", "Jumada al-akhira", "Rajab", "Sha'ban", "Ramadan", "Shawwal", "Dhu al-Qi'dah", "Dhu al-Hijjah" };
                HijriDateLabel.Text = $"{d} {months[m].ToUpper()} {y} AH";
            }
            catch { HijriDateLabel.Text = ""; }
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
            
            DuhaGrid.IsEnabled = _enabledPrayers.Contains("Duha") || _enabledPrayers.Contains("Ishraq");
            AwwabinGrid.IsEnabled = _enabledPrayers.Contains("Awwabin");
            TahajjudGrid.IsEnabled = _enabledPrayers.Contains("Tahajjud");
        }

        private int GetNafalValue(string name)
        {
            if (!_currentDeeds.Prayers.ContainsKey(name)) return 0;
            return _currentDeeds.Prayers[name].FirstOrDefault()?.Value ?? 0;
        }

        public void ReloadCurrentDate()
        {
            if (_currentDeeds != null && _currentDeeds.Date == DateTime.Today.ToString("yyyy-MM-dd"))
            {
                _currentDeeds = TrackerService.Instance.LoadDay(DateTime.Today);
                UpdateOverallProgress();
                UpdateViewForTab();
                UpdateQadhaSummary();
                foreach (var item in PrayerItems) item.Refresh();
            }
        }

        private void RefreshPrayerList()
        {
            PrayerItems.Clear();
            
            DateTime date;
            if (!DateTime.TryParse(_currentDeeds.Date, out date)) date = DateTime.Today;
            
            bool isFriday = date.DayOfWeek == DayOfWeek.Friday;
            string[] sections = isFriday 
                ? new[] { "Fajr", "Jumuah", "Asr", "Maghrib", "Isha", "Adhkar" } 
                : new[] { "Fajr", "Dhuhr", "Asr", "Maghrib", "Isha", "Adhkar" };
            
            foreach (var p in sections)
            {
                if (_currentDeeds.Prayers.TryGetValue(p, out var deeds))
                {
                    bool isEnabled = _enabledPrayers.Contains(p);
                    foreach (var d in deeds)
                    {
                        if (p == "Adhkar")
                        {
                            if (d.Label.Contains("Morning", StringComparison.OrdinalIgnoreCase))
                                d.IsEnabled = isEnabled; // Fajr
                            else if (d.Label.Contains("Evening", StringComparison.OrdinalIgnoreCase))
                                d.IsEnabled = _enabledPrayers.Contains("Adhkar_Evening"); // Asr
                            else
                                d.IsEnabled = isEnabled;
                        }
                        else
                        {
                            d.IsEnabled = isEnabled;
                        }
                    }

                    var item = new PrayerTrackItem
                    {
                        PrayerName = p == "Adhkar" ? "ADHKAR & DUAS" : (p == "Jumuah" ? "JUMU'AH" : p.ToUpper()),
                        Deeds = deeds,
                        IsEnabled = isEnabled
                    };
                    PrayerItems.Add(item);
                }
            }
        }

        private void UpdateOverallProgress()
        {
            int total = 0;
            int checkedCount = 0;
            int qadhaCount = 0;

            DateTime targetDate;
            if (!DateTime.TryParse(_currentDeeds.Date, out targetDate)) targetDate = DateTime.Today;

            foreach (var item in PrayerItems)
            {
                if (!item.IsEnabled) continue;

                foreach (var d in item.Deeds)
                {
                    if (!d.IsEnabled) continue;
                    total++;
                    if (d.IsChecked) checkedCount++;
                }

                // Qadha logic for current view (if it's a fard prayer)
                if (item.PrayerName != "ADHKAR & DUAS")
                {
                    var fardDeed = item.Deeds.FirstOrDefault(d => d.Type == DeedType.Fard);
                    if (fardDeed != null && fardDeed.IsEnabled && !fardDeed.IsChecked)
                    {
                        // For past dates, if it's enabled but not checked, it's Qadha
                        if (targetDate.Date < DateTime.Today)
                        {
                            qadhaCount++;
                        }
                        else
                        {
                            // For today, it's Qadha only if the *next* prayer has started
                            // item.IsEnabled being true only means it HAS started.
                            // We need to know if it has ENDED.
                            if (IsPrayerEnded(item.PrayerName))
                            {
                                qadhaCount++;
                            }
                        }
                    }
                }
            }

            if (total == 0) return;
            int percent = Math.Min(100, (checkedCount * 100) / total);
            OverallProgressPercent.Text = $"{percent}%";
            MainProgressBar.Value = percent;

            StatsCompletedCount.Text = checkedCount.ToString();
            StatsMissedCount.Text = qadhaCount.ToString();
        }

        private bool IsPrayerEnded(string prayerName)
        {
            if (System.Windows.Application.Current.MainWindow is MainWindow mw)
            {
                var times = mw.GetTodayPrayerTimes(); // Assume I'll add this or access field
                if (times == null) return false;

                var now = DateTime.Now;
                return prayerName.ToUpper() switch
                {
                    "FAJR" => now > times.Sunrise,
                    "DHUHR" => now > times.Asr,
                    "JUMUAH" => now > times.Asr,
                    "ASR" => now > times.Maghrib,
                    "MAGHRIB" => now > times.Isha,
                    "ISHA" => false, // Isha ends at next day's Fajr, making today a past date
                    _ => false
                };
            }
            return false;
        }

        private bool ConfirmPastEdit()
        {
            if (_currentDeeds.Date != DateTime.Today.ToString("yyyy-MM-dd"))
            {
                var result = System.Windows.MessageBox.Show(
                    "You are modifying tracking data for a past date.\nAre you sure you want to change this record?", 
                    "Edit Past Record", 
                    System.Windows.MessageBoxButton.YesNo, 
                    System.Windows.MessageBoxImage.Warning);
                return result == System.Windows.MessageBoxResult.Yes;
            }
            return true;
        }

        private bool _isRevertingCheck = false;

        private void SawmToggle_Click(object sender, RoutedEventArgs e)
        {
            if (_isRevertingCheck || !IsLoaded) return;
            
            if (!ConfirmPastEdit())
            {
                _isRevertingCheck = true;
                SawmToggle.IsChecked = !SawmToggle.IsChecked;
                _isRevertingCheck = false;
                return;
            }
            
            _currentDeeds.Sawm = SawmToggle.IsChecked == true;
            SawmIndicator.Visibility = _currentDeeds.Sawm ? Visibility.Visible : Visibility.Collapsed;
            TrackerService.Instance.SaveDay(_currentDeeds);
            UpdateOverallProgress();

            if (System.Windows.Application.Current.MainWindow is MainWindow mw)
            {
                mw.SyncSawmRemote(_currentDeeds.Sawm);
            }
        }

        public void SyncSawmRemote(bool isChecked)
        {
            if (_currentDeeds != null) _currentDeeds.Sawm = isChecked;
            if (SawmToggle != null) SawmToggle.IsChecked = isChecked;
            if (SawmIndicator != null) SawmIndicator.Visibility = isChecked ? Visibility.Visible : Visibility.Collapsed;
            UpdateOverallProgress();
        }

        private void RakatCheck_Click(object sender, RoutedEventArgs e)
        {
            if (_isRevertingCheck || !IsLoaded) return;

            if (!ConfirmPastEdit())
            {
                _isRevertingCheck = true;
                if (sender is System.Windows.Controls.CheckBox cb) cb.IsChecked = !cb.IsChecked;
                _isRevertingCheck = false;
                return;
            }

            TrackerService.Instance.SaveDay(_currentDeeds);
            UpdateOverallProgress();
            UpdateViewForTab();
            foreach (var item in PrayerItems) item.Refresh();

            if (System.Windows.Application.Current.MainWindow is MainWindow mw)
            {
                mw.ReloadHeroTrackerFromDisk();
            }
        }

        private void NafalCount_Click(object sender, RoutedEventArgs e)
        {
            if (!ConfirmPastEdit()) return;

            if (sender is System.Windows.Controls.Button btn && btn.Tag is string tag)
            {
                var parts = tag.Split(':');
                string name = parts[0];
                int delta = int.Parse(parts[1]);

                if (!_currentDeeds.Prayers.ContainsKey(name))
                    _currentDeeds.Prayers[name] = new List<DeedEntry> { new DeedEntry { Label = name, Type = DeedType.Nafl } };

                var entry = _currentDeeds.Prayers[name].FirstOrDefault();
                if (entry == null)
                {
                    entry = new DeedEntry { Label = name, Type = DeedType.Nafl };
                    _currentDeeds.Prayers[name].Add(entry);
                }
                entry.Value = Math.Max(0, entry.Value + delta);
                entry.IsChecked = entry.Value > 0;

                UpdateNafalUI();
                TrackerService.Instance.SaveDay(_currentDeeds);
                UpdateOverallProgress();
                UpdateViewForTab();

                if (System.Windows.Application.Current.MainWindow is MainWindow mw)
                {
                    mw.ReloadHeroTrackerFromDisk();
                }
            }
        }

        private void TrackerTab_Changed(object sender, SelectionChangedEventArgs e)
        {
            // Only respond to tab changes from the actual TrackerTabList, not nested elements
            if (e.OriginalSource == TrackerTabList && IsLoaded)
            {
                UpdateViewForTab();
            }
        }

        private void UpdateViewForTab()
        {
            if (TrackerTabList.SelectedItem is ListBoxItem selected)
            {
                string tab = selected.Content?.ToString() ?? "";
                DailySection.Visibility = tab == "Daily" ? Visibility.Visible : Visibility.Collapsed;
                HistoryHeader.Visibility = tab != "Daily" ? Visibility.Visible : Visibility.Collapsed;
                
                // Hide components initially
                HistoryList.Visibility = Visibility.Collapsed;
                CalendarGrid.Visibility = Visibility.Collapsed;
                TrackerBackButton.Visibility = Visibility.Collapsed;

                UpdateQadhaSummary();

                switch (tab)
                {
                    case "Daily":
                        OverallProgressTitle.Text = LocalizationManager.Instance.GetString("Tracker_DailyCompletion");
                        if (_currentDeeds.Date != DateTime.Today.ToString("yyyy-MM-dd"))
                        {
                            HistoryHeader.Visibility = Visibility.Visible;
                            TrackerBackButton.Visibility = Visibility.Visible;
                            HistorySectionTitle.Text = "PAST ACTIVITY";
                        }
                        UpdateOverallProgress(); // This updates the card for Daily
                        break;
                    case "Weekly":
                        OverallProgressTitle.Text = LocalizationManager.Instance.GetString("Tracker_WeeklyCompletion");
                        HistorySectionTitle.Text = "THIS WEEK (SAT - FRI)";
                        HistoryList.Visibility = Visibility.Visible;
                        LoadWeeklyHistory();
                        UpdateAggregateProgress(7);
                        break;
                    case "Monthly":
                        OverallProgressTitle.Text = LocalizationManager.Instance.GetString("Tracker_MonthlyCompletion");
                        HistorySectionTitle.Text = DateTime.Today.ToString("MMMM yyyy").ToUpper();
                        CalendarGrid.Visibility = Visibility.Visible;
                        LoadMonthlyCalendar(DateTime.Today.Year, DateTime.Today.Month);
                        UpdateAggregateProgress(DateTime.DaysInMonth(DateTime.Today.Year, DateTime.Today.Month));
                        break;
                    case "Yearly":
                        OverallProgressTitle.Text = LocalizationManager.Instance.GetString("Tracker_YearlyCompletion");
                        HistorySectionTitle.Text = DateTime.Today.Year + " SUMMARY";
                        HistoryList.Visibility = Visibility.Visible;
                        LoadYearlyHistory();
                        UpdateAggregateProgress(365);
                        break;
                }
            }
        }

        private void UpdateAggregateProgress(int days)
        {
            var today = DateTime.Today;
            int totalCheckable = 0;
            int checkedCount = 0;
            int qadhaCount = 0;

            for (int i = 0; i < days; i++)
            {
                var date = today.AddDays(-i);
                // Limit monthly/weekly to their boundaries if needed, but simple days back is fine for "last X days" or "this period so far"
                if (days == 7) { /* handle week logic if needed */ }
                
                var deeds = TrackerService.Instance.LoadDay(date);
                var stats = GetStatsForDay(deeds);
                totalCheckable += stats.total;
                checkedCount += stats.completed;
                qadhaCount += stats.qadha;
            }

            if (totalCheckable == 0) return;
            int percent = (checkedCount * 100) / totalCheckable;
            OverallProgressPercent.Text = $"{percent}%";
            MainProgressBar.Value = percent;

            StatsCompletedCount.Text = checkedCount.ToString();
            StatsMissedCount.Text = qadhaCount.ToString();
        }

        private (int total, int completed, int qadha) GetStatsForDay(DailyDeeds deeds)
        {
            int total = 0;
            int completed = 0;
            int qadha = 0;

            DateTime date;
            if (!DateTime.TryParse(deeds.Date, out date)) date = DateTime.Today;

            foreach (var p in deeds.Prayers)
            {
                // We only count Qadha for Fard prayers (Fajr, Dhuhr/Jumuah, Asr, Maghrib, Isha)
                bool isFardPrayer = p.Key == "Fajr" || p.Key == "Dhuhr" || p.Key == "Asr" || 
                                   p.Key == "Maghrib" || p.Key == "Isha" || p.Key == "Jumuah";

                foreach (var d in p.Value)
                {
                    total++;
                    if (d.IsChecked) completed++;
                    else if (isFardPrayer && d.Type == DeedType.Fard)
                    {
                        if (date < DateTime.Today) qadha++;
                        else if (IsPrayerEnded(p.Key)) qadha++;
                    }
                }
            }
            return (total, completed, qadha);
        }

        private void UpdateQadhaSummary()
        {
            var today = DateTime.Today;
            
            // Today
            var todayStats = GetStatsForDay(TrackerService.Instance.LoadDay(today));
            QadhaTodayCount.Text = todayStats.qadha.ToString();

            // Week (Current localized week or last 7 days)
            int weekQadha = 0;
            // Get last 7 days including today
            for(int i=0; i<7; i++)
            {
                var d = TrackerService.Instance.LoadDay(today.AddDays(-i));
                weekQadha += GetStatsForDay(d).qadha;
            }
            QadhaWeekCount.Text = weekQadha.ToString();

            // Month
            int monthQadha = 0;
            for(int i=0; i<today.Day; i++)
            {
                var d = TrackerService.Instance.LoadDay(today.AddDays(-i));
                monthQadha += GetStatsForDay(d).qadha;
            }
            QadhaMonthCount.Text = monthQadha.ToString();

            // Year
            int yearQadha = 0;
            int daysOfYear = today.DayOfYear;
            // Note: Year-to-date calculation. If performance is an issue for users with 1 year data,
            // we will need to implement a background worker or a database.
            for(int i=0; i<daysOfYear; i++)
            {
                var d = TrackerService.Instance.LoadDay(today.AddDays(-i));
                yearQadha += GetStatsForDay(d).qadha;
            }
            QadhaYearCount.Text = yearQadha.ToString();
        }

        private void LoadHistory(int days)
        {
            var today = DateTime.Today;
            var history = new List<HistoryItem>();
            
            for (int i = 0; i < days; i++)
            {
                var date = today.AddDays(-i);
                var deeds = TrackerService.Instance.LoadDay(date);
                int prog = CalculateProgress(deeds);
                
                string label = i switch
                {
                    0 => "Today",
                    1 => "Yesterday",
                    _ => date.ToString("MMM dd")
                };

                history.Add(new HistoryItem 
                { 
                    DateLabel = label,
                    ProgressValue = prog,
                    ProgressText = $"{prog}%",
                    FullDate = date
                });
            }
            
            HistoryList.ItemsSource = history;
        }

        private void LoadWeeklyHistory()
        {
            var today = DateTime.Today;
            // Saturday is DayOfWeek.Saturday (6), Friday is DayOfWeek.Friday (5)
            // We find the most recent Saturday
            int diff = (7 + (int)today.DayOfWeek - (int)DayOfWeek.Saturday) % 7;
            var startOfWeek = today.AddDays(-diff);
            
            var history = new List<HistoryItem>();
            for (int i = 0; i < 7; i++)
            {
                var date = startOfWeek.AddDays(i);
                if (date > today) break;

                var deeds = TrackerService.Instance.LoadDay(date);
                int prog = CalculateProgress(deeds);
                
                history.Add(new HistoryItem 
                { 
                    DateLabel = date.DayOfWeek.ToString() + (date == today ? " (TODAY)" : ""),
                    ProgressValue = prog,
                    ProgressText = $"{prog}%",
                    FullDate = date
                });
            }
            // Reverse so latest is on top
            history.Reverse();
            HistoryList.ItemsSource = history;
        }

        private void LoadYearlyHistory()
        {
            // Group by month for the current year
            var currentYear = DateTime.Today.Year;
            var history = new List<HistoryItem>();

            for (int m = 12; m >= 1; m--)
            {
                if (currentYear == DateTime.Today.Year && m > DateTime.Today.Month) continue;

                // Simple aggregation: average of the month
                int daysInMonth = DateTime.DaysInMonth(currentYear, m);
                if (currentYear == DateTime.Today.Year && m == DateTime.Today.Month) daysInMonth = DateTime.Today.Day;

                long totalProg = 0;
                for (int d = 1; d <= daysInMonth; d++)
                {
                    var date = new DateTime(currentYear, m, d);
                    var deeds = TrackerService.Instance.LoadDay(date);
                    totalProg += CalculateProgress(deeds);
                }

                int avgProg = (int)(totalProg / daysInMonth);
                history.Add(new HistoryItem
                {
                    DateLabel = new DateTime(currentYear, m, 1).ToString("MMMM").ToUpper(),
                    ProgressValue = avgProg,
                    ProgressText = $"{avgProg}%",
                    FullDate = new DateTime(currentYear, m, 1) // For yearly, clicking a month could go to first day of month or a month view
                });
            }
            HistoryList.ItemsSource = history;
        }

        private void LoadMonthlyCalendar(int year, int month)
        {
            CalendarGrid.Children.Clear();
            var firstDay = new DateTime(year, month, 1);
            int daysInMonth = DateTime.DaysInMonth(year, month);
            int startOffset = ((int)firstDay.DayOfWeek + 1) % 7; // Offset for Sunday start if desired, or adjust for Sat

            // Header for Days of Week
            string[] weekDays = { "S", "M", "T", "W", "T", "F", "S" };
            foreach (var day in weekDays)
            {
                CalendarGrid.Children.Add(new TextBlock 
                { 
                    Text = day, 
                    Foreground = System.Windows.Media.Brushes.White, 
                    Opacity = 0.5, 
                    HorizontalAlignment = System.Windows.HorizontalAlignment.Center,
                    FontSize = 10,
                    FontWeight = FontWeights.Bold,
                    Margin = new Thickness(0,0,0,10)
                });
            }

            // Empty slots for offset
            for (int i = 0; i < startOffset; i++)
            {
                CalendarGrid.Children.Add(new Border());
            }

            // Month Days
            var today = DateTime.Today;
            for (int d = 1; d <= daysInMonth; d++)
            {
                var date = new DateTime(year, month, d);
                bool isToday = date == today;
                bool isPast = date < today;
                bool isFuture = date > today;

                int prog = 0;
                if (isPast || isToday)
                {
                    var deeds = TrackerService.Instance.LoadDay(date);
                    prog = CalculateProgress(deeds);
                }

                // Determine cell background color
                SolidColorBrush cellBg;
                SolidColorBrush cellBorder;
                Thickness cellBorderThickness;

                if (isToday)
                {
                    // Today: solid emerald pill — unmistakable
                    cellBg = new SolidColorBrush((WColor)WColorConverter.ConvertFromString("#34D399"));
                    cellBorder = new SolidColorBrush(WColor.FromArgb(0, 0, 0, 0));
                    cellBorderThickness = new Thickness(0);
                }
                else if (isPast && prog > 50)
                {
                    // Good progress: subtle emerald tint
                    cellBg = new SolidColorBrush(WColor.FromArgb((byte)(prog * 0.6 + 15), 52, 211, 153));
                    cellBorder = new SolidColorBrush(WColor.FromArgb(50, 52, 211, 153));
                    cellBorderThickness = new Thickness(1);
                }
                else if (isPast)
                {
                    // Past with little/no progress: glass
                    cellBg = new SolidColorBrush(WColor.FromArgb(20, 255, 255, 255));
                    cellBorder = new SolidColorBrush(WColor.FromArgb(18, 255, 255, 255));
                    cellBorderThickness = new Thickness(1);
                }
                else
                {
                    // Future: totally transparent — just the number
                    cellBg = new SolidColorBrush(WColor.FromArgb(0, 0, 0, 0));
                    cellBorder = new SolidColorBrush(WColor.FromArgb(0, 0, 0, 0));
                    cellBorderThickness = new Thickness(0);
                }

                var border = new Border
                {
                    CornerRadius = new CornerRadius(10),
                    Background = cellBg,
                    BorderBrush = cellBorder,
                    BorderThickness = cellBorderThickness,
                    Margin = new Thickness(2)
                };

                var numBlock = new TextBlock
                {
                    Text = d.ToString(),
                    FontSize = isToday ? 13 : 11,
                    FontWeight = isToday ? FontWeights.Bold : FontWeights.Normal,
                    Foreground = isToday
                        ? new SolidColorBrush((WColor)WColorConverter.ConvertFromString("#064E3B"))
                        : System.Windows.Media.Brushes.White,
                    Opacity = isFuture ? 0.25 : 1.0,
                    HorizontalAlignment = System.Windows.HorizontalAlignment.Center,
                    VerticalAlignment = VerticalAlignment.Center
                };

                var stack = new StackPanel { VerticalAlignment = VerticalAlignment.Center };
                stack.Children.Add(numBlock);

                // Progress dot for past days with some completion
                if (isPast && prog > 0)
                {
                    var dot = new Ellipse
                    {
                        Width = 4,
                        Height = 4,
                        Fill = prog > 50
                            ? System.Windows.Media.Brushes.White
                            : new SolidColorBrush(WColor.FromArgb(120, 255, 255, 255)),
                        HorizontalAlignment = System.Windows.HorizontalAlignment.Center,
                        Margin = new Thickness(0, 3, 0, 0)
                    };
                    stack.Children.Add(dot);
                }

                border.Child = stack;

                var btn = new System.Windows.Controls.Button
                {
                    Tag = date,
                    Padding = new Thickness(0),
                    BorderThickness = new Thickness(0),
                    Background = System.Windows.Media.Brushes.Transparent,
                    Height = 50,
                    IsEnabled = !isFuture,
                    Content = border,
                    Cursor = isFuture ? System.Windows.Input.Cursors.Arrow : System.Windows.Input.Cursors.Hand
                };

                btn.Template = new ControlTemplate(typeof(System.Windows.Controls.Button))
                {
                    VisualTree = new FrameworkElementFactory(typeof(ContentPresenter))
                };

                btn.Click += HistoryItem_Click;
                CalendarGrid.Children.Add(btn);
            }
        }

        private void HistoryItem_Click(object sender, RoutedEventArgs e)
        {
            if (sender is System.Windows.Controls.Button btn && btn.Tag is DateTime targetDate)
            {
                // Logic based on current view level
                // If we are in the Yearly Tab AND the HistoryList is visible, we clicked a Month.
                if (TrackerTabList.SelectedIndex == 3 && HistoryList.Visibility == Visibility.Visible)
                {
                    // If in Yearly tab, clicking a month shows its calendar
                    HistorySectionTitle.Text = targetDate.ToString("MMMM yyyy").ToUpper();
                    HistoryList.Visibility = Visibility.Collapsed;
                    CalendarGrid.Visibility = Visibility.Visible;
                    TrackerBackButton.Visibility = Visibility.Visible;
                    LoadMonthlyCalendar(targetDate.Year, targetDate.Month);
                }
                else
                {
                    // We clicked a Day in the calendar (or from Monthly/Weekly list). Navigate to daily view.
                    LoadData(_enabledPrayers, targetDate);
                    TrackerTabList.SelectedIndex = 0; // Switching to Daily
                }
            }
        }

        private void TrackerBack_Click(object sender, RoutedEventArgs e)
        {
            if (TrackerTabList.SelectedIndex == 0 && _currentDeeds.Date != DateTime.Today.ToString("yyyy-MM-dd"))
            {
                // If in Daily tab viewing past date, go back to Today
                HashSet<string>? currentEnabled = null;
                if (System.Windows.Application.Current.MainWindow is MainWindow mw)
                {
                    currentEnabled = mw.GetEnabledTrackerPrayers();
                }
                LoadData(currentEnabled, DateTime.Today);
            }
            else
            {
                UpdateViewForTab(); // Resets to current tab default
            }
        }

        private async void CloudSync_Click(object sender, RoutedEventArgs e)
        {
            if (!Services.AuthService.Instance.IsSignedIn)
            {
                var prompt = new Views.AuthPromptWindow();
                prompt.Owner = System.Windows.Window.GetWindow(this);
                var result = prompt.ShowDialog();
                if (result != true) return;
            }

            CloudSyncBtn.IsEnabled = false;
            var originalContent = ((StackPanel)((Button)sender).Content);
            var textBlock = (TextBlock)originalContent.Children[1];
            string originalText = textBlock.Text;
            textBlock.Text = "Syncing...";

            try
            {
                await Services.CloudSyncService.Instance.SyncAllAsync();
                textBlock.Text = "Done!";
                System.Windows.MessageBox.Show(
                    "Current month data synced to cloud successfully!",
                    "Cloud Sync",
                    MessageBoxButton.OK,
                    MessageBoxImage.Information);
            }
            catch (Exception ex)
            {
                textBlock.Text = "Failed";
                System.Windows.MessageBox.Show(
                    $"Sync failed: {ex.Message}",
                    "Error",
                    MessageBoxButton.OK,
                    MessageBoxImage.Error);
            }
            finally
            {
                await System.Threading.Tasks.Task.Delay(1500);
                textBlock.Text = originalText;
                CloudSyncBtn.IsEnabled = true;
            }
        }

        private int CalculateProgress(DailyDeeds deeds)
        {
            int total = 0;
            int checkedCount = 0;
            foreach (var p in deeds.Prayers.Values)
            {
                var fardDeed = p.FirstOrDefault(d => d.Type == DeedType.Fard);
                if (fardDeed != null)
                {
                    total++;
                    if (fardDeed.IsChecked) checkedCount++;
                }
                else
                {
                    foreach (var d in p)
                    {
                        total++;
                        if (d.IsChecked || (d.Type == DeedType.Nafl && d.Value > 0)) checkedCount++;
                    }
                }
            }
            return total == 0 ? 0 : (checkedCount * 100) / total;
        }

    }

    public class HistoryItem
    {
        public string DateLabel { get; set; } = "";
        public double ProgressValue { get; set; } = 0;
        public string ProgressText { get; set; } = "";
        public DateTime FullDate { get; set; }
    }

    public class PrayerTrackItem : System.ComponentModel.INotifyPropertyChanged
    {
        public event System.ComponentModel.PropertyChangedEventHandler? PropertyChanged;

        public void Refresh()
        {
            PropertyChanged?.Invoke(this, new System.ComponentModel.PropertyChangedEventArgs(nameof(CompletionText)));
        }
        public string PrayerName { get; set; } = string.Empty;
        public bool IsEnabled { get; set; } = true;
        public List<DeedEntry> Deeds { get; set; } = new();
        public string CompletionText 
        {
            get
            {
                var fardDeed = Deeds.FirstOrDefault(d => d.Type == DeedType.Fard);
                if (fardDeed != null)
                {
                    return fardDeed.IsChecked ? "1/1 Done" : "0/1 Done";
                }
                return $"{Deeds.Count(d => d.IsChecked)}/{Deeds.Count} Done";
            }
        }
    }
}

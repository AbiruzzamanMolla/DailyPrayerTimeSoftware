using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Linq;
using DailyPrayerTime.Native.Services;

namespace DailyPrayerTime.Native
{
    public class PrepCheckItem
    {
        public string Key { get; set; } = "";
        public string Label { get; set; } = "";
        public bool IsChecked { get; set; }
    }

    public class QadrNightItem
    {
        public int Night { get; set; }
        public string Display { get; set; } = "";
        public bool IsMarked { get; set; }
    }

    public partial class RamadanView : System.Windows.Controls.UserControl
    {
        private RamadanState _state = new();
        private bool _isRamadan;
        private int _ramadanDay = -1;
        private bool _eidTakbeerEnabled;
        private ObservableCollection<PrepCheckItem> _prepItems = new();
        private ObservableCollection<QadrNightItem> _qadrItems = new();

        public RamadanView()
        {
            InitializeComponent();
            PrepChecklistControl.ItemsSource = _prepItems;
            QadrNightsControl.ItemsSource = _qadrItems;
        }

        public void LoadData()
        {
            _state = RamadanService.Instance.LoadState();
            var today = DateTime.Today;
            _ramadanDay = RamadanData.GetCurrentRamadanDay(today);
            _isRamadan = _ramadanDay > 0;
            _eidTakbeerEnabled = SettingsManager.Current.EidTakbeerEnabled;

            UpdateStatus();
            UpdateDua();
            UpdatePrepChecklist();
            UpdateGoal();
            UpdateQadrTracker();
            UpdateEidSection();
        }

        private void UpdateStatus()
        {
            if (_isRamadan)
            {
                StatusTitleText.Text = string.Format(LocalizationManager.Instance.GetString("Ramadan_DayStatus"), _ramadanDay);
                StatusSubText.Text = LocalizationManager.Instance.GetString("Ramadan_DayActive");
                RamadanProgressBar.Value = _ramadanDay;
                ProgressLabel.Text = $"{_ramadanDay}/30";
            }
            else
            {
                var eidDate = RamadanData.GetEidDate(DateTime.Today);
                if (eidDate.HasValue)
                {
                    var daysUntil = (eidDate.Value - DateTime.Today.Date).Days;
                    StatusTitleText.Text = string.Format(LocalizationManager.Instance.GetString("Ramadan_DaysUntil"), daysUntil);
                    StatusSubText.Text = LocalizationManager.Instance.GetString("Ramadan_NotRamadan");
                }
                else
                {
                    StatusTitleText.Text = LocalizationManager.Instance.GetString("Ramadan_NotRamadan");
                    StatusSubText.Text = "";
                }
                RamadanProgressBar.Value = 0;
                ProgressLabel.Text = "--";
            }
        }

        private void UpdateDua()
        {
            int day = _isRamadan ? _ramadanDay : DateTime.Today.Day % 10 + 1;
            var dua = RamadanData.GetDuaForDay(day);
            DuaArabicText.Text = dua.Arabic;
            DuaTransliterationText.Text = dua.Transliteration;
            DuaTranslationText.Text = dua.Translation;

            if (_isRamadan)
                DuaDayLabel.Text = string.Format(LocalizationManager.Instance.GetString("Ramadan_DayLabel"), day);
            else
                DuaDayLabel.Text = string.Format(LocalizationManager.Instance.GetString("Ramadan_DuaGeneral"));
        }

        private void UpdatePrepChecklist()
        {
            if (_isRamadan)
            {
                PrepCard.Visibility = System.Windows.Visibility.Collapsed;
                return;
            }

            PrepCard.Visibility = System.Windows.Visibility.Visible;
            _prepItems.Clear();

            foreach (var key in RamadanData.PrepChecklistItems)
            {
                _prepItems.Add(new PrepCheckItem
                {
                    Key = key,
                    Label = LocalizationManager.Instance.GetString(key),
                    IsChecked = _state.PrepChecklist.GetValueOrDefault(key, false)
                });
            }
        }

        private void UpdateGoal()
        {
            if (!_isRamadan)
            {
                GoalCard.Visibility = System.Windows.Visibility.Collapsed;
                return;
            }

            GoalCard.Visibility = System.Windows.Visibility.Visible;

            if (_state.DailyGoals.TryGetValue(_ramadanDay, out var goal))
            {
                GoalInput.Text = goal;
                GoalTodayText.Text = goal;

                if (_state.DailyGoalComplete.TryGetValue(_ramadanDay, out var done) && done)
                {
                    GoalCheckBox.Background = new System.Windows.Media.SolidColorBrush(System.Windows.Media.Color.FromArgb((byte)255, (byte)16, (byte)185, (byte)129));
                    GoalCheckBox.BorderBrush = System.Windows.Media.Brushes.Transparent;
                    GoalTodayText.Opacity = 0.4;
                    GoalTodayText.TextDecorations = System.Windows.TextDecorations.Strikethrough;
                }
                else
                {
                    GoalCheckBox.Background = System.Windows.Media.Brushes.Transparent;
                    GoalCheckBox.BorderBrush = new System.Windows.Media.SolidColorBrush(System.Windows.Media.Color.FromArgb((byte)175, (byte)255, (byte)255, (byte)255));
                    GoalTodayText.Opacity = 0.7;
                    GoalTodayText.TextDecorations = null;
                }
            }
            else
            {
                GoalInput.Text = "";
                GoalTodayText.Text = LocalizationManager.Instance.GetString("Ramadan_GoalNone");
                GoalCheckBox.Background = System.Windows.Media.Brushes.Transparent;
                GoalCheckBox.BorderBrush = new System.Windows.Media.SolidColorBrush(System.Windows.Media.Color.FromArgb((byte)175, (byte)255, (byte)255, (byte)255));
            }

            BuildGoalHistory();
        }

        private void BuildGoalHistory()
        {
            GoalHistoryPanel.Children.Clear();

            var header = new System.Windows.Controls.TextBlock
            {
                Text = LocalizationManager.Instance.GetString("Ramadan_GoalHistory"),
                Foreground = System.Windows.Media.Brushes.White,
                Opacity = 0.5,
                FontSize = 11,
                Margin = new System.Windows.Thickness(0.0, 5.0, 0.0, 5.0)
            };
            GoalHistoryPanel.Children.Add(header);

            for (int d = _ramadanDay - 1; d >= Math.Max(1, _ramadanDay - 7); d--)
            {
                if (!_state.DailyGoals.TryGetValue(d, out var g)) continue;

                bool done = _state.DailyGoalComplete.GetValueOrDefault(d, false);
                var item = new System.Windows.Controls.Border
                {
                    Background = new System.Windows.Media.SolidColorBrush(System.Windows.Media.Color.FromArgb((byte)26, (byte)255, (byte)255, (byte)255)),
                    CornerRadius = new System.Windows.CornerRadius(6.0),
                    Padding = new System.Windows.Thickness(10.0, 6.0, 10.0, 6.0),
                    Margin = new System.Windows.Thickness(0.0, 0.0, 0.0, 4.0)
                };
                var grid = new System.Windows.Controls.Grid();
                grid.ColumnDefinitions.Add(new System.Windows.Controls.ColumnDefinition { Width = System.Windows.GridLength.Auto });
                grid.ColumnDefinitions.Add(new System.Windows.Controls.ColumnDefinition { Width = new System.Windows.GridLength(1.0, System.Windows.GridUnitType.Star) });

                var dayLabel = new System.Windows.Controls.TextBlock
                {
                    Text = $"Day {d}:",
                    Foreground = System.Windows.Media.Brushes.White,
                    Opacity = 0.4,
                    FontSize = 10,
                    Margin = new System.Windows.Thickness(0.0, 0.0, 8.0, 0.0),
                    VerticalAlignment = System.Windows.VerticalAlignment.Center
                };
                var goalLabel = new System.Windows.Controls.TextBlock
                {
                    Text = g,
                    Foreground = System.Windows.Media.Brushes.White,
                    Opacity = done ? 0.4 : 0.7,
                    FontSize = 11,
                    TextDecorations = done ? System.Windows.TextDecorations.Strikethrough : null
                };
                grid.Children.Add(dayLabel);
                grid.Children.Add(goalLabel);
                System.Windows.Controls.Grid.SetColumn(goalLabel, 1);
                item.Child = grid;
                GoalHistoryPanel.Children.Add(item);
            }
        }

        private void UpdateQadrTracker()
        {
            if (!_isRamadan || _ramadanDay < 20)
            {
                QadrCard.Visibility = System.Windows.Visibility.Collapsed;
                return;
            }

            QadrCard.Visibility = System.Windows.Visibility.Visible;
            _qadrItems.Clear();

            for (int night = 21; night <= 30; night++)
            {
                bool marked = _state.LaylatulQadrNights.GetValueOrDefault(night, false);
                string status = marked ? "\u2705" : "\u2610";
                var date = CalculateNightDate(night);
                string dateStr = date.HasValue ? date.Value.ToString("MMM d") : "";
                _qadrItems.Add(new QadrNightItem
                {
                    Night = night,
                    Display = $"{status}  {LocalizationManager.Instance.GetString("Ramadan_QadrNight")} {night}  ({dateStr})",
                    IsMarked = marked
                });
            }
        }

        private DateTime? CalculateNightDate(int ramadanNight)
        {
            try
            {
                var umAlQura = new System.Globalization.UmAlQuraCalendar();
                var today = DateTime.Today;
                if (_ramadanDay > 0)
                {
                    int diff = ramadanNight - _ramadanDay;
                    return today.AddDays(diff);
                }
                return null;
            }
            catch
            {
                return null;
            }
        }

        private void UpdateEidSection()
        {
            var eidDate = RamadanData.GetEidDate(DateTime.Today);
            if (eidDate.HasValue)
            {
                int diff = (eidDate.Value - DateTime.Today.Date).Days;
                if (diff == 0)
                    EidDateText.Text = LocalizationManager.Instance.GetString("Ramadan_EidToday");
                else if (diff == 1)
                    EidDateText.Text = LocalizationManager.Instance.GetString("Ramadan_EidTomorrow");
                else if (diff > 0)
                    EidDateText.Text = string.Format(LocalizationManager.Instance.GetString("Ramadan_EidCountdown"), diff);
                else
                    EidDateText.Text = "";
            }
            else
                EidDateText.Text = "";

            UpdateEidToggleVisual();
        }

        private void UpdateEidToggleVisual()
        {
            if (_eidTakbeerEnabled)
            {
                EidToggleBtn.Background = new System.Windows.Media.SolidColorBrush(System.Windows.Media.Color.FromArgb((byte)255, (byte)16, (byte)185, (byte)129));
                EidToggleKnob.Fill = System.Windows.Media.Brushes.White;
                System.Windows.Controls.Panel.SetZIndex(EidToggleKnob, 1);
                EidToggleKnob.Margin = new System.Windows.Thickness(23.0, 0.0, 0.0, 0.0);
            }
            else
            {
                EidToggleBtn.Background = new System.Windows.Media.SolidColorBrush(System.Windows.Media.Color.FromArgb((byte)42, (byte)255, (byte)255, (byte)255));
                EidToggleKnob.Fill = new System.Windows.Media.SolidColorBrush(System.Windows.Media.Color.FromRgb(156, 163, 175));
                EidToggleKnob.Margin = new System.Windows.Thickness(3.0, 0.0, 0.0, 0.0);
            }
        }

        private void ToggleEid_Click(object sender, System.Windows.Input.MouseButtonEventArgs e)
        {
            _eidTakbeerEnabled = !_eidTakbeerEnabled;
            SettingsManager.Current.EidTakbeerEnabled = _eidTakbeerEnabled;
            SettingsManager.Save();
            UpdateEidToggleVisual();
        }

        private void ToggleQadrNight(object sender, System.Windows.Input.MouseButtonEventArgs e)
        {
            if (sender is System.Windows.FrameworkElement fe && fe.DataContext is QadrNightItem item)
            {
                bool newVal = !item.IsMarked;
                item.IsMarked = newVal;
                _state.LaylatulQadrNights[item.Night] = newVal;
                RamadanService.Instance.SaveState(_state);
                UpdateQadrTracker();
            }
        }

        private void SaveGoal_Click(object sender, System.Windows.Input.MouseButtonEventArgs e)
        {
            if (!_isRamadan) return;
            string goal = GoalInput.Text.Trim();
            if (string.IsNullOrEmpty(goal)) return;

            _state.DailyGoals[_ramadanDay] = goal;
            RamadanService.Instance.SaveState(_state);
            UpdateGoal();
        }

        private void PrepChecklistItem_Changed(object sender, System.Windows.RoutedEventArgs e)
        {
            if (sender is System.Windows.Controls.CheckBox cb && cb.DataContext is PrepCheckItem item)
            {
                _state.PrepChecklist[item.Key] = item.IsChecked;
                RamadanService.Instance.SaveState(_state);
            }
        }

        private void ToggleGoal_Click(object sender, System.Windows.Input.MouseButtonEventArgs e)
        {
            if (!_isRamadan) return;
            if (!_state.DailyGoals.ContainsKey(_ramadanDay)) return;

            bool current = _state.DailyGoalComplete.GetValueOrDefault(_ramadanDay, false);
            _state.DailyGoalComplete[_ramadanDay] = !current;
            RamadanService.Instance.SaveState(_state);
            UpdateGoal();
        }
    }
}

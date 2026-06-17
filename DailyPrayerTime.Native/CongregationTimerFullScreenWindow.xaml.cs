using System;
using System.Windows;
using System.Windows.Input;
using System.Windows.Threading;
using System.Windows.Media;

namespace DailyPrayerTime.Native
{
    public partial class CongregationTimerFullScreenWindow : Window
    {
        private DispatcherTimer _timer;
        private DateTime _targetTime;
        private bool _isEscable;

        public CongregationTimerFullScreenWindow(string prayerName, DateTime targetTime, bool isEscable, string prayerRange)
        {
            InitializeComponent();
            FontSizeHelper.AutoScaleOnLoaded(this);
            _targetTime = targetTime;
            _isEscable = isEscable;

            PrayerNameText.Text = string.Format(LocalizationManager.Instance.GetString("Jamaat_Label"), prayerName);
            
            string timeFmt = SettingsManager.Current.TimeFormat == "24h" ? "HH:mm" : "hh:mm tt";
            TargetTimeText.Text = string.Format(LocalizationManager.Instance.GetString("Jamaat_StartsAt"), targetTime.ToString(timeFmt));
            
            // Format time range display
            if (!string.IsNullOrEmpty(prayerRange))
            {
                PrayerRangeText.Text = string.Format(LocalizationManager.Instance.GetString("Established_Range"), prayerRange);
                PrayerRangeText.Visibility = Visibility.Visible;
            }
            else
            {
                PrayerRangeText.Visibility = Visibility.Collapsed;
            }

            // Apply themes
            ApplyTheme();

            // Set up dismissal state
            if (!_isEscable)
            {
                DismissButton.Visibility = Visibility.Collapsed;
                // Force window to stay on top and prevent focus loss
                this.Deactivated += (s, e) => { 
                    try { this.Focus(); this.Activate(); } catch { }
                };
            }
            else
            {
                DismissButton.Visibility = Visibility.Visible;
            }

            // Keyboard Hook / Input Blocking
            this.PreviewKeyDown += Window_PreviewKeyDown;

            _timer = new DispatcherTimer();
            _timer.Interval = TimeSpan.FromSeconds(1);
            _timer.Tick += (s, e) => UpdateDisplay();
            _timer.Start();
            
            UpdateDisplay();
        }

        private void ApplyTheme()
        {
            var s = SettingsManager.Current;
            try {
                GradStop1.Color = (System.Windows.Media.Color)System.Windows.Media.ColorConverter.ConvertFromString(s.GradientStart);
                GradStop2.Color = (System.Windows.Media.Color)System.Windows.Media.ColorConverter.ConvertFromString(s.GradientEnd);
                
                // Color the Dismiss button dynamically to contrast well
                DismissButton.Background = new SolidColorBrush((System.Windows.Media.Color)System.Windows.Media.ColorConverter.ConvertFromString(s.SecondaryColor));
                DismissButton.Foreground = new SolidColorBrush((System.Windows.Media.Color)System.Windows.Media.ColorConverter.ConvertFromString(s.GradientEnd));
            } catch { }
        }

        private void UpdateDisplay()
        {
            TimeSpan remaining = _targetTime - DateTime.Now;
            if (remaining.TotalSeconds <= 0)
            {
                TimerText.Text = "00:00";
                _timer.Stop();
                this.Close();
                return;
            }
            TimerText.Text = $"{remaining.Minutes:D2}:{remaining.Seconds:D2}";
        }

        private void Window_PreviewKeyDown(object sender, KeyEventArgs e)
        {
            // Block all keys if not esc-able
            if (!_isEscable)
            {
                if (e.Key == Key.System && e.SystemKey == Key.F4)
                {
                    e.Handled = true;
                }
                e.Handled = true;
            }
            else
            {
                // Allow dismissing with Esc key if configured
                if (e.Key == Key.Escape)
                {
                    this.Close();
                }
            }
        }

        private void Dismiss_Click(object sender, RoutedEventArgs e)
        {
            if (_isEscable)
            {
                this.Close();
            }
        }
    }
}

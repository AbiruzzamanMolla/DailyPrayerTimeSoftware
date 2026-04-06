using System;
using System.Windows;
using System.Windows.Threading;
using System.Windows.Media;

namespace DailyPrayerTime.Native
{
    public partial class CongregationTimerPopup : Window
    {
        private DispatcherTimer _timer;
        private DateTime _targetTime;

        public CongregationTimerPopup(string prayerName, DateTime targetTime)
        {
            InitializeComponent();
            _targetTime = targetTime;
            PrayerNameText.Text = $"{prayerName} Congregation";
            
            string timeFmt = SettingsManager.Current.TimeFormat == "24h" ? "HH:mm" : "hh:mm tt";
            TargetTimeText.Text = $"Starts at {targetTime.ToString(timeFmt)}";
            
            ApplyTheme();
            
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
            } catch { }
        }

        private void UpdateDisplay()
        {
            TimeSpan remaining = _targetTime - DateTime.Now;
            if (remaining.TotalSeconds <= 0)
            {
                TimerText.Text = "00:00";
                _timer.Stop();
                return;
            }
            TimerText.Text = $"{remaining.Minutes:D2}:{remaining.Seconds:D2}";
        }

        private void Dismiss_Click(object sender, RoutedEventArgs e)
        {
            this.Close();
        }
    }
}

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
        private DateTime _endTime;

        public CongregationTimerPopup(string prayerName, DateTime targetTime)
            : this(prayerName, targetTime, targetTime)
        {
        }

        public CongregationTimerPopup(string prayerName, DateTime targetTime, DateTime endTime)
        {
            InitializeComponent();
            FontSizeHelper.AutoScaleOnLoaded(this);
            _targetTime = targetTime;
            _endTime = endTime;
            bool isSuhur = prayerName == LocalizationManager.Instance.GetString("Label_SuhurEnds") || prayerName == "Suhur Ends" || prayerName.Contains("Suhur") || prayerName.Contains("Sehri");
            bool isIftar = prayerName == LocalizationManager.Instance.GetString("Label_IftarBegins") || prayerName == "Iftar Begins" || prayerName.Contains("Iftar");

            if (isSuhur || isIftar)
            {
                PrayerNameText.Text = prayerName;
            }
            else
            {
                PrayerNameText.Text = string.Format(LocalizationManager.Instance.GetString("Jamaat_Label"), prayerName);
            }
            
            string timeFmt = SettingsManager.Current.TimeFormat == "24h" ? "HH:mm" : "hh:mm tt";
            if (isSuhur)
            {
                TargetTimeText.Text = string.Format(LocalizationManager.Instance.GetString("Label_EndsAt") ?? "Ends at {0}", targetTime.ToString(timeFmt));
            }
            else if (isIftar)
            {
                TargetTimeText.Text = string.Format(LocalizationManager.Instance.GetString("Label_BeginsAt") ?? "Begins at {0}", targetTime.ToString(timeFmt));
            }
            else
            {
                TargetTimeText.Text = string.Format(LocalizationManager.Instance.GetString("Jamaat_StartsAt"), targetTime.ToString(timeFmt));
            }
            
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
            DateTime now = DateTime.Now;
            TimeSpan remaining = _targetTime - now;
            if (remaining.TotalSeconds <= 0)
            {
                TimerText.Text = "00:00";
            }
            else
            {
                TimerText.Text = $"{remaining.Minutes:D2}:{remaining.Seconds:D2}";
            }

            if (now >= _endTime)
            {
                _timer.Stop();
                this.Close();
            }
        }

        private void Dismiss_Click(object sender, RoutedEventArgs e)
        {
            this.Close();
        }
    }
}

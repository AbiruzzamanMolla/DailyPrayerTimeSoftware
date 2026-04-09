using System;
using System.IO;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Media;
using System.Windows.Threading;
using Newtonsoft.Json;

namespace DailyPrayerTime.DeskBand
{
    public partial class PrayerTimerControl : UserControl
    {
        private DispatcherTimer _timer;
        private readonly string _dataPath;
        private DeskBandData _lastData;

        public PrayerTimerControl()
        {
            InitializeComponent();
            
            _dataPath = Path.Combine(
                Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData), 
                "DailyPrayerTimeNative", 
                "deskband_data.json"
            );

            _timer = new DispatcherTimer();
            _timer.Interval = TimeSpan.FromSeconds(1);
            _timer.Tick += Timer_Tick;
            _timer.Start();

            // Initial load
            UpdateUI();
        }

        private void Timer_Tick(object sender, EventArgs e)
        {
            UpdateUI();
        }

        private void UpdateUI()
        {
            try
            {
                if (!File.Exists(_dataPath))
                {
                    NameText.Text = "App not running";
                    CountdownText.Text = "--:--:--";
                    return;
                }

                string json = File.ReadAllText(_dataPath);
                var data = JsonConvert.DeserializeObject<DeskBandData>(json);

                if (data == null) return;

                // Only update if data changed or every second for countdown
                NameText.Text = data.Label;
                CountdownText.Text = data.Countdown;

                // Colors
                try
                {
                    var color = (Color)ColorConverter.ConvertFromString(data.PrimaryColor);
                    CountdownText.Foreground = new SolidColorBrush(color);
                    AccentBar.Background = new SolidColorBrush(color);
                }
                catch { /* Ignore invalid color */ }

                // Theme/Background adjustment
                if (data.IsNight)
                {
                    BackgroundBorder.Background = new SolidColorBrush(Color.FromArgb(40, 0, 0, 0));
                }
                else
                {
                    BackgroundBorder.Background = new SolidColorBrush(Color.FromArgb(20, 255, 255, 255));
                }

                _lastData = data;
            }
            catch (Exception)
            {
                // Silently ignore file access issues (likely being written to)
            }
        }
    }
}

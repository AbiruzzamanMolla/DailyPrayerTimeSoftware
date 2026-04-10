using System;
using System.IO;
using System.Windows;
using System.Windows.Media;

namespace DailyPrayerTime.Native
{
    public partial class AdhanNotificationWindow : Window
    {
        private MediaPlayer _player = new MediaPlayer();
        
        public double Volume
        {
            get => _player.Volume;
            set => _player.Volume = value;
        }

        public AdhanNotificationWindow(string prayerName, string timeRange, string jamaatTime, string soundPath)
        {
            InitializeComponent();
            
            PrayerTitleText.Text = string.Format(LocalizationManager.Instance.GetString("Adhan_Title"), prayerName);
            PrayerTimeRangeText.Text = timeRange;
            JamaatTimeText.Text = string.Format(LocalizationManager.Instance.GetString("Adhan_Jamaat"), jamaatTime);

            // Center at the top
            this.Left = (SystemParameters.PrimaryScreenWidth - this.Width) / 2;
            this.Top = 20;

            if (!string.IsNullOrEmpty(soundPath) && File.Exists(soundPath))
            {
                try
                {
                    _player.Open(new Uri(soundPath));
                    _player.Play();
                }
                catch (Exception ex)
                {
                    System.Diagnostics.Debug.WriteLine($"Popup Adhan play failed: {ex.Message}");
                }
            }
        }

        private void Close_Click(object sender, RoutedEventArgs e)
        {
            _player.Stop();
            _player.Close();
            this.Close();
        }

        private void Mute_Click(object sender, RoutedEventArgs e)
        {
            _player.Stop();
            MuteBtn.IsEnabled = false;
            MuteBtn.Opacity = 0.5;
        }

        protected override void OnClosed(EventArgs e)
        {
            _player.Stop();
            _player.Close();
            base.OnClosed(e);
        }
    }
}

using System;
using System.IO;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Media;

namespace DailyPrayerTime.Native
{
    public partial class AdhanNotificationWindow : Window
    {
        private MediaPlayer _player = new MediaPlayer();
        private bool _isDuaPlaying = false;
        private bool _isClosed = false;
        
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

            bool isFajr = (prayerName?.IndexOf("Fajr", StringComparison.OrdinalIgnoreCase) >= 0) ||
                          (prayerName?.IndexOf("ফজর", StringComparison.OrdinalIgnoreCase) >= 0) ||
                          (soundPath?.IndexOf("fajr", StringComparison.OrdinalIgnoreCase) >= 0);
            
            if (isFajr)
            {
                FajrRowBorder.Visibility = Visibility.Visible;
                FajrRowCol0.Visibility = Visibility.Visible;
                FajrRowCol1.Visibility = Visibility.Visible;
                FajrRowCol2.Visibility = Visibility.Visible;
            }

            if (!string.IsNullOrEmpty(soundPath) && File.Exists(soundPath))
            {
                try
                {
                    _player.MediaEnded += OnAdhanEnded;
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
            // Only close the popup — do NOT stop audio.
            // Audio continues playing; only Mute can stop it.
            _isClosed = true;
            this.Close();
        }

        private void Mute_Click(object sender, RoutedEventArgs e)
        {
            _player.Stop();
            MuteBtn.IsEnabled = false;
            MuteBtn.Opacity = 0.5;
        }

        private async void OnAdhanEnded(object? sender, EventArgs e)
        {
            if (_isDuaPlaying || _isClosed)
                return;

            _isDuaPlaying = true;
            await Task.Delay(3000);

            if (_isClosed)
                return;

            AdhanReplyContent.Visibility = Visibility.Collapsed;
            DuaContent.Visibility = Visibility.Visible;

            string duaPath = Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "Assets", "dua_after_adhan.wav");
            if (File.Exists(duaPath))
            {
                try
                {
                    _player.Open(new Uri(duaPath));
                    _player.Play();
                }
                catch (Exception ex)
                {
                    System.Diagnostics.Debug.WriteLine($"Dua play failed: {ex.Message}");
                }
            }
        }

        protected override void OnClosed(EventArgs e)
        {
            _isClosed = true;
            _player.MediaEnded -= OnAdhanEnded;
            // Do NOT stop the player here — Close button should not affect audio.
            // Only Mute_Click stops audio explicitly. OnClosed can be reached from
            // either the Close button (audio should keep going) or system close (let it fade).
            base.OnClosed(e);
        }
    }
}

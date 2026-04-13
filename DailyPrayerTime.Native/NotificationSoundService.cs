using System;
using System.IO;
using System.Windows.Media;
using System.Threading.Tasks;
using System.Collections.Generic;
using Batoulapps.Adhan;

namespace DailyPrayerTime.Native
{
    public static class NotificationSoundService
    {
        private static MediaPlayer _player = new MediaPlayer();
        private static MediaPlayer _instantPlayer = new MediaPlayer();
        private static Queue<string> _soundQueue = new Queue<string>();
        private static bool _isPlaying = false;
        private static readonly object _lock = new object();

        static NotificationSoundService()
        {
            _player.MediaEnded += (s, e) => {
                _player.Close();
                ProcessQueueAfterDelay();
            };
        }

        public static void PlayPrayerSound(Prayer prayer, string type)
        {
            if (!SettingsManager.Current.PrayerSoundEnabled) return;

            string prayerName = prayer.ToString().ToLower();
            // Handle naming differences (Fajar vs Fajr)
            if (prayerName == "fajr") prayerName = "fajr"; 
            
            QueueSound(prayerName, type);
        }

        public static void PlayTahajjudSound()
        {
            if (!SettingsManager.Current.PrayerSoundEnabled) return;
            QueueSound("tahajjud", "start");
        }

        private static void QueueSound(string prayer, string type)
        {
            string lang = SettingsManager.Current.PrayerSoundLanguage;
            string fileName = $"{prayer}_{type}.wav";
            string path = Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "Assets", "prayer_notificaitons", lang, fileName);

            if (!File.Exists(path))
            {
                System.Diagnostics.Debug.WriteLine($"Sound file not found: {path}");
                return;
            }

            lock (_lock)
            {
                _soundQueue.Enqueue(path);
                if (!_isPlaying)
                {
                    _isPlaying = true;
                    PlayNext();
                }
            }
        }

        private static void PlayNext()
        {
            string? path = null;
            lock (_lock)
            {
                if (_soundQueue.Count > 0)
                {
                    path = _soundQueue.Dequeue();
                }
                else
                {
                    _isPlaying = false;
                }
            }

            if (path != null)
            {
                try
                {
                    _player.Open(new Uri(path));
                    _player.Play();
                }
                catch (Exception ex)
                {
                    System.Diagnostics.Debug.WriteLine($"Error playing notification sound: {ex.Message}");
                    _isPlaying = false;
                }
            }
        }

        private static async void ProcessQueueAfterDelay()
        {
            // Delay for simultaneous sounds to avoid overlap and clutter
            // User suggested 20-40 seconds. 30 seconds is a good middle ground.
            await Task.Delay(TimeSpan.FromSeconds(30));
            PlayNext();
        }

        public static void PlayRandomTestSound(string lang)
        {
            try
            {
                string path = Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "Assets", "prayer_notificaitons", lang);
                if (Directory.Exists(path))
                {
                    var files = Directory.GetFiles(path, "*.wav");
                    if (files.Length > 0)
                    {
                        var rand = new Random();
                        string randomFile = files[rand.Next(files.Length)];
                        
                        // Use the instant player to bypass the 30s notification queue delay
                        _instantPlayer.Open(new Uri(randomFile));
                        _instantPlayer.Play();
                    }
                }
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"Error playing random test sound: {ex.Message}");
            }
        }
    }
}

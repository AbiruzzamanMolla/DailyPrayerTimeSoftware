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
            if (!SettingsManager.Current.PrayerSoundEnabled)
            {
                AudioLogger.Log($"PlayPrayerSound skipped: prayer sound disabled (prayer={prayer}, type={type})");
                return;
            }

            string prayerName = prayer.ToString().ToLower();
            if (prayerName == "sunrise" || prayerName == "none")
            {
                AudioLogger.Log($"PlayPrayerSound skipped: invalid prayer name (prayer={prayer})");
                return;
            }

            AudioLogger.Log($"PlayPrayerSound queuing: prayer={prayerName}, type={type}");
            QueueSound(prayerName, type);
        }

        public static void PlayTahajjudSound()
        {
            if (!SettingsManager.Current.PrayerSoundEnabled)
            {
                AudioLogger.Log("PlayTahajjudSound skipped: prayer sound disabled");
                return;
            }
            AudioLogger.Log("PlayTahajjudSound queuing: tahajjud start");
            QueueSound("tahajjud", "start");
        }

        private static void QueueSound(string prayer, string type)
        {
            string lang = SettingsManager.Current.PrayerSoundLanguage;
            string fileName = $"{prayer}_{type}.wav";
            string path = Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "Assets", "prayer_notificaitons", lang, fileName);

            if (!File.Exists(path))
            {
                AudioLogger.Log($"QueueSound file not found: {path}");
                System.Diagnostics.Debug.WriteLine($"Sound file not found: {path}");
                return;
            }

            AudioLogger.Log($"QueueSound enqueued: {path}");
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
                    AudioLogger.Log($"PlayNext started: {path}");
                }
                catch (Exception ex)
                {
                    AudioLogger.Log($"PlayNext error: {path} - {ex.Message}");
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
                string dir = Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "Assets", "prayer_notificaitons", lang);
                if (Directory.Exists(dir))
                {
                    var files = Directory.GetFiles(dir, "*.wav");
                    if (files.Length > 0)
                    {
                        var rand = new Random();
                        string randomFile = files[rand.Next(files.Length)];

                        // Use the instant player to bypass the 30s notification queue delay
                        _instantPlayer.Open(new Uri(randomFile));
                        _instantPlayer.Play();
                        AudioLogger.Log($"PlayRandomTestSound started: {randomFile}");
                    }
                    else
                    {
                        AudioLogger.Log($"PlayRandomTestSound no .wav files in: {dir}");
                    }
                }
                else
                {
                    AudioLogger.Log($"PlayRandomTestSound directory not found: {dir}");
                }
            }
            catch (Exception ex)
            {
                AudioLogger.Log($"PlayRandomTestSound error: {ex.Message}");
                System.Diagnostics.Debug.WriteLine($"Error playing random test sound: {ex.Message}");
            }
        }
    }
}

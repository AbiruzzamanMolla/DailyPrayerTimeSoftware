using System;
using System.IO;

namespace DailyPrayerTime.Native
{
    public static class AudioLogger
    {
        private static readonly string LogFile;
        private static readonly object _lock = new object();

        static AudioLogger()
        {
            string appData = StorageService.GetAppDataPath();
            LogFile = Path.Combine(appData, "audio_log.txt");
        }

        public static void Log(string message)
        {
            try
            {
                lock (_lock)
                {
                    string line = $"[{DateTime.Now:yyyy-MM-dd HH:mm:ss}] {message}";
                    File.AppendAllText(LogFile, line + Environment.NewLine);
                }
            }
            catch
            {
                // Silently fail if we can't write to the log
            }
        }
    }
}

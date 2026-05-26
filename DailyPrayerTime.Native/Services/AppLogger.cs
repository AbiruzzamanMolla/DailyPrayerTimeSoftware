using System;
using System.IO;

namespace DailyPrayerTime.Native
{
    public static class AppLogger
    {
        private static readonly string LogFile;
        private static readonly object _lock = new object();

        static AppLogger()
        {
            string appData = StorageService.GetAppDataPath();
            LogFile = Path.Combine(appData, "app_log.txt");
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

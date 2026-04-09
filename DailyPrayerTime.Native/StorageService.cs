using System;
using System.IO;

namespace DailyPrayerTime.Native
{
    public static class StorageService
    {
        private static string? _appDataPath;
        private const string PortableFlagFile = ".portable";
        private const string AppFolderName = "DailyPrayerTimeNative";

        public static string GetAppDataPath()
        {
            if (_appDataPath != null) return _appDataPath;

            string baseDir = AppDomain.CurrentDomain.BaseDirectory;
            string portablePath = Path.Combine(baseDir, PortableFlagFile);

            if (File.Exists(portablePath))
            {
                // Portable mode: Store data in a 'data' subfolder in the executable directory
                _appDataPath = Path.Combine(baseDir, "data");
            }
            else
            {
                // Standard mode: Store data in %APPDATA%
                _appDataPath = Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData), AppFolderName);
            }

            if (!Directory.Exists(_appDataPath))
            {
                Directory.CreateDirectory(_appDataPath);
            }

            return _appDataPath;
        }

        public static bool IsPortable()
        {
            return File.Exists(Path.Combine(AppDomain.CurrentDomain.BaseDirectory, PortableFlagFile));
        }
    }
}

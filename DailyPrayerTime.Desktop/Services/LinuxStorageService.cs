using System;
using System.IO;
using DailyPrayerTime.Shared.Services;

namespace DailyPrayerTime.Desktop.Services
{
    public class LinuxStorageService : IStorageService
    {
        private string? _cachedPath;

        public string GetAppDataPath()
        {
            if (_cachedPath != null) return _cachedPath;

            string baseDir = Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData);
            if (string.IsNullOrEmpty(baseDir))
                baseDir = Path.Combine(
                    Environment.GetEnvironmentVariable("HOME") ?? ".",
                    ".config"
                );

            _cachedPath = Path.Combine(baseDir, "DailyPrayerTimer");
            if (!Directory.Exists(_cachedPath))
                Directory.CreateDirectory(_cachedPath);

            return _cachedPath;
        }
    }
}

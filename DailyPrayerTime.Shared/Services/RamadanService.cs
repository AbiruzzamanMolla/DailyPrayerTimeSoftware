using System;
using System.Collections.Generic;
using System.IO;
using Newtonsoft.Json;

namespace DailyPrayerTime.Shared.Services
{
    public class DuaItem
    {
        public string Arabic { get; set; } = "";
        public string Transliteration { get; set; } = "";
        public string Translation { get; set; } = "";
    }

    public class RamadanState
    {
        public int Year { get; set; }
        public Dictionary<string, bool> PrepChecklist { get; set; } = new();
        public Dictionary<int, string> DailyGoals { get; set; } = new();
        public Dictionary<int, bool> DailyGoalComplete { get; set; } = new();
        public Dictionary<int, bool> LaylatulQadrNights { get; set; } = new();
    }

    public static class RamadanData
    {
        public static List<string> PrepChecklistItems => new()
        {
            "Make sincere intention for Ramadan",
            "Set a Qur'an completion goal",
            "Adjust sleep & meal schedule",
            "Plan charity for the month",
            "Learn Ramadan-specific duas",
            "Stock up on prayer & fasting supplies",
            "Prepare family for Ramadan routine"
        };

        public static int GetCurrentRamadanDay(DateTime gregorianDate)
        {
            return HijriDateHelper.GetRamadanDay(gregorianDate);
        }

        public static bool IsEid(DateTime gregorianDate)
        {
            return HijriDateHelper.IsEid(gregorianDate);
        }

        public static DateTime? GetEidDate(DateTime gregorianDate)
        {
            var (year, month, _) = HijriDateHelper.ToHijri(gregorianDate);
            int eidYear = month <= 9 ? year : year + 1;
            var test = new DateTime(gregorianDate.Year, gregorianDate.Month, 1);
            for (int d = 1; d <= 30; d++)
            {
                var dt = new DateTime(gregorianDate.Year, gregorianDate.Month, d);
                if (HijriDateHelper.IsEid(dt)) return dt;
            }
            return null;
        }
    }

    public class RamadanService
    {
        private static readonly Lazy<RamadanService> _instance = new(() => new());
        public static RamadanService Instance => _instance.Value;

        public string BasePath { get; set; } = "";

        private string GetFilePath()
        {
            return string.IsNullOrEmpty(BasePath) ? "" : Path.Combine(BasePath, "ramadan_state.json");
        }

        public RamadanState LoadState()
        {
            var path = GetFilePath();
            if (string.IsNullOrEmpty(path) || !File.Exists(path))
                return new RamadanState { Year = DateTime.Today.Year };

            try
            {
                var json = File.ReadAllText(path);
                return JsonConvert.DeserializeObject<RamadanState>(json)
                    ?? new RamadanState { Year = DateTime.Today.Year };
            }
            catch
            {
                return new RamadanState { Year = DateTime.Today.Year };
            }
        }

        public void SaveState(RamadanState state)
        {
            var path = GetFilePath();
            if (string.IsNullOrEmpty(path)) return;
            var dir = Path.GetDirectoryName(path);
            if (!string.IsNullOrEmpty(dir) && !Directory.Exists(dir))
                Directory.CreateDirectory(dir);
            state.Year = DateTime.Today.Year;
            File.WriteAllText(path, JsonConvert.SerializeObject(state, Formatting.Indented));
        }
    }
}

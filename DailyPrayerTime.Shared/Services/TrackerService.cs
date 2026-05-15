using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using Newtonsoft.Json;
using DailyPrayerTime.Shared.Models;

namespace DailyPrayerTime.Shared.Services
{
    public class TrackerService
    {
        private static TrackerService? _instance;
        public static TrackerService Instance => _instance ??= new TrackerService();

        public string BasePath { get; set; } = "";

        private string TrackerDir => string.IsNullOrEmpty(BasePath) ? "" : Path.Combine(BasePath, "tracker");

        public DailyDeeds LoadDay(DateTime date)
        {
            if (string.IsNullOrEmpty(TrackerDir)) return NewDeeds(date);
            if (!Directory.Exists(TrackerDir)) Directory.CreateDirectory(TrackerDir);

            string path = Path.Combine(TrackerDir, $"{date:yyyy-MM-dd}.json");
            DailyDeeds? deeds = null;

            if (File.Exists(path))
            {
                try
                {
                    string json = File.ReadAllText(path);
                    deeds = JsonConvert.DeserializeObject<DailyDeeds>(json);
                }
                catch { }
            }

            deeds ??= NewDeeds(date);
            EnsureMandatoryPrayers(deeds);
            return deeds;
        }

        public void SaveDay(DailyDeeds deeds)
        {
            if (string.IsNullOrEmpty(deeds.Date) || string.IsNullOrEmpty(TrackerDir)) return;
            if (!Directory.Exists(TrackerDir)) Directory.CreateDirectory(TrackerDir);

            string path = Path.Combine(TrackerDir, $"{deeds.Date}.json");
            try
            {
                File.WriteAllText(path, JsonConvert.SerializeObject(deeds, Formatting.Indented));
            }
            catch { }
        }

        public List<DailyDeeds> GetHistoryRange(DateTime start, DateTime end)
        {
            var history = new List<DailyDeeds>();
            for (var dt = start.Date; dt <= end.Date; dt = dt.AddDays(1))
                history.Add(LoadDay(dt));
            return history;
        }

        public static bool IsSunnahSawmDay(DateTime date) =>
            date.DayOfWeek == DayOfWeek.Monday || date.DayOfWeek == DayOfWeek.Thursday;

        private static DailyDeeds NewDeeds(DateTime date) =>
            new() { Date = date.ToString("yyyy-MM-dd") };

        private void EnsureMandatoryPrayers(DailyDeeds deeds)
        {
            string[] mandatory = { "Fajr", "Dhuhr", "Asr", "Maghrib", "Isha", "Jumuah" };
            foreach (var p in mandatory)
            {
                if (!deeds.Prayers.ContainsKey(p))
                    deeds.Prayers[p] = RakatParser.Parse(GetDefaultNote(p));
            }

            deeds.EnsurePrayer("Tahajjud");
            deeds.EnsurePrayer("Duha");
            deeds.EnsurePrayer("Awwabin");

            if (!deeds.Prayers.ContainsKey("Adhkar"))
                deeds.Prayers["Adhkar"] = new List<DeedEntry>
                {
                    new() { Label = "Morning Adhkar", Type = DeedType.Nafl },
                    new() { Label = "Evening Adhkar", Type = DeedType.Nafl }
                };
        }

        private static string GetDefaultNote(string prayer) => prayer switch
        {
            "Fajr" => "2 Sunnah, 2 Fard (4 Total)",
            "Dhuhr" => "4 Sunnah, 4 Fard, 2 Sunnah, 2 Nafl (12 Total)",
            "Asr" => "4 Sunnah, 4 Fard (8 Total)",
            "Maghrib" => "3 Fard, 2 Sunnah, 2 Nafl (7 Total)",
            "Isha" => "4 Sunnah, 4 Fard, 2 Sunnah, 2 Nafl, 3 Witr, 2 Nafl (17 Total)",
            "Jumuah" => "4 Sunnah, 2 Fard, 4 Sunnah, 2 Sunnah, 2 Nafl (14 Total)",
            _ => ""
        };
    }
}

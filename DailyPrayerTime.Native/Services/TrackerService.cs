using System;
using System.Collections.Generic;
using System.IO;
using System.IO.Compression;
using System.Linq;
using Newtonsoft.Json;
using DailyPrayerTime.Native.Models;

namespace DailyPrayerTime.Native.Services
{
    public class TrackerService
    {
        private static TrackerService? _instance;
        public static TrackerService Instance => _instance ??= new TrackerService();

        private string TrackerDir => Path.Combine(StorageService.GetAppDataPath(), "tracker");

        private TrackerService()
        {
            if (!Directory.Exists(TrackerDir))
            {
                Directory.CreateDirectory(TrackerDir);
            }
        }

        public DailyDeeds LoadDay(DateTime date)
        {
            string fileName = $"{date:yyyy-MM-dd}.json";
            string path = Path.Combine(TrackerDir, fileName);
            DailyDeeds? deeds = null;

            if (File.Exists(path))
            {
                try
                {
                    string json = File.ReadAllText(path);
                    deeds = JsonConvert.DeserializeObject<DailyDeeds>(json);
                }
                catch { /* Fallback */ }
            }

            if (deeds == null) deeds = new DailyDeeds { Date = date.ToString("yyyy-MM-dd") };
            
            // Ensure mandatory prayers exist with default deeds from localization
            string[] mandatory = { "Fajr", "Dhuhr", "Asr", "Maghrib", "Isha", "Jumuah" };
            foreach (var p in mandatory)
            {
                if (!deeds.Prayers.ContainsKey(p))
                {
                    string note = LocalizationManager.Instance.GetString($"Note_{p}");
                    // Use RakatParser if available, or just empty list if localization fails initially
                    try { deeds.Prayers[p] = RakatParser.Parse(note); } 
                    catch { deeds.Prayers[p] = new List<DeedEntry>(); }
                }
            }

            // Ensure special Nafal tracking
            deeds.EnsurePrayer("Tahajjud");
            deeds.EnsurePrayer("Duha");
            deeds.EnsurePrayer("Awwabin");
            
            // Ensure Adhkar
            if (!deeds.Prayers.ContainsKey("Adhkar"))
            {
                deeds.Prayers["Adhkar"] = new List<DeedEntry>
                {
                    new DeedEntry { Label = "Morning Adhkar", Type = DeedType.Nafl },
                    new DeedEntry { Label = "Evening Adhkar", Type = DeedType.Nafl }
                };
            }
            
            return deeds;
        }

        public void SaveDay(DailyDeeds deeds)
        {
            if (string.IsNullOrEmpty(deeds.Date)) return;
            
            string path = Path.Combine(TrackerDir, $"{deeds.Date}.json");
            try
            {
                string json = JsonConvert.SerializeObject(deeds, Formatting.Indented);
                File.WriteAllText(path, json);
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"Failed to save tracker: {ex.Message}");
            }
        }

        public string? BackupData(string destinationPath)
        {
            try
            {
                if (File.Exists(destinationPath)) File.Delete(destinationPath);
                ZipFile.CreateFromDirectory(TrackerDir, destinationPath);
                return null; // Success
            }
            catch (Exception ex) { return ex.Message; }
        }

        public string? RestoreData(string sourceZipPath)
        {
            try
            {
                if (!File.Exists(sourceZipPath)) return "Source file not found.";
                
                // Clear current tracker data first? User said "Restore", so usually implies replacement.
                foreach (var file in Directory.GetFiles(TrackerDir, "*.json"))
                {
                    File.Delete(file);
                }

                ZipFile.ExtractToDirectory(sourceZipPath, TrackerDir);
                return null; // Success
            }
            catch (Exception ex) { return ex.Message; }
        }

        public List<DailyDeeds> GetHistoryRange(DateTime start, DateTime end)
        {
            var history = new List<DailyDeeds>();
            for (var dt = start.Date; dt <= end.Date; dt = dt.AddDays(1))
            {
                history.Add(LoadDay(dt));
            }
            return history;
        }
        public bool IsSunnahSawmDay(DateTime date)
        {
            // Sunnah: Monday and Thursday
            if (date.DayOfWeek == DayOfWeek.Monday || date.DayOfWeek == DayOfWeek.Thursday) return true;
            return false;
        }
    }
}

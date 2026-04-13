using System;
using System.Collections.Generic;

namespace DailyPrayerTime.Native.Models
{
    public enum DeedType
    {
        Fard,
        Sunnah,
        Nafl,
        Witr,
        Adhkar,
        Custom
    }

    public class DeedEntry
    {
        public string Label { get; set; } = "";
        public int Count { get; set; }
        public bool IsChecked { get; set; }
        public DeedType Type { get; set; }
        public int Value { get; set; } // For counters (e.g., Rakat count for Nafal)
    }

    public class DailyDeeds
    {
        public string Date { get; set; } = ""; // Format: YYYY-MM-DD
        public Dictionary<string, List<DeedEntry>> Prayers { get; set; } = new Dictionary<string, List<DeedEntry>>();
        public bool Sawm { get; set; }
        public List<string> ExtraAmols { get; set; } = new List<string>();

        public void EnsurePrayer(string prayerName)
        {
            if (!Prayers.ContainsKey(prayerName))
            {
                Prayers[prayerName] = new List<DeedEntry>();
            }
        }
    }
}

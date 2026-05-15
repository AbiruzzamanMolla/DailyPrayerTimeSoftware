using System.Collections.Generic;

namespace DailyPrayerTime.Shared.Models
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
        public int Value { get; set; }

        public bool IsEnabled { get; set; } = true;
    }

    public class DailyDeeds
    {
        public string Date { get; set; } = "";
        public Dictionary<string, List<DeedEntry>> Prayers { get; set; } = new();
        public bool Sawm { get; set; }
        public List<string> ExtraAmols { get; set; } = new();

        public void EnsurePrayer(string prayerName)
        {
            if (!Prayers.ContainsKey(prayerName))
                Prayers[prayerName] = new List<DeedEntry>();
        }
    }
}

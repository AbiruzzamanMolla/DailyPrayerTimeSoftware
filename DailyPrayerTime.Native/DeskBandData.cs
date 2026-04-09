using System;

namespace DailyPrayerTime.Native
{
    public class DeskBandData
    {
        public string Label { get; set; } = "Fajr ends in:";
        public string Countdown { get; set; } = "00:00:00";
        public string CurrentPrayer { get; set; } = "--";
        public string NextPrayer { get; set; } = "--";
        public string NextTime { get; set; } = "--";
        public string PrimaryColor { get; set; } = "#10b981";
        public bool IsNight { get; set; } = false;
        public bool IsActive { get; set; } = false;
    }
}

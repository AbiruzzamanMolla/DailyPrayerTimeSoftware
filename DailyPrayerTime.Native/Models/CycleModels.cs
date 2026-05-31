using System;
using System.Collections.Generic;

namespace DailyPrayerTime.Native.Models
{
    public enum CycleStatus
    {
        Unknown,
        Hayd,        // Menstruation
        Tuhr,        // Pure / safe period
        Istihadah    // Irregular bleeding (not haidh)
    }

    public enum Madhab
    {
        Hanafi,
        Shafii,
        Sistani,
        Maliki,
        Hanbali
    }

    public class CycleEntry
    {
        public string StartDate { get; set; } = "";
        public string? EndDate { get; set; }
        public CycleStatus Status { get; set; } = CycleStatus.Hayd;
        public string Madhab { get; set; } = "Sistani";
        public string Notes { get; set; } = "";
    }

    public class CycleMeta
    {
        public int AverageCycleLength { get; set; } = 28;
        public int AveragePeriodLength { get; set; } = 6;
        public string LastPeriodStart { get; set; } = "";
        public int TotalCycles { get; set; } = 0;
    }

    public class CycleDayInfo
    {
        public DateTime Date { get; set; }
        public CycleStatus Status { get; set; } = CycleStatus.Unknown;
        public int CycleDayNumber { get; set; }
        public int PeriodDayNumber { get; set; }
        public bool IsCurrentDay { get; set; }
        public string? StatusText { get; set; }
        public string? StatusTextArabic { get; set; }
        public bool IsQadaRequired { get; set; }
    }

    public static class MadhabRules
    {
        // Returns (minHayd, maxHayd, minTuhur)
        public static (int MinHayd, int MaxHayd, int MinTuhur) GetRules(string madhab)
        {
            return madhab.ToLower() switch
            {
                "hanafi" => (3, 10, 15),
                "shafii" => (1, 15, 15),
                "maliki" => (0, 15, 15),
                "hanbali" => (1, 15, 13),
                "sistani" => (3, 10, 10),
                _ => (3, 10, 10)
            };
        }

        public static bool IsHaydValid(int days, string madhab)
        {
            var (min, max, _) = GetRules(madhab);
            if (min == 0) return days <= max;
            return days >= min && days <= max;
        }

        public static bool IsTuhurValid(int days, string madhab)
        {
            var (_, _, minTuhur) = GetRules(madhab);
            return days >= minTuhur;
        }
    }
}

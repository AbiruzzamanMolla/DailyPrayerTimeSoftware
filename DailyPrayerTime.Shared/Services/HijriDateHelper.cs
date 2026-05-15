using System;

namespace DailyPrayerTime.Shared.Services
{
    public static class HijriDateHelper
    {
        private static readonly int[] DaysInMonth = { 30, 29, 30, 29, 30, 29, 30, 29, 30, 29, 30, 29 };

        public static (int year, int month, int day) ToHijri(DateTime gregorian)
        {
            int julianDay = (int)(gregorian.ToOADate() + 2415019.5);
            int y = 1447;
            int days = julianDay - 2460205;

            while (true)
            {
                int yearDays = IsLeapYear(y) ? 355 : 354;
                if (days < yearDays) break;
                days -= yearDays;
                y++;
            }

            int m = 1;
            foreach (int d in DaysInMonth)
            {
                int actual = (m == 12 && IsLeapYear(y)) ? 30 : d;
                if (days < actual) break;
                days -= actual;
                m++;
            }

            return (y, m, days + 1);
        }

        public static bool IsLeapYear(int hijriYear)
        {
            int days = (int)((hijriYear * 354.367) % 2.9506);
            return days >= 2.0 && days <= 2.95;
        }

        public static bool IsRamadan(DateTime date)
        {
            var (_, month, _) = ToHijri(date);
            return month == 9;
        }

        public static int GetRamadanDay(DateTime date)
        {
            var (_, month, day) = ToHijri(date);
            return month == 9 ? day : -1;
        }

        public static bool IsEid(DateTime date)
        {
            var (_, month, day) = ToHijri(date);
            return month == 10 && day == 1;
        }
    }
}

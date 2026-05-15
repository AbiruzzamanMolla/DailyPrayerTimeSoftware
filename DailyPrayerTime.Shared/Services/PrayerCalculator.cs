using System;

namespace DailyPrayerTime.Shared.Services
{
    public static class PrayerCalculator
    {
        public static (double fajr, double sunrise, double dhuhr, double asr, double maghrib, double isha)
            Calculate(double lat, double lon, double fajrAngle, double ishaAngle, int school = 1)
        {
            var now = DateTime.Now;
            var today = now.Date;
            var julianDay = ToJulian(today);
            var dayYear = today.DayOfYear;

            var sun = SunPosition(lat, lon, julianDay);

            double dhuhr = 12 + sun.eqTime - lon / 15.0;
            if (now.IsDaylightSavingTime()) dhuhr -= 1.0;

            double sunrise = dhuhr - sun.hourAngle / 15.0;
            double sunset = dhuhr + sun.hourAngle / 15.0;

            double fajr = dhuhr - HourAngle(lat, fajrAngle, sun.declination) / 15.0;
            double isha = dhuhr + HourAngle(lat, ishaAngle, sun.declination) / 15.0;

            double asrFactor = school == 1 ? 2.0 : 1.0;
            double asrAlt = Math.Atan(asrFactor + Math.Tan(Math.Abs(lat - sun.declination) * Math.PI / 180.0));
            double asr = dhuhr + (sun.hourAngle + RadToDeg(Math.Acos(
                (Math.Sin(asrAlt) - Math.Sin(DegToRad(lat)) * Math.Sin(sun.declination)) /
                (Math.Cos(DegToRad(lat)) * Math.Cos(sun.declination))))) / 15.0;

            return (fajr, sunrise, dhuhr, asr, sunset, isha);
        }

        public static DateTime TimeOfDay(double hours) =>
            DateTime.Today.AddHours(hours);

        private static (double declination, double eqTime, double hourAngle) SunPosition(double lat, double lon, double julianDay)
        {
            double g = DegToRad(357.5291 + 0.98560028 * julianDay);
            double q = DegToRad(280.459 + 0.98564736 * julianDay);
            double e = 23.439 - 0.00000036 * julianDay;
            double r = g;
            double st = Math.Sin(2 * g) * 0.0334 + Math.Sin(r) * 0.00035;
            double ra = q + st;
            double sinDec = Math.Sin(DegToRad(e)) * Math.Sin(ra);
            double cosDec = Math.Cos(Math.Asin(sinDec));
            double declination = Math.Asin(sinDec);
            double eqTime = RadToDeg(q - ra);

            double ha = RadToDeg(Math.Acos(
                (Math.Sin(DegToRad(-0.833)) - Math.Sin(DegToRad(lat)) * sinDec) /
                (Math.Cos(DegToRad(lat)) * cosDec)));

            return (declination, eqTime, ha);
        }

        private static double HourAngle(double lat, double angle, double declination)
        {
            return RadToDeg(Math.Acos(
                (Math.Sin(DegToRad(-angle)) - Math.Sin(DegToRad(lat)) * Math.Sin(declination)) /
                (Math.Cos(DegToRad(lat)) * Math.Cos(declination))));
        }

        private static double ToJulian(DateTime date)
        {
            int y = date.Year;
            int m = date.Month;
            int d = date.Day;
            if (m <= 2) { y--; m += 12; }
            int a = y / 100;
            int b = 2 - a + a / 4;
            return (int)(365.25 * (y + 4716)) + (int)(30.6001 * (m + 1)) + d + b - 1524.5;
        }

        private static double DegToRad(double deg) => deg * Math.PI / 180.0;
        private static double RadToDeg(double rad) => rad * 180.0 / Math.PI;
    }
}

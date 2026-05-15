using System;

namespace DailyPrayerTime.Shared.Services
{
    public static class QiblaCalculator
    {
        private const double KaabaLat = 21.4225;
        private const double KaabaLon = 39.8262;

        public static double CalculateDirection(double userLat, double userLon)
        {
            double lat1 = DegToRad(KaabaLat);
            double lon1 = DegToRad(KaabaLon);
            double lat2 = DegToRad(userLat);
            double lon2 = DegToRad(userLon);
            double dLon = lon1 - lon2;
            double y = Math.Sin(dLon);
            double x = Math.Cos(lat1) * Math.Tan(lat2) - Math.Sin(lat1) * Math.Cos(dLon);
            double bearing = Math.Atan2(y, x);
            bearing = RadToDeg(bearing);
            return (bearing + 360) % 360;
        }

        private static double DegToRad(double deg) => deg * Math.PI / 180.0;
        private static double RadToDeg(double rad) => rad * 180.0 / Math.PI;
    }
}

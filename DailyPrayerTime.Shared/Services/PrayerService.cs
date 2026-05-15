using System;
using System.Net.Http;
using System.Threading.Tasks;
using Newtonsoft.Json;

namespace DailyPrayerTime.Shared.Services
{
    public class CombinedPrayerTimes
    {
        public DateTime Fajr { get; set; }
        public DateTime Sunrise { get; set; }
        public DateTime Dhuhr { get; set; }
        public DateTime Asr { get; set; }
        public DateTime Maghrib { get; set; }
        public DateTime Isha { get; set; }
        public DateTime Suhur { get; set; }
        public DateTime Iftar { get; set; }

        public CombinedPrayerTimes() { }

        public CombinedPrayerTimes(double lat, double lon, double fajrAngle, double ishaAngle, int school,
            double suhurOffset = 0, double iftarOffset = 0)
        {
            var (f, sr, d, a, m, i) = PrayerCalculator.Calculate(lat, lon, fajrAngle, ishaAngle, school);
            var t = DateTime.Today;
            Fajr = t.AddHours(f).AddMinutes(suhurOffset);
            Sunrise = t.AddHours(sr);
            Dhuhr = t.AddHours(d);
            Asr = t.AddHours(a);
            Maghrib = t.AddHours(m).AddMinutes(iftarOffset);
            Isha = t.AddHours(i);
            Suhur = Fajr;
            Iftar = Maghrib;
        }

        public string CurrentPrayerName(DateTime now)
        {
            if (now >= Isha) return "Isha";
            if (now >= Maghrib) return "Maghrib";
            if (now >= Asr) return "Asr";
            if (now >= Dhuhr) return "Dhuhr";
            if (now >= Sunrise) return "Sunrise";
            if (now >= Fajr) return "Fajr";
            return "Isha";
        }

        public string NextPrayerName(DateTime now)
        {
            if (now < Fajr) return "Fajr";
            if (now < Sunrise) return "Sunrise";
            if (now < Dhuhr) return "Dhuhr";
            if (now < Asr) return "Asr";
            if (now < Maghrib) return "Maghrib";
            if (now < Isha) return "Isha";
            return "Fajr";
        }

        public DateTime NextPrayerTime(DateTime now)
        {
            if (now < Fajr) return Fajr;
            if (now < Sunrise) return Sunrise;
            if (now < Dhuhr) return Dhuhr;
            if (now < Asr) return Asr;
            if (now < Maghrib) return Maghrib;
            if (now < Isha) return Isha;
            return Fajr.AddDays(1);
        }

        public DateTime GetPrayerByName(string name) => name switch
        {
            "Fajr" => Fajr, "Sunrise" => Sunrise, "Dhuhr" => Dhuhr,
            "Asr" => Asr, "Maghrib" => Maghrib, "Isha" => Isha, _ => DateTime.MinValue
        };

        public DateTime GetPreviousPrayerTime(DateTime now)
        {
            if (now >= Isha) return Isha;
            if (now >= Maghrib) return Maghrib;
            if (now >= Asr) return Asr;
            if (now >= Dhuhr) return Dhuhr;
            if (now >= Sunrise) return Sunrise;
            if (now >= Fajr) return Fajr;
            return Isha.AddDays(-1);
        }

        public string GetPrayerNameAt(DateTime time)
        {
            if (time == Fajr) return "Fajr"; if (time == Sunrise) return "Sunrise";
            if (time == Dhuhr) return "Dhuhr"; if (time == Asr) return "Asr";
            if (time == Maghrib) return "Maghrib"; if (time == Isha) return "Isha";
            return "";
        }
    }

    public static class PrayerService
    {
        private static readonly HttpClient Client = new() { Timeout = TimeSpan.FromSeconds(10) };
        public static string CacheBasePath { get; set; } = "";

        public static async Task<CombinedPrayerTimes> GetPrayerTimesAsync(
            double lat, double lon, string methodStr, int school,
            double fajrAngle = 18, double ishaAngle = 17.5,
            int highLatRule = 0, double suhurOffset = 0, double iftarOffset = 0,
            bool useApi = true)
        {
            string dateStr = DateTime.Now.ToString("dd-MM-yyyy");

            if (useApi)
            {
                try
                {
                    string cachePath = string.IsNullOrEmpty(CacheBasePath) ? ""
                        : System.IO.Path.Combine(CacheBasePath, "prayer_cache.json");

                    if (!string.IsNullOrEmpty(cachePath) && System.IO.File.Exists(cachePath))
                    {
                        var json = await System.IO.File.ReadAllTextAsync(cachePath);
                        var cache = JsonConvert.DeserializeObject<PrayerCache>(json);
                        if (cache != null && cache.Date == dateStr
                            && Math.Abs(cache.Latitude - lat) < 0.001
                            && Math.Abs(cache.Longitude - lon) < 0.001)
                            return cache.Data;
                    }

                    int methodId = MapMethod(methodStr);
                    string url = $"https://api.aladhan.com/v1/timings/{dateStr}?latitude={lat}&longitude={lon}&method={methodId}&school={school}";
                    var response = await Client.GetStringAsync(url);
                    var apiData = JsonConvert.DeserializeObject<dynamic>(response);

                    if (apiData?.code == 200)
                    {
                        var timings = apiData.data.timings;
                        var results = new CombinedPrayerTimes
                        {
                            Fajr = ParseTime(timings.Fajr.ToString()).AddMinutes(suhurOffset),
                            Sunrise = ParseTime(timings.Sunrise.ToString()),
                            Dhuhr = ParseTime(timings.Dhuhr.ToString()),
                            Asr = ParseTime(timings.Asr.ToString()),
                            Maghrib = ParseTime(timings.Maghrib.ToString()).AddMinutes(iftarOffset),
                            Isha = ParseTime(timings.Isha.ToString()),
                            Iftar = ParseTime(timings.Maghrib.ToString()).AddMinutes(iftarOffset),
                            Suhur = ParseTime(timings.Imsak.ToString()).AddMinutes(suhurOffset),
                        };

                        if (!string.IsNullOrEmpty(cachePath))
                        {
                            var cache = new PrayerCache
                            {
                                Latitude = lat, Longitude = lon,
                                Method = methodStr, School = school,
                                Date = dateStr, Data = results
                            };
                            var dir = System.IO.Path.GetDirectoryName(cachePath);
                            if (!string.IsNullOrEmpty(dir) && !System.IO.Directory.Exists(dir))
                                System.IO.Directory.CreateDirectory(dir);
                            await System.IO.File.WriteAllTextAsync(cachePath, JsonConvert.SerializeObject(cache));
                        }
                        return results;
                    }
                }
                catch { /* fallback */ }
            }

            return new CombinedPrayerTimes(lat, lon, fajrAngle, ishaAngle, school, suhurOffset, iftarOffset);
        }

        private static DateTime ParseTime(string timeStr)
        {
            var parts = timeStr.Split(':');
            var now = DateTime.Now;
            int hour = int.Parse(parts[0]);
            string minuteStr = parts[1].Trim();
            if (minuteStr.Length > 2) minuteStr = minuteStr[..2];
            int minute = int.Parse(minuteStr);
            return new DateTime(now.Year, now.Month, now.Day, hour, minute, 0, DateTimeKind.Local);
        }

        private static int MapMethod(string method) => method.ToUpper() switch
        {
            "SHIA" => 0, "KARACHI" => 1, "ISNA" => 2, "MWL" => 3,
            "MAKKAH" => 4, "EGYPT" => 5, "TEHRAN" => 7, "GULF" => 8,
            "KUWAIT" => 9, "QATAR" => 10, "SINGAPORE" => 11, "FRANCE" => 12,
            "TURKEY" => 13, "RUSSIA" => 14, "MOONSIGHTING" => 15, "DUBAI" => 16,
            "JAKIM" => 17, "TUNISIA" => 18, "ALGERIA" => 19, "KEMENAG" => 20,
            "MOROCCO" => 21, "BRAZIL" => 22, "PORTUGAL" => 23, "JORDAN" => 24,
            _ => 3
        };
    }

    public class PrayerCache
    {
        public double Latitude { get; set; }
        public double Longitude { get; set; }
        public string Method { get; set; } = "";
        public int School { get; set; }
        public string Date { get; set; } = "";
        public CombinedPrayerTimes Data { get; set; } = new CombinedPrayerTimes();
    }
}

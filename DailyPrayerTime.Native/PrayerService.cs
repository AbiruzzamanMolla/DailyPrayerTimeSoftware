using System;
using System.Net.Http;
using System.Threading.Tasks;
using System.Collections.Generic;
using System.IO;
using Newtonsoft.Json;
using Batoulapps.Adhan;
using Batoulapps.Adhan.Internal;

namespace DailyPrayerTime.Native
{
    public class CombinedPrayerTimes
    {
        public DateTime Fajr { get; set; }
        public DateTime Sunrise { get; set; }
        public DateTime Dhuhr { get; set; }
        public DateTime Asr { get; set; }
        public DateTime Maghrib { get; set; }
        public DateTime Isha { get; set; }
        public DateTime Suhur { get; set; } // End of Sehri (Imsak)
        public DateTime Iftar { get; set; } // Maghrib time
        public string HijriDate { get; set; } = "";
        public int HijriDay { get; set; } = 0;
        public int HijriMonth { get; set; } = 0;
        public int HijriYear { get; set; } = 0;
        public string HijriWeekday { get; set; } = "";

        public CombinedPrayerTimes() { }

        public CombinedPrayerTimes(PrayerTimes local)
        {
            Fajr = local.Fajr.ToLocalTime();
            Sunrise = local.Sunrise.ToLocalTime();
            Dhuhr = local.Dhuhr.ToLocalTime();
            Asr = local.Asr.ToLocalTime();
            Maghrib = local.Maghrib.ToLocalTime();
            Isha = local.Isha.ToLocalTime();
            Suhur = local.Fajr.ToLocalTime().AddMinutes(SettingsManager.Current.SuhurOffset);
            Iftar = local.Maghrib.ToLocalTime().AddMinutes(SettingsManager.Current.IftarOffset);
        }

        public Prayer CurrentPrayer(DateTime now)
        {
            if (now >= Isha) return Prayer.ISHA;
            if (now >= Maghrib) return Prayer.MAGHRIB;
            if (now >= Asr) return Prayer.ASR;
            if (now >= Dhuhr) return Prayer.DHUHR;
            if (now >= Sunrise) return Prayer.SUNRISE; // After Sunrise, before Dhuhr
            if (now >= Fajr) return Prayer.FAJR;
            return Prayer.ISHA; // Post-midnight before Fajr is considered Isha relative to the previous day
        }

        public DateTime TimeForPrayer(Prayer p)
        {
            return p switch
            {
                Prayer.FAJR => Fajr,
                Prayer.SUNRISE => Sunrise,
                Prayer.DHUHR => Dhuhr,
                Prayer.ASR => Asr,
                Prayer.MAGHRIB => Maghrib,
                Prayer.ISHA => Isha,
                _ => DateTime.MinValue
            };
        }
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

    public static class PrayerService
    {
        private static readonly HttpClient client = new HttpClient { Timeout = TimeSpan.FromSeconds(10) };

        public static async Task<CombinedPrayerTimes> GetPrayerTimesAsync(double lat, double lon, string methodStr, int school)
        {
            var s = SettingsManager.Current;
            string dateStr = DateTime.Now.ToString("dd-MM-yyyy");

            // Cache check
            string appData = StorageService.GetAppDataPath();
            string cachePath = Path.Combine(appData, "prayer_cache.json");

            if (s.UseExternalApi && methodStr.ToUpper() != "MANUAL")
            {
                try
                {
                    if (File.Exists(cachePath))
                    {
                        var json = await File.ReadAllTextAsync(cachePath);
                        var cache = JsonConvert.DeserializeObject<PrayerCache>(json);

                        if (cache != null &&
                            cache.Date == dateStr &&
                            cache.Method == methodStr &&
                            cache.School == school &&
                            Math.Abs(cache.Latitude - lat) < 0.001 &&
                            Math.Abs(cache.Longitude - lon) < 0.001)
                        {
                            return cache.Data;
                        }
                    }

                    int methodId = MapMethodToApi(methodStr);
                    string url = $"https://api.aladhan.com/v1/timings/{dateStr}?latitude={lat}&longitude={lon}&method={methodId}&school={school}";
                    
                    var response = await client.GetStringAsync(url);
                    var apiData = JsonConvert.DeserializeObject<dynamic>(response);
                    
                    if (apiData?.code == 200)
                    {
                        var timings = apiData.data.timings;
                        var hijri = apiData.data.date.hijri;
                        
                        var results = new CombinedPrayerTimes
                        {
                            Fajr = ParseApiTime(timings.Fajr.ToString()),
                            Sunrise = ParseApiTime(timings.Sunrise.ToString()),
                            Dhuhr = ParseApiTime(timings.Dhuhr.ToString()),
                            Asr = ParseApiTime(timings.Asr.ToString()),
                            Maghrib = ParseApiTime(timings.Maghrib.ToString()),
                            Isha = ParseApiTime(timings.Isha.ToString()),
                            Suhur = ParseApiTime(timings.Imsak.ToString()).AddMinutes(s.SuhurOffset),
                            Iftar = ParseApiTime(timings.Maghrib.ToString()).AddMinutes(s.IftarOffset),
                            HijriDate = $"{hijri.day} {hijri.month.en} {hijri.year} AH",
                            HijriDay = int.TryParse(hijri.day.ToString(), out int d) ? d : 0,
                            HijriMonth = int.TryParse(hijri.month.number.ToString(), out int m) ? m : 0,
                            HijriYear = int.TryParse(hijri.year.ToString(), out int y) ? y : 0,
                            HijriWeekday = hijri.weekday.ar.ToString()
                        };

                        // Save to cache
                        var cache = new PrayerCache
                        {
                            Latitude = lat,
                            Longitude = lon,
                            Method = methodStr,
                            School = school,
                            Date = dateStr,
                            Data = results
                        };
                        await File.WriteAllTextAsync(cachePath, JsonConvert.SerializeObject(cache, Formatting.Indented));

                        return results;
                    }
                }
                catch { /* Fallback to local */ }
            }

            // Local Fallback or Manual
            var coordinates = new Coordinates(lat, lon);
            var date = DateComponents.From(DateTime.Now);

            CalculationParameters parameters;
            if (methodStr.ToUpper() == "MANUAL")
            {
                parameters = new CalculationParameters(s.FajrAngle, s.IshaAngle);
                parameters.Method = CalculationMethod.OTHER;
            }
            else
            {
                parameters = CalculationMethodExtensions.GetParameters(MapMethodToLocal(methodStr));
            }

            parameters.Madhab = school == 1 ? Madhab.HANAFI : Madhab.SHAFI;
            parameters.HighLatitudeRule = (HighLatitudeRule)s.HighLatitudeRule;

            var local = new PrayerTimes(coordinates, date, parameters);
            return new CombinedPrayerTimes(local);
        }

        private static DateTime ParseApiTime(string timeStr)
        {
            // API returns "HH:mm"
            var parts = timeStr.Split(':');
            var now = DateTime.Now;
            return new DateTime(now.Year, now.Month, now.Day, int.Parse(parts[0]), int.Parse(parts[1]), 0, DateTimeKind.Local);
        }

        private static int MapMethodToApi(string method)
        {
            return method.ToUpper() switch
            {
                "SHIA" => 0,
                "KARACHI" => 1,
                "ISNA" => 2,
                "MWL" => 3,
                "MAKKAH" => 4,
                "EGYPT" => 5,
                "TEHRAN" => 7,
                "GULF" => 8,
                "KUWAIT" => 9,
                "QATAR" => 10,
                "SINGAPORE" => 11,
                "FRANCE" => 12,
                "TURKEY" => 13,
                "RUSSIA" => 14,
                "MOONSIGHTING" => 15,
                "DUBAI" => 16,
                "JAKIM" => 17,
                "TUNISIA" => 18,
                "ALGERIA" => 19,
                "KEMENAG" => 20,
                "MOROCCO" => 21,
                "BRAZIL" => 22,
                "PORTUGAL" => 23,
                "JORDAN" => 24,
                _ => 3
            };
        }

        private static CalculationMethod MapMethodToLocal(string method)
        {
            return method.ToUpper() switch
            {
                "KARACHI" => CalculationMethod.KARACHI,
                "ISNA" => CalculationMethod.NORTH_AMERICA,
                "MWL" => CalculationMethod.MUSLIM_WORLD_LEAGUE,
                "MAKKAH" => CalculationMethod.UMM_AL_QURA,
                "EGYPT" => CalculationMethod.EGYPTIAN,
                "QATAR" => CalculationMethod.QATAR,
                "KUWAIT" => CalculationMethod.KUWAIT,
                "SINGAPORE" => CalculationMethod.SINGAPORE,
                "DUBAI" => CalculationMethod.DUBAI,
                "MANUAL" => CalculationMethod.OTHER,
                _ => CalculationMethod.MUSLIM_WORLD_LEAGUE
            };
        }
    }
}

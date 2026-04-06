using System;
using System.Net.Http;
using System.Threading.Tasks;
using System.Collections.Generic;
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
        public string HijriDate { get; set; } = "";

        public CombinedPrayerTimes() { }

        public CombinedPrayerTimes(PrayerTimes local)
        {
            Fajr = local.Fajr.ToLocalTime();
            Sunrise = local.Sunrise.ToLocalTime();
            Dhuhr = local.Dhuhr.ToLocalTime();
            Asr = local.Asr.ToLocalTime();
            Maghrib = local.Maghrib.ToLocalTime();
            Isha = local.Isha.ToLocalTime();
        }

        public Prayer CurrentPrayer(DateTime now)
        {
            if (now >= Isha) return Prayer.ISHA;
            if (now >= Maghrib) return Prayer.MAGHRIB;
            if (now >= Asr) return Prayer.ASR;
            if (now >= Dhuhr) return Prayer.DHUHR;
            if (now >= Fajr) return Prayer.FAJR;
            return Prayer.NONE;
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

    public static class PrayerService
    {
        private static readonly HttpClient client = new HttpClient { Timeout = TimeSpan.FromSeconds(10) };

        public static async Task<CombinedPrayerTimes> GetPrayerTimesAsync(double lat, double lon, string methodStr, int school)
        {
            var s = SettingsManager.Current;
            if (s.UseExternalApi)
            {
                try
                {
                    int methodId = MapMethodToApi(methodStr);
                    string dateStr = DateTime.Now.ToString("dd-MM-yyyy");
                    string url = $"https://api.aladhan.com/v1/timings/{dateStr}?latitude={lat}&longitude={lon}&method={methodId}&school={school}";
                    
                    var response = await client.GetStringAsync(url);
                    var apiData = JsonConvert.DeserializeObject<dynamic>(response);
                    
                    if (apiData?.code == 200)
                    {
                        var timings = apiData.data.timings;
                        var hijri = apiData.data.date.hijri;
                        
                        return new CombinedPrayerTimes
                        {
                            Fajr = ParseApiTime(timings.Fajr.ToString()),
                            Sunrise = ParseApiTime(timings.Sunrise.ToString()),
                            Dhuhr = ParseApiTime(timings.Dhuhr.ToString()),
                            Asr = ParseApiTime(timings.Asr.ToString()),
                            Maghrib = ParseApiTime(timings.Maghrib.ToString()),
                            Isha = ParseApiTime(timings.Isha.ToString()),
                            HijriDate = $"{hijri.day} {hijri.month.en} {hijri.year} AH"
                        };
                    }
                }
                catch { /* Fallback to local */ }
            }

            // Local Fallback
            var coordinates = new Coordinates(lat, lon);
            var date = DateComponents.From(DateTime.Now);
            var parameters = CalculationMethodExtensions.GetParameters(MapMethodToLocal(methodStr));
            parameters.Madhab = school == 1 ? Madhab.HANAFI : Madhab.SHAFI;
            
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
                "KARACHI" => 1,
                "ISNA" => 2,
                "MWL" => 3,
                "UMM_AL_QURA" => 4,
                "EGYPTIAN" => 5,
                "QATAR" => 10,
                "KUWAIT" => 9,
                "SINGAPORE" => 11,
                "TURKEY" => 13,
                "DUBAI" => 12,
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
                "UMM_AL_QURA" => CalculationMethod.UMM_AL_QURA,
                "EGYPTIAN" => CalculationMethod.EGYPTIAN,
                "QATAR" => CalculationMethod.QATAR,
                "KUWAIT" => CalculationMethod.KUWAIT,
                "SINGAPORE" => CalculationMethod.SINGAPORE,
                "DUBAI" => CalculationMethod.DUBAI,
                _ => CalculationMethod.MUSLIM_WORLD_LEAGUE
            };
        }
    }
}

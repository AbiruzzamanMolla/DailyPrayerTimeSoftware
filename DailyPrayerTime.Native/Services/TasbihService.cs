using System;
using System.Collections.Generic;
using System.IO;
using Newtonsoft.Json;

namespace DailyPrayerTime.Native.Services
{
    public class TasbihService
    {
        private static readonly Lazy<TasbihService> _instance = new(() => new());
        public static TasbihService Instance => _instance.Value;

        public static List<PhraseItem> DefaultPhrases => new()
        {
            new("SubhanAllah", "سُبْحَانَ ٱللَّٰهِ", "Subhaanallaah", "সুবহানাল্লাহ"),
            new("Alhamdulillah", "ٱلْحَمْدُ لِلَّٰهِ", "Alhamdulillaah", "আলহামদুলিল্লাহ"),
            new("AllahuAkbar", "ٱللَّٰهُ أَكْبَرُ", "Allaahu 'Akbar", "আল্লাহু আকবার"),
            new("LaIlahaIllallah", "لَا إِلَٰهَ إِلَّا ٱللَّٰهُ", "Laa 'ilaaha 'illallaah", "লা ইলাহা ইল্লাল্লাহ"),
            new("Astaghfirullah", "أَسْتَغْفِرُ ٱللَّٰهَ", "'Astaghfirullaah", "আসতাগফিরুল্লাহ"),
            new("Durood", "اللَّهُمَّ صَلِّ وَسَلِّمْ عَلَى نَبِيِّنَا مُحَمَّدٍ", "Allahumma salli wa sallim 'ala nabiyyina Muhammad", "আল্লাহুম্মা সাল্লি ওয়া সাল্লামি আলা নাবিইনা মুহাম্মাদ"),
        };

        private string GetFilePath(DateTime date)
        {
            return Path.Combine(StorageService.GetAppDataPath(), $"tasbih_{date:yyyy-MM-dd}.json");
        }

        public Dictionary<string, int> LoadDay(DateTime date)
        {
            var path = GetFilePath(date);
            if (!File.Exists(path)) return new Dictionary<string, int>();

            try
            {
                var json = File.ReadAllText(path);
                return JsonConvert.DeserializeObject<Dictionary<string, int>>(json) ?? new Dictionary<string, int>();
            }
            catch
            {
                return new Dictionary<string, int>();
            }
        }

        public void SaveDay(DateTime date, Dictionary<string, int> counts)
        {
            var path = GetFilePath(date);
            var dir = Path.GetDirectoryName(path);
            if (!string.IsNullOrEmpty(dir) && !Directory.Exists(dir))
                Directory.CreateDirectory(dir);
            File.WriteAllText(path, JsonConvert.SerializeObject(counts, Formatting.Indented));
            // Debounced cloud push
            CloudSyncService.Instance.PushTasbihDebounced(date, counts);
        }
    }

    public class PhraseItem
    {
        public string Key { get; }
        public string Arabic { get; }
        public string EnTranslit { get; }
        public string BnTranslit { get; }

        public PhraseItem(string key, string arabic, string enTranslit, string bnTranslit)
        {
            Key = key;
            Arabic = arabic;
            EnTranslit = enTranslit;
            BnTranslit = bnTranslit;
        }
    }
}

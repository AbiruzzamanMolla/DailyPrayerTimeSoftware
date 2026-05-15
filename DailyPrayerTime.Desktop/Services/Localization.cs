using System;
using System.Collections.Generic;
using System.IO;
using Newtonsoft.Json;

namespace DailyPrayerTime.Desktop.Services
{
    public static class Localization
    {
        private static Dictionary<string, string> _strings = new();
        public static string CurrentLang { get; private set; } = "en";

        public static event Action? LanguageChanged;

        public static void SetLanguage(string langCode)
        {
            CurrentLang = langCode;
            Load(langCode);
            LanguageChanged?.Invoke();
        }

        public static string Get(string key, params object?[] args)
        {
            if (_strings.TryGetValue(key, out var val))
                return args.Length > 0 ? string.Format(val, args) : val;
            return key;
        }

        private static void Load(string langCode)
        {
            _strings.Clear();
            try
            {
                var path = Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "i18n", $"{langCode}.json");
                if (File.Exists(path))
                {
                    var json = File.ReadAllText(path);
                    var data = JsonConvert.DeserializeObject<Dictionary<string, string>>(json);
                    if (data != null)
                        _strings = data;
                }
            }
            catch { /* fallback to empty */ }
        }
    }
}

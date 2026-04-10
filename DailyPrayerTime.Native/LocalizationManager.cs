using System;
using System.Collections.Generic;
using System.IO;
using System.Reflection;
using Newtonsoft.Json;
using WpfApp = System.Windows.Application;

namespace DailyPrayerTime.Native
{
    public class LocalizationManager
    {
        private static LocalizationManager? _instance;
        public static LocalizationManager Instance => _instance ??= new LocalizationManager();

        private System.Windows.ResourceDictionary _i18nDictionary = new System.Windows.ResourceDictionary();
        private string _currentLanguage = "en";

        private LocalizationManager()
        {
            // Find the i18n dictionary in app resources if it exists, or add it
            foreach (var dict in WpfApp.Current.Resources.MergedDictionaries)
            {
                if (dict.Contains("is_i18n_dict"))
                {
                    _i18nDictionary = dict;
                    return;
                }
            }
            
            _i18nDictionary.Add("is_i18n_dict", true);
            WpfApp.Current.Resources.MergedDictionaries.Add(_i18nDictionary);
        }

        public void SetLanguage(string langCode)
        {
            try
            {
                string basePath = AppDomain.CurrentDomain.BaseDirectory;
                string filePath = Path.Combine(basePath, "i18n", $"{langCode}.json");

                if (!File.Exists(filePath))
                {
                    // Fallback to English if file missing
                    if (langCode == "en") return;
                    SetLanguage("en");
                    return;
                }

                string json = File.ReadAllText(filePath);
                var data = JsonConvert.DeserializeObject<Dictionary<string, string>>(json);

                if (data != null)
                {
                    _currentLanguage = langCode;
                    
                    // Store each key in BOTH forms:
                    // 1. With i18n_ prefix → used by C# GetString() calls
                    // 2. Without prefix    → used by XAML {DynamicResource Prayer_Fajr} bindings
                    foreach (var kvp in data)
                    {
                        string bareKey = kvp.Key.StartsWith("i18n_") ? kvp.Key.Substring(5) : kvp.Key;
                        string prefixedKey = kvp.Key.StartsWith("i18n_") ? kvp.Key : "i18n_" + kvp.Key;

                        SetOrAdd(bareKey, kvp.Value);
                        SetOrAdd(prefixedKey, kvp.Value);
                    }
                }
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Error loading language {langCode}: {ex.Message}");
            }
        }

        private void SetOrAdd(string key, string value)
        {
            if (_i18nDictionary.Contains(key))
                _i18nDictionary[key] = value;
            else
                _i18nDictionary.Add(key, value);
        }

        public string GetString(string key)
        {
            // Try both prefixed and non-prefixed as fallback
            string resourceKey = key.StartsWith("i18n_") ? key : "i18n_" + key;
            
            if (_i18nDictionary.Contains(resourceKey))
            {
                return _i18nDictionary[resourceKey] as string ?? key;
            }
            
            // Fallback to exact key if i18n_ prefixed not found
            if (_i18nDictionary.Contains(key))
            {
                return _i18nDictionary[key] as string ?? key;
            }

            return key;
        }

        public string CurrentLanguage => _currentLanguage;
    }
}

using System;
using System.IO;
using Newtonsoft.Json;

namespace DailyPrayerTime.Native
{
    public class AppSettings
    {
        public string BgType { get; set; } = "gradient";
        public string BgColor { get; set; } = "#ffffff";
        public string GradientStart { get; set; } = "#064e3b";
        public string GradientEnd { get; set; } = "#022c22";
        public int GradientAngle { get; set; } = 100;
        public string PrimaryColor { get; set; } = "#10b981";
        public string SecondaryColor { get; set; } = "#34d399";
        
        public int School { get; set; } = 1; // 1 = Hanafi, 0 = Shafi
        public string Method { get; set; } = "KARACHI";
        public int MidnightMode { get; set; } = 0;
        public int LatitudeAdjustmentMethod { get; set; } = 3;
        
        public string TimeFormat { get; set; } = "12h";
        public bool RamadanMode { get; set; } = false;
        
        public bool ShowOverlay { get; set; } = true;
        public bool AutoStart { get; set; } = false;

        public double Latitude { get; set; } = 23.8103;
        public double Longitude { get; set; } = 90.4125;
        public string LocationName { get; set; } = "Dhaka, Bangladesh";

        public double OverlayX { get; set; } = -1;
        public double OverlayY { get; set; } = -1;

        public bool NotificationsEnabled { get; set; } = true;
        
        // Congregation (Jamaat) Settings (Fixed Times in "HH:mm" format)
        public string FajrJamaatTime { get; set; } = "05:15";
        public string DhuhrJamaatTime { get; set; } = "13:30";
        public string AsrJamaatTime { get; set; } = "17:00";
        public string MaghribJamaatTime { get; set; } = "18:45";
        public string IshaJamaatTime { get; set; } = "20:30";
        public int JamaatPopupOffset { get; set; } = 5; // Minutes before Jamaat
        
        // Adhan Sound Alarm Settings
        public bool AdhanAlarmEnabled { get; set; } = false;
        public int AdhanAlarmOffset { get; set; } = 10; // Minutes before Jamaat
        public string AdhanSoundPath { get; set; } = "";
        public bool UseExternalApi { get; set; } = true;
    }

    public static class SettingsManager
    {
        private static readonly string AppDataFolder = Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData), "DailyPrayerTimeNative");
        private static readonly string SettingsFile = Path.Combine(AppDataFolder, "settings.json");

        public static AppSettings Current { get; private set; } = new AppSettings();

        public static void Load()
        {
            if (File.Exists(SettingsFile))
            {
                try
                {
                    string json = File.ReadAllText(SettingsFile);
                    var settings = JsonConvert.DeserializeObject<AppSettings>(json);
                    if (settings != null) Current = settings;
                }
                catch { /* Ignore and use defaults */ }
            }
        }

        public static void Save()
        {
            if (!Directory.Exists(AppDataFolder))
            {
                Directory.CreateDirectory(AppDataFolder);
            }
            string json = JsonConvert.SerializeObject(Current, Formatting.Indented);
            File.WriteAllText(SettingsFile, json);
        }
    }
}

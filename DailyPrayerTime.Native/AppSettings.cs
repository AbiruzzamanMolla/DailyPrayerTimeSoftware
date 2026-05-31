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
        
        public bool ShowTaskbarTimer { get; set; } = false;
        public string TaskbarTimerType { get; set; } = "Enhanced"; // Enhanced, Integrated, DeskBand
        
        public bool UseDeskBand { get; set; } = false; // Legacy
        public bool UseIntegratedTaskbar { get; set; } = false; // Legacy
        public bool UseEnhancedTaskbar { get; set; } = false; // Legacy
        
        public string EnhancedTaskbarPosition { get; set; } = "LeftOfTray";
        public bool ShowHeroPrayerGrid { get; set; } = false;
        public bool AutoStart { get; set; } = false;
        public bool SilentStart { get; set; } = false;

        // Manual Calculation Parameters
        public double FajrAngle { get; set; } = 18.0;
        public double IshaAngle { get; set; } = 17.5;
        public int HighLatitudeRule { get; set; } = 0; // 0 = MiddleOfTheNight, 1 = SeventhOfTheNight, 2 = TwilightAngle

        // Hijri Adjustment
        public int HijriAdjustment { get; set; } = 0;

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
        public string FajrAdhanSoundPath { get; set; } = "";
        public bool TahajjudAdhanEnabled { get; set; } = false;
        public string TahajjudAdhanSoundPath { get; set; } = "";
        public bool AdhanPopupEnabled { get; set; } = true;
        public bool UseExternalApi { get; set; } = true;

        // Granular Adhan Control
        public bool AdhanFajr { get; set; } = true;
        public bool AdhanDhuhr { get; set; } = true;
        public bool AdhanAsr { get; set; } = true;
        public bool AdhanMaghrib { get; set; } = true;
        public bool AdhanIsha { get; set; } = true;
        public int AdhanVolume { get; set; } = 100;

        // Granular Pre-Adhan Reminder Control
        public bool ReminderFajr { get; set; } = true;
        public bool ReminderShuruq { get; set; } = true;
        public bool ReminderDhuhr { get; set; } = true;
        public bool ReminderAsr { get; set; } = true;
        public bool ReminderMaghrib { get; set; } = true;
        public bool ReminderIsha { get; set; } = true;
        public int PreAdhanOffset { get; set; } = 10;

        // Granular Jamaat (Established) Reminder Control
        public bool EstablishedFajr { get; set; } = true;
        public bool EstablishedDhuhr { get; set; } = true;
        public bool EstablishedAsr { get; set; } = true;
        public bool EstablishedMaghrib { get; set; } = true;
        public bool EstablishedIsha { get; set; } = true;

        // Suhur and Iftar Offsets
        public int SuhurOffset { get; set; } = 0;
        public int IftarOffset { get; set; } = 0;
        public string Language { get; set; } = "en";
        public bool PrayerSoundEnabled { get; set; } = true;
        public string PrayerSoundLanguage { get; set; } = "en";

        // Prayer Tracker Settings
        public bool TrackerEnabled { get; set; } = true;
        public bool DeedPopupEnabled { get; set; } = true;
        public int DeedPopupOffsetMinutes { get; set; } = 15; // 15 mins after Jamaat
        public bool DailySummaryPopupEnabled { get; set; } = true;
        public string DailySummaryPopupTime { get; set; } = "22:00"; // 10 PM
        public bool AutoTrackRamadan { get; set; } = true;
        public bool EidTakbeerEnabled { get; set; } = false;

        // Notice API Caching
        public string LastNoticeFetchTime { get; set; } = "";
        public string CachedNoticeResponseJson { get; set; } = "";
        public string NoticeLastShownVersion { get; set; } = "";

        // Font Scale (0.75 = smaller, 1.0 = default, 1.5 = larger)
        public double FontScale { get; set; } = 1.0;

        // Auto Backup Settings
        public string AutoBackupSchedule { get; set; } = "NONE"; // "NONE", "DAILY", "WEEKLY", "MONTHLY"
        public string AutoBackupLocation { get; set; } = "";
        public string LastAutoBackupDate { get; set; } = "";

        // Cloud Sync
        public bool CloudSyncEnabled { get; set; } = false;
        public string? FirebaseUid { get; set; }
        public string? FirebaseEmail { get; set; }
        public string? FirebaseDisplayName { get; set; }
        public string LastSyncAt { get; set; } = "";

        // Cycle Tracker
        public bool CycleEnabled { get; set; } = false;
        public string SelectedCycleMadhab { get; set; } = "Sistani";

        // Leaderboard
        public bool LeaderboardAnonymous { get; set; } = false;
        public string LastHallOfFameMonth { get; set; } = "";
    }

    public static class SettingsManager
    {
        private static string AppDataFolder => StorageService.GetAppDataPath();
        private static string SettingsFile => Path.Combine(AppDataFolder, "settings.json");

        public static AppSettings Current { get; private set; } = new AppSettings();

        public static void Load()
        {
            if (File.Exists(SettingsFile))
            {
                try
                {
                    string json = File.ReadAllText(SettingsFile);
                    var settings = JsonConvert.DeserializeObject<AppSettings>(json);
                    if (settings != null)
                    {
                        Current = settings;
                        // Initialize local defaults if empty
                        InitializeLocalAdhanDefaults();
                    }
                }
                catch { /* Ignore and use defaults */ }
            }
            else
            {
                // No settings file, set initial defaults
                InitializeLocalAdhanDefaults();
            }
        }

        private static void InitializeLocalAdhanDefaults()
        {
            string baseDir = AppDomain.CurrentDomain.BaseDirectory;
            string adhanDir = Path.Combine(baseDir, "Assets", "Adhan");

            if (string.IsNullOrEmpty(Current.AdhanSoundPath))
            {
                string makkah = Path.Combine(adhanDir, "Athan_Makkah.mp3");
                if (File.Exists(makkah)) Current.AdhanSoundPath = makkah;
            }

            if (string.IsNullOrEmpty(Current.FajrAdhanSoundPath))
            {
                string fajr = Path.Combine(adhanDir, "Athan_Al-fajer_-_Malek_chebae.mp3");
                if (File.Exists(fajr)) Current.FajrAdhanSoundPath = fajr;
            }

            if (string.IsNullOrEmpty(Current.TahajjudAdhanSoundPath))
            {
                string makkah = Path.Combine(adhanDir, "Athan_Makkah.mp3");
                if (File.Exists(makkah)) Current.TahajjudAdhanSoundPath = makkah;
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

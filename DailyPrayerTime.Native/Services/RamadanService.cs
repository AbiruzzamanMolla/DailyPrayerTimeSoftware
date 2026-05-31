using System;
using System.Collections.Generic;
using System.Globalization;
using System.IO;
using Newtonsoft.Json;

namespace DailyPrayerTime.Native.Services
{
    public class DuaItem
    {
        public string Arabic { get; set; } = "";
        public string Transliteration { get; set; } = "";
        public string Translation { get; set; } = "";
    }

    public class RamadanState
    {
        public int Year { get; set; }
        public Dictionary<string, bool> PrepChecklist { get; set; } = new();
        public Dictionary<int, string> DailyGoals { get; set; } = new();
        public Dictionary<int, bool> DailyGoalComplete { get; set; } = new();
        public Dictionary<int, bool> LaylatulQadrNights { get; set; } = new();
    }

    public static class RamadanData
    {
        public static List<string> PrepChecklistItems => new()
        {
            "Ramadan_Prep_Intention",
            "Ramadan_Prep_Quran",
            "Ramadan_Prep_Schedule",
            "Ramadan_Prep_Charity",
            "Ramadan_Prep_Duas",
            "Ramadan_Prep_Supplies",
            "Ramadan_Prep_Family",
        };

        public static List<DuaItem> DailyDuas => new()
        {
            new() { Arabic = "رَبَّنَا آتِنَا فِي الدُّنْيَا حَسَنَةً وَفِي الْآخِرَةِ حَسَنَةً وَقِنَا عَذَابَ النَّارِ", Transliteration = "Rabbana atina fid-dunya hasanatan wa fil-akhirati hasanatan waqina azaban-nar", Translation = "Our Lord, give us good in this world and good in the Hereafter, and protect us from the torment of the Fire." },
            new() { Arabic = "اللَّهُمَّ إِنِّي أَسْأَلُكَ الْهُدَى وَالتُّقَى وَالْعَفَافَ وَالْغِنَى", Transliteration = "Allahumma inni as'alukal-huda wat-tuqa wal-'afafa wal-ghina", Translation = "O Allah, I ask You for guidance, piety, chastity, and self-sufficiency." },
            new() { Arabic = "اللَّهُمَّ إِنِّي أَسْأَلُكَ عِلْماً نَافِعاً وَرِزْقاً طَيِّباً وَعَمَلاً مُتَقَبَّلاً", Transliteration = "Allahumma inni as'aluka 'ilman nafi'an wa rizqan tayyiban wa 'amalan mutaqabbalan", Translation = "O Allah, I ask You for beneficial knowledge, good provision, and accepted deeds." },
            new() { Arabic = "اللَّهُمَّ أَعِنِّي عَلَى ذِكْرِكَ وَشُكْرِكَ وَحُسْنِ عِبَادَتِكَ", Transliteration = "Allahumma a'innee 'ala dhikrika wa shukrika wa husni 'ibadatik", Translation = "O Allah, help me to remember You, thank You, and worship You well." },
            new() { Arabic = "رَبَّنَا اغْفِرْ لِي وَلِوَالِدَيَّ وَلِلْمُؤْمِنِينَ يَوْمَ يَقُومُ الْحِسَابُ", Transliteration = "Rabbana-ghfir li wa li-walidayya wa lil-mu'minina yawma yaqumul-hisab", Translation = "Our Lord, forgive me and my parents and the believers on the Day of Reckoning." },
            new() { Arabic = "اللَّهُمَّ إِنَّكَ عَفُوٌّ تُحِبُّ الْعَفْوَ فَاعْفُ عَنِّي", Transliteration = "Allahumma innaka 'afuwwun tuhibbul-'afwa fa'fu 'anni", Translation = "O Allah, You are Forgiving and love forgiveness, so forgive me." },
            new() { Arabic = "رَبِّ اشْرَحْ لِي صَدْرِي وَيَسِّرْ لِي أَمْرِي", Transliteration = "Rabbi-shrah li sadri wa yassir li amri", Translation = "My Lord, expand my chest and make my task easy." },
            new() { Arabic = "اللَّهُمَّ اجْعَلْنِي مِنَ التَّوَّابِينَ وَاجْعَلْنِي مِنَ الْمُتَطَهِّرِينَ", Transliteration = "Allahumma-j'alni minat-tawwabina wa-j'alni minal-mutatahhirin", Translation = "O Allah, make me among those who repent and purify themselves." },
            new() { Arabic = "رَبَّنَا هَبْ لَنَا مِنْ أَزْوَاجِنَا وَذُرِّيَّاتِنَا قُرَّةَ أَعْيُنٍ وَاجْعَلْنَا لِلْمُتَّقِينَ إِمَاماً", Transliteration = "Rabbana hab lana min azwajina wa dhurriyyatina qurrata a'yunin wa-j'alna lil-muttaqina imama", Translation = "Our Lord, grant us spouses and offspring who will be the comfort of our eyes, and make us leaders of the righteous." },
            new() { Arabic = "اللَّهُمَّ لاَ تُؤَاخِذْنِي بِمَا فَعَلَ السُّفَهَاءُ مِنِّي", Transliteration = "Allahumma la tu'akhidhni bima fa'ala as-sufahaa'u minni", Translation = "O Allah, do not hold me accountable for what the foolish among me have done." },
        };

        public static DuaItem GetDuaForDay(int day)
        {
            int index = (day - 1) % DailyDuas.Count;
            return DailyDuas[index];
        }

        public static int GetCurrentRamadanDay(DateTime gregorianDate)
        {
            try
            {
                var umAlQura = new UmAlQuraCalendar();
                int hijriYear = umAlQura.GetYear(gregorianDate);
                int hijriMonth = umAlQura.GetMonth(gregorianDate);
                int hijriDay = umAlQura.GetDayOfMonth(gregorianDate);

                if (hijriMonth == 9) return hijriDay;
                return -1;
            }
            catch
            {
                return -1;
            }
        }

        public static bool IsEid(DateTime gregorianDate)
        {
            try
            {
                var umAlQura = new UmAlQuraCalendar();
                int hijriMonth = umAlQura.GetMonth(gregorianDate);
                int hijriDay = umAlQura.GetDayOfMonth(gregorianDate);
                return (hijriMonth == 10 && hijriDay == 1);
            }
            catch
            {
                return false;
            }
        }

        public static DateTime? GetEidDate(DateTime gregorianDate)
        {
            try
            {
                var umAlQura = new UmAlQuraCalendar();
                int hijriYear = umAlQura.GetYear(gregorianDate);
                int hijriMonth = umAlQura.GetMonth(gregorianDate);

                if (hijriMonth < 9)
                    return umAlQura.ToDateTime(hijriYear, 10, 1, 0, 0, 0, 0);
                else
                    return umAlQura.ToDateTime(hijriYear + 1, 10, 1, 0, 0, 0, 0);
            }
            catch
            {
                return null;
            }
        }
    }

    public class RamadanService
    {
        private static readonly Lazy<RamadanService> _instance = new(() => new());
        public static RamadanService Instance => _instance.Value;

        private string GetFilePath()
        {
            return Path.Combine(StorageService.GetAppDataPath(), "ramadan_state.json");
        }

        public RamadanState LoadState()
        {
            var path = GetFilePath();
            if (!File.Exists(path))
                return new RamadanState { Year = DateTime.Today.Year };

            try
            {
                var json = File.ReadAllText(path);
                return JsonConvert.DeserializeObject<RamadanState>(json) ?? new RamadanState { Year = DateTime.Today.Year };
            }
            catch
            {
                return new RamadanState { Year = DateTime.Today.Year };
            }
        }

        public void SaveState(RamadanState state)
        {
            var path = GetFilePath();
            var dir = Path.GetDirectoryName(path);
            if (!string.IsNullOrEmpty(dir) && !Directory.Exists(dir))
                Directory.CreateDirectory(dir);
            state.Year = DateTime.Today.Year;
            File.WriteAllText(path, JsonConvert.SerializeObject(state, Formatting.Indented));
            // Push to cloud
            _ = CloudSyncService.Instance.PushRamadanAsync(state);
        }
    }
}

using System;
using System.ComponentModel;
using System.Runtime.CompilerServices;
using System.Threading.Tasks;
using System.Timers;
using DailyPrayerTime.Desktop.Services;
using DailyPrayerTime.Shared.Services;

namespace DailyPrayerTime.Desktop.ViewModels;

public class MainWindowViewModel : INotifyPropertyChanged
{
    public event PropertyChangedEventHandler? PropertyChanged;

    private Timer? _timer;
    private CombinedPrayerTimes? _prayerTimes;
    private RamadanState? _ramadanState;
    private int _ramadanDay = -1;

    private string _lastNotifiedPrayer = "";
    private string _statusText = "Initializing...";
    private string _qiblaBearingText = "--°";
    private double _qiblaAngle;
    private string _qiblaDirection = "";
    private string _qiblaLocation = "";
    private string _currentPrayer = "--";
    private string _countdown = "--:--:--";
    private string _nextPrayer = "Loading...";
    private string _hijriDisplay = "";
    private string _gregorianDisplay = "";
    private string _ramadanStatus = "Not in Ramadan";
    private int _ramadanDayProp;
    private string _ramadanDayDisplay = "";
    private string _eidCountdown = "";
    private string _latitude = "23.8103";
    private string _longitude = "90.4125";
    private int _methodIndex = 0;
    private string _ramadanDuaArabic = "";
    private string _ramadanDuaTrans = "";
    private string _ramadanDuaTranslation = "";

    public string StatusText { get => _statusText; set => Set(ref _statusText, value); }
    public string QiblaBearingText { get => _qiblaBearingText; set => Set(ref _qiblaBearingText, value); }
    public double QiblaAngle { get => _qiblaAngle; set => Set(ref _qiblaAngle, value); }
    public string QiblaDirection { get => _qiblaDirection; set => Set(ref _qiblaDirection, value); }
    public string QiblaLocation { get => _qiblaLocation; set => Set(ref _qiblaLocation, value); }
    public string CurrentPrayer { get => _currentPrayer; set => Set(ref _currentPrayer, value); }
    public string Countdown { get => _countdown; set => Set(ref _countdown, value); }
    public string NextPrayer { get => _nextPrayer; set => Set(ref _nextPrayer, value); }
    public string HijriDisplay { get => _hijriDisplay; set => Set(ref _hijriDisplay, value); }
    public string GregorianDisplay { get => _gregorianDisplay; set => Set(ref _gregorianDisplay, value); }
    public string RamadanStatus { get => _ramadanStatus; set => Set(ref _ramadanStatus, value); }
    public int RamadanDay { get => _ramadanDayProp; set => Set(ref _ramadanDayProp, value); }
    public string RamadanDayDisplay { get => _ramadanDayDisplay; set => Set(ref _ramadanDayDisplay, value); }
    public string EidCountdown { get => _eidCountdown; set => Set(ref _eidCountdown, value); }
    public string Latitude { get => _latitude; set => Set(ref _latitude, value); }
    public string Longitude { get => _longitude; set => Set(ref _longitude, value); }
    public int MethodIndex { get => _methodIndex; set => Set(ref _methodIndex, value); }
    public string RamadanDuaArabic { get => _ramadanDuaArabic; set => Set(ref _ramadanDuaArabic, value); }
    public string RamadanDuaTrans { get => _ramadanDuaTrans; set => Set(ref _ramadanDuaTrans, value); }
    public string RamadanDuaTranslation { get => _ramadanDuaTranslation; set => Set(ref _ramadanDuaTranslation, value); }

    public MainWindowViewModel()
    {
        double lat = 23.8103, lon = 90.4125;
        double bearing = QiblaCalculator.CalculateDirection(lat, lon);
        QiblaBearingText = $"{bearing:F1}°";
        QiblaAngle = bearing;
        QiblaDirection = $"Direction: {GetCompassDirection(bearing)}";
        QiblaLocation = "From Dhaka, Bangladesh";

        LoadRamadanState();
        _ = InitPrayerTimes();
    }

    private void LoadRamadanState()
    {
        _ramadanState = RamadanService.Instance.LoadState();
        _ramadanDay = HijriDateHelper.GetRamadanDay(DateTime.Today);
        UpdateRamadanUI();
    }

    private void UpdateRamadanUI()
    {
        if (_ramadanDay > 0)
        {
            RamadanStatus = $"Ramadan Day {_ramadanDay}";
            RamadanDay = _ramadanDay;
            RamadanDayDisplay = $"{_ramadanDay}/30";
        }
        else
        {
            RamadanStatus = "Not in Ramadan";
            RamadanDay = 0;
            RamadanDayDisplay = "--";
        }

        var eidDate = RamadanData.GetEidDate(DateTime.Today);
        if (eidDate.HasValue)
        {
            int diff = (eidDate.Value.Date - DateTime.Today.Date).Days;
            EidCountdown = diff switch
            {
                0 => "Eid Mubarak! Today is Eid! 🎉",
                1 => "Eid is tomorrow!",
                > 1 => $"{diff} days until Eid",
                _ => ""
            };
        }

        int duaDay = _ramadanDay > 0 ? _ramadanDay : DateTime.Today.Day % 10 + 1;
        var (ar, trans, transl) = GetDuaForDay(duaDay);
        RamadanDuaArabic = ar;
        RamadanDuaTrans = trans;
        RamadanDuaTranslation = transl;
    }

    private static (string ar, string trans, string transl) GetDuaForDay(int day)
    {
        var duas = new[]
        {
            ("رَبَّنَا آتِنَا فِي الدُّنْيَا حَسَنَةً", "Rabbana atina fid-dunya hasanah", "Our Lord, give us good in this world"),
            ("اللَّهُمَّ إِنِّي أَسْأَلُكَ الْهُدَى وَالتُّقَى", "Allahumma inni as'alukal-huda", "O Allah, I ask You for guidance"),
            ("اللَّهُمَّ إِنَّكَ عَفُوٌّ تُحِبُّ الْعَفْوَ فَاعْفُ عَنِّي", "Allahumma innaka 'afuwwun tuhibbul-'afwa", "O Allah, You are Forgiving, love forgiveness"),
            ("رَبَّنَا اغْفِرْ لِي وَلِوَالِدَيَّ", "Rabbana-ghfir li wa li-walidayya", "Our Lord, forgive me and my parents"),
            ("رَبِّ اشْرَحْ لِي صَدْرِي وَيَسِّرْ لِي أَمْرِي", "Rabbi-shrah li sadri", "My Lord, expand my chest"),
        };
        var (ar, trans, transl) = duas[(day - 1) % duas.Length];
        return (ar, trans, transl);
    }

    private async Task InitPrayerTimes()
    {
        try
        {
            _prayerTimes = await PrayerService.GetPrayerTimesAsync(23.8103, 90.4125, "KARACHI", 1, useApi: false);

            _timer = new Timer(1000);
            _timer.Elapsed += (s, e) => Avalonia.Threading.Dispatcher.UIThread.Post(Tick);
            _timer.AutoReset = true;
            _timer.Start();
            Tick();
            StatusText = "Running on Avalonia";
        }
        catch (Exception ex)
        {
            StatusText = $"Error: {ex.Message}";
        }
    }

    private void Tick()
    {
        if (_prayerTimes == null) return;

        var now = DateTime.Now;
        string cur = _prayerTimes.CurrentPrayerName(now);
        string next = _prayerTimes.NextPrayerName(now);
        DateTime nextT = _prayerTimes.NextPrayerTime(now);

        TimeSpan diff = nextT - now;
        string count = diff.TotalSeconds > 0
            ? $"{(int)diff.TotalHours:D2}:{diff.Minutes:D2}:{diff.Seconds:D2}"
            : "00:00:00";

        string timeFmt = "hh:mm tt";
        string nextStr = $"\u25b8 {next} at {nextT.ToString(timeFmt)}";

        CurrentPrayer = cur;
        Countdown = count;
        NextPrayer = nextStr;

        if (cur != _lastNotifiedPrayer && cur != "Sunrise" && cur != "--")
        {
            _lastNotifiedPrayer = cur;
            try { LinuxNotificationService.Show("Prayer Time", $"{cur} has begun."); }
            catch { }
        }

        var (hYear, hMonth, hDay) = HijriDateHelper.ToHijri(now);
        string[] hMonths = { "Muharram", "Safar", "Rabi' al-Awwal", "Rabi' al-Thani", "Jumada al-Awwal", "Jumada al-Thani", "Rajab", "Sha'ban", "Ramadan", "Shawwal", "Dhu al-Qi'dah", "Dhu al-Hijjah" };
        HijriDisplay = $"{hDay} {hMonths[hMonth - 1]} {hYear} AH";
        GregorianDisplay = now.ToString("dddd, MMMM d, yyyy");
    }

    public void ApplySettings()
    {
        if (double.TryParse(Latitude, out double lat) && double.TryParse(Longitude, out double lon))
        {
            double bearing = QiblaCalculator.CalculateDirection(lat, lon);
            QiblaBearingText = $"{bearing:F1}°";
            QiblaAngle = bearing;
            QiblaDirection = $"Direction: {GetCompassDirection(bearing)}";

            string[] methods = { "KARACHI", "MWL", "MAKKAH", "ISNA", "EGYPT" };
            string method = methods.Length > MethodIndex ? methods[MethodIndex] : "MWL";

            _prayerTimes = PrayerService.GetPrayerTimesAsync(lat, lon, method, 1, useApi: false).Result;
        }
    }

    private static string GetCompassDirection(double bearing)
    {
        string[] dirs = { "N", "NNE", "NE", "ENE", "E", "ESE", "SE", "SSE",
                          "S", "SSW", "SW", "WSW", "W", "WNW", "NW", "NNW" };
        return dirs[(int)Math.Round(bearing / 22.5) % 16];
    }

    private void Set<T>(ref T field, T value, [CallerMemberName] string? name = null)
    {
        if (!Equals(field, value))
        {
            field = value;
            PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(name ?? ""));
        }
    }

    protected void OnPropertyChanged([CallerMemberName] string? name = null) =>
        PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(name ?? ""));
}

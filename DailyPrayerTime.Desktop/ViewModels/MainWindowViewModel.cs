using System;
using System.ComponentModel;
using System.Runtime.CompilerServices;
using System.Threading.Tasks;
using System.Timers;
using DailyPrayerTime.Shared.Services;

namespace DailyPrayerTime.Desktop.ViewModels;

public class MainWindowViewModel : INotifyPropertyChanged
{
    public event PropertyChangedEventHandler? PropertyChanged;

    private Timer? _timer;
    private CombinedPrayerTimes? _prayerTimes;

    private string _statusText = "Initializing...";
    private string _qiblaBearingText = "--°";
    private string _qiblaDirection = "";
    private string _qiblaLocation = "";
    private string _currentPrayer = "--";
    private string _countdown = "--:--:--";
    private string _nextPrayer = "Loading...";
    private string _tCount = "0";
    private string _tPhrase = "Subhaanallaah";

    public string StatusText { get => _statusText; set => Set(ref _statusText, value); }
    public string QiblaBearingText { get => _qiblaBearingText; set => Set(ref _qiblaBearingText, value); }
    public string QiblaDirection { get => _qiblaDirection; set => Set(ref _qiblaDirection, value); }
    public string QiblaLocation { get => _qiblaLocation; set => Set(ref _qiblaLocation, value); }
    public string CurrentPrayer { get => _currentPrayer; set => Set(ref _currentPrayer, value); }
    public string Countdown { get => _countdown; set => Set(ref _countdown, value); }
    public string NextPrayer { get => _nextPrayer; set => Set(ref _nextPrayer, value); }
    public string TCount { get => _tCount; set => Set(ref _tCount, value); }
    public string TPhrase { get => _tPhrase; set => Set(ref _tPhrase, value); }

    public MainWindowViewModel()
    {
        double bearing = QiblaCalculator.CalculateDirection(23.8103, 90.4125);
        QiblaBearingText = $"{bearing:F1}°";
        QiblaDirection = $"Direction: {GetCompassDirection(bearing)}";
        QiblaLocation = "From Dhaka, Bangladesh";
    }

    public async Task InitializeAsync()
    {
        try
        {
            _prayerTimes = await PrayerService.GetPrayerTimesAsync(
                23.8103, 90.4125, "KARACHI", 1,
                useApi: false);

            _timer = new Timer(1000);
            _timer.Elapsed += (s, e) => UpdateTick();
            _timer.AutoReset = true;
            _timer.Start();

            UpdateTick();
            StatusText = "Running on Avalonia (Linux/macOS) ✓";
        }
        catch (Exception ex)
        {
            StatusText = $"Error: {ex.Message}";
        }
    }

    private void UpdateTick()
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
        string nextStr = $"{next} {nextT.ToString(timeFmt)}";

        CurrentPrayer = cur;
        Countdown = count;
        NextPrayer = nextStr;
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

using System;
using System.ComponentModel;
using System.Runtime.CompilerServices;
using DailyPrayerTime.Shared.Services;

namespace DailyPrayerTime.Desktop.ViewModels;

public class MainWindowViewModel : INotifyPropertyChanged
{
    public event PropertyChangedEventHandler? PropertyChanged;

    private string _statusText = "Loading...";
    private double _qiblaBearing;
    private string _qiblaDirection = "";
    private string _qiblaLocation = "";
    private string _currentPrayer = "Fajr";
    private string _countdown = "04:32:15";
    private string _nextPrayer = "Next: Dhuhr 12:15 PM";

    public string StatusText { get => _statusText; set => Set(ref _statusText, value); }
    public double QiblaBearing { get => _qiblaBearing; set { Set(ref _qiblaBearing, value); OnPropertyChanged(nameof(QiblaBearingText)); } }
    public string QiblaBearingText => $"{QiblaBearing:F1}°";
    public string QiblaDirection { get => _qiblaDirection; set => Set(ref _qiblaDirection, value); }
    public string QiblaLocation { get => _qiblaLocation; set => Set(ref _qiblaLocation, value); }
    public string CurrentPrayer { get => _currentPrayer; set => Set(ref _currentPrayer, value); }
    public string Countdown { get => _countdown; set => Set(ref _countdown, value); }
    public string NextPrayer { get => _nextPrayer; set => Set(ref _nextPrayer, value); }

    public MainWindowViewModel()
    {
        // Calculate Qibla for Dhaka as default demo
        double bearing = QiblaCalculator.CalculateDirection(23.8103, 90.4125);
        QiblaBearing = bearing;
        QiblaDirection = $"Direction: {GetCompassDirection(bearing)}";
        QiblaLocation = "From Dhaka, Bangladesh";
        StatusText = "Cross-platform build — Linux/macOS";
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

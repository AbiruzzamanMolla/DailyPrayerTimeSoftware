using Avalonia;
using Avalonia.Controls;
using Avalonia.Controls.ApplicationLifetimes;
using Avalonia.Markup.Xaml;
using Avalonia.Media;
using DailyPrayerTime.Desktop.Services;
using DailyPrayerTime.Desktop.ViewModels;
using DailyPrayerTime.Desktop.Views;
using DailyPrayerTime.Shared.Services;

namespace DailyPrayerTime.Desktop;

public partial class App : Application
{
    private MainWindowViewModel? _vm;
    private MainWindow? _window;
    private TrayIcon? _trayIcon;

    public override void Initialize()
    {
        AvaloniaXamlLoader.Load(this);
    }

    public override void OnFrameworkInitializationCompleted()
    {
        var storage = new LinuxStorageService();
        string appData = storage.GetAppDataPath();

        TasbihService.Instance.BasePath = appData;
        PrayerService.CacheBasePath = appData;
        TrackerService.Instance.BasePath = appData;
        RamadanService.Instance.BasePath = appData;

        _vm = new MainWindowViewModel();
        _vm.PropertyChanged += (s, e) =>
        {
            if (_trayIcon != null && (e.PropertyName == "CurrentPrayer" || e.PropertyName == "Countdown"))
                _trayIcon.ToolTipText = $"{_vm.CurrentPrayer}: {_vm.Countdown}";
        };

        _window = new MainWindow { DataContext = _vm };

        SetupTrayIcon();

        if (ApplicationLifetime is IClassicDesktopStyleApplicationLifetime desktop)
        {
            desktop.MainWindow = _window;

            desktop.Startup += (s, e) =>
            {
                _window.Show();
            };
        }

        base.OnFrameworkInitializationCompleted();
    }

    private void SetupTrayIcon()
    {
        _trayIcon = new TrayIcon
        {
            ToolTipText = "Daily Prayer Timer - Loading...",
            Icon = new WindowIcon("Assets/avalonia-logo.ico")
        };

        var menu = new NativeMenu();

        var showItem = new NativeMenuItem("Show/Hide");
        showItem.Click += (s, e) =>
        {
            if (_window == null) return;
            if (_window.IsVisible) _window.Hide();
            else { _window.Show(); _window.Activate(); }
        };
        menu.Add(showItem);

        menu.Add(new NativeMenuItemSeparator());

        var prayItem = new NativeMenuItem("Prayer Times") { IsEnabled = false };
        menu.Add(prayItem);

        menu.Add(new NativeMenuItemSeparator());

        var quitItem = new NativeMenuItem("Quit");
        quitItem.Click += (s, e) => { if (ApplicationLifetime is IClassicDesktopStyleApplicationLifetime lifetime) lifetime.Shutdown(); };
        menu.Add(quitItem);

        _trayIcon.Menu = menu;
    }
}

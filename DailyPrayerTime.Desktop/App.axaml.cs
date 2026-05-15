using Avalonia;
using Avalonia.Controls.ApplicationLifetimes;
using Avalonia.Markup.Xaml;
using DailyPrayerTime.Desktop.Services;
using DailyPrayerTime.Desktop.ViewModels;
using DailyPrayerTime.Desktop.Views;
using DailyPrayerTime.Shared.Services;

namespace DailyPrayerTime.Desktop;

public partial class App : Application
{
    public override void Initialize()
    {
        AvaloniaXamlLoader.Load(this);
    }

    public override void OnFrameworkInitializationCompleted()
    {
        var storage = new LinuxStorageService();
        TasbihService.Instance.BasePath = storage.GetAppDataPath();

        if (ApplicationLifetime is IClassicDesktopStyleApplicationLifetime desktop)
        {
            desktop.MainWindow = new MainWindow
            {
                DataContext = new MainWindowViewModel(),
            };
        }

        base.OnFrameworkInitializationCompleted();
    }
}
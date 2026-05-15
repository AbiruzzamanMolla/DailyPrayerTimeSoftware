# Daily Prayer Timer — Cross-Platform Build

Branch: `linux-android-maui`

## Quick Start

```bash
# Build and run on Linux/macOS
dotnet build DailyPrayerTime.Desktop
dotnet run --project DailyPrayerTime.Desktop
```

## Project Structure

```
DailyPrayerTime/
├── DailyPrayerTime.Shared/         ← .NET 8 shared library (zero external deps)
│   ├── Services/                   ← QiblaCalculator, PrayerCalculator, PrayerService,
│   │                                  TasbihService, TrackerService, RamadanService,
│   │                                  HijriDateHelper, RakatParser, IStorageService
│   └── Models/                     ← TrackerModels, AppSettings
├── DailyPrayerTime.Native/         ← Windows WPF (v2.4.0, unchanged)
├── DailyPrayerTime.Desktop/        ← Avalonia UI (Linux/macOS/Windows)
│   ├── Views/MainWindow.axaml      ← 7-tab layout
│   ├── ViewModels/                 ← MVVM with live countdown
│   ├── Styles/Theme.axaml          ← glassmorphism card styles
│   ├── Services/                   ← LinuxStorageService, Localization, LinuxNotificationService
│   ├── i18n/                       ← en.json, bn.json, duas.json
│   └── DailyPrayerTime.Desktop.csproj
└── DailyPrayerTime.CrossPlatform.slnx
```

## Feature Status

### Fully Ported ✅

| Feature | Description |
|---------|-------------|
| Prayer Times | AlAdhan API + local calculation, 22 methods |
| Live Countdown | Real-time prayer countdown on Home tab |
| Qibla Compass | Visual compass with rotating arrow, bearing readout |
| Digital Tasbih | 5 dhikr phrases, tap/click counter, daily save |
| Dua Library | 27 after-salaam duas with Arabic/transliteration/translation |
| Ramadan Module | Day counter, progress bar, daily dua, Eid countdown |
| Deed Tracker | Daily checkboxes, week summary, Sawm toggle |
| Hijri Calendar | Pure C# calculation (no Windows dependency) |
| Settings | Location, 22 calculation methods, notification toggle |
| Language | English/Bangla (বাংলা) i18n with toggle |
| System Tray | Icon with prayer info, show/hide, quit |
| Notifications | Prayer time alerts via notify-send |
| Backup/Restore | Tracker data via ZIP file dialogs |
| Accent Colors | 6-color theme picker |
| Tahajjud Display | Last third of night window |
| Prohibited Times | Sunrise/Zawal/Sunset alerts |
| Smart Fasting | Monday/Thursday, Ayyam al-Bidh detection |
| Advanced Settings | Fajr/Isha angles, high latitude rules, offsets |
| Time Format | 12h or 24h display |
| Calculation Methods | 22 methods including Shia, Turkey, Russia |

### Not Ported (Windows-specific or Future) ⏳

| Feature | Reason |
|---------|--------|
| Enhanced Taskbar Timer | Windows-only → replaced by system tray |
| Floating Overlay | Windows-only → replaced by system tray |
| COM DeskBand | Windows-only COM API |
| Adhan Audio | Needs cross-platform audio library (ffmpeg) |
| Full-Screen Mode | Window manager handles this on Linux |
| Zen Mode | Low priority |
| Granular Notification Controls | Low priority |

## Building for Different Linux Distributions

### Debian/Ubuntu (.deb)
```bash
# Install .NET 8 SDK for Linux
wget https://dot.net/v1/dotnet-install.sh
chmod +x dotnet-install.sh
./dotnet-install.sh --channel 8.0

# Build
dotnet publish DailyPrayerTime.Desktop -c Release -r linux-x64 --self-contained

# The output is in bin/Release/net8.0/linux-x64/publish/
```

### AppImage
Use `dotnet publish` with `linux-x64` runtime, then package the output with `appimagetool`.

## Branch History

```
62054a0 - Tahajjud display + 22 calculation methods
21b3cf3 - Smart fasting detection
03bc0da - i18n localization (en/bn)
93971da - Backup/restore + accent colors
108e7c5 - System tray + notifications
6957e85 - Tracker + About tabs
570d951 - Duas + notification wiring
c200d37 - Ramadan + settings + Hijri
e5707fd - Complete Avalonia UI
5285ae3 - Prayer calculator (pure C#)
a1dd79c - Port shared services
41d5ddf - Initial scaffold
```

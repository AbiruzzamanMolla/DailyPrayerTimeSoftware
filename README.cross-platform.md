# Daily Prayer Timer — Cross-Platform Build

Branch: `linux-android-maui`

## Structure

```
DailyPrayerTime/
├── DailyPrayerTime.Shared/         ← .NET 8 shared library (zero external deps)
│   ├── Services/
│   │   ├── QiblaCalculator.cs      ← pure math Qibla direction
│   │   ├── PrayerCalculator.cs     ← pure C# prayer times (no library needed)
│   │   ├── PrayerService.cs        ← AlAdhan API + local calc
│   │   ├── TasbihService.cs        ← JSON dhikr counter
│   │   ├── TrackerService.cs       ← daily deed tracker
│   │   ├── RamadanService.cs       ← Ramadan state + duas
│   │   ├── HijriDateHelper.cs      ← pure C# Hijri calendar (no UmAlQura)
│   │   ├── RakatParser.cs          ← rakat string parser
│   │   └── IStorageService.cs      ← platform abstraction
│   └── Models/
│       ├── TrackerModels.cs        ← DeedEntry, DailyDeeds
│       └── AppSettings.cs          ← settings model
├── DailyPrayerTime.Native/         ← Windows WPF (existing, v2.4.0)
├── DailyPrayerTime.Desktop/        ← Avalonia UI (Linux/macOS/Windows)
│   ├── Views/MainWindow.axaml      ← 5-tab layout
│   ├── ViewModels/                 ← MVVM with live countdown
│   ├── Styles/Theme.axaml          ← glassmorphism card styles
│   ├── Services/                   ← LinuxStorageService, LinuxNotificationService
│   ├── i18n/duas.json              ← 27 after-salaam duas
│   └── DailyPrayerTime.Desktop.csproj
└── DailyPrayerTime.CrossPlatform.slnx
```

## Porting Status

| Feature | Shared | Avalonia UI |
|---------|--------|-------------|
| Qibla Compass | ✅ | ✅ Visual compass with arrow |
| Digital Tasbih | ✅ | ✅ 5 phrases, count, save |
| Prayer Times | ✅ | ✅ Live countdown |
| Hijri Calendar | ✅ | ✅ Header display |
| Ramadan Module | ✅ | ✅ Countdown, dua, Eid |
| Deed Tracker | ✅ | ❌ Not yet |
| Dua Library | ✅ (27 duas) | ❌ Not yet |
| Settings | ❌ | ⚠️ Basic |
| Notifications | ❌ | ⚠️ Service class only |
| Adhan Audio | ❌ | ❌ |

## Build & Run

```bash
# Restore and build
dotnet restore DailyPrayerTime.CrossPlatform.slnx
dotnet build DailyPrayerTime.Desktop

# Run on Linux/macOS
dotnet run --project DailyPrayerTime.Desktop
```

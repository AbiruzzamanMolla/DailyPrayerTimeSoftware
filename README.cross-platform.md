# Daily Prayer Timer — Cross-Platform Build

Branch: `linux-android-maui`

## Structure

```
DailyPrayerTime/
├── DailyPrayerTime.Shared/     ← .NET Standard 2.0 shared logic
│   └── Services/
│       ├── QiblaCalculator.cs  ← pure math, works everywhere
│       ├── IStorageService.cs  ← platform abstraction
│       └── ...                 ← more to come
├── DailyPrayerTime.Native/     ← Windows WPF (existing, unchanged)
├── DailyPrayerTime.Desktop/    ← Avalonia UI (Linux/macOS/Windows)
│   └── Views/                  ← Avalonia .axaml files
│   └── Services/               ← platform-specific implementations
└── DailyPrayerTime.CrossPlatform.slnx
```

## Build status

| Target | Status |
|--------|--------|
| Windows (WPF) | ✅ Working |
| Linux (Avalonia) | 🚧 Scaffold only |
| macOS (Avalonia) | 🚧 Scaffold only |

## Build for Linux

```bash
# Prerequisites: .NET 8 SDK + Avalonia workload
dotnet restore DailyPrayerTime.CrossPlatform.slnx
dotnet build DailyPrayerTime.Desktop
```

## Porting guide

1. Move calculation services (no UI) into `DailyPrayerTime.Shared/Services/`
2. Update namespace to `DailyPrayerTime.Shared.Services`
3. Create platform implementations in each app project's `Services/` folder
4. Rewrite UI layer (WPF ↔ Avalonia — similar XAML, different controls)

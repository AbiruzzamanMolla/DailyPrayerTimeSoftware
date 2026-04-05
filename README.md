# Daily Prayer Timer (Native)

A premium, glassmorphic Windows desktop application for Islamic prayer time tracking with a sleek taskbar-docked overlay.

![Preview](DailyPrayerTime.Native/Resources/AppIcon.ico)

## 🚀 Key Features
- **Taskbar Overlay**: A minimal, non-intrusive widget that docks to your taskbar (DU Meter style).
- **Popup Expansion**: Hover over the overlay to see a smooth vertical "popup" growth with current and next prayer details.
- **Glassmorphism UI**: Modern, premium design with semi-transparent backgrounds and vibrant Islamic Green gradients.
- **Prohibited Time Alerts**: Automatic detection and visual warnings for sunrise, zawal, and sunset periods.
- **Accurate Adhan Logic**: Powered by the Adhan library with support for various calculation methods and Madhabs.
- **Tray Persistence**: Runs in the background with a system tray icon for quick access and settings.

## 🛠️ Tech Stack
- **Framework**: WPF (.NET 8.0)
- **Styling**: Vanilla XAML with custom glassmorphism styles
- **Library**: `Adhan` for prayer time calculations
- **Notifications**: `Microsoft.Toolkit.Uwp.Notifications`

## ⚙️ Configuration
The app saves your preferences in `%APPDATA%\DailyPrayerTimeNative\settings.json`. You can configure:
- **Location**: Search by city name (using LocationIQ API).
- **Methods**: Karachi, UMM_AL_QURA, North America, etc.
- **Madhab**: Standard (Shafi) or Hanafi.
- **Theme**: Custom primary/secondary colors and background gradients.
- **Auto-Start**: Toggle to run on Windows startup.

## 📦 Building from Source
1. Clone the repository.
2. Open in Visual Studio 2022 or use CLI.
3. Run `dotnet publish -c Release -r win-x64 --self-contained`.

## 📜 Dev Info
Developed by **Abiruzzaman Molla**
[GitHub Profile](https://github.com/AbiruzzamanMolla)

---
© 2026 Abiruzzaman Molla. All Rights Reserved.

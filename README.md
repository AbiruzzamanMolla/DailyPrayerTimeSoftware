# Daily Prayer Timer (Native)

Version: 1.6.5

A premium, glassmorphic Windows desktop application for Islamic prayer time tracking with a sleek taskbar-docked overlay. Compatible with **Windows 10** and **Windows 11** (x64 & x86).

## 📸 App Preview

![Daily Prayer Timer Preview](screenshots/preview.gif)

## 🚀 Key Features

- **Taskbar Overlay**: A minimal, non-intrusive widget that docks to your taskbar (DU Meter style).
- **Popup Expansion**: Hover over the overlay to see a smooth vertical "popup" growth with current and next prayer details.
- **Glassmorphism UI**: Modern, premium design with semi-transparent backgrounds and vibrant Islamic Green gradients.
- **Prohibited Time Alerts**: Automatic detection and visual warnings for sunrise, zawal, and sunset periods.
- **Smart Congregation Entry**: Time selectors in Settings are now strictly filtered to each prayer's valid range (e.g., you can't set Fajr Jamaat at 10 AM).
- **Adhan Sound Management**: Integrated downloader for default Adhan audio and built-in "Test Sound" for verification.
- **Premium Themes**: Full control over primary/secondary colors and background gradients with a native color picker.
- **Accurate Prayer Times:** Real-time countdowns for all five daily prayers.
- **Sunnah Fasting Tracker:** Detection and highlighting for Sunnah fast days.
- **Windows Taskbar Extension (DeskBand):** Persistent timer directly in your Windows 10 taskbar (Optional toggle).
- **Floating Overlay Fallback:** Seamless fallback to a stylish floating overlay for Windows 11.
- **Customization:** Support for various calculation methods, madhabs, and offsets.
- **Smart Update System**: Automatic version checks on startup and a manual "Check for Updates" button in Settings.
- **Tray Persistence**: Runs in the background with a system tray icon for quick access and settings.

## 🌐 Extension Versions

Check out other versions of this tool:
- **Chrome Extension**: [Prayer Time on GitHub](https://github.com/AbiruzzamanMolla/PrayerTime)
- **VS Code Extension**: [Prayer Timer (Bangladesh) on Marketplace](https://marketplace.visualstudio.com/items?itemName=azmolla.prayer-timer-bangladesh)

### Taskbar DeskBand (Windows 10 & 11)
On Windows 10, you can enable the Taskbar integration in Settings. This places the prayer timer directly next to your system tray. 
*Note: On Windows 11, this requires third-party taskbar modification tools (like ExplorerPatcher) as Microsoft has hidden native DeskBand support.*

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

## 📦 Installation
The application comes with professional Windows Installers (`.exe`) for both 64-bit and 32-bit systems.

| Platform | Installer |
|---|---|
| **Windows 10/11 (64-bit)** | `DailyPrayerTimer_Setup_v{VERSION}_x64.exe` |
| **Windows 10/11 (32-bit)** | `DailyPrayerTimer_Setup_v{VERSION}_x86.exe` |

1. Download the installer matching your system architecture from the [Latest Release](https://github.com/AbiruzzamanMolla/DailyPrayerTimeSoftware/releases/latest).
2. Run the installer and follow the wizard to create Desktop and Start Menu shortcuts.

Once installed, you can find **Daily Prayer Timer** on your Desktop or by searching in the Windows Start Menu.

## 🛠️ Building from Source

1. Clone the repository.
2. Open in Visual Studio 2022 or use CLI.
3. Build for your target architecture:
   ```powershell
   # 64-bit
   dotnet publish DailyPrayerTime.Native -c Release -r win-x64 --self-contained -p:PublishSingleFile=true
   # 32-bit
   dotnet publish DailyPrayerTime.Native -c Release -r win-x86 --self-contained -p:PublishSingleFile=true
   ```

## 📜 Dev Info

Developed by **Abiruzzaman Molla**
[GitHub Profile](https://github.com/AbiruzzamanMolla)

---

© 2026 Abiruzzaman Molla. All Rights Reserved.

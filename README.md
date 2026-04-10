# Daily Prayer Timer (Native)

Version: 1.8.3

## 🚀 Daily Prayer Timer (Native) - v1.8.3

A high-performance, native Windows application for accurate prayer times, featuring a premium glassmorphic UI and deep taskbar integration.

---

### 🔥 New in v1.8.1: Close to Tray Fix & Immersive UI
We have pushed the boundaries of the Native experience with a top-to-bottom redesign.
- **Custom Navigation**: A premium, titlebar-less design with integrated menu navigation.
- **Zen Mode**: Immersive focus mode that strips away the UI to leave only the beautiful prayer countdown.
- **Hero Card 2.0**: The main prayer card now features live countdown timers and expanded prayer data.
- **Adhan Presets**: Choose from high-quality recorded adhans (Makkah, Madinah, Alafasi) natively.
- **Tahajjud Support**: Dedicated Tahajjud adhan/alarm for the most blessed time of the night.
- **Prayer Notes**: Rakat counts (Fard, Sunnah, Nafl) integrated directly into the prayer list.
- **Portable Stability**: High-reliability process path resolution for portable installations.

---

### ✨ Key Features
*   **Dual Taskbar Modes**: Choose between the **Integrated Taskbar Window** (Windows 11 Native Source) or the legacy **COM DeskBand**.
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
[![Version](https://img.shields.io/badge/version-1.8.3-blue.svg)](https://github.com/AbiruzzamanMolla/DailyPrayerTimeSoftware/releases)

---

<p align="center">
  <img src="DailyPrayerTime.Native/icon.ico" width="80" height="80" alt="App Icon">
  <br>
  <b>Lightweight. Accurate. Native.</b>
  <br>
  Keep track of your daily prayers and Sunnah fasts directly from your Windows desktop.
</p>

---

## ✨ Features

- **Daily Prayer Times:** Accurate calculations based on your location (Latitude/Longitude).
- **Tabbed Settings:** clean, organized interface for Daily Prayer, Layout & Theme, and Support.
- **Jamaat (Congregation) Support:** Set and track fixed Jamaat times for your local Masjid.
- **Integrated Taskbar Timer:** A TrafficMonitor-inspired native taskbar integration for Windows 11.
- **Legacy DeskBand:** Support for Windows 10 and ExplorerPatcher users.
- **Floating Overlay:** A modern, semi-transparent overlay to keep your prayers in view while you work.
- **Sunnah Fasting Tracker:** Automatic detection and alerts for Monday/Thursday, Ayyam al-Bidh, and special Islamic dates.
- **Prohibited Times (Makruh) Alerts:** Visual countdowns for Sunrise, Zawal, and Sunset.
- **Adhan & Alarms:** Customizable sound alerts before Jamaat time.

## 📸 UI Preview

<p align="center">
  <img src="https://github.com/AbiruzzamanMolla/DailyPrayerTimeSoftware/raw/native-rewrite/screenshots/preview.gif" alt="UI Preview" width="100%">
</p>

| Platform | Link |
|---|---|
| **Windows (64-bit Installer)** | [DailyPrayerTimer_Setup_v1.8.3_x64.exe](https://github.com/AbiruzzamanMolla/DailyPrayerTimeSoftware/releases/latest) |
| **Windows (32-bit Installer)** | [DailyPrayerTimer_Setup_v1.8.3_x86.exe](https://github.com/AbiruzzamanMolla/DailyPrayerTimeSoftware/releases/latest) |
| **Windows (64-bit Portable)** | [DailyPrayerTimer_v1.8.3_Portable_x64.exe](https://github.com/AbiruzzamanMolla/DailyPrayerTimeSoftware/releases/latest) |

### 🚀 Portable Version
The portable version is a single-file executable that stores all its settings and data in a `data` subfolder within the application directory. 
- **No Installation Required**: Just download and run.
- **Easy Backup**: Simply copy the application folder to keep your settings.
- **Stealthy**: Does not write to `%APPDATA%` unless manually configured otherwise.

> [!NOTE]
> To manually enable portable mode on any build, create an empty file named `.portable` in the same directory as the executable.

1. Download the installer matching your system from the [Latest Release](https://github.com/AbiruzzamanMolla/DailyPrayerTimeSoftware/releases/latest).
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

---

### 🙏 Acknowledgements
Special thanks to the entire open-source community. This project draws immense inspiration and ideas from numerous developers, and we are grateful for the help and foundation provided by their projects.

A special thanks to the AI community and **Agentic AI coding assistants** for empowering the rapid development, debugging, and continuous refinement of this software.

#### Special Thanks
- **Islamic Audiobook**: [YouTube Channel](https://www.youtube.com/@islamicaudiobook)
- **BB Podium (Book Reviews)**: [YouTube Channel](https://www.youtube.com/@BBPodium)
- **Audiobook Bangla**: [Website](https://audiobookbangla.com)

---

### 📬 Contact & Support
- **Developer**: Abiruzzaman Molla
- **GitHub**: [github.com/AbiruzzamanMolla](https://github.com/AbiruzzamanMolla)
- **Support Me**: [SupportKori](https://www.supportkori.com/abiruzzaman)

---

© 2026 Abiruzzaman Molla. All Rights Reserved.

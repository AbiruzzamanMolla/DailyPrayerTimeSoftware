<p align="center">
  <img src="DailyPrayerTime.Native/icon.ico" width="80" height="80" alt="App Icon">
</p>

<h1 align="center">Daily Prayer Timer (Native)</h1>

<p align="center">
  <b>Lightweight. Accurate. Native.</b><br>
  A high-performance, native Windows application for accurate prayer times, featuring a premium glassmorphic UI, deep taskbar integration, and full multi-language support. Keep track of your daily prayers and Sunnah fasts directly from your Windows desktop.
</p>

<p align="center">
  <a href="https://github.com/AbiruzzamanMolla/DailyPrayerTimeSoftware/releases/latest">
    <img src="https://img.shields.io/github/v/release/AbiruzzamanMolla/DailyPrayerTimeSoftware" alt="Latest Release">
  </a>
  <img src="https://img.shields.io/github/downloads/AbiruzzamanMolla/DailyPrayerTimeSoftware/total?style=for-the-badge&color=brightgreen" alt="Total Downloads">

</p>

---

## 📥 Download & Install

| Method                         | Link / Command                                                                                                          |
| ------------------------------ | ----------------------------------------------------------------------------------------------------------------------- |
| **WinGet (Recommended)**       | `winget install AbiruzzamanMolla.DailyPrayerTimer`                                                                      |
| **Windows (64-bit Installer)** | [DailyPrayerTimer_Setup_v2.3.1_x64.exe](https://github.com/AbiruzzamanMolla/DailyPrayerTimeSoftware/releases/latest)    |
| **Windows (32-bit Installer)** | [DailyPrayerTimer_Setup_v2.3.1_x86.exe](https://github.com/AbiruzzamanMolla/DailyPrayerTimeSoftware/releases/latest)    |
| **Windows (64-bit Portable)**  | [DailyPrayerTimer_v2.3.1_Portable_x64.exe](https://github.com/AbiruzzamanMolla/DailyPrayerTimeSoftware/releases/latest) |

1. **WinGet**: Open any terminal (PowerShell, CMD, or Terminal) and run the command above.
2. **Manual**: Download the installer matching your system.
3. **Run**: Follow the installer wizard to create Desktop and Start Menu shortcuts.
4. **Launch**: Once installed, find **Daily Prayer Timer** on your Desktop or via the Start Menu.

## 📸 UI Preview

<p align="center">
  <img src="https://github.com/AbiruzzamanMolla/DailyPrayerTimeSoftware/raw/main/screenshots/cover.png" alt="UI Preview" width="100%">
</p>

## ✨ Key Features

### 🕌 Prayer Tracking & Alarms
- **Accurate Prayer Times**: Real-time countdowns for all five daily prayers based on your exact location.
- **Jamaat (Congregation) Support**: Set fixed times for your local Masjid; now displays in the Taskbar for better planning.
- **Granular Notification Controls**: Individually enable/disable Adhan, Pre-Adhan reminders, and Jamaat popups.
- **Adhan Sound Management**: Choose from high-quality recorded adhans (Makkah, Madinah, Alafasi) or use any custom MP3/WAV file.
- **Sunnah Fasting Tracker**: Automatic detection and alerts for Monday/Thursday, Ayyam al-Bidh, and special Islamic dates.
- **Precision Night Logic**: Perfected crossover handling for Tahajjud and Islamic Midnight during early morning hours.
- **Prohibited Times (Makruh) Alerts**: Visual countdowns and warnings for Sunrise, Zawal, and Sunset.
- **Manual Calculation Options**: Custom Fajr/Isha angles, High Latitude rules, Suhur/Iftar offsets, and Hijri adjustments.

### 📈 Spiritual Tracker & Analytics
- **Divine Tracker Dashboard**: A comprehensive spiritual progress monitor with bi-directional sync between the Tracker popup and Hero dashboard.
- **In-Depth Analytics**: Visualize your consistency with aggregate progress cards showing Weekly, Monthly, and Yearly statistics.
- **Automated Qadha Tracking**: Smart logic that calculates missed Fard prayers across all periods (Today, Week, Month, Year).
- **Nafal & Sawm Integration**: Progress now tracks Sunnah prayers (Tahajjud, Duha, Awwabin) and includes a Fasting bonus.
- **Interactive Historian Mode**: Past dates are fully interactive without the "upcoming" blur effect for better retrospective tracking.
- **Double-Precision Logic**: Daily completion percentage calculation now uses double-precision math for 100% accuracy.

### 💻 UI & Integration
- **Premium Glassmorphism UI**: Modern design with semi-transparent backgrounds, vibrant Islamic Green gradients, and custom titlebar-less navigation.
- **Enhanced Startup Experience**: Sleek new application loader for a seamless transition from desktop to app.
- **Dual Taskbar Modes**:
  - **Integrated Taskbar Window**: Native, TrafficMonitor-inspired taskbar integration for Windows 11.
  - **Legacy COM DeskBand**: Support for Windows 10 and ExplorerPatcher users.
- **Floating Overlay**: A stylish fallback overlay to keep prayers in view while you work, featuring vertical "popup" expansion.
- **Zen & Full Screen Modes**: Immersive focus modes that strip away UI distractions for a beautiful prayer countdown.
- **Theme Customization**: Full control over primary/secondary colors and background gradients.
- **Smart Update System**: Automatic version checks on startup with manual "Check for Updates" options.

### 🛡️ Data & Reliability
- **Automatic Data Backup**: Scheduled backups to your preferred directory to keep your progress and settings safe.
- **Portable Mode Support**: Runs as a single-file executable with all settings stored in a local `data` subfolder.
- **Tray Persistence**: Runs efficiently in the background with a system tray icon for instant access.

_(See the `CHANGELOG.md` for a full history of updates including v1.8.x features like Custom Offsets, Zen Mode, and more.)_


## 🚀 Portable Version

The portable version is a single-file executable that stores all its settings and data in a `data` subfolder within the application directory.

- **No Installation Required**: Just download and run.
- **Easy Backup**: Simply copy the application folder to keep your settings.
- **Stealthy**: Does not write to `%APPDATA%` unless manually configured otherwise.

> [!NOTE]
> To manually enable portable mode on any installer build, create an empty file named `.portable` in the same directory as the executable.

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

## 🌍 Translation & Localization Guide

Daily Prayer Timer supports multiple languages. We welcome open-source contributions to add more!

### How to Add a New Language

1. **Find Language Files**: Navigate to the `DailyPrayerTime.Native/i18n` directory.
2. **Create New Dictionary**: Copy the default English file (`en.json`) and rename it to your target language code (e.g., `tr.json` for Turkish, `ar.json` for Arabic).
3. **Translate**: Open your new `json` file and translate all the string values on the right side of the colon.
   - **Important**: Do not translate or modify the keys (the strings on the left side of the colon).
   - Ensure formatting placeholders like `{0}` remain intact.
4. **Register Language**: Open `DailyPrayerTime.Native/SettingsWindow.xaml` and locate the `<ComboBox x:Name="LanguageSelector">` section. Add your new language as a `ComboBoxItem` with the `Tag` matching your new json filename (e.g., `Tag="tr"`).
5. **Submit PR**: Test your changes and submit a Pull Request to help the community!

## 🌐 Extension Versions

Check out other versions of this tool:

- **Chrome Extension**: [Prayer Time on GitHub](https://github.com/AbiruzzamanMolla/PrayerTime)
- **VS Code Extension**: [Prayer Timer (Bangladesh) on Marketplace](https://marketplace.visualstudio.com/items?itemName=azmolla.prayer-timer-bangladesh)

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
- **Telegram**: [Join Now](https://t.me/dailyprayertimersoftware)

---

© 2026 Abiruzzaman Molla. All Rights Reserved.

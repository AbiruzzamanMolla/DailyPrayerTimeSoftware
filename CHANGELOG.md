# Changelog

All notable changes to this project will be documented in this file.

The format is based on [Keep a Changelog](https://keepachangelog.com/en/1.0.0/),
and this project adheres to [Semantic Versioning](https://semver.org/spec/v2.0.0.html).

## [1.7.0] - 2026-04-09

### Added
- **Integrated Taskbar Timer (Windows 11 Native)**: New taskbar integration method inspired by TrafficMonitor. It attaches directly to `Shell_TrayWnd`, providing a native experience on Windows 11 without requiring DeskBand registration.
- **Smart Docking Logic**: The integrated timer automatically anchors itself next to the Notification Area (System Tray icons).
- **Independent Windows Integration Toggles**: Refined the Settings UI to allow independent toggling of:
  - Floating Prayer Overlay
  - Legacy DeskBand (for Win 10/ExplorerPatcher)
  - Integrated Taskbar Timer (Win 11 Source)

### Changed
- **Performance Optimization**: Switched taskbar positioning logic to a low-impact polling system to ensure 0% impact on Windows Explorer's performance.
- **UI Refinement**: Taskbar timer now features a subtle drop shadow for better readability on light/dark taskbar backgrounds.

### Fixed
- Fixed a state-management issue where toggling the taskbar timer via the tray menu didn't always reflect in the Settings window.

## [1.6.5] - 2026-04-09

### Added
- **Native Taskbar DeskBand (Windows 10 & 11)**: Introduced a high-performance COM-based taskbar extension that docks directly next to the system tray for real-time prayer countdowns.
- **Independent Feature Toggles**: Decoupled the Floating Overlay and Taskbar DeskBand, allowing users to enable neither, either, or both simultaneously.
- **Enhanced Windows 11 Compatibility**: Enabled DeskBand registration for all Windows versions, supporting power users with custom taskbar tools like ExplorerPatcher.
- **Improved Tray Menu**: Added granular toggle items for both "Show Floating Overlay" and "Show Taskbar Timer" directly in the system tray context menu.
- **Real-time State Sync**: Implemented a low-latency IPC bridge via JSON to ensure the DeskBand always reflects the main application's state.

### Changed
- **SDK-style Project Migration**: Converted the DeskBand project to modern SDK-style for improved build stability and .NET CLI compatibility.
- **Unified Installer**: Updated both x64 and x86 installers to handle administrative COM registration for the taskbar extension on all systems.

### Fixed
- Fixed a syntax error in `MainWindow.xaml.cs` that previously caused build failures in the notification registration logic.
- Resolved dependency resolution issues for .NET Framework projects in fresh build environments using ReferenceAssemblies.

## [1.6.0] - 2026-04-07
 
 ### Added
 - **API Response Caching**: Implemented a persistent disk cache for Aladhan.com results to significantly speed up app startup and reduce data usage.
 - **Intelligent Cache Invalidation**: Added metadata-driven cache logic that automatically refreshes data if location (latitude/longitude), date, or calculation method settings change.
 
 ### Changed
 - **Hero Section Night Context**: Improved the Hero section experience during the night. The app now correctly shows "**Isha ends in**" instead of "Fajr starts in", providing better religious context.
 - **Accurate Prayer Windows**: Fixed a logic issue where the app would continue showing "Fajr" as active after Sunrise. It now correctly transitions to "**Dhuhr starts in**" immediately after Sunrise.
 
 ### Fixed
 - **Tahajjud Buffer Restoration**: Restored the +15m start and -10m end offsets for Tahajjud prayer calculation to ensure accurate timing for night prayers.
 - **UI Label Synchronization**: Updated the Hero section labels and countdown timers to perfectly match the buffered Tahajjud windows.
 
 ## [1.5.9] - 2026-04-07

### Added
- **Tahajjud Timer**: Integrated a dynamic countdown timer for the last third of the night, automatically appearing in the Hero Card.
- **Nafal Prayer Notices**: Added smart detection for Salat al-Duha and Salat al-Awwabin windows with dedicated "Nafal Notice" cards.
- **Truly Transparent Icons**: Replaced PNG-based icons with high-quality XAML Vector paths to ensure 100% transparency and a professional, box-free look.
- **Dynamic Status Colors**: Vector icons now change colors based on the time of day (Golden for Day, Silver for Night, Warm Orange for Sunrise/Sunset).
- **UI Visibility Improvements**: Brightened prayer labels and increased timer font sizes in the Hero section for better readability during night-time use.

## [1.5.8] - 2026-04-06

### Added
- **Sunnah Fasting Highlights**: Added a dedicated section in "Daily Highlights" to showcase Sunnah and recommended fasting days.
- **Weekly Fasts**: Highlights Mondays and Thursdays with their specific spiritual benefits.
- **Monthly Fasts**: Automatically detects and reminds of Ayyam al-Bidh (13th, 14th, and 15th of Hijri months).
- **Annual Special Fasts**: Added support for 6 Days of Shawwal, Day of Arafah, Day of Ashura, and more.
- **Prohibited Days**: Clear visual warnings for days when fasting is forbidden (Eids and Tashreeq).
- **Smart Date Matching**: Uses high-accuracy API data for Hijri dates with a reliable local calendar fallback.

## [1.5.7] - 2026-04-06

### Added
- **Manual Update Check**: Added a "Check for Updates" button in the Settings window under Developer Info.
- **Improved UI**: Version display in Settings is now dynamic.
- **Code Quality**: Further refactoring of `SettingsWindow.xaml.cs` to use static methods and fixed `DateTimeKind` warnings.

## [1.5.6] - 2026-04-06

### Added
- **Live Countdown**: Added a real-time countdown to Suhur (Ends) and Iftar (Begins) highlights.
- **Smart Status**: Highlights now automatically show "Passed" once the time has reached.
- **Code Quality**: Major refactoring of internal methods to static and performance optimizations based on SonarQube recommendations.
- **Improved Deployment**: Installer output is now versioned (e.g., `DailyPrayerTimer_Setup_v1.5.6.exe`) to prevent accidental overwrites.

## [1.5.5] - 2026-04-06

### Added
- **Update Notifications**: Added automatic check for new versions on startup with a sleek notification banner.
- **Improved UI**: Refined layout for better visibility of update alerts.
- **Bug Fixes**: Resolved minor build warnings and version display inconsistencies.

## [1.5.0] - 2026-04-06

### Added
- **AlAdhan API Integration**: High-accuracy prayer times fetched from AlAdhan.com with automatic offline fallback to local calculations.
- **Improved Islamic Calendar**: Hijri date is now fetched directly from the API for better regional accuracy.
- **Default Adhan Sound**: Automatic download of a high-quality Adhan file on the first run.
- **External API Toggle**: Option to enable/disable external API requests in Settings.

## [1.4.0] - 2026-04-06

### Added
- **Adhan Sound Alarm**: You can now enable an Adhan sound to play a specific number of minutes before the congregation.
- **Custom Sound Support**: Select any MP3 or WAV file from your computer to use as the Adhan.

## [1.3.0] - 2026-04-06

### Added
- **Dashboard Jamaat Visibility**: Current/Next prayer's congregation time is now displayed at the top of the main window.
- **Congregation Guidelines**: Added real-time "Today's Range" hints in settings to help select valid Jamaat times.
- **Dated Notifications**: Prayer alerts now include the current date in the header.

## [1.2.0] - 2026-04-06

### Changed
- **Fixed Congregation Times**: Switched from relative offsets (minutes after start) to actual fixed times (HH:mm) for all 5 prayers.
- **Smart Validation**: Congregation times are now validated to ensure they stay within the prayer windows (e.g., stopping Fajr Jamaat before Sunrise).
- **Settings UI**: Updated with specific time entry fields for better user control.

## [1.1.0] - 2026-04-06

### Added
- **Congregation (Jamaat) Alarms**: You can now set a congregation time (minutes after start) for each of the 5 prayers in settings.
- **Jamaat Countdown Popup**: A high-contrast, premium alarm window appears before the congregation starts, showing a live countdown.
- **Configurable Alarm Trigger**: Set exactly how many minutes before the congregation you want the alarm window to appear.
- **Smart Validation**: Jamaat times are automatically validated to stay within the prayer window (e.g., Fajr Jamaat is always before Sunrise).

## [1.0.1] - 2026-04-06

### Added
- Complete migration from Tauri (JS/Rust) to **Native WPF (.NET 8.0)** for maximum performance and stability.
- **Glassmorphism UI**: Redesigned all windows with premium blurred backgrounds and vibrant Islamic Green themes.
- **Vertical Popup Overlay**: The taskbar overlay now expands upwards like a professional system popup on hover.
- **Improved Taskbar Docking**: Reliable docking and repositioning logic that respects the Windows work area and supports manual dragging.
- **Prohibited Prayer Times**: Integrated prohibited time display (Sunrise, Zawal, Sunset) with visual warnings.
- **Tray Toggle**: Quickly show/hide the overlay directly from the system tray context menu.
- **GitHub Integration**: Added developer profile link in the Settings menu.

### Fixed
- Fixed flickering when hovering over the taskbar overlay.
- Fixed inaccurate prayer status labels (e.g. "Fajr ends in" correctly identified).
- Fixed text clipping for AM/PM and long prayer names.

## [0.4.0] - 2026-04-06

### Added
- Implemented deep native Windows taskbar injection using Rust Win32 bindings (`SetParent` to `Shell_TrayWnd`) so the overlay is natively integrated and cannot be hidden by the Windows taskbar.

## [0.3.0] - 2026-04-01

### Added
- Added "Start with Windows" (autostart) functionality and a toggle setting in the Appearance menu.

## [0.2.0] - 2026-04-01

### Added
- Added a unified Tauri `prayer-update` event broadcaster for better background synchronization.
- Overlay window gracefully attempts to force `Always on Top` state via JavaScript intervals to prevent the Windows 11 taskbar from hiding it.

### Changed
- Overlay taskbar indicator redesigned to be highly compact, native-feeling, and explicitly avoids taskbar overlaps.
- Switched default `WebViewWindow` references to robust emit-based communication.

### Removed
- Removed the hover-popup feature and its associated windows and handlers to guarantee a more lightweight and reliable experience on all hardware bounds.

## [0.1.1] - 2026-04-01

### Added
- Added `WS_EX_NOACTIVATE` flag to the taskbar overlay to prevent focus stealing when dragging or interacting.

### Changed
- Taskbar Overlay now appears directly in the Windows taskbar as a separate icon for easier switching.
- Taskbar Overlay's "Always on Top" behavior is now reinforced using Win32 API `HWND_TOPMOST` to prevent being hidden by other topmost windows.

## [0.1.0] - 2026-03-16

### Added
- Initial release of the Daily Prayer Timer desktop application.
- High-performance architecture using Tauri 2.0 and Rust.
- Real-time prayer dashboard with dynamic gradients and countdowns.
- Draggable taskbar overlay for persistent prayer tracking.
- Native Windows notifications for prayer alerts.
- Global location search and manual coordinate support.
- Offline-first calculation using Adhan-js.
- Theme engine for customizing colors and gradients.
- Settings menu with overlay toggle and persistence.
- System tray integration with "Always Run" (minimize to tray) feature.
- Custom application icons.

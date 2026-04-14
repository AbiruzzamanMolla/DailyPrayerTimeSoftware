# Changelog

All notable changes to this project will be documented in this file.

The format is based on [Keep a Changelog](https://keepachangelog.com/en/1.0.0/),
and this project adheres to [Semantic Versioning](https://semver.org/spec/v2.0.0.html).

//we added jamat time, loader screen, fixed prayer notifcation for tahajjurd ect. then continute

## [2.2.1] - 2026-04-14

### Added

- **Hybrid Hijri Calendar System**: Implemented a region-aware Hijri calendar that prioritizes accurate data from the AlAdhan API when online.
- **Improved Hijri Accuracy**: Fixed discrepancies where the Hijri date was off by 1-2 days (e.g., in Bangladesh) by fetching real-time components (Day, Month, Year).
- **Localized Hijri Months**: Appropriately translates Hijri month names into Bangla and Arabic based on the user's selected language.
- **Smart Fallback**: Automatically reverts to the local UmAlQura calendar when offline or when API requests time out.

## [2.2.0] - 2026-04-14

### Added

- **Divine Tracker Analytics**: Implemented aggregate completion progress for Weekly, Monthly, and Yearly timeframes in the Tracker tab.
- **Aggregate Progress Overview**: New dynamic card at the top of the tracker showing completion percentage, "Completed" count, and "Missed" count across all periods.
- **Automated Qadha Summary**: Dedicated "Qadha Statistics" UI showing missed mandatory prayers for Today, This Week, This Month, and this Year.
- **Strict Qadha Identification**: Intelligent logic that automatically marks Fard prayers as Qadha once the next prayer time has started and it remains unchecked.
- **Historical Activity Headers**: Improved context labels (e.g., "THIS WEEK", "MONTH SUMMARY") across different tracker tabs.
- **Jamat Time Support**: Added the ability to view and track Jamat (congregation) times for prayers.
- **Loading Screen**: Introduced a new loader screen to provide visual feedback and improve the user experience during app startup and background data fetching.
- **Automatic Data Backup**: Scheduled backups to your preferred directory to keep your progress safe.

### Fixed

- **Tahajjud Notifications**: Fixed an issue where prayer notification alerts for Tahajjud were not firing correctly.
- **Build Issue**: Resolved CS1061 compiler error in `TrackerView.xaml.cs` regarding the missing `NextFajr` property.
- **Qadha Calculation Stability**: Refined Isha end-of-time logic to ensure accurate counting only when the calendar day changes.
- **General Stability**: Minor bug fixes and performance improvements across the app.

## [2.1.0] - 2026-04-14`

### Added

- **Multi-Language Audio Notifications**: Added native support for Arabic (العربية) and Bangla (বাংলা) voice notifications.
- **Enhanced Voice Selection**: Settings now features localized display names for audio languages.
- **Random Test Playback**: The "Test Prayer Sound" button now plays a random audio sample from the selected language to provide a quick preview of all notification voices.

### Changed

- **Localized Settings UI**: Improved the prayer sound language dropdown to display native language names instead of folder codes.

## [2.0.0] - 2026-04-13

### Added

- **Tracker Generation 2 Completion**: Finalized the bi-directional synchronization engine between the Tracker popup and the Hero Tracker dashboard.
- **Spiritual Deed Logging**: Integrated a comprehensive deed tracking system into the Tracker, allowing users to log daily good deeds alongside prayers for a complete spiritual overview.
- **Glassmorphism UI Overhaul**: Implemented premium glass-morphism effects for the TrackerView, providing a consistent, modern aesthetic that feels native to the new design system.
- **Tabbed Tracker Navigation**: Introduced an intuitive tabbed interface for the Tracker popup, separating Prayer tracking, Deed logging, and Settings for streamlined interaction.
- **Precision Progress Logic**: Refactored the calculation engine to use double-precision math, ensuring daily completion percentages reach 100% accurately.
- **Expanded Tracking Capabilities**: Included Nafal prayers (Tahajjud, Duha, Awwabin) and Fasting (Sawm) status in the overall daily spiritual compilation.
- **Improved Historian Mode**: Removed the "upcoming" blur effect from past dates, allowing users to clearly review and interact with historical spiritual records.
- **Unified UI Alignment**: Synchronized Sawm status, Rakat counts, and Nafal data across all UI layers instantly without requiring manual refreshes.
- **AI-Orchestrated Core (Graphify)**: Integrated specialized **Graphify** agent workflows to ensure high-performance logic and cross-module synchronization during development.

### Fixed

- **Progress Calculation Bug**: Fixed integer division issue that previously prevented 100% completion from displaying correctly.
- **Sync Lag**: Removed refresh delays between Tracker updates and Dashboard indicators for a real-time experience.
- **UI UX Polish**: Standardized Tracker settings labels to sentence case and optimized tab ordering for better flow.

## [1.9.1] - 2026-04-12

### Added

- **Comprehensive Notification Test System**: Added "Test" buttons for all notification types in Settings (Adhan, Reminders, Congregation Popup, and Prayer Sounds).
- **Multi-Language Prayer Sounds**: Play start and end voice notifications for each prayer in the user's selected language.
- **Tahajjud Midnight Sound**: Precise notification sound at the start of Tahajjud (Islamic Midnight).
- **Sound Asset Management**: Automated language folder detection and standardized sound file mapping.

### Fixed

- **Tahajjud Start Sync**: Improved accuracy of Tahajjud start sound trigger.
- **UI Consistency**: Re-organized test buttons for better accessibility within their respective setting groups.

## [1.9.0] - 2026-04-10

### Added

- **Multi-Language Support**: Complete localization architecture for the entire application.
- **Dynamic Language Switching**: Toggle between English and Bangla (with support for more) instantly from Settings.
- **Localized UI Windows**: Standardized Adhan Notification, Jamaat Alarm, and Taskbar Timer with the new i18n system.
- **Adhan Dua Localization**: Fully localized Adhan Dua including Arabic text, Transliteration, and Accurate Translations.
- **Localization Contributor Guide**: Added `CONTRIBUTING_I18N.md` to help open-source contributors easily add new languages.
- **Automatic String Prefixing**: Robust `LocalizationManager` that handles resource mapping and prefixing automatically for developers — all keys are registered in both bare and `i18n_` prefixed forms so XAML `DynamicResource` and C# `GetString()` calls both work seamlessly.
- **Localized Prayer Names**: All prayer names (Fajr, Sunrise, Dhuhr, Asr, Maghrib, Isha, Nafal) fully localized in hero, prayer list, and nafal sections.
- **Localized Date & Day Names**: Gregorian month names (January → জানুয়ারি) and day names (Friday → শুক্রবার) fully localized via `Month_Gregorian_*` and `Day_*` keys.
- **Localized Countdown Units**: Countdown timer now shows `ঘ মি স` in Bangla (was `h m s` hardcoded) via `Unit_Hour_Short`, `Unit_Min_Short`, `Unit_Sec_Short` keys.
- **Localized Hijri Date**: Hijri calendar always uses localized month names; forces local calculation for non-English locales instead of using the English API response. `Label_HijriSuffix` key added (`AH` / `হিজরি`).
- **Arabic Hamd Header**: Added interactive "إِنَّ الْحَمْدَ لِلَّهِ..." header at the very top of the hero section. Hovering smoothly crossfades (0.3s CubicEase) to the localized translation. Tooltip mode is disabled in Arabic locale to avoid redundancy.
- **i18n Workflow Checklist**: Added `.agent/workflows/i18n-checklist.md` — a mandatory developer guide documenting all localization rules and conventions for future feature development.

### Changed

- **UI Standardization**: Replaced all hardcoded UI strings across the application with dynamic resource bindings.
- **Hero Section Layout**: Arabic Hamd text is now the topmost element inside the hero card.
- **Version Milestone**: Bumped to v1.9.0 to mark the major localization integration.

### Fixed

- **XAML DynamicResource Key Mismatch**: Fixed critical bug where `LocalizationManager` stored keys with `i18n_` prefix only, causing XAML `{DynamicResource Prayer_Fajr}` bindings to never find translations — all keys are now registered in both forms.
- **Hardcoded Countdown Format**: Replaced `"{0}h {1}m {2}s"` string constant with `FormatCountdown()` helper.
- **Hardcoded Date Formatting**: Replaced `DateTime.ToString("dddd")` / `"dd MMMM yyyy"` with localized helpers `GetLocalizedDayName()` and `GetLocalizedDate()`.
- **API Hijri Month Name**: API returns English Hijri month names; now always uses local calculation in non-English mode.

## [1.8.5] - 2026-04-10

### Added

- **Granular Notification Controls**: Individual toggles for Adhan sounds, Pre-Adhan reminders, and Jamaat (Established) popups per prayer (Fajr, Dhuhr, Asr, Maghrib, Isha, Shuruq).
- **Pre-Adhan Reminders**: Toast notifications with sound triggered at a configurable offset before each prayer time.
- **Shuruq Dual Notifications**: "Fajr ending soon" warning (10 min before sunrise) and "Sunrise started" alert at the exact time.
- **Adhan Volume Control**: Global volume slider (0–100%) affecting both background playback and the Adhan popup window.
- **Custom Suhur/Iftar Offsets**: User-configurable minute adjustments (+/-) applied globally to Suhur and Iftar times across Hero card, countdowns, and notifications.
- **Notifications Settings Tab**: Dedicated tab in the Settings window for managing all notification preferences.
- **Manual Calculation System**: Custom Fajr/Isha angles and High Latitude rules for regions where standard timing calculations fail.
- **Hijri Date Adjustment**: Configurable Hijri offset (+/- days) in Settings to align the Islamic date with local moon sightings.

### Changed

- **Stable Hero Layout**: Refactored the Hero card XAML to anchor Sunrise and Sunset displays to the far edges with fixed positions.
- **Default Visibility**: Set the Hero prayer grid to hidden by default for new installations.
- **Test Sound Integration**: "Test Adhan" buttons now respect the Volume slider setting.

### Fixed

- **Version Metadata**: Unified version numbering across project files and assembly metadata.

## [1.8.4] - 2026-04-10

### Added

- **Hero Grid Visibility Toggle**: Added a new setting in "Layout & Theme" to toggle the visibility of the prayer times grid in the Hero segment.
- **F11 Keyboard Support**: Dedicated `F11` key binding for toggling full-screen mode instantly.
- **Prohibited Time Guidance**: Added hover tooltips to the footer prohibited sections (Sunrise, Noon, Sunset) to clarify their purpose as Makruh times.

### Changed

- **Sunrise Relocation**: Moved Sunrise and Sunset times to the Hero card flanking the main timer, enhancing the focused UI.
- **List Cleanup**: Removed redundant "Sunrise" entries from both the Hero grid and the main scrollable prayer list for a cleaner look.

## [1.8.3] - 2026-04-10

### Added

- **Consolidated Notes**: Combined Friday Sunnahs and Fasting Highlights into a single, smart collapsible section that shows all relevant information together.
- **Enhanced Footer**: Added vertical borders (left/right) to the Noon (Zawal) section for better visual separation.
- **Dynamic Window Sizing**: Application now automatically adjusts to 85% of the screen height on launch, with a guaranteed minimum width of 460px.

### Changed

- **Footer Readability**: Updated all status text and time ranges in the prohibited footer to pure white for better contrast and legibility.

### Fixed

- **F11 Full Screen**: Refined the window state logic to ensure the taskbar is reliably hidden when entering full-screen mode.
- **Notes Visibility**: Fixed an issue where the Friday note would suppress other fasting highlights.

## [1.8.2] - 2026-04-10

### Added

- **Immersive Full Screen**: Fixed full-screen mode to correctly hide the Windows taskbar (F11 style), providing a truly distractions-free experience.
- **Context-Aware Ramadan Hero**: Hero card now intelligently switches to "Time Left for Iftar" (counting down to Maghrib) during Asr prayer when Ramadan mode is active.
- **Dynamic Prohibited Status**: The sticky footer now features live status labels ("ACTIVE", "Starts in...", "Passed") for Sunrise, Zawal, and Sunset prohibited windows.

### Changed

- **Information Hierarchy**: Relocated the Prohibited Times list to below the Nafal prayers in the scroll view for a more logical flow.
- **Clean Ramadan UI**: Automatically hides the Suhur/Iftar highlight cards when Ramadan mode is active to reduce screen clutter.

### Fixed

- **Note Overlap**: Restructured the XAML grid for collapsible notes to prevent layout overlapping when expanded.
- **Ramadan Visibility**: Changed Suhur time color in Hero Card to white for better visibility against the Ramadan theme background.

## [1.8.1] - 2026-04-10

### Added

- **Ramadan Mode**: One-tap toggle for spiritual focus, dynamically showing Suhur and Iftar in the Hero Card.
- **Full Screen Mode**: Immersive view for both Zen and Normal modes.
- **Dynamic Prayer Slots**: Context-aware UI that automatically shows Salat al-Duha (after Sunrise) and Tahajjud (after Midnight) in place of Dhuhr/Isha slots.
- **Friday (Jumu'ah) Sunnahs**: Expanded collapsible notes with physical and spiritual Sunnahs for the day of Jumu'ah.
- **Enhanced Card Design**: Improved readability with brighter text, lighter borders, and intuitive start/end times for each prayer.

### Fixed

- **Close Button Behavior**: The close button now hides the application to the tray instead of exiting, maintaining background alarms.
- **Build Warnings**: Resolved all nullability and delegate mismatch warnings for a clean, professional codebase.
- **UI Label Clarity**: Renamed "Shuruq" to "Sunrise" and improved date/day alignment.

## [1.8.0] - 2026-04-10

### Added

- **Complete UI Redesign**: A modern, premium aesthetics overhaul with custom layouts and immersive glassmorphism.
- **Custom Navigation Bar**: Replaced standard Windows title bars with a sleek, integrated navigation and menu system.
- **Hero Card Enhancement**: Added live prayer timers and expanded prayer data directly into the hero card for better at-a-glance awareness.
- **Zen Mode**: A minimalist, immersive UI toggle (`✨` icon) that hides distractions and focuses entirely on the current prayer countdown.
- **Adhan Preset Selection**: High-quality built-in Adhan tracks (Makkah, Madinah, Alafasi, etc.) now available in Settings.
- **Tahajjud Adhan**: Dedicated alarm support for the last third of the night, helping users catch the most blessed time for worship.
- **Improved Adhan Popup**: A professional overlay for Adhan alarms featuring **Mute** and **Dismiss** controls.
- **Prayer Rakat Notes**: Detailed Rakat counts (Sunnah, Fard, Nafl) integrated directly into the UI for all 5 prayers and Jumu'ah.
- **Portable Mode Robustness**: Automatic Registry synchronization and path resolution (`Environment.ProcessPath`) ensures auto-start works even if the portable folder is moved.
- **Jumu'ah (Friday) Support**: Dynamic display logic specifically for Friday congregational prayers.

### Fixed

- **Taskbar Integration Stability**: Resolved flickering and disappearance issues on Windows 11.
- **Initialization Sync**: Fixed the slight lag in prayer data synchronization during application startup.

## [1.7.8] - 2026-04-09

### Added

- **Portable Version Support**: Introduced a self-contained portable mode. The application now checks for a `.portable` flag file in its directory; if found, all settings and data are stored in a local `data/` folder instead of `%APPDATA%`.
- **UI Enhancement**: Added a high-quality UI preview GIF to the README for better project visualization.
- **Build Automation**: Created `publish_portable.ps1` for automated, single-file portable builds.

## [1.7.7] - 2026-04-09

### Fixed

- **Improved Taskbar Persistence**: The Taskbar Timer now aggressively re-asserts its 'Topmost' status every second. This prevents it from being hidden when the Start menu or system tray flyouts are opened.
- **Reliable Startup Data**: Implemented a handshake between the main app and the taskbar window to ensure prayer data appears instantly upon the first launch, eliminating the temporary `--:--:--` display.
- **Click-Through Support**: Added `WS_EX_TRANSPARENT` to the taskbar window, allowing user clicks to pass through to the taskbar area behind the timer.

## [1.7.6] - 2026-04-09

### Fixed

- **Taskbar Timer Flicker (Root Cause Fixed)**: Completely rewrote the Integrated Taskbar Timer to work as a standalone topmost window instead of using `SetParent` into `Shell_TrayWnd`. On Windows 11, the `SetParent` approach causes the taskbar to continuously repaint and fight the child window, resulting in constant flickering. The new approach positions the timer window over the taskbar using screen coordinates — the same method used by modern system tools.
- **Eliminated Redundant Win32 Calls**: Position is now cached; `SetWindowPos` is only called when the position actually changes (e.g., taskbar moves or DPI changes), reducing CPU overhead and visual jitter.
- **Update Checker False Positive**: Fixed an issue where 4-part versions (e.g., `1.7.5.1`) would always trigger the "Update Available" banner because the internal version reader only returned 3 parts. All future releases will use standard 3-part semantic versioning (e.g., `1.7.6`).

## [1.7.5.1] - 2026-04-09

### Fixed

- **Integrated Taskbar Stabilization**:
  - Eliminated continuous flickering by optimizing Win32 `SetWindowPos` flags to prevent excessive redraws.
  - Resolved application startup freeze where the taskbar showed "00:00:00 Asr" by using neutral placeholders and forcing a data refresh post-calculation.
  - Improved robustness of high-DPI scaling calculations in the taskbar integration.

## [1.7.5] - 2026-04-09

### Added

- **Developer Profile Polished**: Reordered information for better readability (Name -> Email -> Projects).

### Fixed

- **Integrated Taskbar Timer Reliability**:
  - Resolved the "00:00:00 Asr" freeze by forcing an immediate data sync upon window creation.
  - Improved window lifecycle management to ensure the taskbar window correctly restarts after being toggled in Settings.
  - Added error boundaries around integration updates (DeskBand/Taskbar) to prevent one failure from halting the entire sync loop.
  - Enhanced logging and handle verification for safer reparenting to `Shell_TrayWnd`.

## [1.7.4] - 2026-04-09

### Added

- **Responsive Settings UI**: The settings window now scales beautifully with window resizing, using flexible layouts for various resolutions.
- **Support & Contact Refresh**: Restructured the tab for better navigation:
  - Moved version information to the Updates section.
  - Added a new **Sponsors** section (Islamic Audiobook YT, Book Review YT, Audiobook Bangla).
  - Integrated email contact in the developer profile.
  - The "Save Settings" button now intelligently hides when viewing the Contact tab.

### Fixed

- Fixed synchronization between Floating Overlay and Integrated Taskbar Timer.
- Resolved Taskbar Timer display issues by ensuring thread-safe UI updates.
- Improved Taskbar Timer accuracy and DPI scaling awareness.

## [1.7.3] - 2026-04-09

### Added

- **Major UI Restructuring**: Reorganized Settings into three intuitive tabs:
  - **Daily Prayer**: Consolidated Location, Calculation, and Jamaat settings.
  - **Layout & Theme**: Dedicated section for Windows Integrations and Appearance.
  - **Support & Contact**: Centralized project links and #define AppVersion "1.7.5.1"
- Expanded **Developer Profiles** with links to Portfolio, npm, Packagist, and Marketplace.

### Fixed

- Fixed **Integrated Taskbar Timer (Win 11 Source)** not initializing on application startup.
- Improved **DPI Awareness** for the Taskbar Timer, ensuring correct alignment on high-resolution displays.

## [1.7.1] - 2026-04-09

### Added

- **Tabbed Settings UI**: Restructured the Settings window into three organized tabs: "General", "Jamaat Times", and "Support & Contact" for better usability.
- **Support Links**: Added direct links to the Windows App, Chrome Extension, and VS Code Extension in the Contact tab.
- **Acknowledgements**: Added a dedicated section in README to thank the open-source community for their inspiration and contributions.

### Changed

- Improved Settings window organization by decoupling functional settings from support/dev information.

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

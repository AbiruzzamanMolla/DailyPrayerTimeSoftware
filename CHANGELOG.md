# Changelog

All notable changes to this project will be documented in this file.

The format is based on [Keep a Changelog](https://keepachangelog.com/en/1.0.0/),
and this project adheres to [Semantic Versioning](https://semver.org/spec/v2.0.0.html).

## [2.6.2] - 2026-06-17

### Added

- **Automated Update System**: Introduced an option to auto-install updates on close. When enabled, updates are silently downloaded in the background while the application runs, and the setup installer or portable updater executes automatically when the user exits the application.

## [2.6.1] - 2026-06-17

### Fixed

- **Google Sign-In WebView2 crash**: Redirected the WebView2 User Data Folder (UDF) to `%LOCALAPPDATA%` to prevent `E_ACCESSDENIED (0x80070005)` errors when the application runs from write-restricted directories (e.g. `C:\Program Files`).
- **Tray Flyout Theme Integration**: Fully re-styled the taskbar tray flyout window with the premium dark green gradient theme and glassmorphic translucent prayer highlights.
- **Loader Window Theme Integration**: Updated the loader/about dialog with the premium dark green gradient background, white readable text, and glassmorphic button styles.
- **Build Warning Suppression**: Resolved the `NETSDK1187` MSBuild locale normalization warnings during compiler execution.

## [2.6.0] - 2026-06-17

### Added

- **Full-Screen Blocked Congregation Reminder**: Introduced a brand new full-screen notification mode for the congregation (Jamaat) reminders that blocks the desktop and shows real-time countdown, prayer range timing, and congregation start times.
- **Dismissal Option Configuration**: Added settings options to toggle whether the full-screen blocked notification is dismissable (allowing Esc-key or close button) or strictly blocks the screen until the countdown expires.
- **Dynamic Accent styling**: The full-screen notification is beautifully styled with the active gradient colors and custom button contrast elements to match the app theme dynamically.
- **Bilingual localizations**: Completed Bengali and English translations for all new display modes and configurations.

## [2.5.3] - 2026-06-09

### Added

- **Modern Taskbar Tray Flyout Menu**: Implemented a custom WPF-based flyout window toggled by the system tray icon, showing exactly 5 daily prayer times (start/end ranges) with a dynamic Gregorian and Hijri date header.
- **Taskbar Menu Localization**: Full dynamic i18n support for the taskbar menu, including month/day names, countdown texts ("starts in" / "ends in"), and relative remaining times.

### Fixed

- **Coordinates & Location Layout**: Split coordinates and location name into two lines to prevent layout clipping and truncation for long names.
- **Accidental Main App Opening**: Disabled system tray icon double-click handler to prevent accidentally opening the main window.
- **Isha Timing Clipping**: Expanded flyout window height to `560` to ensure Isha and other list items are not clipped.
- **C# Compiler Warnings**: Cleaned up nullability compiler warnings in `CloudSyncService.cs`.

## [2.5.2] - 2026-06-03

### Added

- **Weekly & Monthly Report Cards**: Replaced the fixed monthly report card generator with a modern popup window allowing selection between "Weekly Report" and "Monthly Report".
- **Dynamic Period Selector**: Introduced a select box to choose from the last 10 weeks (Saturday to Friday) or the last 12 months.
- **WPF ComboBox Custom Theme**: Implemented a dark green ControlTemplate for the ComboBox to match the premium green theme, resolving styling conflicts where text was invisible.
- **Localisation Suite**: Added full translation support for the new dialog elements in English, Bangla, Arabic, Hindi, Indonesian, Malayalam, Tamil, and Telugu.

### Fixed

- **Report Card Breakdown Stats**: Fixed a bug where the monthly card breakdown stats (Fajr, Dhuhr, etc.) were displaying `0%` when today was empty. They are now accumulated over the entire target period.

## [2.5.1] - 2026-06-02

### Added

- **In-App Support Contact Form**: Added a premium, glassmorphic Contact Us card inside the Settings > Support tab. Users signed in to Cloud Sync can now send bug reports, suggestions, or help requests directly to the developer from within the app.
- **Root-level Firestore Integrations**: Configured globally-scoped REST collections for `contact_messages` and `mail` in `FirestoreRestHelper.cs` to store submissions cleanly in database root paths.
- **Trigger Email Integration**: Submissions write to the global `mail` collection, which is watched by the standard Firebase Trigger Email extension to automatically forward queries to the developer's email address (`abiruzzaman.molla@gmail.com`).
- **Complete Localization**: Full localization suite added for contact form fields and statuses across English, Bengali, Hindi, Arabic, Malayalam, Tamil, Telugu, and Indonesian language packs.

### Changed

- **Settings Support Card Ordering**: Rearranged cards inside the Settings Support tab so that the Contact Us card is positioned directly underneath the Updates card at the very top of the scroll view.

## [2.5.0] - 2026-06-02

### Added

- **Global Leaderboard**: Implemented a fully functional Monthly Leaderboard visible inside the Tracker tab. All signed-in users can push their prayer statistics to a shared, global Firestore collection and compete on a ranked standings board sorted by completion rate and total prayers. Displays medals (🥇🥈🥉) for the top three, highlights the current user's own row, and includes their rank badge.
- **Hall of Fame Tab**: Added a "Hall of Fame" sub-tab inside the Leaderboard that stores and displays the previous months' Top 3 ranked users, preserved permanently for historical recognition.
- **Leaderboard Tab in Tracker Navigation**: Embedded the Leaderboard view directly into the Tracker tab's sub-navigation bar. The tab is docked to the far right of the row to visually distinguish it from the standard Daily/Weekly/Monthly/Yearly tabs.
- **Asymmetric Premium Glassmorphic Leaderboard Tabs**: Styled the Monthly and Hall of Fame toggle buttons with custom-radius `ControlTemplate` styles. The Monthly tab has a left-rounded corner profile (`8,0,0,8`) and Hall of Fame is right-rounded (`0,8,8,0`), creating a connected pill button group with forest-green active states and translucent glass inactive states.
- **Google Sign-In via Embedded WebView2**: Replaced the broken `signInWithRedirect` OAuth flow that failed due to browser `sessionStorage` partitioning with a self-contained in-app WPF Window powered by `WebView2`. The window navigates to Google's auth URI, intercepts the final `__/auth/handler?code=` callback URL, and closes automatically — all without ever opening an external browser tab.
- **Cloud Sync Error Logging**: All Firestore REST API errors (read, write, delete, collection query, and JSON parsing failures) are now written to the built-in `app_log.txt` file with full stack traces, alongside the existing `Debug.WriteLine` output. CloudSyncService, LeaderboardService, and FirestoreRestHelper all participate in this diagnostic logging.
- **Cloud Sync Error Propagation**: Sync failures in `CloudSyncService` are now explicitly bubbled up to the UI layer using `throw;`, so users see a descriptive error popup (`Sync failed: Firestore write failed: ...`) instead of a silent no-op success message.
- **Monthly Tracker Report Card**: Added a "Card" button in the Tracker header that generates a premium portrait-format shareable image (1080×1920px) summarizing the user's prayer statistics for the month, complete with a gradient background, stats grid, prayer-by-prayer progress bars, and a QR code.

### Changed

- **Leaderboard Navigation is Localization-Proof**: The Tracker tab selection logic now uses the XAML item `Name` attribute (e.g. `TabLeaderboard`) rather than its visible localized `Content` text. This ensures the tab routing works correctly in all supported languages (Bengali, Arabic, etc.) without any code changes.
- **Progress Report Card Layout Expanded**: The monthly stats card now uses `HorizontalAlignment.Stretch` for the stats grid, expanding the six stats tiles to fill the full 920px container width. Font sizes, padding, and progress bar heights have been substantially scaled up for a polished, premium look.
- **Dynamic Progress Bar Rendering in Card**: Replaced fixed-width progress bars (hardcoded at `400px`) with a star-column `Grid` layout, allowing each bar to stretch proportionally across the full card width. Bar height scaled from `12px` to `16px` with corner radius `8`.
- **Sync Button and Card Button Styling**: Applied a new reusable `PremiumGlassButton` style to the Card and Sync buttons in the Tracker header. The template supports disabled state gracefully (no white-out flicker during sync), hover/press glow effects, and consistent transparent dark-glass aesthetics.
- **Cloud Sync AppLogger Integration**: All `Debug.WriteLine` error messages in Firestore, Cloud Sync, and Leaderboard services are now duplicated into the persistent `app_log.txt` file — visible from the built-in Settings log viewer without needing a debugger.

### Fixed

- **Firestore REST Collection Parsing Crash**: The `ParseFirestoreCollection` method was calling `JArray.Parse(json)` on a Firestore REST response, which is actually a JSON Object with a `"documents"` array key. This caused a silent JSON exception and an empty leaderboard on every fetch. Fixed by parsing as `JObject` and safely extracting `root["documents"]` as a `JArray`.
- **Private Collection Scoping for Leaderboard**: All Firestore REST endpoints were hardcoded to prefix paths with `/users/{uid}/`, which incorrectly placed the shared `leaderboard` and `hall_of_fame` collections inside each user's private subdirectory. They are now routed to the global database root for all-user visibility. Private collections (`tracker`, `tasbih`, `ramadan`, `cycle`) remain securely sandboxed.
- **`JToken`/`JValue` Serialization Crash**: Newtonsoft.Json deserializes internal nested dictionary values as `JValue`/`JToken` wrapper types. The old `ConvertToFirestoreValue` method matched only native C# types (`string`, `bool`, `int`, etc.), causing a `JsonSerializationException` when encountering `JValue` wrappers like `"2 Sunnah (Mu'akkadah)"`. Added explicit top-priority JToken type dispatch before all native type checks.
- **`DateTime` Serialization Crash**: C# `DateTime` values (e.g. timestamps for checked prayers) triggered a `JsonSerializationException` when passed to `ConvertToFirestoreValue` because they are not Firestore-native primitives. Added explicit handling to format them as ISO-8601 strings (`"o"` format). The same fix applies to `JTokenType.Date` from Newtonsoft.
- **Off-Screen Report Card Rendering (Blank Image)**: The programmatically generated monthly card `FrameworkElement` was never added to the visual tree, causing its layout to remain at 0×0. Fixed by calling `Measure`, `Arrange`, and `UpdateLayout` on the card element before passing it to `RenderTargetBitmap`, ensuring correct dimensions and visual output.
- **Build Errors from CornerRadius on Non-Border Elements**: Resolved `MC3072` XAML compilation errors caused by applying `CornerRadius` on element types that don't support it (e.g. plain `Button`). Fixed using proper `ControlTemplate`-based approaches that bind `CornerRadius` through the element's `Tag` property.

## [2.4.4] - 2026-05-26


### Added

- **Embedded Modern Settings Screen**: Completed the migration of the settings interface from a standalone popup window into a fully integrated, premium glassmorphic `UserControl` embedded directly inside the main application window.
- **Single-Row Tabbed Settings Navigation**: Arranged all settings category tabs in a single row with modern custom icons (`🕋, 📊, 🔔, 🎨, ⚙️, 📄`) matching the main application navigation design.
- **Dynamic Layout & Footer Management**: Configured settings to automatically hide the main bottom navigation bar (`FooterNavigationBar`) when opened to optimize space and prevent overlapping.
- **Exhaustive Help & Settings Directory**: Expanded the help and documentation tab within settings with exhaustive, human-readable guidelines explaining every single settings parameter, calculation method, custom volume setting, and startup option.
- **Built-in System Log Viewer**: Integrated an interactive diagnostic log viewer card in the Help tab supporting standard application events (`app_log.txt`) and audio event logs (`audio_log.txt`). Includes a dynamic "Refresh" control and a high-fidelity "Copy Logs" button that copies entries into the system clipboard with tactile button-state micro-animations.
- **Ultra-Slim Fluent Scrollbars**: Overrode the native scrollbar style with an elegant, custom-themed Fluent track designed at an ultra-slim width of **2px** with responsive hover/drag translucent coloring to remain completely out of the way.

### Changed

- **Zen Mode Refinements**: Redesigned Zen Mode layout to completely collapse bottom tabs, checklist tracker cards, and remote API notice banners (like the Arafah recommendation bar) for an ultra-pure focus, while maintaining the essential 5-prayer timing grids inside the main card. Banners are cleanly re-evaluated and restored upon exiting Zen Mode.
- **Global Font Scaling Optimization**: Revamped `FontSizeHelper` with a thread-safe, weak-referencing global listener (`ConditionalWeakTable`) that automatically scales newly loaded elements (like switched dynamic tab controls) without requiring visual tree traversals or leaking memory.

### Fixed

- **First-Click Blank Tab Bug**: Resolved an initialization order bug where the ComboBox and TabControl indices failed to sync on first-click by explicitly forcing selected index initialization in the Settings constructor and show actions.
- **Combo Dropdown Text Invisibility**: Fixed ComboBox text coloring issues inside the Log Viewer by applying a high-contrast dark foreground style to match standard dropdown backgrounds.
- **Compilation Warnings Cleaned**: Resolved all nullability and ambiguous type warnings (CS8618, CS8600, CS8604, CS8625) across `MainWindow.xaml.cs`, `EnhancedTaskbarWindow.xaml.cs`, and `NoticeModels.cs` for a completely warning-free compile under .NET 8.

## [2.4.3] - 2026-05-26

### Added

- **API-Driven Notification System**: Integrated a dynamic, remote notification banner that fetches updates from `audiobookbangla.com`. The banner supports custom titles, HTML-rendered messages, and dismissible functionality with local caching to reduce server load.
- **Global Localization Expansion**: Added complete interface translations for 6 new languages: Hindi, Tamil, Telugu, Malayalam, Indonesian, and Arabic. Users can switch to these languages seamlessly from the Settings menu.
- **Unified Taskbar Settings**: Reorganized the taskbar integration settings by introducing a unified "Show Taskbar Timer" toggle along with a dropdown to select the preferred timer style (Enhanced, Integrated, or DeskBand).
- **Advanced System Tray Menu**: Redesigned the right-click tray menu to show real-time prayer information including the current prayer name, start time, and end time. The menu layout is now reorganized for better accessibility and includes a unified toggle to quickly show or hide the taskbar timer.

### Changed

- **UI Streamlining**: Moved the app's global font scaling controls (A−, A○, A+) from the main window's top menu bar directly into the Settings (Appearance section) for a cleaner UI layout. The keyboard shortcuts (Ctrl+Plus, Ctrl+Minus, Ctrl+0) remain fully functional.
- **Duas & Supplications Multi-Language Support**: Replaced the English/Bangla toggle buttons in the Tasbih section with a full language dropdown supporting all 8 available languages (English, Bengali, Hindi, Tamil, Telugu, Malayalam, Indonesian, Arabic). Translations fall back to English when not available for a selected language.

### Fixed

- **Zen Mode Layout Consistency**: Fixed a visual bug where the top and bottom margins were misaligned when entering Zen Mode due to improper handling of the new notification banner visibility.

## [2.4.2] - 2026-05-26

### Added

- **In-App Feature Documentation**: Added a new "Document" tab in Settings that provides a complete, human-readable walkthrough of every feature in the application. Covers Home, Salat, Tracker, Qibla, Tasbih, Ramadan tabs, all Settings sections, taskbar integrations, and keyboard shortcuts — written in plain, conversational language.

## [2.4.1] - 2026-05-26

### Added

- **Audio Event Logging**: Added file-based logging of all audio/sound events (adhan, notification sounds, dua, test sounds) to `audio_log.txt` in the app data directory to help debug audio issues.
- **Global Font Scaling**: Added font increase/decrease/reset buttons (A− A○ A+) in the title bar with `Ctrl+Plus` / `Ctrl+Minus` / `Ctrl+0` keyboard shortcuts. Font scale persists across sessions and applies to all windows.

### Fixed

- **Missing Sound File Handling**: Added detailed logging when sound files are not found, helping identify missing notification assets.

## [2.4.0] - 2026-05-14

### Added

- **Qibla Compass**: New dedicated tab (🧭) with a 240px compass rose, dynamic direction arrow, and bearing readout. Calculates precise Qibla direction from user's location to the Kaaba (21.4225°N, 39.8262°E) using spherical trigonometry. Shows direction name (N, NNE, NE, etc.) and numeric angle. Includes Recalculate button.
- **Digital Tasbih (Dhikr Counter)**: New dedicated tab (📿) with 5 Arabic dhikr phrases (SubhanAllah, Alhamdulillah, Allahu Akbar, La ilaha illallah, Astaghfirullah). Tap or press Space/Enter to count. Includes decrement, reset, and target-snap buttons. Auto-saves daily totals to JSON. Scale animation on increment.
- **Duas Section in Tasbih Tab**: Tab now has dual mode — Tasbih counter and Dua viewer. Includes 26 after-salaam duas and 1 Witr salaam dua, each with full Arabic text, transliteration, and translation in both English and Bangla. Language selector toggles between English/Bangla. Accordion-style cards with collapsible content. INotifyPropertyChanged for smooth expand/collapse.
- **Ramadan Complete Module**: New dedicated tab (🌙) with six integrated sections:
  - **Status Banner**: Live countdown showing current Ramadan day (1-30) with progress bar, or days until next Ramadan.
  - **Daily Dua**: Curated set of 10 duas (Arabic + transliteration + translation), auto-rotated daily.
  - **Pre-Ramadan Preparation**: 7-item checklist visible before Ramadan, auto-saves each item.
  - **Daily Spiritual Goal**: Text input to set today's goal, toggle to mark complete (strikethrough), shows last 7 days history.
  - **Laylatul Qadr Tracker**: Appears during the last 10 nights (Ramadan 21-30). Click to mark/unmark each night. Shows corresponding Gregorian date.
  - **Eid Takbeer Notification**: Toggle to enable a Windows toast notification on Eid day (Shawwal 1, calculated via UmAlQuraCalendar) with the full Takbeer text.
- **Enhanced Taskbar Timer (TrafficMonitor-style)**: New taskbar integration that sits directly on the taskbar with no visible window border. Shows current prayer + countdown + next prayer in a single compact line. Color-coded dot (green=active, yellow=<10min, red=prohibited, gray=upcoming). Four user-selectable positions (Left of Tray, Right of Tray, Center, Left Near Start). Enable via tray menu checkbox or Settings > Layout & Themes. Right-click for position picker/settings/hide. Left-click does nothing (no accidental overlay toggle).
- **Full-Screen Mode (F11)**: Now properly hides the Windows taskbar, custom title bar, and bottom tab bar. Uses primary screen dimensions with Topmost=true to cover the entire display. Restores original window bounds on exit.
- **Tab Navigation**: Added 6-tab bottom navigation bar (Home, Salat, Tracker, Qibla, Tasbih, Ramadan) with emoji icons.

### Changed

- **Toggle Ramadan Mode → Toggle Sawm Mode**: Renamed the toolbar button tooltip and changed the icon from ☾ to 🕌 to better reflect the fasting focus.
- **Full-Screen Exit**: Window now restores to its original position and size instead of fixed minimums.

### Fixed

- **Taskbar Timer Flickering**: Enhanced Taskbar Timer now caches its window position and dimensions (`_lastX/Y/W/H`). `SetWindowPos` is only called when values actually change, eliminating the single-frame flicker on taskbar refocus.
- **Color.FromArgb Compilation**: Fixed byte-casting for all `Color.FromArgb()` calls across RamadanView and TasbihView to resolve ambiguous `byte`/`int` overload resolution under implicit usings.
- **Namespace Ambiguity**: Resolved all `System.Drawing` vs `System.Windows.Media` type conflicts (Color, Brushes, Cursors, FontFamily, Point, KeyEventArgs, HorizontalAlignment, FrameworkElement, UserControl) by fully qualifying or aliasing types.

## [2.3.2] - 2026-04-22

### Changed

- **UI**: Removed the top-left "Support Us" button from the main window.
- **Window Controls**: Added a Maximize button to the top-right window controls alongside Minimize and Close.
- **Navigation Redesign**: Replaced the vertical scrolling layout with a modern, 3-tab bottom navigation system (Home, Salat, Tracker).
- **Tab Rename**: Renamed the "Daily" tab to "Salat" and updated its icon to a clock (🕒) for better clarity.
- **Tracker Integration**: Moved the Tracker feature from a top-bar toggle button to its own dedicated tab in the bottom navigation.
- **Premium Cards**: Updated the prayer sections to use a unified, premium glassmorphic `PremiumCardStyle`.
- **Fard Title**: Added a "Fard Prayers" localized title to match the "Nafal Prayers" layout in the Daily tab.
- **Highlights Refactor**: Adjusted the Home tab "Daily Highlights" grid logic to consistently show 3 items (Suhur, Iftar, Fasting Tracker) in normal mode, and streamline to 1 item (Fasting Tracker) in Ramadan mode.
- **Friday UI Logic**: Implemented dynamic visibility for Friday prayers. The Jumu'ah card now exclusively replaces the Dhuhr card on Fridays until the Jumu'ah Jamaat time passes, after which it automatically reverts to the standard Dhuhr card.
- **Hero Context**: Updated the Hero section to intelligently display "Jumu'ah" instead of "Dhuhr" on Fridays during the appropriate time window.
- **Centered Layout**: Refactored the Home tab to be vertically and horizontally centered on the screen. The Hero section and highlights now dynamically adjust their position for a more balanced "premium" look.

### Fixed

- **Adhan Popup – Close Button**: Fixed incorrect behavior where the Close button was stopping the Adhan audio. The Close button now only dismisses the popup window. The Adhan sound continues to play in the background. Only the **Mute** button stops the audio, as intended.

## [2.3.1] - 2026-04-16

### Fixed

- **Window Ownership Crash**: Fixed the "Window Ownership" crash and corrected the misleading error messages.

## [2.3.0] - 2026-04-14

### Added

- **Interactive Adhan Reply Guide**: A new dual-stage popup during the Adhan. It first presents an exact transliterated and localized response guide—complete with Hadith references—instructing users on what to reply to each call of the Mu'adhdhin.
- **Dynamic Fajr Context**: The reply guide intelligently displays the specific Fajr response ("الصَّلاَةُ خَيْرٌ مِنَ النَّوْمِ") only when the Fajr Adhan is playing.
- **Automated Post-Adhan Dua**: When the Adhan completes, the popup waits 3 seconds before automatically shifting to display the Dua After Adhan, simultaneously playing the localized audio track (`dua_after_adhan.wav`).
- **Official WinGet Support**: Daily Prayer Timer is now available on the official Microsoft WinGet repository. Users can now install and update the application via terminal using `winget install AbiruzzamanMolla.DailyPrayerTimer`.
- **UI/UX Polish**: Updated the Adhan popup with modern, slim scrollbars and glassmorphic touches.

### Fixed

- **C# Build Health**: Resolved nullability warnings (`CS8622`) related to event handlers within WPF `AdhanNotificationWindow`.

## [2.2.1] - 2026-04-14

### Added

- **Hybrid Hijri Calendar System**: Implemented a region-aware Hijri calendar that prioritizes accurate data from the AlAdhan API when online.
- **Improved Hijri Accuracy**: Fixed discrepancies where the Hijri date was off by 1-2 days (e.g., in Bangladesh) by fetching real-time components (Day, Month, Year).
- **Arabic Weekday Display**: Added support for showing the Arabic weekday name (e.g., الثلاثاء) alongside the Hijri date when online.
- **Localized Hijri Months**: Appropriately translates Hijri month names into Bangla and Arabic based on the user's selected language.
- **Smart Fallback**: Automatically reverts to the local UmAlQura calendar when offline or when API requests time out.

### Fixed

- **Settings Window Layout**: Restored visibility of the Save and Cancel buttons by fixing window dimension clipping and resolving a height/min-height mismatch.
- **UI Responsiveness**: Ensured the Settings window scales correctly to accommodate new configuration items.

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

# Changelog

All notable changes to this project will be documented in this file.

The format is based on [Keep a Changelog](https://keepachangelog.com/en/1.0.0/),
and this project adheres to [Semantic Versioning](https://semver.org/spec/v2.0.0.html).

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

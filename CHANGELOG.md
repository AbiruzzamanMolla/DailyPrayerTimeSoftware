# Changelog

All notable changes to this project will be documented in this file.

The format is based on [Keep a Changelog](https://keepachangelog.com/en/1.0.0/),
and this project adheres to [Semantic Versioning](https://semver.org/spec/v2.0.0.html).

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

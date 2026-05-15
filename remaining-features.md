# Remaining Features — Cross-Platform Port

These features exist in the Windows WPF app but are **not yet ported** to the Avalonia cross-platform version (`linux-android-maui` branch).

---

## 1. Windows-Specific (Cannot Port)

These require Win32 APIs, COM, or Windows-only platform features:

| Feature | Reason | Alternative in Linux build |
|---------|--------|---------------------------|
| Enhanced Taskbar Timer | `SetWindowPos`/`FindWindow` into `Shell_TrayWnd` | Replaced by system tray ✅ |
| Legacy COM DeskBand | Windows COM Explorer extension | No equivalent on Linux |
| Floating Overlay Window | Win32 `WS_EX_TOOLWINDOW` + `SetWindowPos` | Replaced by system tray ✅ |
| Windows Toast Notifications | `Microsoft.Toolkit.Uwp.Notifications` | Replaced by `notify-send` ✅ |
| Win32 Full-Screen | Window chrome manipulation | Window manager handles this |
| Registry Auto-Start | Windows Registry Run key | Use `~/.config/autostart/` instead |
| Win32 ColorDialog | `System.Windows.Forms.ColorDialog` | Use cross-platform color picker |

---

## 2. Cross-Platform (Can Port, Low Priority)

These are technically portable but add complexity for limited benefit:

| Feature | Effort | Notes |
|---------|--------|-------|
| **Per-prayer notification controls** | Medium | Checkboxes for Adhan/Reminder/Established per prayer in Settings |
| **Jamaat (Congregation) times input** | Medium | 5 time pickers + validation in Settings |
| **Location search (LocationIQ API)** | Medium | Autocomplete textbox + API call |
| **Adhan audio playback** | High | Needs cross-platform audio lib (`FFmpeg`/`NAudio`) |
| **Tahajjud adhan alarm** | Medium | Timer-based audio trigger |
| **Deed popup after prayer** | Low | Small popup after prayer time changes |
| **Daily summary popup** | Low | End-of-day summary dialog |
| **Loader splash screen** | Low | Initial loading animation |
| **Gradient color pickers** | Low | Custom gradient start/end colors |
| **Prayer sound language** | Low | Audio file selection per language |
| **Basmala hover animation** | Very Low | Arabic/translation crossfade |
| **Auto-backup scheduling** | Low | Daily/weekly/monthly ZIP timer |
| **Prayer list with all rakat details** | Low | Inline rakat count + jamaat time per prayer |
| **Nafal card with expanded info** | Low | Duha/Awwabin/Tahajjud time ranges |

---

## 3. Build & Deployment

| Task | Status |
|------|--------|
| Avalonia Desktop build | ✅ Builds with 0 errors |
| Linux `.deb` package | ❌ Not yet |
| Linux AppImage | ❌ Not yet |
| macOS build test | ❌ Not tested |
| Wayland compatibility | ❌ Not tested |

## Summary

**+3043 lines of cross-platform code** have been ported from the WPF app. All **core functionality** works on Linux/macOS via Avalonia UI. The remaining items are either Windows-specific (cannot port) or low-priority enhancements.

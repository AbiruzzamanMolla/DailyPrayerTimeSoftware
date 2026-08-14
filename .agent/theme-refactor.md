# Implementation Plan - Dynamic Greenish Glassmorphic Theme

Convert the WPF desktop application to a fully dynamic themed interface. Replace all hardcoded colors, borders, text foregrounds, and backgrounds across all views, windows, popups, and overlays with dynamic resources. Implement a dedicated, premium **Greenish Glassmorphic** theme.

## Open Questions

> [!IMPORTANT]
> **Dynamic Theme Settings Configuration:**
> 1. Currently, `AppSettings.cs` stores custom primary, secondary, and gradient colors. Should we lock the application to only use the Greenish Glassmorphic theme, or should we make the "Greenish Glassmorphic" theme the default option that the user can toggle/customize?
> 2. For Glassmorphism, WPF relies on `AllowsTransparency="True"` and semi-transparent backgrounds. Should we keep the window blur active using DwmApi (blur behind/acrylic) if the operating system supports it, or is a semi-transparent gradient layout sufficient?

## Proposed Changes

### Theme Infrastructure

#### [MODIFY] [ThemeHelper.cs](file:///j:/Web%20Development/abir/software/DailyPrayerTime/DailyPrayerTime.Native/Helpers/ThemeHelper.cs)
- Redefine default theme colors in `ThemeHelper.cs` to match a premium "Greenish Glassmorphic" palette.
- Register additional dynamic resources for text foregrounds, card backgrounds, border brushes, highlight/accent colors, and overlay-specific styles.
- Example Greenish Glassmorphic palette resources:
  - `ThemeWindowBgBrush`: LinearGradientBrush with translucent stops (`#E6052E21` to `#E601140F` with ~90% opacity).
  - `ThemeCardBgBrush`: SolidColorBrush with `#1AFFFFFF` or `#2510B981`.
  - `ThemeCardBorderBrush`: SolidColorBrush with `#35FFFFFF` or `#4010B981`.
  - `ThemeTextBrush`: White (`#FFFFFF`).
  - `ThemeTextMutedBrush`: Muted light green-gray (`#A7F3D0` or opacity-based).
  - `ThemeAccentBrush`: Vibrant light green/gold (`#34D399` or `#A3E635`).
  - `ThemeButtonBgBrush`: `#10B981`.
  - `ThemeButtonHoverBgBrush`: `#059669`.

### Window and Views refactoring (Removing Hardcoded Colors)

We will modify each XAML file to replace hardcoded values (like `#064E3B`, `#022C22`, `#FBBF24`, `#1AFFFFFF`, `#111827`, `#White`, `#9ca3af`, etc.) with their dynamic counterparts:

#### [MODIFY] [App.xaml](file:///j:/Web%20Development/abir/software/DailyPrayerTime/DailyPrayerTime.Native/App.xaml)
- Set up application-wide styles that bind properties to `DynamicResource`.

#### [MODIFY] [MainWindow.xaml](file:///j:/Web%20Development/abir/software/DailyPrayerTime/DailyPrayerTime.Native/MainWindow.xaml)
- Replace hardcoded background gradients, titlebar colors, tab hover styles, and card borders with `DynamicResource`.

#### [MODIFY] [DailySummaryPopup.xaml](file:///j:/Web%20Development/abir/software/DailyPrayerTime/DailyPrayerTime.Native/DailySummaryPopup.xaml)
- Replace all hardcoded button colors, text foregrounds, and borders.

#### [MODIFY] [DeedPopup.xaml](file:///j:/Web%20Development/abir/software/DailyPrayerTime/DailyPrayerTime.Native/DeedPopup.xaml)
- Update checkbox chips, buttons, and backgrounds.

#### [MODIFY] [AdhanNotificationWindow.xaml](file:///j:/Web%20Development/abir/software/DailyPrayerTime/DailyPrayerTime.Native/AdhanNotificationWindow.xaml)
- Update notification overlays and sound control panel colors.

#### [MODIFY] [OverlayWindow.xaml](file:///j:/Web%20Development/abir/software/DailyPrayerTime/DailyPrayerTime.Native/OverlayWindow.xaml)
- Ensure prayer time widgets and labels use the dynamic theme.

#### [MODIFY] [SettingsView.xaml](file:///j:/Web%20Development/abir/software/DailyPrayerTime/DailyPrayerTime.Native/SettingsView.xaml)
- Make settings tabs, options, text fields, and panels follow the new theme.

#### [MODIFY] [TrackerView.xaml](file:///j:/Web%20Development/abir/software/DailyPrayerTime/DailyPrayerTime.Native/TrackerView.xaml)
- Update stats cards, grids, and progress indicators.

#### [MODIFY] [TasbihView.xaml](file:///j:/Web%20Development/abir/software/DailyPrayerTime/DailyPrayerTime.Native/TasbihView.xaml)
- Update counter widgets and action buttons.

## Verification Plan

### Automated Tests
- Build check: Run msbuild/dotnet build to verify compilation:
  `dotnet build DailyPrayerTime.Native/DailyPrayerTime.Native.csproj`

### Manual Verification
- Launch the application and verify all pages (MainWindow, Settings, Ramadan, Tasbih, Tracker) load correctly with the theme.
- Trigger the Deed Tracking Popup, Daily Summary Popup, and Adhan Notification Window to verify consistent styling.

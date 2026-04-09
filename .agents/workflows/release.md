---
description: How to prepare and publish a new release for Daily Prayer Timer (Native)
---

# Release Workflow (Native WPF)

Follow this checklist for updating the Daily Prayer Timer native application (.NET 8.0).
Compatible with **Windows 10** and **Windows 11** (x64 & x86).

## 1. Prepare Release

### 1.1 Update Version Numbers

Update the version info in:

- **`DailyPrayerTime.Native/DailyPrayerTime.Native.csproj`**: Update `<Version>` tag.
- **`DailyPrayerTime.DeskBand/DailyPrayerTime.DeskBand.csproj`**: Update `<Version>` or metadata if necessary.
- **`installer_x64.iss`**: Update `AppVersion` define.
- **`installer_x86.iss`**: Update `AppVersion` define.
- **`SettingsWindow.xaml`**: Update the version display in the Dev Info section.

### 1.2 Update Documentation

- **`README.md`**: Update feature list or screenshots if UI changed and update version number also.
- **`CHANGELOG.md`**: Add entry for the new version with `Added`, `Changed`, `Fixed`.

## 2. Validation

Run the app in debug mode to verify:

1. Prayer times are accurate.
2. Prohibited time warnings trigger correctly.
3. Taskbar overlay docks correctly and handles hover/expansion.
4. Settings are saved and loaded after app restart.
5. Tray icon menu works correctly.

## 3. Build & Compile Application (Dual Architecture)

Build standalone, self-contained executables for both **64-bit** and **32-bit** Windows, then compile professional installers for each.

### 3.1 Build Release Binaries

```powershell
# Build DeskBand Library (.NET Framework 4.8)
dotnet build DailyPrayerTime.DeskBand -c Release

# Build 64-bit (x64) Main App
dotnet publish DailyPrayerTime.Native -c Release -r win-x64 --self-contained -p:PublishSingleFile=true -p:PublishReadyToRun=true -p:IncludeNativeLibrariesForSelfExtract=true

# Build 32-bit (x86) Main App
dotnet publish DailyPrayerTime.Native -c Release -r win-x86 --self-contained -p:PublishSingleFile=true -p:PublishReadyToRun=true -p:IncludeNativeLibrariesForSelfExtract=true
```

### 3.2 Compile Installers

```powershell
# Compile 64-bit Installer
& "C:\Program Files (x86)\Inno Setup 6\iscc.exe" "installer_x64.iss"

# Compile 32-bit Installer
& "C:\Program Files (x86)\Inno Setup 6\iscc.exe" "installer_x86.iss"
```

- **Output Location (x64)**: `Output/DailyPrayerTimer_Setup_v{VERSION}_x64.exe`
- **Output Location (x86)**: `Output/DailyPrayerTimer_Setup_v{VERSION}_x86.exe`

## 4. Git Versioning

```powershell
git add .
git commit -m "chore: release version x.x.x"
git tag vx.x.x
git push origin main --tags
```

## 5. GitHub Release

Create a release on GitHub and upload **both** installers.

```powershell
gh release create vx.x.x `
  ./Output/DailyPrayerTimer_Setup_v{VERSION}_x64.exe `
  ./Output/DailyPrayerTimer_Setup_v{VERSION}_x86.exe `
  --title "vx.x.x" `
  --notes "Release notes here"
```

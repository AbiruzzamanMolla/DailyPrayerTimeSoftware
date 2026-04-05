---
description: How to prepare and publish a new release for Daily Prayer Timer (Native)
---

# Release Workflow (Native WPF)

Follow this checklist for updating the Daily Prayer Timer native application (.NET 8.0).

## 1. Prepare Release

### 1.1 Update Version Numbers
Update the version info in:
- **`DailyPrayerTime.Native/DailyPrayerTime.Native.csproj`**: Update `<Version>` tag.
- **`SettingsWindow.xaml`**: Update the version display in the Dev Info section.

### 1.2 Update Documentation
- **`README.md`**: Update feature list or screenshots if UI changed.
- **`CHANGELOG.md`**: Add entry for the new version with `Added`, `Changed`, `Fixed`.

## 2. Validation
Run the app in debug mode to verify:
1. Prayer times are accurate.
2. Prohibited time warnings trigger correctly.
3. Taskbar overlay docks correctly and handles hover/expansion.
4. Settings are saved and loaded after app restart.
5. Tray icon menu works correctly.

## 3. Build Application
Build the standalone, single-file executable for Windows x64.

```powershell
dotnet publish -c Release -r win-x64 --self-contained -p:PublishSingleFile=true -p:PublishReadyToRun=true -p:IncludeNativeLibrariesForSelfExtract=true
```

- **Output Location**: `DailyPrayerTime.Native/bin/Release/net8.0-windows10.0.19041.0/win-x64/publish/DailyPrayerTime.Native.exe`

## 4. Git Versioning
```powershell
git add .
git commit -m "chore: release version x.x.x"
git tag vx.x.x
git push origin main --tags
```

## 5. GitHub Release
Create a release on GitHub and upload the `.exe`.

```powershell
gh release create vx.x.x `
  ./DailyPrayerTime.Native/bin/Release/net8.0-windows10.0.19041.0/win-x64/publish/DailyPrayerTime.Native.exe `
  --title "vx.x.x" `
  --notes "Release notes here"
```

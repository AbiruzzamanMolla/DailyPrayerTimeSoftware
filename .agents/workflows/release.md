---
description: How to prepare and publish a new release for Daily Prayer Timer
---

# Release Workflow

Follow this checklist every time you update the application, add features, or fix bugs. This ensures consistency and proper version control for the Windows desktop application.

## 1. Prepare Release

### 1.1 Update Version Numbers

Update the version number in the following files. Use [Semantic Versioning](https://semver.org/) (e.g., `0.1.0` -> `0.1.1` for patches, `0.2.0` for features).

- **`package.json`**: Update `"version": "x.x.x"`
- **`src-tauri/tauri.conf.json`**: Update `"version": "x.x.x"`

### 1.2 Update Documentation

- **`README.md`**: If new features were added or installation steps changed, update the relevant sections.

### 1.3 Update Changelog

- **`CHANGELOG.md`**: Add a new entry for the version (create the file if it doesn't exist).
  - Header format: `## [x.x.x] - YYYY-MM-DD`
  - Categories: `### Added`, `### Changed`, `### Fixed`, `### Removed`
  - List all significant changes since the last release.

## 2. Validation

### 2.1 Run Tests

Ensure the application runs correctly in development mode.

```bash
npm run tauri dev
```

- Check console for errors.
- Verify the taskbar overlay toggle and dragging functionality.
- Confirm prayer times are calculating correctly for the current location.

### 2.2 Verify Build Configuration

Ensure `src-tauri/tauri.conf.json` has the correct `productName` ("Daily Prayer Timer") and `identifier` ("com.dailyprayertimer.app").

## 3. Build Application

Generate the production executable and installer for Windows.

### 3.1 Windows Desktop (EXE)

```bash
npm run tauri build
```

- **Output Location**: `src-tauri/target/release/bundle/nsis/` (for Windows installer `.exe`)
- Test the generated `.exe` on your system to ensure everything works in production mode.

## 4. Version Control (Git)

### 4.1 Commit Changes

Stage and commit all changes, including the version bumps and changelog.

```bash
git add .
git commit -m "chore: release version x.x.x"
```

### 4.2 Create Tag

Tag the commit with the version number.

```bash
git tag vx.x.x
```

_Example: `git tag v0.1.1`_

### 4.3 Push to Remote

Push the commit and the tags to the remote repository (GitHub).

```bash
git push origin main
git push origin --tags
```

### 4.4 Publish GitHub Release

After pushing tags, use the GitHub CLI to automate the release creation and upload the generated Windows installer:

```bash
gh release create vx.x.x \
  ./src-tauri/target/release/bundle/nsis/Daily-Prayer-Timer_x.x.x_x64-setup.exe \
  --title "vx.x.x" \
  --notes "Release vx.x.x"
```

## 5. Deployment

- Confirm the `.exe` installer appears on the GitHub Releases page at: `https://github.com/AbiruzzamanMolla/DailyPrayerTimerSoftware/releases`
- Notify users of the update.

#!/bin/bash
# Build AppImage for Daily Prayer Timer
# Requires: appimagetool, dotnet SDK 8.0
# Install appimagetool: wget -O /tmp/appimagetool https://github.com/AppImage/AppImageKit/releases/latest/download/appimagetool-x86_64.AppImage

set -e

SCRIPT_DIR="$(cd "$(dirname "${BASH_SOURCE[0]}")" && pwd)"
REPO_DIR="$(dirname "$SCRIPT_DIR")"
OUTPUT_DIR="$REPO_DIR/Output"
APP_DIR="$SCRIPT_DIR/appimage/AppDir"

echo "=== Daily Prayer Timer - AppImage Builder ==="

# Step 1: Publish
echo "[1/3] Publishing .NET app..."
dotnet publish "$REPO_DIR/DailyPrayerTime.Desktop/DailyPrayerTime.Desktop.csproj" \
  -c Release -r linux-x64 --self-contained \
  -o "$APP_DIR/usr/lib/dailyprayertimer"

# Step 2: Set up AppDir
echo "[2/3] Setting up AppDir..."

# AppRun entry point
cat > "$APP_DIR/AppRun" << 'EOF'
#!/bin/bash
HERE="$(dirname "$(readlink -f "${0}")")"
export LD_LIBRARY_PATH="$HERE/usr/lib:$LD_LIBRARY_PATH"
export XDG_DATA_DIRS="$HERE/usr/share:$XDG_DATA_DIRS"
exec "$HERE/usr/lib/dailyprayertimer/DailyPrayerTime.Desktop" "$@"
EOF
chmod +x "$APP_DIR/AppRun"

# Desktop file
cat > "$APP_DIR/dailyprayertimer.desktop" << 'EOF'
[Desktop Entry]
Name=Daily Prayer Timer
Comment=Prayer times, Qibla, Tasbih, Duas, Ramadan tracker
Exec=dailyprayertimer
Icon=dailyprayertimer
Terminal=false
Type=Application
Categories=Utility;Religion;
EOF

# Create a minimal icon (1x1 transparent PNG as placeholder)
# In production, replace with a proper 256x256 icon
mkdir -p "$APP_DIR/usr/share/icons/hicolor/256x256/apps"
# Use avalonia-logo from assets as base
if [ -f "$REPO_DIR/DailyPrayerTime.Desktop/Assets/avalonia-logo.ico" ]; then
    cp "$REPO_DIR/DailyPrayerTime.Desktop/Assets/avalonia-logo.ico" "$APP_DIR/dailyprayertimer.ico" 2>/dev/null || true
fi

# Copy i18n
cp -r "$APP_DIR/usr/lib/dailyprayertimer/i18n" "$APP_DIR/usr/lib/dailyprayertimer/i18n" 2>/dev/null || true

# Step 3: Build AppImage
echo "[3/3] Building AppImage..."
VERSION="2.4.0"
ARCH=x86_64
OUTPUT="$OUTPUT_DIR/DailyPrayerTimer-${VERSION}-${ARCH}.AppImage"

# Try appimagetool from common locations
APPIMAGETOOL=""
for path in /tmp/appimagetool /usr/local/bin/appimagetool /usr/bin/appimagetool; do
    if [ -x "$path" ]; then
        APPIMAGETOOL="$path"
        break
    fi
done

if [ -z "$APPIMAGETOOL" ]; then
    echo "appimagetool not found. Downloading..."
    wget -q "https://github.com/AppImage/AppImageKit/releases/latest/download/appimagetool-x86_64.AppImage" -O /tmp/appimagetool
    chmod +x /tmp/appimagetool
    APPIMAGETOOL=/tmp/appimagetool
fi

ARCH=x86_64 "$APPIMAGETOOL" "$APP_DIR" "$OUTPUT"
chmod +x "$OUTPUT"

echo "=== Done ==="
echo "AppImage: $OUTPUT"
echo ""
echo "Run with: $OUTPUT"

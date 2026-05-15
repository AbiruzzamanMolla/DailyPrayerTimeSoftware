#!/bin/bash
# Build .deb package for Daily Prayer Timer
# Run this on Debian/Ubuntu with dpkg-deb installed

set -e

# Paths
SCRIPT_DIR="$(cd "$(dirname "${BASH_SOURCE[0]}")" && pwd)"
REPO_DIR="$(dirname "$SCRIPT_DIR")"
OUTPUT_DIR="$REPO_DIR/Output"
DEB_DIR="$SCRIPT_DIR/deb"
BUILD_DIR="/tmp/dailyprayertimer-deb-build"

echo "=== Daily Prayer Timer - .deb Package Builder ==="

# Step 1: Publish the app
echo "[1/3] Publishing .NET app for linux-x64..."
dotnet publish "$REPO_DIR/DailyPrayerTime.Desktop/DailyPrayerTime.Desktop.csproj" \
  -c Release -r linux-x64 --self-contained \
  -o "$BUILD_DIR/usr/lib/dailyprayertimer" \
  /p:PublishSingleFile=false

# Step 2: Assemble package structure
echo "[2/3] Assembling package structure..."
mkdir -p "$BUILD_DIR/DEBIAN"
cp -r "$DEB_DIR/DEBIAN/control" "$BUILD_DIR/DEBIAN/"

mkdir -p "$BUILD_DIR/usr/bin"
cat > "$BUILD_DIR/usr/bin/dailyprayertimer" << 'SCRIPT'
#!/bin/bash
exec /usr/lib/dailyprayertimer/DailyPrayerTime.Desktop "$@"
SCRIPT
chmod +x "$BUILD_DIR/usr/bin/dailyprayertimer"

# Desktop file
mkdir -p "$BUILD_DIR/usr/share/applications"
cp "$DEB_DIR/usr/share/applications/dailyprayertimer.desktop" "$BUILD_DIR/usr/share/applications/"

# Copy i18n files
cp -r "$BUILD_DIR/usr/lib/dailyprayertimer/i18n" "$BUILD_DIR/usr/lib/dailyprayertimer/i18n" 2>/dev/null || true

# Step 3: Build .deb
echo "[3/3] Building .deb package..."
VERSION=$(grep "^Version:" "$DEB_DIR/DEBIAN/control" | awk '{print $2}')
mkdir -p "$OUTPUT_DIR"
fakeroot dpkg-deb --build "$BUILD_DIR" "$OUTPUT_DIR/dailyprayertimer_${VERSION}_amd64.deb"

echo "=== Done ==="
echo "Package: $OUTPUT_DIR/dailyprayertimer_${VERSION}_amd64.deb"
echo ""
echo "Install with: sudo dpkg -i $OUTPUT_DIR/dailyprayertimer_${VERSION}_amd64.deb"

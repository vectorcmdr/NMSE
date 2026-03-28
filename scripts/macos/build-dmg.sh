#!/usr/bin/env bash
# ================================================================
# build-dmg.sh - Build an NMSE macOS DMG for Wine-based usage
#
# Creates a macOS .app bundle containing the NMSE Windows build
# and a launcher script that finds Wine on the user's system
# (Whisky, CrossOver, or Homebrew Wine).
#
# The resulting DMG is a drag-and-drop installer:
#   1. Open the DMG
#   2. Drag NMSE.app to Applications
#   3. Double-click NMSE.app to launch (Wine must be installed)
#
# Prerequisites (build host):
#   - macOS (any architecture)
#   - hdiutil (built into macOS)
#   - NMSE published build (self-contained win-x64)
#
# Usage:
#   ./build-dmg.sh /path/to/nmse-publish-output [output-file.dmg]
#
# Output:
#   NMSE-x64.dmg  (~40-60 MB, Windows build + launcher)
#
# Users need Wine installed separately (Whisky recommended):
#   brew install --cask whisky
# ================================================================

set -euo pipefail

SCRIPT_DIR="$(cd "$(dirname "${BASH_SOURCE[0]}")" && pwd)"
REPO_ROOT="$(cd "$SCRIPT_DIR/../.." && pwd)"

# Input validation
if [[ $# -lt 1 ]]; then
    echo "Usage: $0 <path-to-nmse-publish-output> [output-file.dmg]"
    echo ""
    echo "  <path-to-nmse-publish-output>  Directory containing NMSE.exe and Resources/"
    echo "  [output-file.dmg]              Optional output path (default: NMSE-x64.dmg)"
    exit 1
fi

NMSE_PUBLISH_DIR="$1"

if [[ ! -f "$NMSE_PUBLISH_DIR/NMSE.exe" ]]; then
    echo "ERROR: NMSE.exe not found in $NMSE_PUBLISH_DIR"
    echo "Run 'dotnet publish NMSE.csproj -c Release -r win-x64 --self-contained' first."
    exit 1
fi

# Verify hdiutil is available (should be on all macOS)
if ! command -v hdiutil >/dev/null 2>&1; then
    echo "ERROR: hdiutil not found. This script requires macOS."
    exit 1
fi

OUTPUT_FILE="${2:-$SCRIPT_DIR/NMSE-x64.dmg}"

# Create temporary build directory
BUILD_DIR="$(mktemp -d)"
echo "[BUILD] Working directory: $BUILD_DIR"

# ── Create .app bundle structure ──────────────────────────────
APP_DIR="$BUILD_DIR/NMSE.app"
CONTENTS="$APP_DIR/Contents"
MACOS="$CONTENTS/MacOS"
RESOURCES="$CONTENTS/Resources"
NMSE_APP_DIR="$RESOURCES/NMSE"

mkdir -p "$MACOS" "$RESOURCES" "$NMSE_APP_DIR"

# Copy NMSE Windows build files
echo "[BUILD] Copying NMSE application ..."
cp -R "$NMSE_PUBLISH_DIR"/. "$NMSE_APP_DIR/"

# ── Create launcher script ────────────────────────────────────
echo "[BUILD] Creating launcher script ..."
cat > "$MACOS/nmse-launcher" <<'LAUNCHER_EOF'
#!/bin/bash
# ──────────────────────────────────────────────────────────────
# NMSE macOS Wine Launcher
#
# Searches for Wine in common macOS locations and launches
# NMSE.exe.  Displays a dialog if Wine is not found.
#
# Supported Wine sources (checked in order):
#   1. Whisky.app (free, recommended)
#   2. Homebrew wine64 (Apple Silicon: /opt/homebrew)
#   3. Homebrew wine64 (Intel: /usr/local)
#   4. CrossOver.app (commercial)
#   5. Anything on $PATH
# ──────────────────────────────────────────────────────────────

SCRIPT_DIR="$(cd "$(dirname "$0")" && pwd)"
NMSE_DIR="$SCRIPT_DIR/../Resources/NMSE"
NMSE_EXE="$NMSE_DIR/NMSE.exe"

if [[ ! -f "$NMSE_EXE" ]]; then
    osascript -e 'display dialog "NMSE.exe not found.\nThe application may be damaged." buttons {"OK"} default button "OK" with title "NMSE Error" with icon stop' 2>/dev/null
    echo "ERROR: NMSE.exe not found at $NMSE_EXE" >&2
    exit 1
fi

# ── Locate Wine ───────────────────────────────────────────────
WINE=""

# Whisky (free, recommended for Apple Silicon)
WHISKY_WINE="/Applications/Whisky.app/Contents/Resources/Libraries/Wine/bin/wine64"
# Homebrew (Apple Silicon)
BREW_ARM="/opt/homebrew/bin/wine64"
# Homebrew (Intel)
BREW_INTEL="/usr/local/bin/wine64"
# CrossOver (commercial)
CROSSOVER_WINE="/Applications/CrossOver.app/Contents/SharedSupport/CrossOver/bin/wine64"

for candidate in "$WHISKY_WINE" "$BREW_ARM" "$BREW_INTEL" "$CROSSOVER_WINE"; do
    if [[ -x "$candidate" ]]; then
        WINE="$candidate"
        break
    fi
done

# Fallback to $PATH
if [[ -z "$WINE" ]]; then
    if command -v wine64 >/dev/null 2>&1; then
        WINE="$(command -v wine64)"
    elif command -v wine >/dev/null 2>&1; then
        WINE="$(command -v wine)"
    fi
fi

if [[ -z "$WINE" ]]; then
    osascript -e 'display dialog "Wine is required to run NMSE on macOS.\n\nRecommended (free): Install Whisky\nhttps://getwhisky.app\n\nbrew install --cask whisky\n\nSee the included README for other options." buttons {"OK"} default button "OK" with title "NMSE — Wine Required" with icon caution' 2>/dev/null
    echo "ERROR: Wine not found. Install Whisky: brew install --cask whisky" >&2
    exit 1
fi

# ── Configure Wine environment ────────────────────────────────
export WINEPREFIX="${HOME}/Library/Application Support/NMSE/wineprefix"
export WINEARCH=win64
export WINEDLLOVERRIDES="mscoree=d;mshtml=d"
export WINEDEBUG="-all"

# Create prefix on first run
if [[ ! -d "$WINEPREFIX/drive_c" ]]; then
    echo "Creating Wine prefix (first run) ..."
    "$WINE" wineboot --init 2>/dev/null || true
fi

exec "$WINE" "$NMSE_EXE" "$@"
LAUNCHER_EOF
chmod +x "$MACOS/nmse-launcher"

# ── Create Info.plist ─────────────────────────────────────────
echo "[BUILD] Creating Info.plist ..."
cat > "$CONTENTS/Info.plist" <<'PLIST_EOF'
<?xml version="1.0" encoding="UTF-8"?>
<!DOCTYPE plist PUBLIC "-//Apple//DTD PLIST 1.0//EN"
  "http://www.apple.com/DTDs/PropertyList-1.0.dtd">
<plist version="1.0">
<dict>
    <key>CFBundleExecutable</key>
    <string>nmse-launcher</string>
    <key>CFBundleName</key>
    <string>NMSE</string>
    <key>CFBundleDisplayName</key>
    <string>NMSE - No Man's Save Editor</string>
    <key>CFBundleIdentifier</key>
    <string>com.vectorcmdr.nmse</string>
    <key>CFBundleVersion</key>
    <string>1.0</string>
    <key>CFBundleShortVersionString</key>
    <string>1.0</string>
    <key>CFBundlePackageType</key>
    <string>APPL</string>
    <key>CFBundleIconFile</key>
    <string>NMSE</string>
    <key>LSMinimumSystemVersion</key>
    <string>12.0</string>
    <key>NSHighResolutionCapable</key>
    <true/>
    <key>NSHumanReadableCopyright</key>
    <string>NMSE — Open Source Save Editor for No Man's Sky</string>
</dict>
</plist>
PLIST_EOF

# ── Copy icon ─────────────────────────────────────────────────
if [[ -f "$REPO_ROOT/Resources/app/NMSE.ico" ]]; then
    cp "$REPO_ROOT/Resources/app/NMSE.ico" "$RESOURCES/NMSE.ico"
    echo "[BUILD] Copied application icon"
fi

# ── Create DMG staging area ───────────────────────────────────
echo "[BUILD] Staging DMG contents ..."
DMG_STAGING="$BUILD_DIR/dmg-staging"
mkdir -p "$DMG_STAGING"

# Copy .app bundle
cp -R "$APP_DIR" "$DMG_STAGING/"

# Add Applications symlink for drag-and-drop install
ln -s /Applications "$DMG_STAGING/Applications"

# Add README
cat > "$DMG_STAGING/README.txt" <<'README_EOF'
NMSE - No Man's Save Editor (macOS via Wine)
=============================================

This application requires a Wine compatibility layer to run.

INSTALLATION:
  1. Drag NMSE.app to your Applications folder
  2. Install Wine (if not already installed)
  3. Double-click NMSE.app to launch

WINE OPTIONS (pick one):

  Whisky (Free, recommended for Apple Silicon Macs):
    https://getwhisky.app
    brew install --cask whisky

  CrossOver (Paid, best Apple Silicon support):
    https://www.codeweavers.com/crossover

  Wine via Homebrew (Intel Macs):
    brew install --cask wine-stable

NMS SAVE FILE LOCATIONS:
  Steam (native macOS):
    ~/Library/Application Support/HelloGames/NMS/<profile>/

  Steam (via Wine/Whisky):
    Inside the Wine bottle under:
    drive_c/users/<user>/AppData/Roaming/HelloGames/NMS/

For detailed instructions, visit:
  https://github.com/vectorcmdr/NMSE
README_EOF

# ── Build DMG ─────────────────────────────────────────────────
echo "[BUILD] Creating DMG ..."
hdiutil create \
    -volname "NMSE" \
    -srcfolder "$DMG_STAGING" \
    -ov \
    -format UDZO \
    "$OUTPUT_FILE"

echo "[BUILD] ✓ DMG created: $OUTPUT_FILE"
echo "[BUILD] Size: $(du -sh "$OUTPUT_FILE" | cut -f1)"

# Cleanup
rm -rf "$BUILD_DIR"
echo "[BUILD] Done."

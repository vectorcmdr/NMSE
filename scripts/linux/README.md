# NMSE Linux Scripts

Scripts and configuration files for running NMSE on Linux via Wine.

## Contents

| File | Description |
|------|-------------|
| `nmse.sh` | Main launch script - detects Wine, manages prefix, launches NMSE |
| `build-appimage.sh` | Builds a self-contained AppImage bundling NMSE + Wine |
| `AppRun` | Entry point used inside the AppImage (called by the AppImage runtime) |
| `nmse.desktop` | FreeDesktop `.desktop` entry for system/AppImage integration |
| `bottles.yml` | Bottles configuration reference for setting up an NMSE bottle |

## Quick Start

### Option A: Manual Wine (Recommended for most users)

```bash
# 1. Install Wine 9.0+
sudo apt install wine          # Ubuntu/Debian
sudo dnf install wine          # Fedora
sudo pacman -S wine            # Arch

# 2. Download the latest NMSE Windows build from:
#    https://github.com/vectorcmdr/NMSE/releases
# Or use the GitHub API:
DOWNLOAD_URL=$(curl -s https://api.github.com/repos/vectorcmdr/NMSE/releases/tags/latest \
  | grep -o '"browser_download_url": "[^"]*\.zip"' \
  | head -1 | cut -d'"' -f4)
wget "$DOWNLOAD_URL" -O NMSE-latest.zip
unzip NMSE-latest.zip -d app/

# 3. Run NMSE
./nmse.sh
```

### Option B: AppImage (Self-contained, no Wine install needed)

```bash
# Download the pre-built AppImage from the releases page:
#   https://github.com/vectorcmdr/NMSE/releases
# (Look for NMSE-x86_64.AppImage if available)
chmod +x NMSE-x86_64.AppImage
./NMSE-x86_64.AppImage
```

### Option C: Bottles (GUI Wine manager)

See `docs/bottles-linux-guide.md` for step-by-step Bottles setup, or reference `bottles.yml`
for the configuration values.

## Building the AppImage

To build an AppImage yourself:

```bash
# 1. Publish NMSE on Windows (self-contained)
dotnet publish NMSE.csproj -c Release -r win-x64 --self-contained

# 2. Copy the publish output to your Linux machine
scp -r bin/Release/net10.0-windows/win-x64/publish/ linux-host:~/nmse-build/

# 3. On Linux, run the build script
./build-appimage.sh ~/nmse-build/
```

This produces `NMSE-x86_64.AppImage` - a single file users can download and run.

## Save File Locations

NMSE under Wine uses `Z:\` to access the Linux filesystem. Common NMS save locations:

| Installation | Linux Path | Wine Path |
|-------------|-----------|-----------|
| Steam (native Proton) | `~/.local/share/Steam/steamapps/compatdata/275850/pfx/drive_c/users/steamuser/AppData/Roaming/HelloGames/NMS` | `Z:\home\<user>\.local\share\Steam\...` |
| Steam (Flatpak) | `~/.var/app/com.valvesoftware.Steam/data/Steam/steamapps/compatdata/275850/...` | `Z:\home\<user>\.var\app\...` |

The `nmse.sh` launch script automatically detects and displays your save location on startup.

## Troubleshooting

| Issue | Solution |
|-------|----------|
| Wine not found | Install Wine 9.0+ from your package manager or WineHQ |
| Font rendering issues | Run `winetricks corefonts` in the NMSE Wine prefix |
| DPI scaling wrong | Run `./nmse.sh --winecfg` -> Graphics tab -> adjust DPI |
| App doesn't start | Run `./nmse.sh --debug` and check `nmse-wine.log` |
| Prefix corrupted | Run `./nmse.sh --reset-prefix` to recreate it |

## Full Documentation

- [Wine Linux Guide](../../docs/wine-linux-guide.md) - comprehensive setup and troubleshooting
- [Bottles Guide](../../docs/bottles-linux-guide.md) - Bottles GUI Wine manager setup
- [Cross-Platform Work Plan](../../_ref/cross-platform-workplan.md) - full migration roadmap

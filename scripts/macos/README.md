# NMSE macOS Scripts

Scripts and configuration files for running NMSE on macOS via Wine compatibility layers.

## Contents

| File | Description |
|------|-------------|
| `build-dmg.sh` | CI script - builds a macOS DMG containing NMSE.app with Wine launcher |
| `nmse-whisky.rb` | Homebrew Cask formula - installs NMSE as a Whisky-managed app |
| `README.md` | This file |

## macOS Options

NMSE is a Windows WinForms application. On macOS, it runs via a Wine compatibility layer.
Three options are available:

### Option 1: Whisky (Free, Recommended)

[Whisky](https://getwhisky.app) is a free, open-source Wine wrapper with a native SwiftUI interface.
It supports Apple Silicon (M1/M2/M3/M4) via Rosetta 2.

**Quick setup:**
```bash
# Install via Homebrew
brew install --cask whisky

# Download NMSE Windows build
# Then follow the guide: docs/whisky-macos-guide.md
```

Or use the Homebrew Cask formula to automate the download:
```bash
# From the NMSE repository root:
brew install --cask scripts/macos/nmse-whisky.rb
```

See [Whisky macOS Guide](../../docs/whisky-macos-guide.md) for detailed instructions.

### Option 2: CrossOver (Paid, Best Apple Silicon Support)

[CrossOver](https://www.codeweavers.com/crossover) is a commercial Wine distribution ($74/year 👎)
with the best Apple Silicon compatibility and professional support.

See [CrossOver macOS Guide](../../docs/crossover-macos-guide.md) for setup instructions.

### Option 3: Direct Wine (Intel Macs only)

For Intel-based Macs, you can install Wine directly:
```bash
brew install --cask wine-stable
wine /path/to/NMSE.exe
```

This does **not** work on Apple Silicon Macs without Rosetta 2 + CrossOver/Whisky.

## NMS Save File Location on macOS

| Installation | Path |
|-------------|------|
| Steam (native macOS) | `~/Library/Application Support/HelloGames/NMS/<profile>/` |
| Steam (CrossOver/Whisky) | Inside the Wine bottle under `drive_c/users/<user>/AppData/Roaming/HelloGames/NMS/` |

## Homebrew Cask Formula

The `nmse-whisky.rb` file is a Homebrew Cask formula that:
1. Downloads the latest NMSE Windows build from GitHub Releases
2. Extracts it to `~/Applications/NMSE/`
3. Depends on Whisky being installed
4. Provides a launch helper

To use it locally:
```bash
brew install --cask ./nmse-whisky.rb
```

To submit to Homebrew's cask tap (for `brew install --cask nmse`), the formula
would need to be submitted to [homebrew-cask](https://github.com/Homebrew/homebrew-cask).

## Full Documentation

- [Whisky macOS Guide](../../docs/whisky-macos-guide.md) - recommended free option
- [CrossOver macOS Guide](../../docs/crossover-macos-guide.md) - paid option with best Apple Silicon support
- [Cross-Platform Work Plan](../../_ref/cross-platform-workplan.md) - full migration roadmap

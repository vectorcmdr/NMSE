## NMSE — No Man's Save Editor v1.0.391 (preview)

> This is a preview build. Please practice safe backup practices and expect some bugs.

### Changelog

#### Bug Fixes:

- Fix for inventory sub-panel item detail numeric up/down control values being defaulted to 1 for negative numbers when clicking apply, making some tech/upgrades unusable.

<br />

<details>
<summary>Previous Changelogs</summary>

### Previous Changelog 1.0.390 (preview)

#### Bug Fixes:

- Fix regression for some ship technology item filter causing incorrect values for the items charge/amount (resulting in corrupted parts in slots).

<br />

### Previous Changelog 1.0.389 (preview)

#### Features:

- Add 'fake/glitch' galaxy 257 (Yilsrussimil) to the galaxy list. (Issue #33)

#### Bug Fixes:

- Fix galaxy 256 name (Odyalutai) in galaxy list. (Issue #33)

<br />

### Previous Changelog 1.0.385 (preview)

#### Bug Fixes:

- Fix for "Ship" type Technology item filtering for starships (Sentinel and Corvette ships).

<br />

### Previous Changelog 1.0.384 (preview)

#### Bug Fixes:

- Small fix for edge case crashes in icon loading for the inventory grid resulting in a broken image instead of the item icon.

<br />

### Previous Changelog 1.0.383 (preview)

#### Bug Fixes:

- Critical fix for corvette import edge cases where import could fall back to Seed<->TS lookup and collide base data, causing the import to steal another corvettes base data, invalidating the other corvette in the process.

<br />

### Previous Changelog 1.0.382 (preview)

#### Bug Fixes:

- Small fix for corvette tech inventories visual bug, where they lost the override information from internal parts to construction part IDs when resized.

<br />

### Previous Changelog 1.0.381 (preview)

#### Features:

- Starship panel now support importing ships with modified model resource filenames (like Orb Explorers). They will display with a "Modified" tag in the list.
- Export container for starships has been modified for better cross-compatibility (it is backwards compatible with older versions).

#### Bug Fixes:

- Fix for issues with importing corvettes into empty slots.


<br />

### Previous Changelog 1.0.380 (preview)

#### Features:

- Starship panel buttons cleaned up and converged into a less confusing set of different functions.
- Starship panel code cleanup (internal).

#### Bug Fixes:

- Starship wrappers and invalidation methods improved over previous builds to stop edge-case refuse data and better align import/export.


<br />

### Previous Changelog 1.0.379 (preview)

#### Features:

- Starship panel now has an indicator for corvette optimisation to show if the current parts list has been optimised or not (red cross = not optimised / green tick = optimised). (Feature Request #24)
- Importing a ship will now populate an empty slot if you have one in your roster (including .nmsship ZIP packages). (Issue/FR #26)
- Minor tweaks to Starship panel layout.
- Account rewards panel now lists the expedition number for season rewards and has support for both account unlock, and save file redemption for each item (allowing for per save control). (Issue/FR #25)
- NMSE.Extractor (developer facing tool) updated to parse additional reward info.


<br />

### Previous Changelog 1.0.376 (preview)

#### Features:

- Add import file type filter to Corvette import button for .nmsship ZIP packages from NMS Model IO Tool.

<br />

### Previous Changelog 1.0.375 (preview)

#### Features:

- Enhance ship names in drop down list (ships with no custom name now show their slot, type and class - named ships show their slot, name and class). (Feature Request #23)
- Increase all base state value maximums to int.MaxValue to completely unclamp Starship, Multi-tool and Freighter stats. (Feature Request #23)
- Settlement population value max clamp raised to 400. Colored warnings changed to exclusive values.

#### Bug Fixes:

- Additional fixes for XBOX / Game Pass saves and some general enhancements and safeties around console save editing. (Issue #18, Issue #22)

<br />

### Previous Changelog 1.0.374 (preview)

#### Features:

- Discoveries panel renamed to Catalogue to align with the game.
- Optimise button added to Corvettes in the Starship panel (re-orders components for better handling stats and quicker loading times).
- Support for NMS Model IO Tool .nmsship importing.
- macOS .dmg available (requires Wine/Whisky/etc. to be installed).
- Raw JSON Editor improvements:
    - Basic inline editing.
    - In window export/import buttons.
    - Simple 'show changes' diff viewer.
    - Search back/forward.
    - Basic notifier to show if changes were made.
    - Breadcrumbs (with links) for current key/value.
    - Basic type icons in tree ({} (properties), [] (arrays), A (text), # (numbers), ✓ (booleans), ∅ (null)).
    - Drag-and-drop array reordering.
    - Basic undo/redo stack for Edit, Add, Delete actions.
    - Additional keyboard shortcuts:
        - Copy (<kbd>Ctrl+C</kbd>)
        - Search focus (<kbd>Ctrl+F</kbd>)
        - Clear (<kbd>Esc</kbd>)
        - Undo / Redo (<kbd>Ctrl+Z</kbd> / <kbd>Ctrl+Y</kbd>).
        - Search forward / back (<kbd>F3</kbd> / <kbd>Shift+F3</kbd>)


#### Bug Fixes:

- Fix for XBOX Game Pass (container size and new header, blobs load into expected panels). (Issue [#18](https://github.com/vectorcmdr/NMSE/issues/18))
- Fix for high byte characters and binary data (fixes special character parsing in some places).
- Fix for caret stripping in Known Technologies and Known Products.
- Raw JSON Editor fix for value write-back failing under some input conditions.
- Corvette ship -> base matching algorithm updated with more robust approach.
- Minor tweaks to Settlement UI.
- Fix CSS typo in companion site.

<br />

### Previous Changelog 1.0.369 (preview)

#### Features:

- Introduces additions to the Settlement Panel:
    - Sub panel added.
    - Additional stats in stats panel.
    - Clamps loosened on stats with color indicators for outside of "game rules" bounds.
    - Production moved to it's own tab.
    - Building States (experimental) tab added. Contains initial reverse engineered state data and an ability to set building states from list or integer.
    - Building Editor (experimental) tab added. Contains the same reverse engineered states but with the ability to set each bit in the bitflag (for custom states).
- Linux AppImage builds now on CI
- Both Windows and Linux downloads now supported on the website (via latest build fetch).

#### Bug Fixes:

- Fix for XBOX Game Pass save paths (add current path and leave support for legacy install paths). (Issue [#18](https://github.com/vectorcmdr/NMSE/issues/18))
- Fixes for locale/cultural input settings (InvariantCulture) in many places. Apologies to our European friends! (Issue [#13](https://github.com/vectorcmdr/NMSE/issues/13))
- Fix for Corvette Technology losing it's proper icon on drag-and-drop operations. (Issue [#19](https://github.com/vectorcmdr/NMSE/issues/19))
- Fix for Corvette Import/Export (TS<->Seed tolerance for imprecision). (Issue [#17](https://github.com/vectorcmdr/NMSE/issues/17))
- Data integrity fixes for values exceeding internal clamps in saves if not edited directly (bypass clamping validation).
- Add additional safety for images in memory (if corruption in memory occurs).
- Minor buttons resize fixes. (Issue [#9](https://github.com/vectorcmdr/NMSE/issues/9))

<br/>

### Previous Changelog 1.0.362 (preview)

- Fixed Frigates AOT trimming error due to use of `DisplayMember` (reflection metadata was stripped) in favour of overrides only.

<br/>

### Previous Changelog 1.0.361 (preview)

- Project moved to Native AOT with trimmig for builds. Users don't require .NET 10 to be separately installed anymore.
- Fix for icon loading issue for Windows taskbar (due to DB loading workaround).

<br/>

### Previous Changelog 1.0.356 (preview)

- Further GDI hardening to help with crash-to-desktop for some users. ()
- Add item dialog fixes for discoveries panels (multi-select bugs).
- Fix for comma separated floats in the UI (via InvariantCulture).
- Further chnages to buttons for DPI scaling (AutoSize).
- UI localisation string fixes.

These fixes and changes address Issues #7, #5, #13, #9, and #11

<br/>

### Previous Changelog 1.0.355 (preview)

This preview release contains critical bug fixes and additions for the following:

- Fix GDI disposal/safety bug causing crash-to-desktop. (Should close Issue #7)
- Timestamp and default to last for save file dialog. (Should close Issue #8)
- Chest resizing expanded. (Should close Issue #10)
- Fix UI scaling/DPI issues with text and buttons. (Should close Issue #9)

#### Also addresses these items from the Discord support channel (these should close Issue #11):
- Account Rewards search filter causing crash.
- PS4 saves not loading (context swapper not looking at keys deep enough).
- Save slot name in slot loader.
- Split save load toolbar (Directory / Slot + File).
- Changes to handling for special characters such as λ & Ŧ in save names (and other strings).
- Placeholder back-text for Starships, Multi-tools, Frigates, Freighters, Companions with procedural names (without custom names).
- Change "Known Locations" to "Teleport Destinations".
- Add XBOX exclusive (specific special) helmet to NMSE.Extractor and platform rewards DB.
- Settlement perks needs expanding to 18.
- Backups should exclude .dds files from cache backup.
- Backup call needs moving to start of save call to avoid pointless backup bloat.
- Raw JSON Editor find next bugs need fixing.

</details>

<br />

_Documentation will need some future updates to cover these changes._

**Thanks everyone for your help so far squashing launch bugs. Please keep the feedback and testing coming!**

Happy travels interlopers! 👨‍🚀
**_-vector_cmdr_**

### Getting Started

User guides are available from the [repo](https://github.com/vectorcmdr/NMSE/blob/main/docs/user/README.md), or via the [website](https://nmse.vectorcmdr.xyz/).

Download via **Assets** below (Windows (ZIP), Linux (AppImage) and macOS (DMG - requires Wine)).

> Linux and macOS users can also run NMSE manually via Wine — see the [guides](https://github.com/vectorcmdr/NMSE#-cross-platform-via-wine).

> 🛡️ VirusTotal scans for peace of mind are pending [here](#): pending...

<!-- 
> 🛡️ VirusTotal scans for peace of mind are available [here](https://www.virustotal.com/gui/file/86a36fc8dc34134403b86909814623c14b22d381b9c3bb53bd15806206806298?nocache=1): passing ✔️
 -->

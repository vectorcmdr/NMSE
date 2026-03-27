## NMSE — No Man's Save Editor v1.0.369 (preview)

> This is a preview build. Please practice safe backup practices and expect some bugs.

### Changelog

#### Features:

- Introduces additions to the Settlement Panel:
    - Sub panel added.
    - Additional stats in stats panel.
    - Clamps loosened on stats with color indicators for outside of "game rules" bounds.
    - Production moved to it's own tab.
    - Building States (experimental) tab added. Contains initial reverse engineered state data and an ability to set building states from list or integer.
    - Building Editor (experimental) tab added. Contains the same reverse engineered states but with the ability to set each bit in the bitflag (for custom states).
- Linux AppImage builds now on CI (thanks @maniro-x)
- Both Windows and Linux downloads now supported on the website (via latest build fetch).

#### Bug Fixes:

- Fix for XBOX Game Pass save paths (add current path and leave support for legacy install paths). (Issue [#18](https://github.com/vectorcmdr/NMSE/issues/18))
- Fixes for locale/cultural input settings (InvariantCulture) in many places. Apologies to our European friends! (Issue [#13](https://github.com/vectorcmdr/NMSE/issues/13))
- Fix for Corvette Technology losing it's proper icon on drag-and-drop operations. (Issue [#19](https://github.com/vectorcmdr/NMSE/issues/19))
- Fix for Corvette Import/Export (TS<->Seed tolerance for imprecision). (Issue [#17](https://github.com/vectorcmdr/NMSE/issues/17))
- Data integrity fixes for values exceeding internal clamps in saves if not edited directly (bypass clamping validation).
- Add additional safety for images in memory (if corruption in memory occurs).
- Minor buttons resize fixes. (Issue [#9](https://github.com/vectorcmdr/NMSE/issues/9))

<br />


<details>
<summary>Previous Changelogs</summary>

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

Download via **Assets** below (for Windows and Linux).

> Linux and macOS users can also run NMSE via Wine — see the [guides](https://github.com/vectorcmdr/NMSE#-cross-platform-via-wine).

> 🛡️ VirusTotal scans for peace of mind are pending [here](#): pending...

<!-- 
> 🛡️ VirusTotal scans for peace of mind are available [here](https://www.virustotal.com/gui/file/86a36fc8dc34134403b86909814623c14b22d381b9c3bb53bd15806206806298?nocache=1): passing ✔️
 -->
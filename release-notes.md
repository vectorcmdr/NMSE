## NMSE — No Man's Save Editor v1.0.362 (preview)

### Changelog

- Fixed Frigates AOT trimming error due to use of `DisplayMember` (reflection metadata was stripped) in favour of overrides only.

<br />

<details>
<summary>Previous Changelog 1.0.361 (preview)</summary>

### Changelog

- Project moved to Native AOT with trimmig for builds. Users don't require .NET 10 to be separately installed anymore.
- Fix for icon loading issue for Windows taskbar (due to DB loading workaround).

</details>

<details>
<summary>Previous Changelog 1.0.356 (preview)</summary>

### Changelog

- Further GDI hardening to help with crash-to-desktop for some users. ()
- Add item dialog fixes for discoveries panels (multi-select bugs).
- Fix for comma separated floats in the UI (via InvariantCulture).
- Further chnages to buttons for DPI scaling (AutoSize).
- UI localisation string fixes.

These fixes and changes address Issues #7, #5, #13, #9, and #11

</details>

<details>
<summary>Previous Changelog 1.0.355 (preview)</summary>

### Changelog

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

Download via **Assets** below.

> Linux and macOS users can run NMSE via Wine — see the [guides](https://github.com/vectorcmdr/NMSE#-cross-platform-via-wine).

> 🛡️ VirusTotal scans for peace of mind are pending [here](#): pending...

<!-- 
> 🛡️ VirusTotal scans for peace of mind are available [here](https://www.virustotal.com/gui/file/86a36fc8dc34134403b86909814623c14b22d381b9c3bb53bd15806206806298?nocache=1): passed ✔️
 -->
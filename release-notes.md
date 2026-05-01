## NMSE — No Man's Save Editor v1.1.52 (preview)

> This is a preview build. Please practice safe backup practices and expect some bugs.

### Changelog

#### Bug Fixes:

- The "Check Consistency" button in Account Rewards has had the comparison list tightened based on further analysis of the `Known*`, etc. arrays. It will now show substantially less potential items for manual syncing across state arrays. More changes may come soon to further tighten this list. (Per Issue #64)
- The "Check Consistency" dialog box now shows a basic warning message. (Per Issue #64)
- Advanced save utility has had it's slot management logic updated to fix incorrect slots/files being managed when moving/swapping/copying/deleting. (Per Issue #63)

<br />

<details>
<summary>Previous Changelogs</summary>

### Changelog 1.1.49 (preview)

#### Features:

- Added support to Settlement Panel for displaying settlements that match any Owner ID in your saves discovery owners, even for duplicate platforms IDs, refuse IDs, etc.

#### Bug Fixes:

- Fix for importing to empty slot for NMS Model IO Tool exported corvettes by adding a base data structure rebuilder for the base JSON node (these files don't include full base data - only object data). In theory this fix will also allow other malformed corvette imports with varying degrees of success - your mileage may vary importing malformed data.

<br />

### Changelog 1.1.48 (preview) - hotfix

#### Bug Fixes:

- Fixed a bug with French special character ranges U+0080 - U+00FF (e.g. éèàç) being parsed/written correctly after the unicode change in the JSON parser.

Affected users will unfortunately need to re-enter these names as the fix cannot be made 'self-healing' in this case. Sorry about that French friends!

<br />

### Changelog 1.1.47 (preview) - hotfix

#### Bug Fixes:

This is a quick hotfix to correct bulk recharge skipping procedural technology items due to not filtering the seed suffix from the item ID.
Apologies everyone!

<br />

### Changelog 1.1.46 (preview) - hotfix

#### Bug Fixes:

This is a quick hotfix to correct the bulk inventory actions in the Tools menu (they didn't get re-tested after the backend update) and to bump the game version to 6.34.

<br />

### Changelog 1.1.45 (preview)

#### Features:

- Updated Raw JSON Editor:
    - Text view now has basic syntax colouring and node folding
    - Split view now available (text view + tree view in one window with node syncing)
    - Diffing window "Show Changes" improved with custom rendering, basic syntax colouring, line numbers, Myers diff algorithm, hunk management.
    - Memory management rebalanced to help lower it despite the additional editing features.
- NumericUpDown UI controls fully swapped to a custom solution for supporting doubles (backend) and large decimals (frontend). (per Issue #56)
- Inventory bulk actions are available in the "Tools" menu for repair/refill/recharge on all inventories. (per Discord Feature Request)

#### Bug Fixes:

- Backend and rendering of the Inventory Grid has been rewritten to reduce handles, drastically lowering them to 10% of previous use and protecting against potential edge case crashes preemptively.
- Rewards no longer subscribe to a sync event automatically, allowing users to keep out of sync reward/known* arrays. Syncing is still available via editing a reward/known or by using the Check Consistency button.
- Raw number guards extended via the NUD replacement and additional changes to panel calls. (per Issue #56)
- All string based numbers in the save are now managed in multiple logic layers for a double (backend) decimal (frontend) round-trip to avoid actual precision loss, or perceived precision loss via G17 and custom handling. (per Issue #56)
- Supported Game Version label in auto build fixed. (per Discord Bug Report)
- Overall tweaks to memory management and JSON object handling (not user facing).
- Minor change to unicode symbols handling (not user facing).
- Companion Database updated to do reverse lookup on part descriptors for creatures with shared rigs (like HERMITCRAB) for accessory rules. (per Issue #61)

<br />

### Previous Changelog 1.1.29 (preview)

#### Features:

- Companion pet battle affinity details are now loaded from the DB (derived from game MXML ForceAffinity rules).

#### Bug Fixes:

- Added localisation for ability/move list. (per Issue #59)
- Fixed mechanical and anomalous pet battle affinities via feature change. (per Issue #58)
- Added additional raw double guard method and changes to calls for all string based high precision numbers to further mitigate erroneous precision loss on ~billionths decimal values. (per Issue #56)

<br />

### Previous Changelog 1.1.28 (preview)

#### Features:

- JSON key mappings update for Game Update 6.33.
- Minor DB updates for Game Update 6.33.
- Companion pet battle moves support updated for the new key location/system in Game Update 6.33 (which means access to cooldown/boost is gone).
- Companion pet battle ability details re-ordered to prioritise effect over type.
- Companion pet accessory customisation updated with game rules for slots and with in game color palette.

#### Bug Fixes:

- Fixed bug with companion pet accessory display / selection via the feature change above. (Per Issue #51)
- Fixed erroneous Auto / Manual tagging on save files in the UI. (Per Issue #55)
- NMS FloatValue fields changed to be cast to double always to avoid non-integral / non-integer precision issues. (Per Issue #56)

<br />

### Previous Changelog 1.1.20 (preview)

#### Features:

- Added the ability to induce an egg from a companion (and place / replicate it into the exosuit inventory). (per Discord FR)
- Added type matchup information to the Companion panel Battle tab for companion affinity.
- Added pet battle team selection to the battle tab.
- Added new DB for creatures in place of outdated hardcoded DB, includes minor UI improvements due to this.
- Updated inventory UI to include a two section Slot Details and Item Picker that function independently (per FR #44):
    - Slot details now displays the currently selected slot for reference.
    - Item picker is enhanced with icon and description elements for the selected item.
    - Item picker adds/replaces items directly instead of via slot details.
    - Item class mini icon is displayed next to the icon.
    - Item descriptions are now available via a tooltip on hovering over the information icon in place of the description block.
    - Forced item reselection is reduced with this new workflow.
- Updated the base moving functionality in the Bases tab to use a vector coordinate transforming algorithm (using Gram-Schmidt process).
- Added terrain edit clearing for bases (per FR #12).
- Added updated galactic core colour system for galaxy display (via PR#43 - thanks maniro-x)
- Improved display of galaxy information in the Teleport Destinations tab.
- Added additional milestones to the milestone list (more to come).

#### Bug Fixes:

- Updated the companion pet battle class display to better show when it is not in override and is using the procedural class values (currently unable to display).
- Fixed an import bug with pet accessories.
- Updated pet battle Affinity names in the UI to in game version instead of MXML lookup names.
- Updated icon and delayed load system to improve icon reliability on load splash.
- Significant internal cleanup (non user facing).
- Test suite cleanup.

<br />

### Previous Changelog 1.1.17 (preview)

> ⚠️ _Please use the companion editing responsibly for PvP. Don't have fun at the expense of other players._

> _Game table companion pet battle features are based on available data and testing. As always, I welcome additional input on the implementation via Issues._

#### Features:

- Updated for 6.32 Xeno Arena.
- Added small, simple loading splash for better loading feedback.
- Added accessory customisation to companion panel:
    - Change left / right / chest accessories and their colors and scale.
- Added new 'Battle' tab to the companion panel that supports editing:
    - Stats class override support (Health, Agility, Combat Effectiveness).
    - Holo-Arena victories count.
    - Mutation progress.
    - Gene edits available (points you can use for leveling stats, or rerolling skills).
    - Health / Agility / Combat gene modification.
    - 5x Ability slots editing (selection of base ability types by ID with description) and their cooldown and score boost value.
    - Displays companion affinity and move information.
- Account rewards reworked to better sync reward states:
    - Added a sync check between seen/unlocked state arrays.
    - Added a check consistency button that gives feedback with adjustment buttons (based on sync state).
    - Added Known Specials tab in Catalogue that lists the seen/redeemed/known special items from account rewards for extra use.
- Added Export / Import JSON node to the Raw JSON Editor via the right click context menu.

#### Bug Fixes:

- Fix for Technology Module and Upgrades filtering in inventory types (via DB re-categorisation).
- Fix for delete key not working in Raw JSON Editor.
- Fix for "Repair" and "Repair All" in inventories not removing the damaged items from the slot (but still repairing on load to game).
- Fix for accountdata.hg accidental compression (game gracefully loaded, so was non-breaking).
- Fix for inventory sorting to stop inventories from sorting based on the previous selection on a same session save reload / panel switch. Now defaults to "None" between same session load / switch.
- Changed "Backup" and "Restore" button naming in the Bases tab so they match the Export / Import naming of the other panels.
- Numerous internal fixes and changes (non user facing).

<br />

### Previous Changelog 1.0.397 (preview)

#### Features:

- Database updated with 6.30.0.1 items, titles, etc.
- JSON key mappings updated for 6.30.0.1.
- Companion count updated to new limit of 30.

<br />

### Previous Changelog 1.0.396 (preview)

#### Features:

- Inventory item details / picker now has a minimum size to help preserve the UI.
- Inventory item details / picker has the 5 digit seed value for proc tech in a separate field with a generate button below. (Issue #37)
- Frigate trait selector now shows the type for the effect and not just the effect value. (FR #39)

#### Bug Fixes:

- Fix for procedural tech items so they now correctly allocate a proper 5 digit seed value and don't mangle them under particular circumstances. (Issue #37)
- Fix for the repair function in inventory so that it now correctly sets damage, amount and fully installed values so items don't get "stuck" in the tech inventory when repaied but not installed in game. (Issue #38)

<br />

### Previous Changelog 1.0.393 (preview)

#### Features:

- Inventory grids now have additional sorting/stacking functionality (thanks thiago-rcarvalho):
    - Inventory grids now support sorting by name/category.
    - Inventory grids can have items sent to other inventories to auto-stack (such as exosuit to chest, ship or freighter).
    - Inventory grid slots/cells have a new pin button to protect the slot from auto stacking.

#### Bug Fixes:

- Further changes have been made to the way that upgrade/procedural tech installation filters default values based on the MXML defaults to hopefully capture any items that are installing as broken.

<br />

### Previous Changelog 1.0.391 (preview)

#### Bug Fixes:

- Fix for inventory sub-panel item detail numeric up/down control values being defaulted to 1 for negative numbers when clicking apply, making some tech/upgrades unusable.

<br />

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
- macOS .dmg available (requires Wine/Gcenx Wine Builds/etc. to be installed).
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

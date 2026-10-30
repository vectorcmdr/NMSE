## NMSE - No Man's Save Editor v1.5.12

### Changelog

Forsaken update? Happy spooky season! 🎃🦇

#### Features:

- **DB and mappings updated for Game Update 7.11 (Forsaken)**
- Added Larva Cannon to the multi-tool list.
- Improved the metadata for the Linux AppImage. (per AppImage team listing requirements)
- Improved the Bases list to show types, have better sort selection and allow legacy and temporary bases. (per Issue / FR #146)
- Improved Base export/import to include extra information for terrain and positionals. (per Discord FR)
- Improved the Companion list by splitting pets and eggs, and adding sort options. (per FR #145)
- Improved Milestone panel categorisation. (per #140)
- Added JSON bookmarking allowing users to bookmark favourite/common keys/nodes. (per Discord FR)

#### Bug Fixes:

- Fixed hotkey labels. (per Issue #141)
- Fixes for Catalogue Wonders. (per Issue #137)

<br />

<details>
<summary>Previous Changelogs</summary>

### Changelog v1.4.29

#### Features:

- Add external change detection options to Edit menu. (per FR #135)
- Add Xbox account external change detection for data blob sections.
- Add in-game quick menu hotkey editing support. (per FR #76)
- Improve platform save path detection.
- Add Sort All Chests feature to the chest inventories (per PR #128) - thanks `@radialmonster`

#### Bug Fixes:

- Fix for multiple external change popups. (per Issue #135)
- Fix for high display scaling on some UI fields. (per Issue #133)
- Fix for extra 'unknowable' game items in the Known Technologies list. (Per Issue #136)

<br />

### Changelog v1.4.22

#### Features:

- **DB and mappings updated for Game Update 7.04 (Cosmos) + Experimental 23/9.**
- Added full Procedural Tech support:
    - Procedural names.
    - Procedural stats on item picker pane tooltip.
    - Suggested 'best roll' seeds based on +/- weight.
    - Stats/seed search.
    - Tech can be packed & tech packages can be installed from the context menu.
- Added Pet Battle true class and stat support:
    - True x/x/x classes displayed.
    - Procedural stats displayed.
    - Added a seed search implementation that can search for a seed pair from a class and stat request.
    - Searched seeds are stamped with `0xC0D3` and have subtle limitations on search depth (because we don't have 585k years to wait for the full space).
- Small improvement to loading times and DB size. (per Discord)
- Improved JSON parser for Emojis and Rare Glyphs used by some user names to improve cross-platform interop.
- Added initial Xbox XGS save format support (works alongside current Xbox support). (per FR #125)
- Added Xbox save slot copy support. (per FR #124)
- Added JSON Editor settings persistence to config. (per Discord)
- Added control tokens per platform to descriptions, so that they show `[E]` etc.
- Added Developer Commentary support to the catalogue panel. (per Discord)
    - Can be set like other collected knowledge items, or double clicked to view and set individual commentary unlocks.
- Added external save change detection. (per Discord)

#### Bug Fixes:

- Fix for Starship Legacy Colours bug in export/import. (per Issue #126)
- Fix for entitlement platform rewards types to allow them to be redeemed correctly. (per Issue #129)
- Fix for Autophage Word sync and add Shards milestone tracking. (per Issue 131)
- Fix for live change switching in Starship and Exocraft panel so that changes to crafts don't require a save before changing to a new one. (per Discord)
- Fix for Fishing in catalogue not recording to all lists. (per Issue #134)
- Fix for Known Words export/import. (per Discord)
- Fix for hotloading issues in panels. (per Issue #123)

<br />

### Changelog v1.3.16

_**It's another pretty big one with a lot going on under the hood.**_

Happy NMS 10th Anniversary everyone! 🥳
(Special hello to all the Project Skyscraper Operators out there 😉)

#### Features:

- **DB and mappings updated for Game Update 7.03 (Cosmos).**
    - Adds all the new items, rewards, tools, ships, parts, etc. from the update and expedition.
- Add new items types (such as space station buildables) to the Inventory item picker system.
- Added support for Space Station ownership acceleration.
- Added support for Space Station bases and Space bases.
- Added support for Space POI system:
    - Note: There is an algorithm involved in unpacking the data used for this and NMSE captures it on first load of a save. This means that the earlier the system is in exploration, the more accurate NMSE can resolve the POI types to the slots. Even if it can't identify it (not enough pattern info), it can still change the states without issue.
    - Makes Space POIs determinable.
    - Makes Space POI state able to be changed (Hidden, Undiscovered, Discovered, Complete) - they can never be changed to lower than their starting state (so no hiding the stars!).
- Added 'Discoveries' library system.
    - Creates a folder and stores discovery based info.
    - Currently only used for Space POIs, but will be extended to collections and discoveries in the future.
- Added support to the Raw JSON Editor for an isolated node view in split view mode (allows you to view only that nodes code). (per FR #118)
- Added a Database Search panel that allows users to search the loaded item database without needing to load a save.
    - Shows additional hidden data.
    - Allows for wildcard searching.
- Added Catalogue Completion system to the catalogue panel set, which allows you to manage the in game catalogue UI completion independent of unlocks.
    - Adds Wonders, Collected Knowledge, Fossils, Raw Materials and Discovery Stats management.
    - Extends cross support into the other Known* panels etc.
- Improvements to some underlying code and some other cleanup tasks (non-user-facing).

#### Bug Fixes:

- Fix for Consistency Dialog and DataView trimming crashes. (per Issue #121 & Discord report)
- Fix for Switch format save meta files. (per Discord report)
- Fix for Preset game mode selection UI bug.
- Fix for Xbox blob date/time race condition resulting in wrong date in UI. (per Issue #122)
- Fix for themes not saving to config for reload. (per Issue #117)

<br />

### Changelog 1.2.23

#### Features:

- Added support for freighter bases in the Bases panel.
- Added support for base objects editing in the Bases panel via a new sub-panel tab.
- Added a backup picker system to the Restore save functionality + some minor improvements to the backup manager. (per Discord feedback)
- Added a preload 'Lock Overlay' for when the user hasn't yet loaded a save file to help guide users. (per Discord feedback)
- Username detection improved. (per Discord feedback)

#### Bug Fixes:

- Item categorisation fixes for the Extractor and database JSON for GAS items and somme Swarm crafting items. (per Issue #112)
- Fix for "The Wraith" ship to allow the correct tech install. (per Issue #110)
- Fix for some further UI scaling / DPI issues at higher scaling sizes (such as 150%). (per Issue #88)
- Fix for "Save Transfer" from another platform to XBOX/XGP containers/blobs format so they are rebuilt properly.

<br />

### Changelog 1.2.7

#### Features:

- **DB and mappings updated for Game Update 6.45.1 (The Swarm) to include the three trophies**
- Added basic light / dark theme support (dark theme is a little limited due to WinForms constraints that still support Wine). (per FR #101)
    - This can be found under the main toolstrip at the top of the editor.
- Added backup directory selection. (per FR/Issue #106)
- Added backup directory fallback and hardening for restricted file systems (such as squashfs within AppImages). (per FR/Issue #106)
- Added active context switching / handling for base vs expedition save structures (improves use in Expedition saves).
- Added player outfit selector and export / import support (see new extension in Export Config panel). (per NMScord request)
- Added A->Z / Z->A sorting to the Bases list.
- Added ability to delete bases. 9Per FR #104)
- Increased system index to support the whole range (0-4095) instead of the older low range (phantom blocks, Purple Systems, etc. are now reachable via the UI). (per Issue #105)
- Added "MAX" button to currencies. (per FR #103)
- Added "GOTO JSON" System (per FR #67):
    - Each panel that has a *sensible* section of JSON to navigate to has a button for it in the top right of that panel.
    - The GOTO JSON button will send you to the Raw JSON Editor to the "master" key for the section.
    - Hover over the button to see the tooltip for what section it is sending you to.
    - The system is limited to certain sets of master keys / values / arrays to keep things sensible and manageable - it may be extended in the future.
    - While using it to jump to a JSON block can't hurt your save - please consider it an advanced use feature. Bug reports are welcome.

#### Bug Fixes:

- Improved loading Splash DPI / Scaling support. (per NMScord report)
- Fixed wrong location being displayed in the Exocraft Station panel. (per Issue #102)
- Fixes for false flag DataModified events in all panels (false "Unsaved Changes" prompts). (per Issue #109)
- Fix for base NPC moving missing the final array element. (per Discord report)
- Fix for TechPack duplication (high byte writing issue). (per Discord report)
- Fix for expedition order sorting in the Account Rewards UI. (per Discord report)

<br />

### Changelog 1.1.93 (preview)

#### Features:

- **DB and mappings updated for Game Update 6.43 (The Swarm).**
- User Guide link now in the Help menu. (per FR #93)
- Added Exocraft station management to the Exocraft panel (new tab) (per FR #79):
    - Allows viewing details and deleting stations.
    - Lists separate stations around the galaxies and those in your bases.
- Added "Known Recipes" panel to the Recipes panel in the Catalogue that functions similarly to the other "Known" Catalogue panels. (per FR #96)
- Added _**EXPERIMENTAL**_ Multiplayer panel to the MainStats panel. It provides:
    - Viewing of the co-op / team multiplayer keys.
    - **NOTE:** Most are overridden by the game and functionality is not guaranteed.<br/>
      Given the testing/issues for this game update, the stability and use of these keys is likely to change as HG tweak this concept for future use.
    - This panel may be locked down or removed in the future.
- Inventory grid panel has had a rework of the Item Picker pane, moving the search above the information per user feedback poll. (per FR #99)
- NMSE Extractor updated for BC7 (not user facing).

#### Bug Fixes:

- Multi-tools fixes / additions (per Issue #98):
    - Updated multi-tool type detections that are based on stats (via game file reference).
    - Corrected erroneous test resource names that didn't get replaced on launch.
    - Added size change option.
- Fixed timestamp not updating in the UI on slot reload from the menu. (per Discord)
- More robust user name detection; now lists all that have an ID in Discovery section and then falls back to user ID bases. (per Discord)

<br />

### Changelog 1.1.79 (preview)

#### Features:

- AppConfig (`NMSE.conf`) moved to be alongside the executable - attempts a one time best effort config transition from the old directory. (per FR #86)
- Sails for Solar ships added to the Customisation tab (the colour change is local only as the game shows default blue in multiplayer for fabricated ships). (per FR #90)
- Ship Customisation tab UI rearranged and some names changed to better match in-game names. (per FR #91)
- Pet Battle ability list has a restricted move system to support abilities that HG remove from the UI but keep in the data. (per Issue #82)
    - `REVIVE` is now no longer selectable as a new ability and only displayed for companions that currently have it assigned. It was removed by HG and forwarded to a STUB ability.
- The application now subscribes to per-monitor DPI settings. (per Issue #88)
- Some localisation strings updated across different languages.
- Updater pattern changed for potentially better handling when run in synced folders (OneDrive, Dropbox, Drive, iCloud, Box, pCloud, SugarSync, etc.). (per Issue #85)
    - Updater call methods hardened.
    - Folder checks for known sync folders.
    - Back-off and timeout update options to help with file lock during sync hashing.
- Updated additional PS4 format support (HTOS .hg, SW bin-wrap .hg). The 6x PS4 formats still need proper user testing for the non `.dat` formats.
- Improved backup management under restricted environments and compatibility layers.

#### Bug Fixes:

- Fixed companion pet body part descriptors not being deep copied to an egg when it is induced. (per Issue #89)
- Numerous buttons and fields updated for high res high scale setups (such as 4K @ 150%). (per Issue #88)
- Some DataModified events weren't being captured to notify the user to save changes if they were closing after changes. These are now fixed. (per Issue #87)
- ZIP backups now only check and include `.hg` files specifically, instead of the current directory, helping with backups when users load a save file from a weird place with many files, such as sitting on a bloated desktop, or in the root of a drive.
- Fixed some name updates not refreshing from the underlying JSON layer when changed. Including Base name not being reflected until reload. (per Issue #92)
- Fixed ship type change not being refreshed visually in the prefix in the ship list, or in the scene resource field despite the underlying data being modified successfully.
- Pressing <kbd>Enter</kbd> in a field now commits the data immediately to the in memory data and ends focus in the field, instead of only clicking to change focus, or using <kbd>Tab</kbd>. (per Issue #92)

<br />

### Changelog 1.1.64 (preview)

#### Features:

- "Clear All Terrain Except Base" added to the Bases panel - allows clearing of terrain edits, excluding edits tied to bases. (Per Issue #78) 
- Magic bytes / headers for multiple PS4 save formats updated to newer versions to support more up to date formats.
- Starship Archive support added. (Per Issue #3) Allows:
    - Importing of archived ships into the active list from the games archived ships slots.
    - Moving of a starship to the archived ships list from the active list.
- Multi-tool Archive support added. (Per Issue #3) Allows:
    - Importing of archived multi-tools into the active list from the games archived multi-tool slots.
    - Moving of a multi-tool to the archived multi-tool list from the active list.
> Archives Caveat: <br />
> Archives arrays store procedural names to string when stored in game, but standard arrays don't under any normal circumstances. <br />
> This means they may display with no name if archived from the editor with a procgen name as the editor cannot generate the names. <br />
> There is no way around this without reverse engineering the name generation markov assembler and robinhood tables.

#### Bug Fixes:

- Syncing of `Seen*` arrays removed from the rewards sync process so catalogued seen items aren't affected by items that aren't unlocked. Will be used for a future "seen" panel.

<br />

### Changelog 1.1.61 (preview)

#### Features:

- Mappings / DB updated for 6.36 (no user facing changes).
- Add editing for Warp to Next Space Battle. (Per FR #70)
- Improved updater to use native solution over scripted solution. (Per FR #72)
- Added release notes link in about menu. (Per FR #46)
- Added release notes to and improved the new update available popup dialog (from this build onwards). (Per FR #46)
- Added the ability to re-order bases in the teleport list (and array list) - note: the game will still replace the first few with "immediate local teleports" in the UI, such as close-by bases and freighters. This seems unavoidable. (Per FR #73)
- Added ability to remove all terrain edits in the save from the Bases panel. (Per FR#66)
- Backup management cleaned up. `.backup` files are no longer produced / used in favour of the `.zip` backups.
- Progression levels added to the Guilds milestones and minor milestones re-arrangement / cleanup. (Per FR #68)
- Added ability to enable Purple System warping from the Player -> General -> Current Coordinates section (enables the warping and adds the drive to KnownTech). (Per FR #77)
- Added "Customisation" tab to the Starship panel (experimental feature) (Per FR #29) which allows:
    - Modification of the scene resource (model) filename for all ships for ship glitching (like orbs).
    - Modification of customisation options for player built ships.

#### Bug Fixes:

- Additional filter changes to the "Check Consistency" dialog box sync list (Per Issue #64)

<br />

### Changelog 1.1.52 (preview)

#### Bug Fixes:

- The "Check Consistency" button in Account Rewards has had the comparison list tightened based on further analysis of the `Known*`, etc. arrays. It will now show substantially less potential items for manual syncing across state arrays. More changes may come soon to further tighten this list. (Per Issue #64)
- The "Check Consistency" dialog box now shows a basic warning message. (Per Issue #64)
- Advanced save utility has had it's slot management logic updated to fix incorrect slots/files being managed when moving/swapping/copying/deleting. (Per Issue #63)

<br />

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

**Thanks to everyone for your support and for choosing this editor. Please keep the feedback coming!**

Happy travels interlopers! 👨‍🚀
**_-vector_cmdr_**

### Getting Started

User guides are available from the [repo](https://github.com/vectorcmdr/NMSE/blob/main/docs/user/README.md), or via the [website](https://nmse.vectorcmdr.xyz/).

Download via **Assets** below (Windows (ZIP), Linux (AppImage) and macOS (DMG - requires Wine)).

> Linux and macOS users can also run NMSE manually via Wine - see the [guides](https://github.com/vectorcmdr/NMSE#linux--macos).

> 🛡️ VirusTotal scans for peace of mind are pending [here](#): pending...

<!-- 
> 🛡️ VirusTotal scans for peace of mind are available [here](https://www.virustotal.com/gui/file/86a36fc8dc34134403b86909814623c14b22d381b9c3bb53bd15806206806298?nocache=1): passing ✔️
 -->

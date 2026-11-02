# NMSE User Guide

Welcome to **NMSE (NO MAN'S SAVE EDITOR)** - the open source No Man's Sky Save Editor.<br>
This guide walks you through every feature of the application so you can confidently edit your *No Man's Sky* save files.

---

## Table of Contents

- [Getting Started][getting-started]
- [Opening a Save File][opening-save]
- [Saving Your Changes][saving-changes]
- [Backups & Restoring][backups]
- [Tabs Overview][tabs-overview]
- **Editing Guides:**
  - [Player Stats][player-stats]
  - [Exosuit][exosuit]
  - [Multi-tools][multitools]
  - [Starships][starships]
  - [Fleet (Freighter, Frigates & Squadron)][fleet]
  - [Exocraft (Vehicles)][exocraft]
  - [Companions (Pets)][companions]
  - [Bases & Storage][bases]
  - [Catalogue (Technologies, Products, Specials, Words, Glyphs, Locations, Fishing, Recipes, Wonders, Knowledge, Fossils, Raw Materials & Stats)][catalogue]
  - [Milestones][milestones]
  - [Settlements][settlements]
  - [ByteBeats][bytebeat]
  - [Account Rewards][account]
  - [Export Settings][export-config]
  - [Raw JSON Editor][raw-json]
  - [Database Search][database-search]
- [Importing & Exporting Inventories][import-export]
- [Language & Theme][language]
- [Keeping NMSE Updated][keeping-updated]
- [Frequently Asked Questions][faq]

---

## Getting Started

### What You Need

- **Windows 10 or 11** (64-bit). Release builds are also published as a Linux AppImage (with bundled Wine) and a macOS DMG (Wine launcher)
- A *No Man's Sky* save file from any supported platform

### Installing NMSE

1. Download the latest version from the [**Releases page**][releases]
2. Extract the `.zip` file to any folder on your computer
3. Double-click **`NMSE.exe`** to launch the editor

> 💡 No installation is required - NMSE is fully portable. You can run it from a USB drive or any folder.


While NMSE is a native Windows application, it runs on **Linux** and **macOS** via Wine compatibility layers. Release builds are published as a Windows `.zip`, a Linux AppImage with bundled Wine, and a macOS DMG with a Wine launcher. Guides are available for [Wine][guide-wine], [Bottles][guide-bottles], [Gcenx Wine Builds][guide-gcenx-wine] (free, recommended for macOS) and [CrossOver][guide-crossover] (paid, supported).

### Supported Platforms

NMSE can edit save files from all platforms that *No Man's Sky* supports:

| Platform | Supported | Auto-Detect | Notes |
|----------|:-----------:|:-----------:|-------|
| Steam | ✅ | ✅ | Saves found automatically |
| GOG | ✅ | ✅ | Saves found automatically |
| Xbox Game Pass | ✅ | ✅ | Saves found automatically |
| PlayStation 4 | ✅ | - | Requires manual file transfer - supports `memory.dat`, SaveWizard `NOMANSKY` `.hg` and homebrew `savedataNN.hg` files |
| Nintendo Switch | ✅ | - | Requires manual file transfer - detected from `savedataNN.hg` and matching `manifestNN.hg` files |

---

## Opening a Save File

Your save directory and files should be auto-detected and shown in the toolbar. The toolbar has two rows: **Directory** and **Backup** on the first row, then **Save Slot**, **File**, <kbd>Load</kbd> and <kbd>Save</kbd> on the second row.

If a save and slot are already selected, click <kbd>Load</kbd> and you're good to go. If they aren't, there are two ways to open your save files:

### Method 1: Open Save Directory (Recommended)

1. Click <kbd>Browse...</kbd> or <kbd>File > Open Save Directory</kbd> in the menu bar
2. Navigate into your save directory and choose it
3. A list of available save slots will appear in the UI
4. Select the save slot and save file you want to edit
5. Click <kbd>Load</kbd>

While no save is loaded, every editing tab shows a no-save overlay and its controls are greyed out. **Export Settings** and **Database Search** stay usable because they do not need a save.

> ℹ️ Save folders under Linux (using the AppImage, you can browse with either / or Z:).
> 
> - SteamOS: ```/home/deck/.local/share/Steam/steamapps/compatdata/275850/pfx/drive_c/users/steamuser/AppData/Roaming/HelloGames/NMS/st_<steamid>/```
> - Flatpak: ```/var/home/<username>/.var/app/com.valvesoftware.Steam/.local/share/Steam/steamapps/compatdata/275850/pfx/drive_c/users/steamuser/AppData/Roaming/HelloGames/NMS/st_<steamid>/```
> - Package: ```/home/<username>/.steam/steam/steamapps/compatdata/275850/pfx/drive_c/users/steamuser/AppData/Roaming/HelloGames/NMS/st_<steamid>/```
> - GOG (Heroic Launcher): ```/home/<username>/Games/Heroic/Prefixes/default/No Mans Sky/pfx/pfx/drive_c/users/steamuser/AppData/Roaming/HelloGames/NMS/DefaultUser/```
>
> 💡 Make a link in your /home/<username> so it is easier next time!

### Method 2: Load Individual Save File (Manual)

1. Click <kbd>File > Load Save File</kbd>
2. Browse to the save file on your computer
3. Select the file and click **Open**

*Use this method for PlayStation or Nintendo Switch saves that you've copied to your PC. PS4 users can open `memory.dat` directly (the Save Slot and File combos then show Auto/Manual saves), or an individual `savedataNN.hg`. Switch saves use `savedataNN.hg` with a matching `manifestNN.hg`. SaveWizard `NOMANSKY` `.hg` files are written back in the same format.*

### Platform File Layouts

Save files differ between platforms. NMSE auto-detects the layout when you open a directory:

| Platform | Files | Notes |
|----------|-------|-------|
| Steam / GOG | `save.hg`, `save2.hg`, ... plus `mf_*.hg` meta files | Steam uses `st_<steamid>` folders, GOG uses `DefaultUser` |
| Xbox Game Pass | `xgs` slot folders (`Slot1Auto\data`, `Slot1Manual\meta`, ...) or legacy `containers.index` plus GUID-named save blobs | Usually under `%LOCALAPPDATA%\Packages\HelloGames*\SystemAppData\` (`xgs\<id>\` or `wgs\<id>\`) |
| PlayStation 4 | `memory.dat`, `savedataNN.hg` (HTOS/homebrew), or SaveWizard `NOMANSKY` `.hg` | `memory.dat` holds up to 5 slots, each with an auto and a manual save |
| Nintendo Switch | `savedataNN.hg` data files with matching `manifestNN.hg` files | Game slots start at `savedata02.hg`; `savedata00.hg` is settings |

When you select a PS4 `memory.dat` slot, the **Save Slot** combo lists the game slot and the **File** combo lists its **Auto** and **Manual** saves for you to load.

---

## Saving Your Changes

After making edits, save your changes using one of these methods:

- <kbd>File > Save</kbd> - Overwrites the original file (a ZIP backup is created automatically)
- <kbd>File > Save As</kbd> - Save to a new location without overwriting the original

> ⚠️ **Important:** While NMSE creates automatic backups, it's good practice to keep your own copy as well. Always keep a backup of your original save files before editing.

---

## Backups & Restoring

Every time you save, NMSE creates a timestamped ZIP backup of the save directory. The **10 most recent** backups for each save folder are kept, and older ones are deleted automatically.

### Where Backups Live

Backups are written to the first usable location in this order:

1. The **Backup** folder selected by the **Backup:** combo on the toolbar
2. A `Save Backups` folder next to `NMSE.exe`
3. A `Save Backups` folder under your TEMP directory (a last-resort fallback so backups are never silently skipped)

If you move your saves around, check the **Backup:** combo so your backups stay where you expect them.

### Choosing a Backup to Restore

If something goes wrong, you can restore from a backup via the **Edit** menu:

- <kbd>Edit > Restore Backup (All)</kbd> - pick a backup, then restore every file it contains (save files, meta files and account data) back into the save directory
- <kbd>Edit > Restore Backup (Single)</kbd> - pick a backup, then restore only the currently loaded save file

Both options open the **Backup Picker**, which lists the available backups newest first with their created time, size and location. Select the one you want and confirm.

| Backup Picker |
|-------|
| <img src="../img/backup-picker-dialog.png"> |

---

## Tabs Overview

NMSE organises all editing features into tabs along the top of the window. Click any tab to switch to that section. <br>
Most sections support export and import in different ways (with cross-editor compatibility).

| Tab | What It Does |
|-----|-------------|
| **Player** | Health, currencies, difficulty presets, coordinates, outfits, guides, titles, multiplayer (experimental), save tools |
| **Exosuit** | Personal exosuit inventory and technology |
| **Multi-tools** | Multi-tool selection, stats, seeds, archive, and inventory |
| **Starships** | Ship selection, stats, seeds, customisation, archive, and inventory |
| **Fleet** | Freighter, frigates, and squadron stats, seeds, and inventories |
| **Exocraft** | Exocraft vehicle inventories and summoning stations |
| **Companions** | Companion management, creature creator, pet battles |
| **Bases & Storage** | Bases and NPCs, base objects, space stations and POIs, chest inventories and storage containers |
| **Catalogue** | Known technologies, products, specials, words, glyphs, teleport locations, fishing, recipes, wonders, collected knowledge, fossils, raw materials and discovery stats |
| **Milestones** | Journey milestones and statistics |
| **Settlements** | Settlement management and building editor |
| **ByteBeats** | ByteBeat music library editor |
| **Account Rewards** | Platform and season rewards |
| **Export Settings** | Custom export/import settings |
| **Raw JSON Editor** | Advanced JSON tree editor for power users |
| **Database Search** | Read-only search of the whole item database (no save required) |

Most tabs also have sub-tabs, for example Player (General, Guides, Titles, Multiplayer), Starships (Ship Details, Customisation), Exocraft (Exocrafts, Summoning Stations) and Bases & Storage (Bases, Chests, Storage, Systems).

> 💡 **Tip:** Tabs load their data when you first click on them to save on initial load time, so switching tabs may take a brief moment the first time but won't again after that. The Catalogue tab is unloaded again when you leave it to keep memory use down, so it reloads when you return to it.

### GOTO JSON Navigation

Most panels have a small bookmark button that jumps straight to the matching data in the Raw JSON Editor. NMSE asks you to confirm first, then syncs every panel's edits to the in-memory JSON, switches to the Raw JSON Editor and highlights the target node. Use it when you want to inspect or hand-edit exactly what a panel controls.

---

## Player Stats

The **Player** tab lets you edit your character's core stats, game settings, outfits, unlocked guides and titles.

| General | Guide | Titles |
|-------|-------|-------|
| <img src="../img/player-general.png"> | <img src="../img/player-guide.png"> | <img src="../img/player-titles.png"> |

### What You Can Edit (General Tab)

| Field | Description |
|-------|-------------|
| **Health** | Your current health value |
| **Shield** | Your current shield value |
| **Energy** | Your current energy value |
| **Units** | Basic in-game currency |
| **Nanites** | Nanite clusters currency for technology upgrades |
| **Quicksilver** | Premium currency earned from missions for synthesis |
| **Save Name** | The name of the save file |
| **Save Summary** | The summary text stored for the save |
| **Total Play Time** | Total play time recorded for the save |
| **Third Person Camera** | Whether the character uses the third person camera |
| **Last Save Date** | When the game last wrote the save |
| **Current Preset** | The active difficulty preset (Normal, Relaxed, Survival, Permadeath, Creative or Custom). Changing it also updates the save's Game Mode |
| **Easiest Used** | The easiest difficulty preset this save has used |
| **Hardest Used** | The hardest difficulty preset this save has used |
| **Expedition Number** | The expedition (season) number for the save |
| **Account Name** | The account name recorded in the save |
| **Outfits** | Select, Export, Import and Copy to Custom Data player outfits |
| **Player State** | Your current state (on foot, in ship, etc.) |
| **Portal Interference Active** | Whether portal interference is active |
| **Enable Purple System Warping** | Enables warping to purple systems |
| **Space Battle** | Trigger a space battle event |

### Editing Coordinates

You can change your galactic position by editing the coordinate fields. This lets you teleport to specific systems or planets.

Current Coordinates shows your current location and details. You can manually edit and apply coordinates from the right hand column via the number fields, or via glyph buttons.<br>
The <kbd>Convert to Coords</kbd> button turns a 12-character portal code into coordinates, and the galaxy row shows the galaxy name with an indicator dot.<br>
As an additional bit of fun, you can use the `Coordinate Roulette!` button to send yourself somewhere completely random (_you will be prompted to confirm_).

> ⚠️ **Caution:** Changing coordinates will move you to a different location in the galaxy. Make sure you know where you want to go!

### Outfits

The **Outfits** row on the General tab manages your saved player outfits:

- Pick an outfit from the dropdown to select it
- <kbd>Export</kbd> saves the selected outfit to a file and <kbd>Import</kbd> loads one from a file
- <kbd>Copy to Custom Data</kbd> copies the selected outfit into the save's custom data so the character uses it

### Advanced Save Utilities

The **Advanced Save Utilities** section works on the save directory rather than just the loaded save:

- <kbd>Copy Slot</kbd>, <kbd>Move Slot</kbd>, <kbd>Swap Slots</kbd> and <kbd>Delete Slot</kbd> act on the source and destination slots you pick (up to 15 slots are supported). The source list only shows slots that contain a save, and the buttons are disabled with a note when the save format does not support slot operations
- <kbd>Transfer to Platform...</kbd> copies the current save into another platform's format (Steam, GOG, Xbox Game Pass, PS4 or Switch). Pick the destination platform, then the destination folder and slot

Slot operations work for Steam, GOG, Switch, PS4 streaming saves and Xbox Game Pass saves. PS4 saves in the monolithic `memory.dat` format (SaveWizard/Apollo) cannot use slot copy, move, swap or delete.

After an operation completes, the save directory is reloaded so the UI reflects the new slot layout.

> ⚠️ **Caution:** Ensure you have a backup of your save when using this utility! It performs complex operations for advanced users only.

### Guide Tab

Each guide category has its own grid with **Seen** and **Unlocked** columns for every topic. Use the filter box to find topics, or <kbd>Unlock All</kbd> / <kbd>Lock All</kbd> to change the whole list at once.

### Titles Tab

The titles grid shows the title ID, title, description and an **Unlocked** checkbox per row. Use <kbd>Unlock All</kbd> / <kbd>Lock All</kbd> to change every title at once.

### Multiplayer (Experimental)

The **Multiplayer** sub-tab edits Swarm co-op keys stored under `CommonStateData.SeasonData`: team configuration (community team, team ship seeds and palettes), purchase and transfer restrictions, player setup (forced race, weapon and ship seeds) and other co-op state.

> ⚠️ **Warning:** This panel is entirely experimental and results are not guaranteed. Some values are enforced by the game and cannot be changed.
>
> You must tick the confirmation checkbox before the controls unlock, and NMSE recommends backing up your save before making any changes here.

| Multiplayer (Experimental) |
|-------|
| <img src="../img/player-multiplayer.png"> |

---

## Exosuit

The **Exosuit** tab shows your personal inventory in a visual grid layout.

| Exosuit Cargo | Exosuit Tech |
|-------|-------|
| <img src="../img/exosuit-cargo.png"> | <img src="../img/exosuit-tech.png"> |

### Inventory Sections

- **Cargo** - Main player cargo inventory
- **Technology** - Installed technology modules and upgrades

### Editing Inventory Slots

- **Click** on any slot to select it
- **Right-click** a slot to see available actions
- Use the item details / picker to view and change what's in a slot
- Edit stack sizes to change quantities
- Move items between slots by dragging
- Duplicate items into empty slots by using <kbd>ctrl</kbd>/<kbd>cmd</kbd> + dragging

### Adding Items

1. Click an empty slot
2. Use the item picker dialog to search for items
3. Select the item and confirm with <kbd>Add Item</kbd>

The picker shows an icon, name, category and ID for each item, supports selecting several items at once, and includes a manual **Item ID** box for adding an item by ID when you know it.

> 💡 **Tip:** You can search items by name using the search bar in the item picker.

### Resizing Inventory

- Change the width / height and use the <kbd>Resize</kbd> to confirm

### Export Import

- Cargo and Technology inventories allow you to export and import an inventory layout via the <kbd>Import</kbd> and <kbd>Export</kbd> buttons.

### Inventory Slot Actions

The grid actions shared by every inventory panel are available from the right-click menu on a slot:

- <kbd>Add Item</kbd> / <kbd>Remove Item</kbd> - add an item via the picker or clear the slot
- <kbd>Enable/Disable Slot</kbd> and <kbd>Enable All Slots</kbd> - toggle whether a slot is usable in game
- <kbd>Repair Slot</kbd> / <kbd>Repair All Slots</kbd> - repair damaged technology
- <kbd>Supercharge Slot</kbd> / <kbd>Supercharge All Slots</kbd> (technology grids only) - move the supercharged slot. Supercharging is disabled on cargo, chest and storage grids because the game fixes those slots
- <kbd>Fill Stack</kbd> / <kbd>Refill All Stacks</kbd> - fill the slot or every stack to its maximum
- <kbd>Recharge All Technology</kbd> - recharge all technology in the grid
- <kbd>Copy Item</kbd> / <kbd>Paste Item</kbd> - copy an item between slots
- <kbd>Pin Slot</kbd> - keep a slot in place for auto-stack (Exosuit and Starship cargo grids)
- <kbd>Sort by Name</kbd> / <kbd>Sort by Category</kbd> - sort the grid

The same recharge, refill and repair actions are also available from the <kbd>Tools</kbd> menu for the whole save.

### Inventory Grid Sort, Auto-Stack and Pinned Slots

These behaviours are shared across the cargo grids (see Exosuit for the slot actions):

- Cargo grids have a <kbd>Sort</kbd> control at the top of the grid (None, Name or Category)
- The <kbd>Auto-Stack</kbd> button moves matching items to the destinations allowed for that inventory:
  - Exosuit Cargo: Chests, Starship Cargo or Freighter Cargo
  - Starship Cargo: Chests or Freighter Cargo
- The context menu can also auto-stack only the slot you right-clicked
- You can pin a slot from the context menu. Pinned slots are ignored by auto-stack
- If you use the single-slot auto-stack action on a pinned slot, the action is blocked
- If the destination stack becomes full, the extra items go to another free slot in the same destination when possible
- If there is no valid free slot, the remaining items stay in the source inventory

---

## Multi-tools

The **Multi-tools** tab lets you manage your collection of multi-tools.

| Multi-tools |
|-------|
| <img src="../img/multitool.png"> |

### What You Can Edit

| Field | Description |
|-------|-------------|
| **Name** | Your multi-tool's custom name |
| **Type** | Standard, Rifle, Royal, Alien, Experimental, Sentinel, Atlantid, Staff variants (Autophage), Direwasp Disintegrator, Starbound, etc. |
| **Size** | The multi-tool body size used by the shared model |
| **Class** | C, B, A, or S class |
| **Seed** | The procedural generation seed (changes appearance etc.) |
| **Base Stats** | Damage, Mining, and Scan stats |
| **Inventory** | Technology slots, inventory resize, import / export |

Editing the inventory works the same as the other inventory panels (see Exosuit).

### Switching Multi-tools

Use the dropdown at the top to switch between your multi-tools. The inventory grid below will update to show the selected multi-tool's contents.

### Managing Multi-tools

Delete a multi-tool with the <kbd>Delete</kbd> button.

Export / Import via the <kbd>Export</kbd> and <kbd>Import</kbd> buttons.

Set the selected multi-tool to the primary multi-tool with the <kbd>Make Primary</kbd> button.

Changing the **Type** updates the tool's model and **Size** as well as its stats. If you change the primary multi-tool, NMSE keeps the equipped weapon data in sync so the game does not rebuild it on load.

### Multi-tool Archive

The game limits how many multi-tools you can carry, so NMSE provides an archive:

- <kbd>Move Selected to Archive</kbd> stores the selected multi-tool in the archive
- <kbd>Import from Archive to Empty Slot</kbd> lets you pick an archived multi-tool and restores it into the first free slot

Your **primary** multi-tool cannot be archived, and the last remaining multi-tool cannot be archived either, so make a different tool primary first.

### Changing Multi-tool Seed

Changing the **Seed** value will change how your multi-tool looks and its base stats. Each seed generates a unique combination of parts and colours. You can find seeds shared by the community online to get specific appearances.

> 💡 **Tip:** Write down your current seed or export your multi-tool before changing it, so you can go back if you don't like the new one!

---

## Starships

The **Starships** tab lets you manage up to 12 starships in your collection, including corvettes.

| Starship Cargo | Starship Tech |
|-------|-------|
| <img src="../img/starship-cargo.png"> | <img src="../img/starship-tech.png"> |

### What You Can Edit

| Field | Description |
|-------|-------------|
| **Name** | Your ship's custom name |
| **Type** | Fighter, Explorer, Hauler, Shuttle, Exotic, Solar, Sentinel, Corvette, Living Ship and specials such as the Utopia Speeder, Golden Vector, Starborn Runner/Phoenix, Boundary Herald, The Wraith and Vintage Interceptor |
| **Class** | C, B, A, or S class |
| **Seed** | The procedural generation seed (changes appearance) |
| **Base Stats** | Damage, Shield, Hyperdrive, and Manoeuvrability stats |
| **Inventory** | Cargo and Technology slots, inventory resize, import / export |

### Switching Ships

Use the dropdown at the top of the panel to switch between your ships. Each ship has its own inventory grid.

### Managing Starships

Set the ship to use old colours via the <kbd>[ ] Use Old Colour</kbd>

Delete a ship with the <kbd>Delete</kbd> button.

Export / Import via the <kbd>Export</kbd> and <kbd>Import</kbd> buttons.

Set the selected ship to the primary starship with the <kbd>Make Primary</kbd> button.

### Ship Archive

The game limits how many ships you can claim, so NMSE provides an archive:

- <kbd>Move Selected to Archive</kbd> stores the selected ship in the archive (cargo is lost when archiving)
- <kbd>Import from Archive to Empty Slot</kbd> lets you pick an archived ship and restores it into the first free slot

Your **primary** ship cannot be archived, and corvettes cannot be archived at all because the game does not support it.

### Ship Customisation

For ships that support it, the **Customisation** tab offers deeper appearance editing than the seed alone:

- Pick the **Ship Scene (Model) Resource** to change the model
- Choose parts per slot under **Parts**
- Choose **Texture Options** (paint styles) per texture group
- Choose colour channels and a **Paint Palette**, plus the solar sail colour where the ship has one

> ⚠️ **Note:** Some part, palette and texture combinations will not work together, and some palettes or textures overwrite particular colour slots with a special colour.
>
> Sail colour changes are local only and are not seen by other players.
>
> Customisation is not available for Corvette ships because they use a different system.

| Ship Customisation |
|-------|
| <img src="../img/starship-customisation.png"> |

### Cargo Sort and Auto-Stack

Starship Cargo uses the shared sorting, auto-stack and pinning behaviour described in the Exosuit section, with Chests or Freighter Cargo as auto-stack destinations.

### Changing Ship Appearance

Changing the **Seed** value will change how your ship looks and its base stats. Each seed generates a unique combination of parts and colours. You can find seeds shared by the community online to get specific ship appearances.

> 💡 **Tip:** Write down your current seed or export your ship before changing it, so you can go back if you don't like the new one!

### Corvette Support

Corvette ships are supported with some special considerations:
- Corvettes work jankily in the game save data
- Due to how corvettes work, you should summon your corvette and then set a new starship to primary before editing
- NMS saves only store the Technology slots in the starship keys for your last 'Recently Boarded Corvette'. If you intend to edit a corvettes inventories, you should always have this ship as your second last summoned ship for safety
- Corvette inventory slots are reverse looked up from the base data to show the correct technology slots. If you don't see them, you need to cycle the corvette per above
- NMSE has some safety prompts around these to help to guide you when editing corvette starships safely


Corvette type starships allow the following extra features:
- Snapshot / import a snapshot of the technology and ship status of a corvette, to reduce annoyance from the game jumbling the slots
- Optimise the build order of base components in the corvette design via the <kbd>Optimise Build</kbd> button. A Tick or Cross indicator shows the current optimisation status, and the confirmation reports how many objects were moved (or that the build was already optimised)

| Corvettes |
|-------|
| <img src="../img/corvette-parts.png"> |

---

## Fleet (Freighter, Frigates & Squadron)

The **Fleet** tab contains three sub-tabs for managing your capital ship (freighter) and fleet (frigate fleet and squadron).

### Freighter

Edit your freighter's stats, inventory, technology, and view the functional room list.

| Freighter Cargo | Freighter Tech | Freighter Rooms |
|-------|-------|-------|
| <img src="../img/freighter-cargo.png"> | <img src="../img/freighter-tech.png"> | <img src="../img/freighter-rooms.png"> |

| Field | Description |
|-------|-------------|
| **Name** | Your freighter's custom name |
| **Type** | Tiny, Small, Normal, Capital, Pirate |
| **Class** | C, B, A, or S class |
| **Seeds** | The procedural generation seeds (changes crew appearance and freighter appearance + stats) |
| **Crew Race** | The race of the crew NPC on board |
| **Crew Seed** | Crew NPC seed (changes crew appearance) |
| **Base Stats** | Hyperdrive and Fleet Coordination stats |
| **Items** | Number of objects in the freighter base |
| **Inventory** | Cargo and Technology slots, inventory resize, import / export |
| **Rooms** | Freighter base room presence, with GOTO buttons for the JSON data |

Freighter Cargo has a <kbd>Sort</kbd> control at the top of the grid (see Exosuit for the shared grid actions).

### Freighter Base Backup & Restore

The <kbd>Export Freighter</kbd> and <kbd>Import Freighter</kbd> buttons work on the freighter **base** rather than the whole freighter:

- <kbd>Export Freighter</kbd> saves the freighter base to a file (unavailable when no base exists)
- <kbd>Import Freighter</kbd> restores a saved freighter base over your existing freighter base. It needs an existing freighter base slot to restore into

### Frigates

Manage your fleet of up to 30 frigates by selecting them from the list.

| Frigates |
|-------|
| <img src="../img/frigates.png"> |

| Field | Description |
|-------|-------------|
| **Name** | Frigate name |
| **Type** | Combat, Exploration, Industrial, Trade, Support, etc. |
| **Class** | C, B, A, or S class |
| **NPC Race** | Gek, Vy'keen, Korvax |
| **Seeds** | Home and Model seed (changes appearance + stats) |
| **Traits** | Special bonuses and abilities |
| **Stats** | Combat, exploration, industrial, and trade ratings, plus fuel cost, duration, loot, repair, damage reduction and stealth |
| **Totals** | Expeditions, Successful, Failed and Damaged counts |
| **Damage** | Current damage state, with a <kbd>Repair</kbd> button |
| **State** | Idle, On Expedition, Damaged or Awaiting Debrief |
| **Progress / Mission** | Shows the current frigate mission and level state (Fast forward levels and finish expeditions with buttons) |

> 💡 **Tip:** Because traits determine class for frigates, changing a frigate's class runs an algorithm to determine a set of stats for that class and changing traits updates the class.

Export / Import / Delete and Copy via the <kbd>Export</kbd> and <kbd>Import</kbd>, <kbd>Copy</kbd> and <kbd>Delete</kbd> buttons.

### Squadron

Edit your squadron of up to 4 pilots by selecting them from the list.

| Squadron |
|-------|
| <img src="../img/squadron.png"> |

| Field | Description |
|-------|-------------|
| **Race** | Gek, Vy'keen, Korvax |
| **Rank** | C, B, A, or S class |
| **Ship Type** | The type of ship your wingman flies |
| **NPC Resource** | The NPC model resource used for the pilot |
| **Ship Resource** | The ship model resource used for the pilot's ship |
| **NPC Seed** | Pilot NPC seed (changes appearance) |
| **Ship Seed** | Pilot ship seed (changes appearance) |
| **Traits Seed** | Pilot NPC stats/traits (changes appearance) |
| **[ ] Slot Unlocked** | Pilot slot is unlocked |

Export / Import / Delete via the <kbd>Export</kbd>, <kbd>Import</kbd>, and <kbd>Delete</kbd> buttons.

---

## Exocraft (Vehicles)

The **Exocraft** tab lets you manage your exocraft vehicle inventories in the same manner as the other inventories. See Exosuit for more details.<br>
The inventories can be Resized, Imported and Exported via the width/height setters and <kbd>Resize</kbd> button, they can also be exported and imported via the <kbd>Export</kbd> and <kbd>Import</kbd> buttons.

| Exocraft Cargo | Exocraft Tech |
|-------|-------|
| <img src="../img/exocraft-cargo.png"> | <img src="../img/exocraft-tech.png"> |

Each exocraft (Roamer, Nomad, Colossus, Pilgrim, Nautilon, Minotaur) has its own inventory section showing installed technology.

Use the dropdown at the top of the panel to switch between your exocraft. Each exocraft has its own inventory grid.

If the exocraft is currently deployed, it will be indicated on the panel and can be undeployed via the <kbd>Undeploy</kbd> button.

You can set the selected Exocraft to primary by using the <kbd>[ ] Primary Vehicle</kbd> toggle.

The camera state can be set using the <kbd>[ ] Third Person Camera</kbd> toggle.

The Minotaur AI Pilot can be enabled or disabled by using the <kbd>[ ] Minotaur AI Pilot</kbd> toggle.

Export / Import via the <kbd>Export</kbd> and <kbd>Import</kbd> buttons.

### Summoning Stations

The <kbd>Summoning Stations</kbd> sub-tab lists the exocraft summoning stations stored in your save, split into:

- **Individual Stations** - standalone stations placed in the world
- **Part of Bases** - stations that belong to one of your bases

Select a station to view its details: name, timestamp, base, galactic address, region seed, position, galaxy, portal code (with glyphs and signal booster address), voxel coordinates, solar system and planet. Use <kbd>Delete Station</kbd> to remove it from the save (with a confirmation prompt), or the GOTO buttons to open the related JSON.

> 💡 **Tip:** Deleting a station that is part of a base removes it from that base, so use <kbd>Delete Station</kbd> carefully.

| Summoning Stations |
|-------|
| <img src="../img/exocraft-summoning.png"> |

---

## Companions (Pets)

The **Companion** tab lets you manage your collection of pets / companions, as well as edit their parts and stats.

The system is complex and has some internal game constraints that can be hard to work with, so results may vary.

You can use the <kbd>Creature Builder (Web)</kbd> button above the list of companions and eggs to go to the NMSCD Creature Builder site, and import / set your companions per the site - but be warned that the site has some incorrect identifiers based on model scenes that will need to be substituted.

You can also use the builtin editor to edit game rule compatible creatures. Currently there is no preview for the model though.

Via the <kbd>Battle</kbd> tab you can also edit the Xeno Arena pet battle features of your companion pets and your team format.

| Companions (Stats + Builder) | Pet Battles |
| --- | --- |
| <img src="../img/companions.png"> | <img src="../img/companions-pet-battles.png"> |

### What You Can Edit

| Field | Description |
|-------|-------------|
| **[ ] Slot Unlocked** | Unlock / lock slot |
| **Species** | Your pet's species |
| **Name** | Your pet's custom name |
| **Type** | Companion type (internal names) |
| **Biomes** | Natural biome selection |
| **Predator [ ]** | Set predator state |
| **Has Fur [ ]** | Set fur state |
| **Scale** | Companion scale |
| **Trust** | Companion trust value |
| **Birth Time** | Companion time of birth |
| **Last Egg Time** | Companion last egg time |
| **Induce Egg** | Induce an egg from this companion |
| **Custom Species Name** | Custom species name |
| **Egg Modified [ ]** | Egg modification state |
| **Summoned [ ]** | Companion summoned state |
| **Allow Reroll [ ]** | Companion reroll state |
| **UA** | UA value |
| **Seeds** | Creature, secondary, species, genus, bone scale, colour base |
| **Stats** | Companion stats (Helpfulness, aggression, independence, hungry, lonely, trust increase/decrease dates) |
| **Descriptors (Parts)** | Companion creature model parts and descriptors |
| **Accessory Customisation** | Companion accessory customisation |

The <kbd>Induce Egg</kbd> button can be used to create a companion egg from the selected companion. It will enter the egg list, and produce an egg in the exosuit inventory.

A creatures model parts / descriptors can be edited per creature with the game files rule limitations via the <kbd>Descriptors (Parts)</kbd> section

> 💡 **Tip:** <br>
>The Descriptors / Parts can be edited depending on the selected species and type so that you can select different body part types.<br>
> These are defined by what the game allows, so different companions will offer different options (some of which are fixed).

Additionally, the accessories of the companion can be set, coloured and reset via the <kbd>Accessory Customisation</kbd> section.
This includes their move / ability list, mutation progress, stats, class overrides and different displays of ability statistics.

The <kbd>Battle</kbd> tab edits the Xeno Arena battle system for the selected pet:

- Affinity, with its weak and strong matchups
- Core stat class overrides (health, agility and combat)
- Treats eaten, genes available and mutation progress
- Battle victories
- Per-slot moves, cooldowns and score boosts
- Your 3-pet battle team format

Export / Import / Delete via the <kbd>Export</kbd>, <kbd>Import</kbd>, and <kbd>Delete</kbd> buttons.

---

## Bases & Storage

### Bases Tab

The **Bases** tab shows your base NPCs and their race and seed (editable), as well as your base information and objects. Use the dropdown at the top of the Base Info section to switch between your bases.
It also shows the base version, original base version, galactic address, position and forward vectors, auto power, screenshot position, owner identifiers and last edited information.

It supports all base types (planetary, space, station, freighter, corvette).

Base types are marked in the list with a prefix: `[F]` freighter, `[S]` space station, `[C]` corvette and `[A]` asteroid/space base. Planetary bases have no prefix.

Use <kbd>Export</kbd> to save the selected base to a raw `.nmsbase` file. If the base has matching terrain edits, NMSE also writes a sidecar named `<base file>.terrain.json` beside it; keep both files together when moving or backing up the base. <kbd>Import</kbd> reads the base and applies the sidecar terrain automatically, then replaces the selected base only when it is the same type (Home to Home, Freighter to Freighter, and so on). <kbd>Import as New</kbd> adds the imported base as an additional entry of the same type, which is useful when moving bases between saves. Older NMSE versions can still import the raw base file, but will ignore the sidecar and therefore not restore position or terrain.

Other base actions:

- Reorder the base list with <kbd>Move Up</kbd>, <kbd>Move Down</kbd>, <kbd>To Top</kbd>, <kbd>To Bottom</kbd>, <kbd>A-Z</kbd> and <kbd>Z-A</kbd>
- <kbd>Delete Base</kbd> removes the selected base from the save (with a confirmation prompt)
- <kbd>Summon NPC to Base</kbd> moves the selected NPC worker to the selected base
- <kbd>Move Base Computer</kbd> moves the base computer to another component's location. It prompts you to select a base component by ID
- <kbd>Clear Terrain Edits at Base</kbd> clears terrain edits for the selected base
- <kbd>Clear All Terrain Edits In Save</kbd> and <kbd>Clear All Terrain Edits (Except Bases)</kbd> clear terrain edits across the whole save

| Bases |
|-------|
| <img src="../img/bases.png"> |

#### Objects (Advanced)

The **Objects** tab next to the base info is an advanced editor for the objects in the selected base. It shows a filterable object list. Selecting an object shows its Object ID, user data, position, up and at values.

This panel is for advanced users.

| Base Objects |
|-------|
| <img src="../img/base-objects.png"> |

### Chests Tab

You can edit the contents of your numbered storage chests / containers (0-9) via tabs. Each container has its own inventory grid with export, import and usual editing capabilities.

Chest inventories also have a <kbd>Sort</kbd> control at the top of the grid, and a GOTO button for the JSON data.

You can edit the names of your storage chests via the fields at the top of the tab.

| Chests |
|-------|
| <img src="../img/chests.png"> |

### Storage Tab

You can edit the contents of your special storage containers. Each container has its own inventory grid with export, import and usual editing capabilities, plus a GOTO button for the JSON data.

| Storages |
|-------|
| <img src="../img/storage.png"> |

| Tab | Use |
|-------|-------------|
| **Ingredient Storage** | Ingredient storage inventory for cooking |
| **Corvette Parts Cache** | Corvette parts cache inventory for building corvette starships |
| **Base Salvage Capsule** | Salvaged parts capsule from deleted bases |
| **Rocket** | Rocket inventory |
| **Fishing Platform** | Fishing platforms inventory |
| **Fish Bait** | Fishing bait inventory |
| **Food Unit** | Food unit inventory for cooking |
| **Freighter Refund (unused)** | Freighter base deletion refund inventory - not used in the game, be careful if choosing to edit! (included for mod support) |

### Systems (Space Stations & Space POIs)

The **Systems** sub-tab at the end of the Bases & Storage panel edits the Cosmos space station and space POI data stored in your save. Select a system from the list on the left - systems are named from the station base, your discoveries, or the portal code when no name is known.

#### Space Stations

- View the system address, portal code and glyphs
- Edit the seven station stats, including Vy'keen, Gek and Korvax standing, mercenary and merchant guild standing, and space POI missions
- Set the station mission states (Claim Station Tutorial, Space Base Tutorial and Station Own Wiki) to Not Started, Active or Completed
- <kbd>Set Claimable Values</kbd> raises the stats to the minimum values needed to claim the station (guild standing 15, race standing 30, space POI missions 5) without lowering values that are already higher
- <kbd>Go to Base in Bases Tab</kbd> jumps to the matching station base

| Space Stations |
|-------|
| <img src="../img/systems-stations.png"> |

#### Space POIs

Space POI discovery data is stored as two packed bitflag integers (Packed Data 0 and 1). You can enter a decimal value or a `0x` hex value, or use the per-type slot combos to set each POI slot's state to Hidden, Undiscovered, Discovered or Completed. The raw values and slot states show the same data, so editing either updates the other.

> ⚠️ **Note:** POI types are inferred from the save data and the game's space POI table, so some entries may show as Unidentified. The editor remembers the slot types it has learned for each system.
>
> If a system shows no POI data yet, travel to that system in game first - the game unpacks a system's POIs while you are there.

| Space POIs |
|-------|
| <img src="../img/systems-pois.png"> |

Use the GOTO buttons to open the station stats or packed POI data in the Raw JSON Editor.

---

## Catalogue

The **Catalogue** tab manages your discovery progress and knowledge, with sub-tabs for Known Technologies, Known Products, Known Specials, Known Words, Known Glyphs, Teleport Locations, Fishing, Recipes, Wonders, Collected Knowledge, Fossils, Raw Materials and Discovery Stats.

### Known Item Lists

Known Technologies, Known Products and Known Specials use one unified list each. The list contains every valid item for that tab, not only entries already present in your save.

Each row has one main <kbd>Known</kbd> checkbox. For normal use, knowing an item also marks it as seen:

- <kbd>Known</kbd> writes the save-known arrays for that tab. Technology writes `KnownTech`; Products write `KnownProducts`; Specials write `KnownSpecials`, and SpecialShop buildings also write `KnownProducts`. A non-building SpecialShop item never writes `KnownProducts`.
- Checking Known also adds the item to the account seen array. Technology uses `SeenTechnologies`; Products and Specials use `SeenProducts`.
- Unchecking Known removes the save-known state but leaves the account seen state alone. The game has no "unsee" operation, and Seen is shared across the account.
- Redeeming a Special also adds its account `UnlockedSpecials` entry. Unchecking Known does not remove that account unlock.

An advanced <kbd>Show seen</kbd> toggle adds a separate **Seen** column for the current session only. It lets power users inspect and clear account seen entries without touching Known, and resets to hidden when the editor restarts. Leave it off for normal editing.

The columns are:

- **Icon**, **Name**, **ID** and **Category**.
- **Expedition**: the expedition number for expedition rewards. Items that are not expedition rewards show `-`.
- **Description**: the item description. Hover the cell to see the full text.
- **Catalogue**: `Y` means the item is part of the verified catalogue completion set and therefore affects the Catalogue counter. `N` means the item is valid but does not affect that counter.
- **Known**: the save-known add/remove checkbox.
- **Seen**: the advanced account-seen add/remove checkbox, shown only when <kbd>Show seen</kbd> is enabled.

Each tab shows two counters at the bottom:

- `All: a / a (b%)` covers every item in the list that is save-known.
- `Catalogue: x / y (z%)` counts the game's catalogue visibility: `KnownTech` for technology, and `KnownProducts` or `SeenProducts` for products and specials.

### Add All and Complete Catalogue

<kbd>Add All</kbd> is view-scoped. It adds every item in the current view, including its Seen entry:

- With no filters, it adds every valid item in the list, including `Catalogue = N` items.
- With a text filter such as `Wall`, it adds only the visible matches.
- With <kbd>Catalogue only</kbd> ticked, it adds only catalogue-counted items.
- With a text filter and <kbd>Catalogue only</kbd>, it uses the intersection of both.

<kbd>Remove All</kbd> uses the same current-view scope and removes only the Known state. The counters remain global; filtering only changes which rows are visible and which rows a bulk action affects.

| Filtered catalogue view |
|-------|
| <img src="../img/catalogue-add-all-filtered.png"> |

The top-level <kbd>Complete Catalogue</kbd> button is a separate convenience action. It completes the save-known arrays using the game's item paths and marks the account seen entries needed by the in-game wiki, including `SeenSubstances` for the Materials page. It is not the same as <kbd>Add All</kbd>. The normal Known checkbox and <kbd>Add All</kbd> also never write `SeenSubstances`; raw materials are handled by the Raw Materials tab and by <kbd>Complete Catalogue</kbd>.

The old per-tab Add Technology / Add Product / Add Special pickers and Remove Selected buttons are replaced by the Known checkbox, the advanced Seen column and the two list actions.

Use the filter box to search names, IDs, categories, descriptions and expedition numbers. Searching for `catalogue` or `counted` shows catalogue-counted items; `not catalogue` or `not counted` shows non-catalogue items. The <kbd>Catalogue only</kbd> checkbox applies the same filter directly.

Export and Import operate on the known entries. The GOTO JSON button remains at the right of the filter row. The completion sub-tabs (Wonders, Collected Knowledge, Fossils, Raw Materials and Discovery Stats) retain their own <kbd>Complete All</kbd> / <kbd>Clear All</kbd> tools.

> ?? **Note:** Names shown as `[?] Name` have no official localisation in the game files and are editor-curated approximations.

### Other Sections

Words, glyphs, locations, fishing and recipes retain their dedicated controls. Words and glyphs use learn / unlearn buttons; Fishing and Known Recipes keep their existing add, remove, export and import actions.

#### Known Technologies
View and manage every valid technology, including technologies outside the current catalogue. Known writes `KnownTech` and marks `SeenTechnologies`; the advanced Seen column can clear the account entry separately.

| Known Technologies |
|-------|
| <img src="../img/discoveries-tech.png"> |

#### Known Products
View and manage every valid product, including non-catalogue products. Known writes `KnownProducts` and marks `SeenProducts`. Non-building SpecialShop cosmetics and trails are listed in Specials, not Products.

| Known Products |
|-------|
| <img src="../img/discoveries-product.png"> |

#### Known Specials
View and manage every valid special item. Known writes `KnownSpecials` and, for SpecialShop buildings, `KnownProducts`; it also adds the account `UnlockedSpecials` entry and marks `SeenProducts`.

| Known Specials |
|-------|
| <img src="../img/discoveries-specials.png"> |

#### Known Words
View and manage the alien words you've learned from each race (Gek, Vy'keen, Korvax).<br>
You can learn / unlearn all words, all words for a specific race, a selection of words, or individually.

| Known Words |
|-------|
| <img src="../img/discoveries-words.png"> |

#### Known Glyphs
View and learn portal glyphs. You can learn / unlearn all 16 glyphs at once.

| Known Glyphs |
|-------|
| <img src="../img/discoveries-glyphs.png"> |

#### Teleport Locations
Known teleport locations are displayed in a list with their **Name**, **Type**, **Galaxy**, **Portal Code (Hex)**, **Portal Code (Dec)** and **Signal Booster** address.

| Known Locations |
|-------|
| <img src="../img/discoveries-locations.png"> |

Entries can be filtered via the search and sorted via the headers.

Selecting an entry allows you to view the portal glyphs for that location as well as the galaxy it is in at the bottom of the tabs panel.

Use the <kbd>Delete Selected</kbd> button to delete the currently selected location(s).

Use the <kbd>Travel to System</kbd> button to set the players coordinates to this system and travel to it.

#### Fishing
Browse the fish you have caught and some basic stats. Add fish to your catch list via the <kbd>Add Fish</kbd> button. Remove them via the <kbd>Remove Selected</kbd> button.

Count and Largest Catch stats can be edited by changing their values. Some fish are used as cooking ingredients, and those recipes live in the Recipes database.

| Fishing |
|-------|
| <img src="../img/discoveries-fish.png"> |

#### Recipes
The **Recipes** sub-tab has inner tabs:

- **Known Recipes** - your learned recipes with a completion counter and <kbd>Add All Missing</kbd>, plus add, remove, export and import
- **Recipe Info** - additional information for the selected recipe, shown at the bottom of the panel
- **Recipes** - a basic version of the complete recipe database with ingredients, organised by type with filtering and sorting

| Recipes |
|-------|
| <img src="../img/discoveries-recipes.png"> |

#### Wonders
Track the wonders you have discovered. Each row can be completed individually, or use <kbd>Complete All</kbd> / <kbd>Clear All</kbd> to change the whole list.

| Wonders |
|-------|
| <img src="../img/catalogue-wonders.png"> |

#### Collected Knowledge
Track the knowledge entries you have collected. This grid supports per-row progress values, plus <kbd>Complete All</kbd> / <kbd>Clear All</kbd>.

| Collected Knowledge |
|-------|
| <img src="../img/catalogue-collected-knowledge.png"> |

#### Fossils
Track the fossils you have seen. Fossils are stored in your account data (`accountdata.hg`), so this tab needs account data to be available for the selected platform.

| Fossils |
|-------|
| <img src="../img/catalogue-fossils.png"> |

#### Raw Materials
Track the raw materials you have seen. Like Fossils, these are stored in your account data (`accountdata.hg`). The top-level <kbd>Complete Catalogue</kbd> action also marks the compiled `SeenSubstances` entries during catalogue completion; product checkboxes and <kbd>Add All</kbd> never write them.

| Raw Materials |
|-------|
| <img src="../img/catalogue-raw-materials.png"> |

#### Discovery Stats
View and edit the discovery statistics tracked for your save, with <kbd>Complete All</kbd> / <kbd>Clear All</kbd> for the whole grid.

| Discovery Stats |
|-------|
| <img src="../img/catalogue-discovery-stats.png"> |

---

## Milestones

The **Milestones** tab lets you view and edit your journey milestones and game statistics.
It follows the in-game menus: **Milestones**, **Lifeforms**, **Factions** and **Other Stats**.

| Milestones | Lifeforms |
|------------|-----------|
| <img src="../img/milestones-main.png"> | <img src="../img/milestones-lifeforms.png"> |

| Factions | Other Stats |
|----------|-------------|
| <img src="../img/milestones-factions.png"> | <img src="../img/milestones-other.png"> |

You can edit milestone progress values and global statistics tracked by the game. Guild rows also show rank progress and promotion values. Use the **Find Stat** button to open a searchable reference table with the in-game name, editor name, internal stat ID and location of every stat. Select a result to jump to it. Stats that appear in more than one menu are marked as shared and stay in sync. The GOTO button opens the JSON data.

---

## Settlements

The **Settlement** tab lets you manage your settlements.

Select your settlement from the drop down menu.

Use the <kbd>Delete</kbd> button to delete a settlement.<br>
The settlement is not removed from your known locations (teleporter) on purpose, so that you can go back to that location if you want. It can be removed from the known locations panel.

Export / Import via the <kbd>Export</kbd> and <kbd>Import</kbd> buttons.

### Stats & Perks

| Stats & Perks |
|-------------|
| <img src="../img/settlements-stats.png"> |

| Field | Description |
|-------|-------------|
| **Name** | Selected settlement name |
| **Seed** | Settlement seed (appearance, stats) |
| **Class** | Settlement class |
| **NPC Race** | Settlement NPC race |
| **Decision Type** | Settlements current decision event type |
| **Last Decision** | Settlements last decision event date |
| **Population** | Current settlement population |
| **Max Population** | Maximum settlement population |
| **Stats** | Happiness, productivity, upkeep, sentinel buildup, debt, alert buildup and bug attack buildup |
| **Timers** | Last population, upkeep, debt, alert, bug attack and decision times |
| **Mini Missions** | Mission seed and start time |
| **Perks** | Active settlement bonuses (seed field for procedural perks) |

Set values and timers from the available fields for each stats.

Select Perks from the list and enter a seed on the right hand side of the list for procedural perks.

### Production

| Field | Description |
|-------|-------------|
| **Production** | What your settlement produces |

| Production |
|-------------|
| <img src="../img/settlements-production.png"> |

Use the edit button to change what your settlement is producing.

### Building States

| Field | Description |
|-------|-------------|
| **Building State Slots** | Edit the plots to change the status of the building in that plot |

| Building States |
|-------------|
| <img src="../img/settlements-building-states.png"> |

For each plot (numbered 01 to 48), you can set the buildings status via the number entry field (or drop down list for known safe values). <br>
An approximation of the building is noted below the plots fields.

These are bitflag composites that pack multiple booleans and small numbers into a single integer. Editing these manually is for advanced users.

> This functionality is experimental and based on initial reverse engineering works.

### Building Editor

| Field | Description |
|-------|-------------|
| **Building Editor** | Edit an individual building/plots bitfields directly |

| Building Editor |
|-------------|
| <img src="../img/settlements-building-editor.png"> |

Select the plot via the slot field. You can set the buildings status via the raw value number entry field. <br>
The tick boxes below allow you to manipulate the bitflags in the bitfields within the composite integer. Each tickbox will manipulate the bits and produce the new integer to match.

This panel is for advanced users and requires either known useful values, or a solid understanding of the bitflags to net the most benefit.

> This functionality is experimental and based on initial reverse engineering works.

---

## ByteBeats

The **ByteBeats** tab lets you manage your ByteBeat music library.

| ByteBeats |
|------------|
| <img src="../img/bytebeats.png"> |

ByteBeat is No Man's Sky's built-in music creation tool. With NMSE, you can export, import, and manage your saved ByteBeat compositions - allowing you to share with other artists and enthusiasts online!

| Field | Description |
|-------|-------------|
| **Name** | Track name |
| **Author Username** | Track author username |
| **Author Online ID** | Track authors recorded online ID for the entry |
| **Author Platform** | Authors online / play platform ID (ST, PS, etc.) |
| **Data [ 1-8 ]** | Track data (8 channels) |
| **[ ] Shuffle** | Shuffle playback |
| **[ ] Autoplay On Foot** | Playback selected track on foot |
| **[ ] Autoplay In Ship** | Playback selected track in ship |
| **[ ] Autoplay In Vehicle** | Playback selected track in vehicle |

Export / Import / Delete tracks via the <kbd>Export</kbd>, <kbd>Import</kbd>, and <kbd>Delete</kbd> buttons. Use the GOTO button to open the library in the Raw JSON Editor.

---

## Account Rewards

The **Account** tab shows seasonal reward (expedition), Twitch drops and platform specific rewards data.

NMSE auto-detects the account data file (`accountdata.hg`) from the selected save directory, and the status line at the top shows how many rewards were loaded.

| Account Rewards | Platform Rewards |
|------------|------------|
| <img src="../img/account-rewards.png"> | <img src="../img/platform-rewards.png"> |

### What You Can Edit

| Field | Description |
|-------|-------------|
| **Season Rewards** | Expedition / season rewards |
| **Twitch Rewards** | Twitch Drop rewards |
| **Platform Rewards** | Platform-specific entitlements |

The reward grids show **Unlocked on Account** and **Redeemed in Save** columns. Each tab can unlock / lock all, unlock individually, filter the list, and use:

- <kbd>Redeem All</kbd> / <kbd>Remove All</kbd> - mark every reward as redeemed (or remove the redeemed state) in the save
- <kbd>Unlock All</kbd> / <kbd>Lock All</kbd> - change the unlocked state on the account

Unchecking **Redeemed in Save** removes the save's redeemed entry and its matching Known entry but leaves the account unlocked. That is intentional: it makes the reward available to redeem again without losing the account unlock. The editor now also handles checking and unchecking Redeemed in the same session correctly.

The <kbd>Check Consistency</kbd> button compares your account and save reward arrays and reports entries where the states do not match, such as rewards redeemed but missing from the Known lists, or listed as known but not redeemed. It also checks Known Products and Known Tech, understands that entitlements have no redeem array, and marks unlocked-but-not-redeemed Known entries as ambiguous (shown in orange) rather than automatically deciding they are stale. This is an information tool for advanced users and does not indicate a problem.

| Consistency Check |
|-------|
| <img src="../img/consistency-dialog.png"> |

Platform rewards may require the user settings MXML file to also be edited directly depending on your platform. It will auto find the file on supported platforms, if not browse to `GCUSERSETTINGSDATA.MXML` in the NMS `Binaries\SETTINGS\` directory

> 💡 **Tips:** <br>
> Due to how the game works, Twitch drops and platform rewards, you need to set your game to offline before starting the game to unlock the rewards. <br>
> You may also need to edit the platform rewards after launching the game (or lock the file from editing until after launch) due to how the game recreates the user settings MXML at runtime.

---

## Export Settings

The **Export Settings** tab lets you set up export naming conventions and file preferences.

| Export Extensions | Export Templates |
|------------|------------|
| <img src="../img/export-extensions.png"> | <img src="../img/export-templates.png"> |

Configure how exported files are named and what format they use. The settings cover every exportable artifact, including inventories, ships, multi-tools, outfits, corvette snapshots, bases, discoveries, settlements and ByteBeats.

- <kbd>Save Settings</kbd> stores your changes, and <kbd>Reset to Defaults</kbd> restores the built-in naming templates and extensions
- Use the Help sub-tab for the list of template variables such as `{name}`, `{seed}` and `{timestamp}`

---

## Raw JSON Editor

The **Raw JSON Editor** tab provides direct access to the underlying save file data in a tree view.

| Raw JSON Editor |
|------------|
| <img src="../img/raw-json-editor.png"> |

### When to Use This

The Raw JSON editor is for **advanced users** who need to edit values that aren't exposed in the other tabs. It shows the complete save file structure as a navigable tree view, text view, or a split view of both.

It supports basic syntax highlighting, undo/redo (`Ctrl+Z` / `Ctrl+Y`), node export/import and change diffing. The file selector on the toolbar switches between the **save file** and the **account data** file when account data is available.

### How to Use

1. Navigate the tree by expanding nodes
2. Click on a value to edit it
3. Modify the value in the edit field
4. Press Enter to confirm the change

You can also right-click a node for Add Property / Add Array Item, Delete, Copy Key / Copy Value / Copy Path, Export Node and Import Node. Array and object entries can be reordered by dragging them.

> ⚠️ **Caution:** Editing raw JSON values incorrectly can corrupt your save file. Only use this if you know what you're doing, and always keep a backup!

Use the <kbd>Tree View</kbd>, <kbd>Text View</kbd> or <kbd>Split View</kbd> buttons to switch between the different view modes. In split view, tick <kbd>Isolate Node</kbd> to edit just the selected node, and in the text views use <kbd>Format</kbd> and <kbd>Validate</kbd> to tidy or check the JSON.

A breadcrumb row under the toolbar always shows where the selected node is in the document, so you can see the full path while editing text or in split view.

The <kbd>Bookmarks</kbd> row stores named paths for the current save or account file. Select a node and use <kbd>Add</kbd> to name it, then pick it from the dropdown to jump straight back. <kbd>Manage</kbd> lets you rename or delete bookmarks. Bookmarks created while <kbd>Isolate Node</kbd> is active reopen in Split view with that node isolated; unresolved paths are reported without deleting the bookmark.

Use the <kbd>Expand All</kbd> and <kbd>Collapse All</kbd> buttons to fold/unfold all.

> 💡 **Tip:** The <kbd>Expand All</kbd> button will unfold up to half a million rows of keys and information depending on your save's age and density, and can take a **very significant** amount of time to complete. It will warn you before you expand, and a <kbd>Stop</kbd> button appears while it runs.

The <kbd>Show Changes</kbd> button can be used to show a basic diff (up to a maximum set of changes) for the save edits that have been made to the JSON objects.

Use the <kbd>Search...</kbd> field to search by string, key, etc.

GOTO JSON buttons on other panels bring you straight here: NMSE syncs the other panels, opens the matching node and selects it in the tree.

For advanced edits, it is recommended to export the whole save JSON (or relevant node) and make changes in a capable external editor such as Sublime, VS Code, etc.

---

## Database Search

The **Database Search** tab is a read-only lookup tool for the whole item database loaded from the game data. It does not need a save file, so it stays available even when the editing tabs are locked.

- Type in the **Search all items...** box to search by id, name, group, description, category and other fields (`Ctrl+F` focuses the box, `Escape` clears it)
- Space-separated terms must all match; a term without wildcards is matched as a substring, while `*` matches any text and `?` matches a single character
- Use the **Source** dropdown to limit results to one item type
- The result list shows the item icon, name, category, ID and source, and a details pane shows every available field
- The status line shows how many items are in the database, or how many match your search

| Database Search |
|-------|
| <img src="../img/database-search.png"> |

Useful when you want to check an item's ID or properties before adding it to an inventory or editing JSON.

---

## Importing & Exporting Inventories

NMSE supports importing and exporting inventory data, making it easy to share loadouts with other players or make backups / inject into other items.

### Exporting

1. Navigate to the inventory you want to export (Exosuit, Ship, etc.)
2. Use the export function to save the inventory to a file

### Importing

1. Navigate to the target inventory
2. Use the import function and select your file
3. The items will be loaded into the inventory

### Whole Save JSON

Use <kbd>Tools > Export JSON</kbd> and <kbd>Tools > Import JSON</kbd> to back up or restore the loaded save as a readable JSON file. This covers everything in the save, including outfits, corvette snapshots, bases, discoveries, settlements and ByteBeats. Imported JSON is loaded into all panels so you can review it before saving.

### Compatibility

NMSE can import inventories, multi-tools, ships, etc. from multiple save editor formats via key-matching, so you can share loadouts even if your friends use different tools.<br>
Any keys that can be matched successfully will be imported.

Supported formats include NMSE's own exports, NomNom wrapper exports and NMSSaveEditor formats, and obfuscated keys are de-obfuscated automatically during import.

---

## Language & Theme

NMSE supports the **16 game languages**. To change the display language:

1. Click **Language** in the menu bar
2. Select your preferred language from the list

The entire application - menus, labels, item names, and descriptions - will update to the selected language.

### Supported Languages

| Language | Code |
|----------|------|
| English (UK) | en-GB |
| English (US) | en-US |
| French | fr-FR |
| Italian | it-IT |
| German | de-DE |
| Spanish | es-ES |
| Spanish (Latin America) | es-419 |
| Portuguese | pt-PT |
| Portuguese (Brazil) | pt-BR |
| Russian | ru-RU |
| Polish | pl-PL |
| Dutch | nl-NL |
| Simplified Chinese | zh-CN |
| Traditional Chinese | zh-TW |
| Japanese | ja-JP |
| Korean | ko-KR |

### Theme

Use the **Theme** menu to switch between **System**, **Light** and **Dark**. System follows your Windows theme automatically.

---

## Keeping NMSE Updated

NMSE checks for updates automatically a couple of seconds after startup and prompts you when a newer version is available. You can also check manually at any time:

1. Click <kbd>Help > Check for Updates...</kbd>
2. If an update is available, review the release notes and choose whether to download and install it
3. The update downloads in place, applies itself and relaunches NMSE

> 💡 **Tip:** If NMSE is running from a cloud-synced folder such as OneDrive, the sync software may briefly lock the new executable after an update. If NMSE does not relaunch automatically, wait a moment and start it manually.

---

## Frequently Asked Questions

### Where are my save files?

NMSE auto-detects save locations for Steam, GOG, and Xbox Game Pass. Every detected store is offered in the directory dropdown (a Steam profile and a Game Pass container can both appear), and selecting a parent folder such as `...\SystemAppData`, `...\xgs` or `...\wgs` is accepted and resolved to the save container automatically. If auto-detect doesn't work, save files are typically located at:

- **Steam:** `%APPDATA%\HelloGames\NMS\st_<steamid>\`
- **GOG:** `%APPDATA%\HelloGames\NMS\DefaultUser\`
- **Xbox Game Pass:** `%LOCALAPPDATA%\Packages\HelloGames*\SystemAppData\xgs\<id>\` (the folder containing the `Slot1Auto`, `Slot1Manual` folders) or the legacy `...\SystemAppData\wgs\<id>\` folder containing `containers.index`

### How do I load an Xbox Game Pass save?

Open the save container folder with <kbd>File > Open Save Directory</kbd>:

- **xgs (current format):** open `...\SystemAppData\xgs\<id>\` - the folder that holds `AccountData`, `Slot1Auto`, `Slot1Manual` and so on. NMSE lists those folders as the available slots
- **wgs (legacy format):** open `...\SystemAppData\wgs\<id>\` - the folder that contains `containers.index`. NMSE reads the index and lists the save slots stored in the GUID-named blobs next to it

Both formats are read and written in place, so the Xbox cloud sync container identity is preserved. When you edit an `xgs` save, NMSE also updates the matching `wgs` copy if one exists. Close No Man's Sky before loading or saving, so the game is not writing to the same container.

### Can I edit PlayStation or Switch saves?

Yes! You'll need to transfer the save file to your PC first via the homebrew of your choice (PS4: Apollo, SaveWizard, etc. - Switch: JKSV, etc.), edit it with NMSE, then transfer it back. Use <kbd>File > Load Save File</kbd> to open manually transferred saves.

### How do I load a PS4 memory.dat, SaveWizard NOMANSKY file, or PS4 HTOS savedata*.hg save?

- `memory.dat`: open it with <kbd>File > Load Save File</kbd>, then pick the game slot and its Auto or Manual save from the toolbar combos
- SaveWizard `NOMANSKY` `.hg` files: open the file directly; NMSE writes it back in the same format
- PS4 HTOS/homebrew saves: open the folder containing the `savedataNN.hg` and `manifestNN.hg` files; game slots start at `savedata02.hg`

### How do I load a Switch save? What files must I copy?

Copy the `savedataNN.hg` data files together with their matching `manifestNN.hg` companion files, then open the folder with NMSE. Game data starts at `savedata02.hg`; `savedata00.hg` is the settings file.

### Will editing my save get me banned?

No Man's Sky does not have an anti-cheat system for save file editing. However, always use save editing responsibly and keep backups.<br>
We do not support or condone cheating or editing at the expense of others. Please be responsible!

### Why are the editing tabs greyed out before I load a save?

Every editing tab is locked until a save is loaded and shows a no-save overlay while locked. **Export Settings** and **Database Search** stay usable because they do not need a save. Load a save with <kbd>File > Open Save Directory</kbd> or <kbd>File > Load Save File</kbd> to unlock the rest.

### My save won't load - what do I do?

1. Make sure you're using the [latest version of NMSE][releases]
2. Check that your save is from a supported game version
3. Try restoring from a backup (<kbd>Edit > Restore Backup (All)</kbd> or <kbd>Edit > Restore Backup (Single)</kbd>) and pick a backup in the Backup Picker
4. Ensure you haven't caused the issue externally with other tools or edits first
5. If the issue persists, [report a bug][issues-bug] - ensuring that you give clear and thorough information

### Can I undo my changes?

You can restore any of the last 10 automatic ZIP backups that NMSE creates when you save. Use <kbd>Edit > Restore Backup (All)</kbd> or <kbd>Edit > Restore Backup (Single)</kbd> and pick the backup you want.

### Where are my backups stored, and how many are kept?

NMSE keeps the 10 most recent backups for each save folder as timestamped ZIP files. They are written to the folder chosen in the **Backup:** toolbar combo, or to a `Save Backups` folder next to `NMSE.exe` or in TEMP if no folder is configured. See [Backups & Restoring][backups].

### How do I choose which backup to restore?

Use <kbd>Edit > Restore Backup (All)</kbd> or <kbd>Edit > Restore Backup (Single)</kbd>. Both open the **Backup Picker**, where you choose from the available backups (listed newest first with their created time, size and location). All restores every file in the backup, while Single restores only the currently loaded save file.

### How do I update NMSE?

NMSE checks for updates automatically on startup. You can check manually with <kbd>Help > Check for Updates...</kbd>; when an update is available you can download and install it in place, and the app relaunches itself. See [Keeping NMSE Updated][keeping-updated].

### Why are some item names shown as "[?] Name"?

Those names have no official localisation in the game files. NMSE shows an editor-curated approximation prefixed with `[?]` so you know the name is not from the game itself.

### Why are my account rewards not visible in game?

The Account Rewards tab edits the account data file (`accountdata.hg`), which NMSE auto-detects from the selected save directory. For Twitch drops and platform rewards you need to launch the game offline for the rewards to be picked up, and you may also need to edit the platform rewards after launch (or lock the user settings MXML file until after launch) because the game recreates it at runtime. Platform rewards may also need the `GCUSERSETTINGSDATA.MXML` file edited directly.

### What does Multiplayer edit, and why does it need a confirmation checkbox?

The Multiplayer sub-tab edits Swarm co-op keys under `CommonStateData.SeasonData`, such as the cached community team, team ship seeds and palettes, purchase and transfer restrictions, and start-up player setup. The feature is experimental and some values are enforced by the game, so it is gated behind a confirmation checkbox, and you should back up your save before using it.

### Does Database Search need a loaded save?

No. Database Search is a read-only lookup over the game's item database and works without a save loaded, alongside Export Settings.

### Why do my corvette technology slots look wrong?

NMS only stores the technology slots for your last recently boarded corvette, and corvette data is jumbled in the save. Summon the corvette, set another starship as primary before editing, and keep the corvette as your second last summoned ship - see the Corvette Support notes in [Starships][starships].

---

<div align="center">

**Need more help?** [Open an issue][issues-bug] or join the [community Discord][discord].

Made with ❤️ by [**vectorcmdr**][github-owner]

</div>

<!-- Link Definitions ----------------------------------->

<!-- Internal Navigation -->
[getting-started]: #getting-started
[opening-save]: #opening-a-save-file
[saving-changes]: #saving-your-changes
[backups]: #backups--restoring
[tabs-overview]: #tabs-overview
[player-stats]: #player-stats
[exosuit]: #exosuit
[multitools]: #multi-tools
[starships]: #starships
[fleet]: #fleet-freighter-frigates--squadron
[exocraft]: #exocraft-vehicles
[companions]: #companions-pets
[bases]: #bases--storage
[catalogue]: #catalogue
[milestones]: #milestones
[settlements]: #settlements
[bytebeat]: #bytebeats
[account]: #account-rewards
[export-config]: #export-settings
[raw-json]: #raw-json-editor
[database-search]: #database-search
[import-export]: #importing--exporting-inventories
[language]: #language--theme
[keeping-updated]: #keeping-nmse-updated
[faq]: #frequently-asked-questions

<!-- Other Guides -->
[guide-wine]: ../dev/wine-linux-guide.md
[guide-bottles]: ../dev/bottles-linux-guide.md
[guide-gcenx-wine]: ../dev/gcenx-macos-guide.md
[guide-crossover]: ../dev/crossover-macos-guide.md

<!-- External Links -->
[releases]: https://github.com/vectorcmdr/NMSE/releases/latest
[issues-bug]: https://github.com/vectorcmdr/NMSE/issues/new?template=bug_report.md
[issues-feature]: https://github.com/vectorcmdr/NMSE/issues/new?template=feature_request.md
[github-owner]: https://github.com/vectorcmdr
[discord]: https://discord.gg/WbDQKKP3us

# UI Layer

## Overview

The UI layer (`UI/`) is a WinForms front end built around a tabbed `MainFormResources`
window that hosts specialised panels. Each panel is a `UserControl` that normally follows
a `LoadData` / `SaveData` contract. Panels delegate all game-specific knowledge to
[Core Logic classes](core-logic.md) and use [Data databases](data-layer.md) for item
lookups and icon rendering.

**Key design principles:**

1. **Deferred loading** - only the active tab is populated when a save is opened; other
   tabs load on first selection. This keeps startup fast.
2. **Direct mutation** - inventory edits write directly to the in-memory `JsonObject` slots
   during interaction, not on save. `SaveData` is mostly a sync pass for non-inventory
   fields.
3. **Flicker-free updates** - `RedrawHelper.Suspend` / `Resume` hides a control and suspends
   layout during bulk control creation, and `SuspendLayout` / `ResumeLayout` prevents
   intermediate layout passes.
4. **Theme-aware rendering** - controls read colours from `ThemeColors` and repaint on
   `ThemeManager.ThemeChanged`, so the Light, Dark and System themes all render correctly.

The main window class is `MainFormResources` (`UI/MainForm.cs`). `UI/MainForm.Designer.cs`
is a generated resource class (icons and other designer-managed resources), not a layout
partial.

---

## MainForm

| | |
|---|---|
| File | `UI/MainForm.cs` |
| Class | `MainFormResources` |
| Purpose | Top-level window that orchestrates all panels, handles file open, save and backup restore, and manages deferred tab loading |

### Tab Layout

MainForm uses a `DoubleBufferedTabControl` with 16 tabs:

| Index | Panel | Description |
|---|---|---|
| 0 | MainStatsPanel | Player stats, save info, coordinates, Guides, Titles, Multiplayer |
| 1 | ExosuitPanel | Personal cargo and technology inventories |
| 2 | MultitoolPanel | Multitool selection and editing |
| 3 | StarshipPanel | Ship selection, editing and customisation |
| 4 | FleetPanel | Container for Freighter, Frigates and Squadron sub-tabs |
| 5 | ExocraftPanel | Exocraft inventories and Summoning Stations |
| 6 | CompanionPanel | Companion management, genes, accessories and pet battles |
| 7 | BasePanel | Bases, chests, storage and the Systems (space station) tab |
| 8 | CataloguePanel | Known technologies, products, specials, words, glyphs, locations, fish, recipes, wonders, knowledge, fossils, raw materials and discovery stats |
| 9 | MilestonePanel | Milestones in game-menu layout, global statistics and searchable stat reference |
| 10 | SettlementPanel | Settlement stats, production, building states and building editor |
| 11 | ByteBeatPanel | ByteBeat music editor |
| 12 | AccountPanel | Season, Twitch and platform rewards |
| 13 | ExportConfigPanel | Export naming and extension preferences |
| 14 | RawJsonPanel | Raw JSON tree, text and split editor |
| 15 | DatabaseSearchPanel | Read-only searchable view of the item database |

Tab titles are localised from the `tab.*` keys (`tab.player` through `tab.database_search`).
Index 13 (Export Settings) and index 15 (Database Search) never require a loaded save and are
exempt from the editor lock.

### Initialization Flow

```
1. Program.Main()
   - Remove stale .old executable from a previous self-update
   - Install global exception handlers and DPI awareness
   - Create and show SplashForm
   - Create MainFormResources, call SetSplash(splash), then PerformStartup()
   - Application.Run(mainForm)

2. MainFormResources constructor
   - Create all panels (FleetPanel receives Freighter, Frigate and Squadron)
   - Embed RecipePanel as a Catalogue sub-tab via AddRecipeTab()
   - Wire DataModified, CrossInventoryTransferCompleted and GoToJsonRequested
   - Initialize form, menus, both toolbars, status bar and tabs
   - InstallEditorLock()
   - Subscribe to ThemeManager.ThemeChanged and apply the current theme

3. PerformStartup()
   - LoadConfig() (recent directories and backup paths)
   - ApplySavedTheme()
   - LoadDatabase() (items, recipes, supplementary databases, words, icons)
   - ApplyStartupLanguage()
   - PopulateSaveSlots()
   - On Form.Shown: await the icon preload task, set Opacity = 1, close the splash,
     then run a non-blocking background update check

4. User opens a save file
   - SaveFileManager.LoadSaveFile() returns a JsonObject; context transforms and
     active-context detection are registered inside the loader
   - Capture the Raw JSON diff baseline
   - Load tab 0 (MainStatsPanel) immediately; other tabs load on first selection
```

`LoadDatabase` wires the item database and icon manager to the panels that consume them
(Exosuit, Starship, Multitool, Exocraft, Catalogue, Settlement, Fleet, Base, Account,
Main Stats and Database Search). FleetPanel cascades to Freighter and Frigate. Panels that
use secondary databases receive them through their own setters (`SetDatabases`,
`SetCatalogueDatabase`, `SetRecipeDatabase`, `SetWordDatabase`, `LoadRewardsDatabase`).
Icon preloading runs on a background task (`_iconPreloadTask`) started during database load.

### Menu Structure

| Menu | Items |
|---|---|
| **File** | Open Save Directory (Ctrl+O), Load Save File (Ctrl+L), Save (Ctrl+S), Save As (Ctrl+Shift+S), Exit (Alt+F4) |
| **Edit** | Reload (F5), Restore Backup (All), Restore Backup (Single) |
| **Tools** | Export JSON, Import JSON, Recharge All Technology, Refill All Stacks, Repair All Slots, Repair All Technology |
| **Language** | All 16 BCP 47 language tags (en-GB, fr-FR, it-IT, de-DE, es-ES, ru-RU, pl-PL, nl-NL, pt-PT, es-419, pt-BR, zh-CN, zh-TW, ko-KR, ja-JP, en-US) |
| **Theme** | System, Light, Dark |
| **Help** | GitHub Page, User Guide, Sponsor Development, Check for Updates, Release Notes, About |

The bulk Tools actions (`InventoryBulkActions`) rewrite technology charge, stack counts and
damage states across `PlayerStateData`, mark the save dirty and reload every loaded inventory
panel. Help items open the project URLs in the default browser; Check for Updates runs
`UpdateService` and shows either the update prompt or an up-to-date message.

### Toolbar

Two stacked `ToolStrip` rows:

| Row | Controls |
|---|---|
| 1 | `Directory:` label, directory combo, `Browse...`, separator, `Backup:` label, backup path combo, `Browse...` |
| 2 | `Save Slot:` label, slot combo, `File:` label, file combo, separator, `Load`, `Save` |

The directory combo lists recent and default save directories. The backup combo lists
recent backup root paths and can be browsed or typed into. Load and Save are enabled
according to whether a platform-appropriate slot is selected. Toolbar labels are localised
through the `toolbar.*` keys.

### Language Support

The **Language** menu allows switching the display language for item names, descriptions,
subtitles and all UI strings. Each tag corresponds to a localisation JSON file in
`Resources/json/lang/`, plus a UI string file in `Resources/ui/lang/`. When a language is
selected:

1. `UiStrings.Load(tag)` loads the UI string table (always loading the English fallback first).
2. `LocalisationService.LoadLanguage(tag)` loads the flat key-value game-data file.
3. `GameItemDatabase.ApplyLocalisation()` swaps all item display fields (Name, NameLower,
   Subtitle, Description) with localised values, backing up English originals. A
   DescriptionLocStr-based fallback is used when the primary Name_LocStr chain fails (common
   for procedural technology items whose Name_LocStr base key differs from the lang-file base).
4. `RewardDatabase.ApplyLocalisation()` swaps reward display names.
5. `WordDatabase.ApplyLocalisation()` swaps word display text (with group key fallback).
6. `RecipeDatabase`, `TitleDatabase`, `FrigateTraitDatabase`, `SettlementDatabase`,
   `WikiGuideDatabase`, `CompanionAccessoryDatabase` and `SpacePoiTableDatabase` apply
   localisation similarly.
7. `AccountPanel.RefreshRewardNames()` re-resolves cached reward display strings from the
   now-localised `GameItemDatabase` and `RewardEntry` data.
8. `FrigatePanel.RefreshTraitCombos()`, `SettlementPanel.RefreshPerkCombos()` and
   `RecipePanel.RefreshLanguage()` repopulate controls whose text embeds localised names.
9. Every loaded panel is reloaded through `RefreshLoadedPanels()`, then
   `MainForm.ApplyUiLocalisation()` re-applies menu, toolbar, tab and panel strings,
   including `NoSaveOverlay.RefreshLocalisation()`.
10. The language preference is persisted via `AppConfig.Language` for next startup.
11. The status bar shows the language tag and counts of localised items, rewards, words and
    UI strings (`status.language_localised`).

Language loading runs inside a small modal progress dialog and performs the database work on
a background task so the UI stays responsive. The app defaults to **en-GB** on first launch.
On subsequent launches `ApplyStartupLanguage()` restores the last selected language from
`AppConfig` after `LoadDatabase()`.

If the file is not found, the localisation service falls back to English and the affected
databases are reverted to their English defaults, and the status bar shows a fallback
message. Selecting a different language re-applies from the English baseline, so switching
languages does not accumulate drift. All internal editor logic (string comparisons,
filtering) always runs against IDs, not display names.

### Theme System

| Component | File | Purpose |
|---|---|---|
| ThemeManager | `Core/ThemeManager.cs` | Holds the current `AppTheme` (System, Light, Dark), resolves `Effective` (expands System using the OS preference) and raises `ThemeChanged` |
| ThemeColors | `Core/ThemeColors.cs` | Declares `Palette` records with named colour slots for Light and Dark, covering background, input, grid, menu, tool strip, inventory and JSON syntax colours |
| ThemeApplicator | `UI/ThemeApplicator.cs` | Walks a form or control tree and applies the active palette, preserving semantically coloured controls and restoring panel borders in light mode |

The **Theme** menu sets the theme, persists it to `AppConfig.Theme` and updates its check
marks. `ApplySavedTheme()` restores the saved value on startup, and `ReapplyTheme()`
re-applies the palette whenever `ThemeManager.ThemeChanged` fires. Controls with custom
painting (`InventoryGridPanel`, `JsonSyntaxTextBox`, `InvariantNumericTextBox`,
`NoSaveOverlay`, `SpaceStationSubPanel`) subscribe to the same event and repaint themselves.
`DoubleBufferedTabControl` (defined at the bottom of `UI/Panels/BasePanel.cs`) paints its
tabs from the active palette and is used by MainForm and several panels to avoid tab flicker.

### Editor Lock (NoSaveOverlay)

While no save file is loaded, every editor tab is locked:

- `InstallEditorLock()` adds a `NoSaveOverlay` to each tab page except Export Settings
  (index 13) and Database Search (index 15), which never need save data.
- The enabled state of every control under the tab content is captured, then the whole tree
  is disabled. `UpdateEditorLockState()` switches the overlay on or off and restores the
  captured states when a save is loaded.
- The overlay shows the localised `lock.title` and `lock.hint` messages and is themed via
  `ThemeColors`.
- `GetTabContent()` skips overlays when MainForm needs the real panel for a tab.

### Deferred Tab Loading

```
OnTabChanged:
  if no save loaded: return

  if leaving the Catalogue tab (index 8):
    CataloguePanel.SaveData() then PurgeData()
    remove index 8 from _loadedTabIndices

  if the selected tab is not in _loadedTabIndices:
    hide the tab content
    SuspendLayout -> LoadPanelForTab(index) -> ResumeLayout(false)
    show the content, add the index to _loadedTabIndices

  if switching to RawJsonPanel (and not during GOTO JSON navigation):
    SyncAllPanelData() then RawJsonPanel.RefreshTree()
```

Only panels that have been loaded are synced during `SyncAllPanelData`. This avoids calling
`SaveData` on panels that were never shown. The Catalogue panel is deliberately purged when
leaving the tab because it is one of the heaviest (grids with scaled icon bitmaps) and it is
safe to reload on demand.

### Data Flow

```
Open File:
  SaveFileManager.LoadSaveFile(path)
    returns JsonObject (root); context transforms and active-context detection are
    applied inside the loader
  _currentSaveData = root
  RawJsonPanel.CaptureBaseline(root)

Per Panel:
  panel.LoadData(_currentSaveData)
    extracts its slice via Logic constants
    populates UI controls

User Edits:
  InventoryGridPanel writes directly to JsonObject slots
  Other controls raise DataModified so _hasUnsavedChanges = true

Save File:
  SyncAllPanelData()
    each loaded panel.SaveData(_currentSaveData)
  SaveFileManager.SaveToFile(path, _currentSaveData, ...)
```

### GOTO JSON Navigation

Panels raise `GoToJsonRequested` (an `EventHandler<GoToJsonEventArgs>`, see
`UI/Util/GoToJsonEventArgs.cs`) from small header buttons next to the data they edit.
MainForm asks for confirmation (`common.goto_json_confirm`), calls `SyncAllPanelData()`,
switches to the Raw JSON Editor tab (index 14) and navigates to the supplied path segments
with `RawJsonPanel.NavigateToPath()`. Sources include Exosuit, Multitool, Starship, Fleet,
Freighter, Frigate, Squadron, Exocraft, Companion, Base, Systems (space station), Catalogue,
Milestone, Settlement and ByteBeat.

### Backup Restore

Edit menu items **Restore Backup (All)** and **Restore Backup (Single)** search all known
backup roots, list the backup ZIPs for the current save directory and open
`BackupPickerDialog` to choose one. That dialog lists backups newest first with columns for
the backup file name, creation time, size and location. After a confirmation prompt the ZIP
is restored (all contained files, or just the loaded save) and the current save is reloaded
through the platform-appropriate loader (`ReloadCurrentSave()`).

---

## Panel Architecture

Most panels follow this contract:

```csharp
public partial class XxxPanel : UserControl
{
    public void SetDatabase(GameItemDatabase? database);
    public void SetIconManager(IconManager? iconManager);
    public void LoadData(JsonObject saveData);
    public void SaveData(JsonObject saveData);
    public event EventHandler? DataModified;
}
```

The contract is not universal. Optional members in common use are:

| Member | Purpose |
|---|---|
| `ApplyUiLocalisation()` | Refresh UI strings after a language switch |
| `event EventHandler<GoToJsonEventArgs> GoToJsonRequested` | Request raw JSON navigation |
| `SetSaveScopeKey(string)` | Scope persisted UI state (for example pinned slots) to one save |
| `SetAccountData(JsonObject?)` | Supply loaded `accountdata` to completion sub-panels |
| `PurgeData()` | Release heavy cached rows and icons (Catalogue tabs) |

`ExportConfigPanel` has no `SaveData` (configuration is saved separately), and
`DatabaseSearchPanel` has neither `LoadData` nor `SaveData` because it never touches save
data. `MultiplayerPanel` and the completion sub-panels have `LoadData` / `SaveData` but are
hosted inside parent panels rather than registered as top-level tabs.

Panels extract their relevant JSON subtree from `saveData` (usually via
`saveData.GetObject("PlayerStateData")`) and forward it to child controls or use Logic class
methods to read/write fields.

### Panel Reference

#### Equipment Panels

| Panel | File | Description |
|---|---|---|
| **ExosuitPanel** | `UI/Panels/ExosuitPanel.cs` | Two `InventoryGridPanel` instances for the General cargo (10x12 max) and Technology (10x6 max) inventories. Uses `ExosuitLogic` for JSON key constants and export filenames. Supports cross-inventory transfer when auto-stacking cargo, and per-save pinned slots via `SetSaveScopeKey()`. |
| **MultitoolPanel** | `UI/Panels/MultitoolPanel.cs` | Selector that iterates the multitool array (no fixed cap). Displays name, seed, type, class and the size, damage, mining and scan stats. Each tool has a Store inventory grid. Supports archive move and import. Uses `MultitoolLogic` for type lookups and seed validation. |

#### Starship Panel

| Panel | File | Description |
|---|---|---|
| **StarshipPanel** | `UI/Panels/StarshipPanel.cs` | Selector that iterates the ship ownership array (no fixed cap). Displays name, seed, type, class and base stats (damage, shield, hyperdrive, maneuverability). Two inventory grids (Cargo and Technology) plus outer **Ship Details** and **Customisation** tabs. The Customisation tab edits scene, paint and texture slots for non-corvette ships; corvette detection disables it. Supports export/import, make primary, archive move/import, technology snapshot export/import and the corvette optimiser. Uses `StarshipLogic` extensively. |

#### Fleet Panels

| Panel | File | Description |
|---|---|---|
| **FleetPanel** | `UI/Panels/FleetPanel.cs` | Container panel with sub-tabs for Freighter, Frigates and Squadron. Hosts the three sub-panels, forwards their `DataModified` and `GoToJsonRequested` events to MainForm and cascades `SetDatabase` / `SetIconManager` / `LoadData` / `SaveData` / `ApplyUiLocalisation` to them. |
| **FreighterPanel** | `UI/Panels/FreighterPanel.cs` | Outer **Freighter** and **Rooms** sub-tabs. Edits freighter type, class, crew race, home/model/crew seeds and base stats. The Freighter page hosts Cargo and Technology inventory grids; the Rooms page summarises detected freighter rooms. Uses `FreighterLogic` for type/race mapping and room detection. |
| **FrigatePanel** | `UI/Panels/FrigatePanel.cs` | List of up to 30 frigates with type, grade, race and 11 stat categories. Uses `FrigateLogic` for type/grade lookups and `FrigateTraitDatabase` for trait editing. `RefreshTraitCombos()` repopulates trait dropdowns after JSON databases load and after language changes. |
| **SquadronPanel** | `UI/Panels/SquadronPanel.cs` | Squadron pilot management: ship type, pilot race and rank. Uses `SquadronLogic` for resource-to-type mapping. |

#### Vehicle and Companion Panels

| Panel | File | Description |
|---|---|---|
| **ExocraftPanel** | `UI/Panels/ExocraftPanel.cs` | Two main tabs. **Exocrafts** has a vehicle selector for the six types (Roamer, Nomad, Colossus, Pilgrim, Nautilon, Minotaur), name and camera/AI options, and inner Cargo and Technology inventory grids. **Summoning Stations** lists individual stations and stations that are part of bases, with details including name, timestamp, base name, galactic address, region seed, position, galaxy, portal code, signal booster, voxel coordinates, solar system and planet. Uses `ExocraftLogic` for owner/type mapping. |
| **CompanionPanel** | `UI/Panels/CompanionPanel.cs` | Up to 30 companion creatures (`CompanionLogic.MaxPetSlots`). Stats and Battle tabs cover species selection from `CompanionDatabase`, biome type, seed editing, appearance customisation from `CreaturePartDatabase`, gene editing, accessories from `CompanionAccessoryDatabase`, egg induction (raises `ExosuitCargoModified` so the Exosuit grid refreshes) and the pet battle team. Uses `CompanionLogic`. |

#### Structure Panels

| Panel | File | Description |
|---|---|---|
| **BasePanel** | `UI/Panels/BasePanel.cs` | Four inner tabs. **Bases**: base selection (planetary, freighter, space station, corvette and asteroid/space kinds, marked with `[F]`/`[S]`/`[C]`/`[A]` prefixes), position editing, NPC workers, terrain-edit actions, an Info / Objects right-hand editor, freighter room editing and move base computer. Raw base export/import is handled by `BaseExportLogic`: the `.nmsbase` file stays a raw base for older NMSE builds, with matching terrain written to a `.nmsbase.terrain.json` sidecar; replace is type-for-type and Import as New supports all recognised base kinds. **Chests**: 10 standard chest inventories. **Storage**: 8 special storage container tabs plus a freighter refund tab. **Systems**: hosts `SpaceStationSubPanel` (see Space Station Panels). Uses `BaseLogic` for position swapping and storage key definitions. |
| **SettlementPanel** | `UI/Panels/SettlementPanel.cs` | Four tabs: **Stats & Perks**, **Production**, **Building States** and **Building Editor**. The stats page edits 8 settlement stats with min/max sliders and a perk list with beneficial/detrimental indicators; production edits output items; the building pages edit placed building states and per-building fields. Uses `SettlementLogic` and `SettlementPerkDatabase`. `RefreshPerkCombos()` repopulates perk dropdowns after JSON databases load and after language changes. |

#### Catalogue and Knowledge Panels

| Panel | File | Description |
|---|---|---|
| **CataloguePanel** | `UI/Panels/CataloguePanel.cs` | 13-tab hub for discovery and completion editing: Known Technologies, Known Products, Known Specials, Known Words, Known Glyphs, Known Locations, Known Fish, Recipes, Wonders, Knowledge, Fossils, Raw Materials and Stats. The three Known item tabs host `CatalogueItemsPanel` lists with separate Known and Seen checkboxes, `All` / `Catalogue` counters, a Catalogue Y/N column, filter-scoped Add All/Remove All and a shared Complete Catalogue action. The glyph tab renders portal glyphs with `CoordinateHelper`; the locations tab shows galaxy information. Uses `CatalogueLogic`, `CatalogueItemCatalog`, `CatalogueDatabase` and `WordDatabase`. Hosts the completion sub-panels (see Shared Controls) and the account catalogue data supplied by MainForm. `PurgeData()` releases cached rows and icons when the tab is left, and `AddRecipeTab()` embeds the RecipePanel. |
| **CatalogueItemsPanel** | `UI/Panels/CatalogueItemsPanel.cs` | Unified item list for one Known tab. Renders Icon, Name, ID, Category, Expedition, Description, Catalogue and Known columns; computes content/header widths; exposes Add All/Remove All scoped to visible rows, catalogue-only filtering and export/import. Writes through `CatalogueItemCatalog`. |
| **RecipePanel** | `UI/Panels/RecipePanel.cs` | Embedded inside the Catalogue Recipes tab (inner Known Recipes and Recipe Info tabs). Displays crafting and refining recipes from `RecipeDatabase`. Bidirectional lookup: what produces an item, and what uses it as an ingredient. `RefreshLanguage()` repopulates the grid with localised item names on language change. |
| **MilestonePanel** | `UI/Panels/MilestonePanel.cs` | Main Stats and Other Stats tabs for milestone progress and global stat editing. Section icons from `MilestoneLogic.SectionIconMap`. Read/write via `MilestoneLogic` stat helpers. |
| **KnowledgeCompletionPanel** | `UI/Panels/KnowledgeCompletionPanel.cs` | Collected Knowledge completion (pages and story completers) derived from `CompletionGridPanel`. |
| **WondersCompletionPanel** | `UI/Panels/WondersCompletionPanel.cs` | Wonder slot completion for planet, creature, flora, mineral, treasure and weird base part records. |
| **AccountCataloguePanel** | `UI/Panels/AccountCataloguePanel.cs` | Account-level Fossils and Raw Materials completion (`SeenProducts` / `SeenSubstances`). |
| **DiscoveryStatsPanel** | `UI/Panels/DiscoveryStatsPanel.cs` | Discovery Stats completion for `DISC_*` and `FISH_*` global stat targets. |

#### Gameplay Panels

| Panel | File | Description |
|---|---|---|
| **MainStatsPanel** | `UI/Panels/MainStatsPanel.cs` | Four tabs. **General**: player stats (health, shield, energy, units, nanites, quicksilver), save info (save name, summary, play time, last save date, presets, account name), player outfits (export/import/copy), current and edited coordinates (galaxy, portal codes and glyphs, signal booster, distance to centre), and space battle controls. **Guides**: wiki topic grids using `WikiGuideDatabase` categories (English categories are used for grid placement via `GetEnglishCategory()`). **Titles**: title selection from `TitleDatabase`. **Multiplayer**: hosts the experimental `MultiplayerPanel`. Includes save-slot utilities. Uses `MainStatsLogic` for stat field definitions and read/write. |
| **MultiplayerPanel** | `UI/Panels/MultiplayerPanel.cs` | Experimental editor for the multiplayer co-op keys under `CommonStateData.SeasonData`: alien race, community team and team ship seeds. Hosted by the MainStatsPanel Multiplayer tab; raises `DataModified`. |
| **ByteBeatPanel** | `UI/Panels/ByteBeatPanel.cs` | ByteBeat music composition editor. Manages the ByteBeat JSON subtree in `PlayerStateData`, with per-song export, import and delete, and global shuffle/autoplay flags. |
| **AccountPanel** | `UI/Panels/AccountPanel.cs` | Manages rewards from `accountdata.hg` in three pages: Season Rewards, Twitch Rewards and Platform Rewards (with Steam MXML sync on PC). Unlock All / Lock All / Redeem All / Remove All bulk actions and filters per page. A **Check Consistency** button opens `ConsistencyDialog` to repair mismatches between reward arrays. Uses `AccountLogic`. `RefreshRewardNames()` re-resolves cached display strings after a language change so reward grids show localised names. |

#### Configuration and Debug Panels

| Panel | File | Description |
|---|---|---|
| **ExportConfigPanel** | `UI/Panels/ExportConfigPanel.cs` | File Extensions, Naming Templates and Help tabs for the `ExportConfig` singleton: file extension preferences, naming templates with variable substitution. No `SaveData` method (config is saved separately). |
| **RawJsonPanel** | `UI/Panels/RawJsonPanel.cs` | Raw JSON editor with Tree, Text and Split views. The text and split views use `JsonSyntaxTextBox`. Supports isolated-node editing in the split view, inline tree editing, Show Changes diffing against a captured baseline via `RawJsonLogic.DiffLine`, undo/redo, a read-only account data view, and named per-save/account bookmarks (`RawJsonPanel.Bookmarks.cs`, `RawJsonBookmarksLogic`). `RefreshTree()`, `NavigateToPath()`, `CaptureBaseline()` and `NotifyDataChanged()` are called by MainForm and the GOTO JSON handler. |

#### Space Station Panels

`UI/Panels/SpaceStationPanel.cs` contains the `SpaceStationSubPanel` used by the BasePanel
**Systems** tab. It shows a left-hand list of space station systems from
`PlayerStateData.Stats`, and a right-hand editor with two inner tabs:

| Tab | Contents |
|---|---|
| **Space Stations** | Address, portal code, glyph rendering, seven station stats (edited with `InvariantNumericTextBox`), Set Claimable Values and Go To Base buttons, and the Cosmos mission state combos. |
| **Space POIs** | Packed space POI discovery data (`PackedData0` / `PackedData1`) as decimal or hex editors plus per-slot discovery level combos, with layout inference from `SpacePoiTableDatabase` and `SpacePoiSlotInference`. |

The sub-panel raises `DataModified`, `GoToJsonRequested` and `GoToBaseRequested`, and uses
`SpaceStationLogic` throughout.

#### Database Search

| Panel | File | Description |
|---|---|---|
| **DatabaseSearchPanel** | `UI/Panels/DatabaseSearchPanel.cs` | New read-only tab 15. Searches every item loaded from `Resources/json` by id, name, description, category and other fields, with substring and `*` / `?` wildcard matching. Shows a virtualised result list with a details pane and item icon. Independent of any loaded save, so it is exempt from the editor lock and never raises data-modification events. |

---

## Shared Controls

### InventoryGridPanel

| | |
|---|---|
| File | `UI/Panels/InventoryGridPanel.cs` |
| Purpose | Visual inventory grid with item management, used by nearly every panel |

The most complex UI component. Renders inventory slots as a 10-column grid with 72x104 px
cells. Each cell shows an icon, item count, charge indicator, supercharge glow and pin
marker.

**Configuration methods** (normally called before `LoadInventory`):

| Method | Description |
|---|---|
| `SetDatabase(database)` | Set item database for picker and display |
| `SetIconManager(iconManager)` | Set icon source for cell rendering |
| `SetInventoryOwnerType(ownerType)` | "Suit", "Ship", "Weapon", "Freighter" or "Vehicle", filters tech items |
| `SetInventoryGroup(group)` | Inventory group for stack-size lookups from `InventoryStackDatabase` |
| `SetIsTechInventory(bool)` | Show charge indicators and tech adjacency borders |
| `SetIsCargoInventory(bool)` | Reject technology items |
| `SetIsStorageInventory(bool)` | Mark the grid as storage (affects auto-stack targets) |
| `SetIsChestInventory(bool)` | Mark the grid as a base chest |
| `SetSuperchargeConstraints(maxSlots, maxRow)` | Limit supercharged slot placement |
| `SetSuperchargeDisabled(bool)` | Disable supercharge actions |
| `SetSlotToggleDisabled(bool)` | Disable the enable/disable slot action |
| `SetMaxSupportedLabel(text)` | Capacity note shown by the grid |
| `SetPinSlotFeatureEnabled(bool)` | Enable pinned slots for auto-stack |
| `SetPinnedSlots(coords)` | Apply the persisted set of pinned slot coordinates |
| `SetSortingEnabled(bool)` | Show the sort mode combo |
| `BeginBatchUpdate()` / `EndBatchUpdate()` | Suppress redraws during bulk cell changes |
| `RefreshItemFilters()` | Rebuild the picker type and category filters |
| `SetCorvetteContext(saveRoot, shipIndex)` | Enable corvette base part resolution |
| `ClearCorvetteContext()` | Disable corvette base part resolution |
| `SetExportFileName(name)` | Default filename for export dialogs |
| `SetExportFileFilter(export, import, ext)` | File dialog filters and default extension |
| `LoadInventory(inventory)` / `SaveInventory(inventory)` | Load from and write back to the JSON slot array |
| `RefreshToolbarActions()` | Refresh toolbar visibility and enabled state |
| `ApplyUiLocalisation()` | Refresh localised labels and tooltips |

**Item editing flow:**

1. User selects a slot. The detail panel on the right shows the item and its fields.
2. The inline item picker (a Search box plus Type, Category and Item combo boxes in the
   detail panel) filters the item list. This replaced the old modal picker; the grid no
   longer uses `ItemPickerDialog`.
3. Choosing an item and pressing Apply writes directly to the slot's `JsonObject`,
   including count, charge, damage factor and supercharge state as applicable.
4. The grid refreshes the affected cell.

**Toolbar and context menu:**

| Area | Actions |
|---|---|
| Toolbar | Width / Height fields with **Resize**, **Sort** combo (None, Name, Category), **Auto-Stack** dropdown (To Chests, To Starship, To Freighter), **Export** and **Import** buttons |
| Context menu | Add Item, Remove Item, Enable/Disable Slot, Enable All Slots, Pin Slot, Repair Slot, Repair All Slots, Supercharge Slot, Supercharge All Slots, Fill Stack, Recharge All Technology, Refill All Stacks, Copy Item, Paste Item, Sort by Name, Sort by Category, Auto-Stack to Chests, Auto-Stack to Starship, Auto-Stack to Freighter |

**Interaction details:**

- Slots support drag and drop (with a drag threshold and drop-target highlight) to move or
  swap items within the grid.
- Sort modes reorder slots by name or category and preserve pinned slots.
- Pinned slots are excluded from auto-stack and are persisted per save scope by the host
  panel; changes raise `PinnedSlotsChanged`.
- Auto-stack moves matching stacks into chests, a starship or the freighter and raises
  `AutoStackToStorageRequested`, `AutoStackToStarshipRequested` or
  `AutoStackToFreighterRequested` (plus the equivalent selected-slot events) so the host can
  refresh the destination panel.
- Resize changes the inventory dimensions in the JSON data.

**Tech adjacency rendering:** When `SetIsTechInventory(true)`, the grid queries
`TechAdjacencyDatabase` to find items with matching `BaseStatType` values. Adjacent tech
items receive coloured synergy borders using the `LinkColourHex` from the database.

**Corvette base part resolution:** For corvette ships, `CV_` technology items map to
buildable base parts. `SetCorvetteContext` collects base part objects; the grid uses
`GameItemDatabase.CorvetteBasePartTechMap` and greedy first-match to resolve display names.

---

### ItemPickerDialog

| | |
|---|---|
| File | `UI/Util/ItemPickerDialog.cs` |
| Purpose | Modal dialog for selecting one or more game items from a filterable list |

After a redesign the dialog presents a filterable `DataGridView` with Icon, Name, Category
and ID columns, a filter text box with a clear button, multi-row selection, and a manual ID
entry box with an invalid-ID warning for entries that are not in the database. It is used by
`CataloguePanel` and `SettlementPanel` for item selection. `InventoryGridPanel` uses its
inline picker instead.

---

### CompletionGridPanel

| | |
|---|---|
| File | `UI/Controls/CompletionGridPanel.cs` |
| Purpose | Shared base for the catalogue completion sub-tabs |

Abstract base class that provides a row grid with a per-row checkbox, a completion counter,
**Complete All** and **Clear All** buttons, and an item filter box. Subclasses implement
`PopulateRows()` and the apply/clear semantics. `LoadData(saveData, catalogue)` supplies the
save data and verified catalogue pack; `SetDatabase` / `SetIconManager` provide names and
icons; `PurgeData()` releases cached rows and icons. Concrete subclasses are
`KnowledgeCompletionPanel`, `WondersCompletionPanel`, `AccountCataloguePanel` and
`DiscoveryStatsPanel`, all hosted by `CataloguePanel`. Data changes raise `DataModified`.

---

### InvariantNumericTextBox

| | |
|---|---|
| File | `UI/Controls/InvariantNumericTextBox.cs` |
| Purpose | Numeric editor with spinner buttons that stores full-precision doubles |

Replaces the built-in `NumericUpDown` throughout the UI. `NumericUpDown` stores its value as
`decimal`, which silently truncates 1 to 2 significant digits in a
`double` / `decimal` / `double` round-trip; this control keeps the value in `double` and
formats it with the `G17` specifier (17 significant digits, enough to represent every IEEE
754 double). Supports an optional `Minimum` and `Maximum`, up/down buttons, arrow keys, mouse
wheel stepping and a dropdown-free `NumericValue` property that is `null` for an empty or
invalid field. Panel fields for coordinates, scale, counts and station stats use it.

---

### JsonSyntaxTextBox

| | |
|---|---|
| File | `UI/Controls/JsonSyntaxTextBox.cs` |
| Purpose | Owner-drawn JSON viewer/editor with line numbers, syntax colouring and folding |

A fully custom control with no `RichTextBox`. Text is stored as a list of lines and only
visible lines are rendered during paint, so memory and scrolling cost are flat even for very
large files. Features include line numbers, JSON syntax colouring, node-level folding,
keyboard editing, caret tracking, clipboard support and an undo/redo stack. Its handle
budget is deliberately small: 3 HWNDs per instance (the control plus two scrollbars), with
GDI objects cached and shared, and a thread-pool caret timer rather than a WinForms timer.
Used by `RawJsonPanel` for the Text and Split views.

---

### NoSaveOverlay

| | |
|---|---|
| File | `UI/Controls/NoSaveOverlay.cs` |
| Purpose | Full-page overlay shown over editor tabs while no save file is loaded |

Displays the localised `lock.title` and `lock.hint` messages, blocks interaction with the
panel beneath it, and repaints itself from `ThemeColors` when the theme changes. Managed by
MainForm (see Editor Lock). `RefreshLocalisation()` updates its text after a language
switch.

---

### ColorEmojiLabel

| | |
|---|---|
| File | `UI/Controls/ColorEmojiLabel.cs` |
| Purpose | Label control that renders colour emoji/glyphs via GDI+ on Windows 10 and later |

Standard WinForms labels render emoji in monochrome. This custom control uses GDI+
`DrawString` with the Segoe UI Emoji font to produce full-colour emoji rendering. Falls back
to standard rendering on older Windows versions. Used minimally for symbol rendering.

---

## Dialogs and Utility Windows

### SplashForm

| | |
|---|---|
| File | `UI/SplashForm.cs` |
| Purpose | Lightweight startup splash with progress reporting |

Shown by `Program.Main()` before the main form is created and closed by MainFormResources
once the window is fully rendered. It is a dark, borderless, top-most window with the
application name, a status label and an owner-drawn green progress bar.
`SetProgress(percent, statusText)` clamps the value and repaints immediately. Fonts and the
progress bar are disposed with the form.

---

### BackupPickerDialog

| | |
|---|---|
| File | `UI/BackupPickerDialog.cs` |
| Purpose | Modal picker for choosing a backup ZIP to restore |

Lists backups newest first with columns for the backup file name, creation time, size and
location. The first item is preselected, double-click confirms, and `SelectedZipPath` returns
the chosen path. Used by Edit menu **Restore Backup (All)** and **Restore Backup (Single)**.

---

### ConsistencyDialog

| | |
|---|---|
| File | `UI/Dialogs/ConsistencyDialog.cs` |
| Purpose | Account reward consistency checker and fixer |

Opened from the AccountPanel **Check Consistency** button. Shows each consistency issue in a
scrollable grid (icon, ID, name and issue description) with per-row buttons to add the item
to the missing array or remove it from the current array, plus bulk **Fix All** buttons.
`HasChanges` reports whether any issue was resolved so the AccountPanel can mark the save
dirty. Uses `AccountLogic.ConsistencyIssue` and the item database for icons and names.

---

## UI Utilities

### FontManager

| | |
|---|---|
| File | `UI/Util/FontManager.cs` |
| Purpose | Manage the embedded NMS fonts via `PrivateFontCollection` |

Loads two embedded fonts: `NMSGeoSans_Kerned.ttf` for general text (`CreateFont`,
`CreateHeadingFont`, `ApplyFont`, `ApplyHeadingFont`) and `NMS_Glyphs_Mono.ttf` for portal
glyph rendering (`CreateGlyphFont`). Fonts are loaded from assembly resources so they do not
need to be installed system-wide; each falls back to a system family if loading fails.
`ApplyFont` / `ApplyHeadingFont` also enable `UseCompatibleTextRendering` so GDI+ uses the
private font. The GeoSans font is provided by NMSCD's No Man's Sky Universal Font under the
OFL license.

---

### RedrawHelper

| | |
|---|---|
| File | `UI/Util/RedrawHelper.cs` |
| Purpose | Suppress and resume control painting to eliminate flicker |

Cross-platform painting suspension implemented with standard WinForms members rather than
native messages:

```
RedrawHelper.Suspend(control)  // Visible = false, SuspendLayout()
// ... rebuild child controls ...
RedrawHelper.Resume(control)   // ResumeLayout(true), Visible = true, Invalidate(true), Update()
```

Hiding the control prevents all `WM_PAINT` processing on the control and its child tree
(unlike `SuspendLayout` alone, which only defers layout). Toggling `Visible` on a control
that already has a window handle only calls `ShowWindow(SW_HIDE / SW_SHOW)` and does not
destroy or recreate native handles in the subtree, which keeps it safe for the planned
cross-platform migration. Used by `InventoryGridPanel` during grid reconstruction to prevent
visible intermediate states.

---

### GalaxyDisplayHelper

| | |
|---|---|
| File | `UI/Util/GalaxyDisplayHelper.cs` |
| Purpose | Render galaxy core dots for galaxy names and grid cells |

Uses `GalaxyDatabase.GetGalaxyCoreColorValue()` to paint an anti-aliased core dot in the
galaxy's colour. Provides `CreateGalaxyCoreDotImage()` for a standalone image,
`ConfigureGalaxyDotLabel()` to attach the dot to a label, and `PaintGalaxyCell()` to draw
the name text plus dot inside a `DataGridView` cell paint event. Used by the catalogue
location and galaxy displays.

# Core Logic Classes

## Overview

The Core layer (`Core/`) encapsulates game-specific rules, lookups, and data
transformations so that UI panels stay thin. Most classes are `internal static` with no
constructors or instance state, which keeps them unit-testable without WinForms.

A few services intentionally hold state or are instantiated:

- `ExportConfig` is a public singleton with user-editable templates and extensions.
- `ThemeManager` holds the current theme; `ThemeColors` provides the palettes.
- `UpdateService` is a static service holding an `HttpClient`.
- `DiscoveriesFile` and `SpacePoiSlotInference` define instance types (data-file records
  and the inference input/output types).

**Pattern:**

```
Panel.LoadData(saveData)
  --> Logic.ReadSomething(jsonObject)  // pure extraction
  --> populate UI controls

Panel.SaveData(saveData)
  --> read UI controls
  --> Logic.WriteSomething(jsonObject, values)  // pure mutation
```

Panels call Logic methods with `JsonObject` arguments; Logic methods navigate the JSON
tree, read or write fields, and return typed results. Logic classes never touch UI
controls.

> **Tests:** `NMSE.Tests` targets `net10.0` without WinForms and compiles linked copies of
> pure-logic source files. When adding a new pure-logic file, add a `<Compile Include>`
> link in `NMSE.Tests/NMSE.Tests.csproj` if it should be covered by tests.

---

## Logic Classes

### AccountLogic

| | |
|---|---|
| File | `Core/AccountLogic.cs` |
| Purpose | Platform and season reward unlock state across the account file (`accountdata.hg`), Xbox `containers.index` account data, PS4 fallback, and the Steam settings MXML |

Loads account data from `accountdata.hg`, from the Xbox AccountData blob, or (as a
fallback) from a PS4 `savedata00.hg`. Extracts `UserSettingsData`, builds lists of
unlocked season, Twitch, and platform rewards, reads the `Seen*` arrays, and syncs the
reward arrays in `PlayerStateData`. Also backs the Account Rewards consistency checker.

**Key methods:**

| Method | Description |
|--------|-------------|
| `LoadAccountData(saveDirectory)` | Loads and parses account data, returns an `AccountData` bundle |
| `LoadXboxAccountData(slotInfo)` | Loads account data from an Xbox blob slot |
| `GetUnlockedSet(JsonArray?)` | Converts a JSON array of strings into a case-insensitive `HashSet` |
| `GetRedeemedSets(...)` | Reads the redeemed season/Twitch sets from player state |
| `BuildRewardRows(rewardsDb, unlocked, redeemed)` | Merges database entries with unlock/redeem state for display |
| `SaveRewardList(rewards, userSettings, key)` | Writes unlocked IDs back to a JSON array |
| `SaveRedeemedRewards(saveData, seasonRedeemed, twitchRedeemed, database)` | Writes redeemed arrays back to PlayerStateData |
| `SyncKnownArraysForChangedRewards(...)` | Keeps `Known*` arrays aligned with changed reward state |
| `IsNonTechReward(rewardId)` | Classifies rewards that must not be written to `KnownTech` |
| `CleanStaleKnownEntries(...)` | Removes stale entries from known arrays |
| `CheckConsistencyStructured(...)` | Builds the issue list used by `ConsistencyDialog` |
| `ResolveConsistencyIssue(...)` | Applies an add/remove fix for one consistency issue |

`AccountPanel.SaveData` refreshes its redeemed-state snapshots after each save.
Without that, checking and unchecking Redeemed in one session would skip the
`Known*` cleanup and leave the reward looking redeemed in game.

`CheckConsistencyStructured` maps each Known* reward entry to its correct Redeemed*
array by reward category, checks season, Twitch and platform redemption, ignores
entitlement items that have no redeem array, and checks KnownProducts/KnownTech as
well as KnownSpecials. A known entry that is not redeemed but is unlocked on the
account is marked ambiguous instead of being treated as definitely stale.

---

### BaseLogic

| | |
|---|---|
| File | `Core/BaseLogic.cs` |
| Purpose | Base building operations: position swapping, base computer moves, terrain edit cleanup, and storage container management |

Provides full base transforms and cleanup helpers used by the Bases panel.

**Key methods:**

| Method | Description |
|--------|-------------|
| `SwapPositions(a, b)` | Exchanges the `Position`, `Up`, and `At` fields between two base objects |
| `SwapPlayerBases(a, b)` | Swaps two player bases including metadata |
| `MoveBaseComputer(base, ...)` | Moves the base computer using the full coordinate transform |
| `ClearTerrainEdits(base)` | Removes terrain edits belonging to one base |
| `ClearAllTerrainEditsExceptBases()` | Removes terrain edits that are not tied to bases |
| `ClearAllTerrainEdits()` | Removes every terrain edit in the save |

Also defines `ChestInventoryKeys` (10 standard chest names) and `StorageInventories`,
the special storage containers such as Ingredient Storage, Rocket, and Fishing Platform
with their JSON keys, display names, and export filenames.

---

### CatalogueLogic

| | |
|---|---|
| File | `Core/CatalogueLogic.cs` |
| Purpose | Word/language knowledge, Known* item arrays, portal glyph tracking, and discovery management |

Defines race columns and prefixes for the nine race slots (Gek, Vy'keen, Korvax,
Traveller, Atlas, Autophage, and others; `TotalRaceCount = 9`), including the expanded
`RacePrefixes` (`^TRA_`, `^WAR_`, `^EXP_`, `^ROBOT_`, `^AUTO_`, and so on). Holds the
technology and product item-type sets used to filter catalogue views.

**Key methods:**

| Method | Description |
|--------|-------------|
| `LoadKnownItemIds(...)` / `SaveKnownItemIds(...)` | Read/write the Known* arrays |
| `LoadGlyphBitfield(...)` / `SaveGlyphBitfield(...)` | Read/write portal glyph completion |
| `SetWordFlagsForRace(...)` | Marks word flags for a race |
| `SyncWordStats(...)` | Keeps word statistics aligned with flags |
| `StripCaretPrefix(id)` / `EnsureCaretPrefix(id)` | Handle the `^` prefix used by database IDs |

---

### ItemUnlockRules

| | |
|---|---|
| File | `Core/ItemUnlockRules.cs` |
| Purpose | Canonical item-to-array rules for known and seen catalogue state |

Centralises the game-verified classification so the catalogue UI, completion actions
and reward code do not diverge:

- Technology uses `KnownTech` and `SeenTechnologies`.
- Products use `KnownProducts` and `SeenProducts`.
- SpecialShop items use `KnownSpecials`; buildings also use `KnownProducts`.
- Non-building SpecialShop items are not product-learnable.
- Catalogue visibility uses `KnownTech` for technology, and `KnownProducts` or
  `SeenProducts` for products/specials.

---

### CatalogueItemCatalog

| | |
|---|---|
| File | `Core/CatalogueItemCatalog.cs` |
| Purpose | Unified row model and separate save-known/account-seen state for the three item tabs |

Builds one `CatalogueItemEntry` per valid, completion-list or save-known item, with
Description and expedition number. Completion membership is derived from
`CatalogueDatabase.Known*ForCompletion`; expedition numbers come from
`RewardDatabase.SeasonRewards` (loaded from `Rewards.json` when needed).

`IsKnown`/`IsPartiallyKnown` cover only the save arrays. `IsSeen` covers only the account
Seen array. `SetKnown` writes the canonical save arrays and, when adding, also marks the
item seen; unchecking removes only the known arrays. `SetSeen` writes only the account
Seen array. For Specials, a Known write also adds the account `UnlockedSpecials` entry,
matching a game special redemption. The Seen column is hidden by default and can be
shown for the current session only for advanced unsee operations; it is not persisted
to app settings.

`GetCatalogueCompletion` uses the game's wiki visibility rule through
`ItemUnlockRules`: `KnownTech` for technology, and `KnownProducts` or `SeenProducts` for
products and specials. `GetKnownCount` drives `All: a / b`. No product path writes
`SeenSubstances`; those are handled by the Raw Materials/Complete Catalogue path.

---

### CatalogueCompletionLogic

| | |
|---|---|
| File | `Core/CatalogueCompletionLogic.cs` (+ `.Account`, `.CatalogueItems`, `.Knowledge`, `.Stats`, `.Wonders` partials) |
| Purpose | Catalogue completion counters, complete-catalogue compatibility and "clear all" operations |

Computes per-page status and completion counters and applies bulk completion operations
for the Catalogue tab, Account rewards, Collected Knowledge, Discovery Stats, and
Wonders.

**Representative methods:**

| Method | Description |
|--------|-------------|
| `CountMissingCatalogue(...)` / `CompleteCatalogue(...)` | Count and complete the catalogue using the game-visible known/seen rules, including all compiled `SeenSubstances` |
| `ApplyKnowledgePage(...)` / `ClearKnowledgePage(...)` | Complete or clear one Collected Knowledge page |
| `ApplyKnowledgePageProgress(...)` | Set page progress from a UI value |
| `ApplyKnowledgeCompleters(...)` / `ClearKnowledgeCompleters(...)` | Apply or clear the story completers |
| `GetKnowledgeCompleterStatuses(...)` | Builds status rows for the completion grid |
| `ApplyKnowledgeCompleter(...)` / `ApplyKnowledgeCompleterProgress(...)` | Complete one completer |
| `GetCompletionCounts(...)` | Counts completed pages for the tab headers / counters |
| Account / Wonders / Stats helpers | Completion logic for the other catalogue surfaces |

---

### KnowledgeCatalogue

| | |
|---|---|
| File | `Core/KnowledgeCatalogue.cs` |
| Purpose | Baked definitions of the Collected Knowledge pages and story completers |

Holds the verified `KnowledgePage` definitions (`Pages`), the default race slots used
when completing interaction pages, the language/glyph page mapping, the story completer
GLOBAL counters, and the Base Computer / Dev Notes page coordinates. `CatalogueCompletionLogic.Knowledge`
consumes these definitions.

---

### CompanionLogic

| | |
|---|---|
| File | `Core/CompanionLogic.cs` |
| Purpose | Companion operations: species lookup, deletion, accessories, pet battles, and slot management |

Biome data is delegated to `CompanionDatabase.BiomeTypes` (17 entries, from Lush through
GasGiant to All). Also covers accessory customisation, pet battle editing, import/export,
slot unlocking, and restricted move handling.

**Key methods:**

| Method | Description |
|--------|-------------|
| `LookupSpeciesName(speciesId)` | Queries `CompanionDatabase` for a display name |
| `DeleteCompanion(companion)` | Clears all fields (ID, name, seeds, accessories) to reset a slot |
| Accessory helpers | Read/write companion accessory descriptors and colours |
| Battle helpers | Read/write battle stats, abilities, and gene edits |

---

### DatabaseSearchLogic

| | |
|---|---|
| File | `Core/DatabaseSearchLogic.cs` |
| Purpose | Query parsing and wildcard/glob matching for the Database Search panel |

Parses user queries (including wildcard patterns) and matches them against the loaded
databases, producing the filtered result sets shown in the Database Search tab. The
panel works without a loaded save.

---

### DiscoveriesFile

| | |
|---|---|
| File | `Core/DiscoveriesFile.cs` |
| Purpose | Base type for the long-term editor data files stored in `<AppDir>/Discoveries/` |

Discoveries files are versioned JSON documents with a `Version` header, written
atomically via `Config/DiscoveriesStore` (temp file then move, corrupt/newer-version
backup). `Core/SpacePoiLayoutsFile.cs` is the first concrete data set
(`space-poi-layouts.json`), storing per-system space POI slot layouts.

---

### ExosuitLogic

| | |
|---|---|
| File | `Core/ExosuitLogic.cs` |
| Purpose | Constants for exosuit inventory keys, export filenames, and grid size labels |

Defines cargo and tech inventory JSON keys (`Inventory`, `Inventory_TechOnly`), export
filenames (`exosuit_cargo_inv.json`, `exosuit_tech_inv.json`), and grid size labels.
Size labels are localised through `UiStrings.Format("common.max_supported", ...)` rather
than hard-coded strings.

---

### ExocraftLogic

| | |
|---|---|
| File | `Core/ExocraftLogic.cs` |
| Purpose | Exocraft management, owner type mapping, localisation, and export filename generation |

Defines the six exocraft vehicle types with their array indices: Roamer (0), Nomad (1),
Colossus (2), Pilgrim (3), Nautilon (5), Minotaur (6). `GetOwnerTypeForVehicle` returns
the tech category owner (Colossus, Submarine, Mech, or Exocraft) used by
`InventoryGridPanel` for tech filtering.

**Additional methods:**

| Method | Description |
|--------|-------------|
| `VehicleTypeLocKeys` / `GetLocalisedVehicleTypeName(...)` | Localised vehicle type names |
| `BuildExportFileName(...)` / `BuildVehicleExportFileName(...)` | Export filename generation |
| `ParseGalacticAddressToVoxel(...)` | Extracts voxel coordinates from a galactic address |

---

### FreighterLogic

| | |
|---|---|
| File | `Core/FreighterLogic.cs` |
| Purpose | Freighter class, type, crew, stats, inventory, and room management |

Maps freighter model filenames to type names (Tiny, Small, Normal, Capital, Pirate) and
crew NPC resource paths to race display names (Gek, Vy'keen, Korvax). Reads and writes
base stat bonuses (`^FREI_HYPERDRIVE`, `^FREI_FLEET`) via `StatHelper`.

**Key methods:**

| Method | Description |
|--------|-------------|
| `LoadFreighterData(...)` / `SaveFreighterData(...)` | Read/write all editable freighter fields |
| `KnownRooms` / `DetectFreighterRooms(...)` | Known room list and room detection |
| `ResizeFreighterInventory(...)` | Changes the freighter inventory size |
| Localisation helpers | Localised type/crew names and export filenames |

---

### FrigateLogic

| | |
|---|---|
| File | `Core/FrigateLogic.cs` |
| Purpose | Fleet frigate management: types, grades, races, stats, expeditions, and trait editing |

Defines the frigate types (including `DeepSpaceCommon`), the three crew races (mapped
between internal NMS names like "Traders" and display names like "Gek"), and the stat
categories. The class grade is no longer stored as a field: it is computed from the
frigate's trait net score, and trait edits adjust to reach a target grade.

**Key methods:**

| Method | Description |
|--------|-------------|
| `ComputeClassFromTraits(...)` | Derives the grade from trait net score |
| `AdjustTraitsForTargetGrade(...)` | Adjusts traits to reach a selected grade |
| `AutoAdjustTraitsForTypeChange(...)` | Re-balances traits when the frigate type changes |
| `LevelVictoriesRequired(...)` | Level-up requirements |
| Expedition/state helpers | Read and write expedition state and progress |

---

### InventoryBulkActions

| | |
|---|---|
| File | `Core/InventoryBulkActions.cs` |
| Purpose | Whole-save bulk inventory operations |

Provides the Tools menu bulk actions across tech and cargo inventories
(`RechargeAllTechnology`, `RefillAllStacks`, `RepairAllSlots`,
`RepairAllTechnology`) plus the inventory auto-stack helpers used by the grid panels.
Called from the main form rather than a single panel.

---

### MainStatsLogic

| | |
|---|---|
| File | `Core/MainStatsLogic.cs` |
| Purpose | Player health, shield, energy, and currency management |

Defines the stat fields (Health, Shield, Energy, Units, Nanites, Quicksilver) with JSON
keys and maximum values. `ReadStatValue` reads from `PlayerStateData`, handling
int/long/double/decimal types and clamping to valid ranges. `WriteStatValues` writes the
stats back, supports optional raw values, and only writes values that changed.

**Key methods:**

| Method | Description |
|--------|-------------|
| `ReadStatValue(...)` / `ReadRawStatValue(...)` | Read clamped / raw stat values |
| `WriteStatValues(..., rawValues)` | Write changed stats, using unchecked unsigned casts for currency fields |
| `PresetToGameMode(...)` / `ApplyGameModeForPreset(...)` | Map difficulty presets to game modes and apply them |

---

### MilestoneLogic

| | |
|---|---|
| File | `Core/MilestoneLogic.cs` |
| Purpose | Milestone tracking, guild ranks, and global statistics management |

Maps the 16 milestone sections to icon files (for example `UI-MILESTONES.PNG`,
`UI-GEK.PNG`, `UI-PIRATE.PNG`). Locates the `^GLOBAL_STATS` group inside
`PlayerStateData.Stats` and provides `ReadStatEntryValue` / `WriteStatEntryValue`
helpers that handle both `IntValue` and `FloatValue` entry fields.

**Additional members:**

| Method | Description |
|--------|-------------|
| `GuildLeveledStats` | Guild milestone stat definitions |
| `GetGuildRank(...)` / `GetGuildMaxRank(...)` / `GetGuildNextRankIn(...)` | Guild rank calculations |
| `FindGlobalStats(saveData)` | Locates the global stats group |

---

### MultitoolLogic

| | |
|---|---|
| File | `Core/MultitoolLogic.cs` |
| Purpose | Multitool type, class, inventory, archive, and stat management |

Defines 17 multitool types with their model filenames: Standard, Rifle, Royal, Alien,
Pristine, Experimental, Sentinel, Sentinel B, Switch, Staff, Staff NPC, Staff Ruin,
Staff Bone, Atlantid, Voltaic Staff, Direwasp Disintegrator, and Starbound. Also
defines the four class grades (C/B/A/S).

**Key methods:**

| Method | Description |
|--------|-------------|
| `BuildToolList(multitools)` | Lists owned tools, skipping empty slots by seed validity |
| `LoadToolData(tool)` / `SaveToolData(tool, playerState, values, isPrimary)` | Read/write all editable tool fields |
| `DeleteToolData(tool)` | Resets a slot in place to preserve index alignment |
| Archive helpers (`MoveToolToArchive`, `ImportToolFromArchive`, ...) | Manage `ArchivedMultitools` slots |
| `GetArchivedWeaponClass(tool)` | Maps a model to the game's `WeaponStatClass` for archive metadata |

When the primary tool is edited, `SaveToolData` keeps `CurrentWeapon.Filename` and
`CurrentWeapon.GenerationSeed` in sync and updates `WeaponInventory`, matching the
game's own save invariants.

---

### OutfitLogic

| | |
|---|---|
| File | `Core/OutfitLogic.cs` |
| Purpose | Outfit list management, export/import, and copy to the active customisation |

Reads the outfit list from player state, exports and imports outfit JSON, and copies a
selected outfit into the active character customisation data.

---

### RawJsonLogic

| | |
|---|---|
| File | `Core/RawJsonLogic.cs` |
| Purpose | Formatting, parsing, editing, and diffing utilities for the Raw JSON Editor |

A substantial utility class backing the Raw JSON Editor rather than a handful of
formatting helpers. Includes:

- Line/format helpers (`ToLines`, `FormatJson`, `ToDisplayString`).
- Value serialisation and parsing (`SerializeValue`, `ParseValue`).
- Edit helpers (`FormatValueForEdit`, `ParseInputValue`) for inline editing.
- A Myers diff engine with compact diff output and context headers, used by the
  "Show Changes" viewer.
- Snippet APIs for exports and tests.

---

### SaveContext

| | |
|---|---|
| File | `Core/SaveContext.cs` |
| Purpose | Regular vs Expedition context switching |

Resolves which `PlayerStateData` (base or expedition) the panels should read and write.
All context-aware readers go through this class rather than hard-coding
`BaseContext`/`ExpeditionContext`.

---

### SettlementLogic

| | |
|---|---|
| File | `Core/SettlementLogic.cs` |
| Purpose | Settlement stats, population, decisions, production, and building state management |

Defines the settlement stat labels (the first being "Max Population"; the top-level
`Population` field is separate with `PopulationMax = 400` and `PopulationSoftMax = 200`),
the decision types, the alien race display names, and the building state decoder
(`BuildingStateSlotCount = 48`, `SettlementBuildingState`). Holds a curated list of 50+
allowed production item names and provides `BuildAllowedProductionItems(database)` to
create an ID-to-name mapping filtered against the `GameItemDatabase`. Caps production
quantity at 999 and settlement slots at 100.

**Key methods:**

| Method | Description |
|--------|-------------|
| `FilterSettlements(...)` | Filters the settlement list for display |
| `LoadSettlementData(...)` / `SaveSettlementData(...)` | Read/write editable settlement fields |
| `FindImportTargetIndex(...)` | Finds a target index for imports |
| Building state decoder | Reads and writes the bitfield-based building states |

---

### SpaceStationLogic

| | |
|---|---|
| File | `Core/SpaceStationLogic.cs` |
| Purpose | Per-system space station and Space POI support |

Provides per-system station statistics, Cosmos mission state handling, packed
discovery editing, conversion between universe addresses and portal codes used by
the Systems tab, and `BaseKind` classification of `PersistentPlayerBases` entries
(planetary, freighter, space station, corvette and asteroid/space bases) for the
Bases panel.

---

### SpacePoiSlotInference

| | |
|---|---|
| File | `Core/SpacePoiSlotInference.cs` |
| Purpose | Infers the POI type of space POI slot layouts |

Given the observed per-slot level values and a table of generation rules
(`SpacePoiTypeRule`), infers which POI type each slot holds. The soundness rule: a slot
is only labelled when every consistent layout allocates it the same type, so partially
allocated tails stay unlabelled.

---

### SpacePoiSlotTokens

| | |
|---|---|
| File | `Core/SpacePoiSlotTokens.cs` |
| Purpose | Token helpers for remembered per-system space POI layouts |

Each of the 32 slots is represented by a token: a POI type name, `.` for slots the
inference could not resolve (`Unknown`), or `-` for slots no consistent layout allocates
(`Unallocated`). Provides `Parse`, `Format`, and `Merge` for the remembered layout
strings; merge lets fresh inference win while never downgrading a known value.

---

### SpacePoiLayoutsFile

| | |
|---|---|
| File | `Core/SpacePoiLayoutsFile.cs` |
| Purpose | Persisted per-system space POI slot layouts |

Concrete `DiscoveriesFile` data set storing each system's slot layout in
`<AppDir>/Discoveries/space-poi-layouts.json`, with versioned migration from the legacy
`NMSE.conf` keys.

---

### SquadronLogic

| | |
|---|---|
| File | `Core/SquadronLogic.cs` |
| Purpose | Squadron pilot and ship management |

Maps pilot NPC resource filenames to races (including Traveller and Iteration variants)
and ship model filenames to display types (Hauler, Fighter, Explorer, Golden Vector,
Golden Rasamama S36, Boundary Herald, Vintage Interceptor, Corvette, and so on).
Provides race localisation, seeds, display names, deletion, and 4 pilot ranks (C/B/A/S).

**Key methods:**

| Method | Description |
|--------|-------------|
| `GetPilotDisplayName(...)` | Builds the display name for a pilot |
| Seed helpers | Read/write pilot/ship seeds |
| `DeletePilot(...)` | Resets a squadron slot |
| Localisation helpers | Localised races and ship types |

---

### StarshipLogic

| | |
|---|---|
| File | `Core/StarshipLogic.cs` |
| Purpose | Ship data transformation, type lookups, stat management, customisation, corvette tools, and archives |

The largest logic class. Maintains a `ShipInfo` dictionary mapping 18 resource filenames
to display metadata (type name, keywords, cargo/tech max labels). Key methods:

| Method | Description |
|--------|-------------|
| `GetShipInfo(filename)` | Returns display name, cargo label, tech label for a resource path |
| `BuildShipList(shipOwnership)` | Creates a list of owned ships from the JSON array |
| `LoadShipData(ship, playerState, index)` | Extracts all editable ship fields into a typed record |
| `SaveShipData(ship, playerState, values)` | Writes a record of edited values back to the JSON tree |
| `DeleteShipData(ship)` | Clears a ship slot |
| `IsCorvette(filename)` | Checks if a ship is a corvette (`BIGGS.SCENE.MBIN`) |
| `GetOwnerTypeForShip(typeName)` | Maps type to inventory owner; also maps "The Wraith" to AlienShip and "Vintage Interceptor" to RobotShip |
| `CountValidShips(shipOwnership)` | Counts non-empty slots by checking seed validity |
| Customisation helpers | Read/write `CharacterCustomisationData` (scene, slots, textures, palette) |
| Corvette helpers | Corvette base creation, import/export and the build optimiser |
| Archive helpers | Move/import archived ships (`.nmsship` ZIP import) |

Uses `StatHelper` to read/write base stats (`^SHIP_DAMAGE`, `^SHIP_SHIELD`, etc.) from
inventory `BaseStatValues` arrays.

---

### ThemeManager / ThemeColors

| | |
|---|---|
| Files | `Core/ThemeManager.cs`, `Core/ThemeColors.cs` |
| Purpose | Application theme state and palettes |

`ThemeManager` holds the current theme (`System`, `Light`, `Dark`) and applies it;
`ThemeColors` provides the palettes used by `UI/ThemeApplicator.cs` and the panels. This
is one of the stateful Core services rather than a static logic class.

---

### UpdateService

| | |
|---|---|
| File | `Core/UpdateService.cs` |
| Purpose | Self-updater against GitHub Releases |

Queries the GitHub Releases API, parses version tags and release assets, converts release
notes Markdown to display text, and offers in-app self-update with cloud-sync advisories.

---

### AppJsonContext

| | |
|---|---|
| File | `Core/AppJsonContext.cs` |
| Purpose | Source-generated `System.Text.Json` context for AOT-safe serialisation |

All `System.Text.Json` serialisation in the app goes through this source-generated
context so the trimmed Native AOT build keeps the required metadata.

---

## Utility Classes (`Core/Utilities/`)

### ExportConfig

| | |
|---|---|
| File | `Core/ExportConfig.cs` |
| Purpose | User-configurable naming templates and file extensions for inventory import/export |

A public singleton. Stores per-entity file extensions (for example `.nmsship`,
`.nmstool`, `.nmscorv`, `.nmssnap`, `.nmsst`, `.nmsfreight`) and naming templates with
single-brace variable substitution, for example `{ship_name}_{type}_{class}`. Available
variables include `{player_name}`, `{ship_name}`, `{multitool_name}`,
`{freighter_name}`, `{frigate_name}`, `{settlement_name}`, `{vehicle_name}`,
`{vehicle_type}`, `{creature_seed}`, `{chest_number}`, `{timestamp}`, `{type}`,
`{class}`, `{seed}`, `{race}`, `{rank}`, `{species}`, and `{base_name}`.

Serialises through `AppJsonContext` (source-generated). Dialog helpers:
`BuildDialogFilter`, `BuildOpenFilter`, and `BuildImportFilter`, which also support the
NMSSaveEditor and NomNom wrapper formats alongside native NMSE files.

---

### StringHelper

| | |
|---|---|
| File | `Core/Utilities/StringHelper.cs` |
| Purpose | String normalisation and filesystem-safe filenames |

Includes `SanitizeFileName(name)` (invalid filename characters and spaces become
underscores, empty input returns `"unnamed"`), plus `CollapseWhitespace`, `ToTitleCase`,
`NormalizeDisplayString`, `JoinNonEmpty`, and `Truncate`.

---

### InventoryImportHelper

| | |
|---|---|
| File | `Core/Utilities/InventoryImportHelper.cs` |
| Purpose | Detect and unwrap inventory JSON from multiple external save editor formats |

When importing an inventory file, the JSON may be wrapped in format-specific envelopes.
The class checks a prioritised list of ten known wrapper paths, including:

- `["Store"]` (NMSSaveEditor multitool format)
- `["Data", "Multitool", "Store"]` (NomNom data envelope)
- `["Data", "Vehicle", "Inventory"]` and `["Data", "Vehicle", "Inventory_TechOnly"]`
- `["Data", "Starship", "Inventory"]` and `["Data", "Starship", "Inventory_TechOnly"]`
- `["Data", "Freighter", "Inventory"]` and `["Data", "Freighter", "Inventory_TechOnly"]`
- `["Data", "Inventory"]` and `["Data", "Inventory_TechOnly"]`

If no known path matches, `FindInventoryBfs` performs a breadth-first search (up to
depth 4) for any object containing a `"Slots"` array. Helpers include
`FindInventoryObject`, `IsNomNomWrapper`, and `UnwrapNomNom`; specialised unwrappers
handle NomNom companion, frigate, and pilot wrappers.

---

### MxmlRewardEditor

| | |
|---|---|
| File | `Core/MxmlRewardEditor.cs` |
| Purpose | Read and write Steam `GCUSERSETTINGSDATA.MXML` for platform rewards |

Auto-detects the Steam install path and locates the MXML settings file. Parses nested
`<Property>` elements to extract reward IDs (prefixed with `^` to match database
conventions) and reconstructs the XML tree on write with sequential `_index`
attributes. Handles the legacy flat format by migrating it to the nested structure.

**Key methods:**

| Method | Description |
|--------|-------------|
| `AutoDetectMxmlPath()` | Locates the Steam settings MXML |
| `ReadUnlockedRewards(...)` | Reads unlocked reward IDs |
| `WriteUnlockedRewards(...)` | Writes reward IDs back, rebuilding indices |
| `SyncPlatformRewards(...)` | Syncs platform rewards with the account data |

---

### SeedHelper

| | |
|---|---|
| File | `Core/Utilities/SeedHelper.cs` |
| Purpose | Hex seed validation and normalisation |

`NormalizeSeed` trims, uppercases hex digits, and ensures a lowercase `0x` prefix (for
example `0xABCD1234`). `NormalizeSeedOrInteger` accepts either a hex seed or a plain
integer. `IsValidHexSeed` validates format and enforces a maximum length of 18
characters (`0x` plus 16 hex digits).

---

### StatHelper

| | |
|---|---|
| File | `Core/Utilities/StatHelper.cs` |
| Purpose | Read and write `BaseStatValues` arrays in inventory objects |

Inventory JSON objects contain a `BaseStatValues` array of `{BaseStatID, Value}`
entries. `ReadBaseStatValue(inventory, statId)` returns the numeric value (or 0.0);
`ReadBaseStatText` returns the original text for `RawDouble` values.
`WriteBaseStatValue` updates the value in place and skips writes when the value is
unchanged, preserving the original JSON text representation. Used by `StarshipLogic`,
`FreighterLogic`, `MultitoolLogic`, and the stat editing panels.

---

### Other utilities

| Class | File | Purpose |
|-------|------|---------|
| `CoordinateHelper` | `Core/Utilities/CoordinateHelper.cs` | Convert between galaxy/system/planet/position coordinates and display forms |
| `InventorySlotHelper` | `Core/Utilities/InventorySlotHelper.cs` | Shared slot index/validity helpers for inventory grids |
| `MathHelper` | `Core/Utilities/MathHelper.cs` | Numeric helpers shared across logic classes |
| `NmsColourPalette` | `Core/Utilities/NmsColourPalette.cs` | NMS colour palette values and conversion helpers |
| `NumericParseHelper` | `Core/Utilities/NumericParseHelper.cs` | Culture-invariant numeric parsing for UI and data files |
| `ProceduralSeedHelper` | `Core/Utilities/ProceduralSeedHelper.cs` | Procedural seed generation and formatting |
| `RawNumberGuard` | `Core/Utilities/RawNumberGuard.cs` | Safe integer writes that preserve JSON number types |

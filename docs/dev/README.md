# NMSE - No Man's Save Editor

## Project Overview

NMSE (No Man's Save Editor) is an open-source .NET WinForms desktop application for
viewing and editing No Man's Sky save files, originally designed and developed by [**vector_cmdr**][githubOwner].

It supports saves from every platform the game ships on (Steam, GOG, Xbox Game Pass,
PlayStation 4, and Nintendo Switch) and handles each platform's unique file layout,
compression, and encryption transparently.

The editor loads a save file into an in-memory JSON tree built from custom `JsonObject` and
`JsonArray` classes (not `System.Text.Json`), presents the data through categorized tab
panels, and writes changes back while preserving binary-safe round-trip fidelity.

A companion console tool, NMSE.Extractor, mines the game's PAK archives to produce the JSON
databases and icons that the editor uses at runtime.

The codebase follows a strict separation between *Logic* classes (pure, static, testable
data manipulation) and *Panel* classes (WinForms UI binding). All game-specific knowledge
lives in the Core and Data layers so the UI layer stays thin.

## Solution Structure

| Project | Description |
|---------|-------------|
| **NMSE** (`NMSE.csproj`) | Main application (WinForms and Core) -- Core, Data, IO, Models, UI, Config, Resources |
| **NMSE.Tests** (`NMSE.Tests/`) | Unit tests for the main application |
| **NMSE.Extractor** (`NMSE.Extractor/`) | Console tool that extracts game data from NMS PAK files |
| **NMSE.Extractor.Tests** (`NMSE.Extractor.Tests/`) | Unit tests for the extractor |
| **NMSE.Site** (`NMSE.Site/`) | Static web companion site (HTML/JS/CSS) for GitHub Pages |

Build output is redirected to `Build/bin/` and `Build/obj/` via `Directory.Build.props`.
The solution file is `NMSE.slnx` (modern XML format). `NMSE.Site` is a static companion
site and is not part of the solution.

### Build System

The project targets .NET 10.0 (Windows) with Native AOT and trimming enabled for release.

- **Development**: `dotnet build` produces managed IL (fast, supports debugging).
- **Release**: `dotnet publish -c Release` produces a self-contained, trimmed, Native AOT
  executable. Users do not need .NET installed. The CI workflow uses this for releases.
- Tiered PGO is enabled for development builds (`dotnet run`); it has no effect on AOT output.
- `version.json` is the single source of truth for major/minor/patch; MSBuild generates
  `BuildInfo.g.cs` (git-ignored) at build time.
- Native AOT needs `NMSE.TrimmerRoots.xml` to keep the DataGridView header cell constructors
  that the framework creates reflectively during grid teardown.
- Release builds zip their output automatically via the `ZipBuildOutput` / `ZipPublishOutput`
  MSBuild targets. Release assets are `NMSE-<version>-Release.zip`,
  `NMSE-<version>-Release-x64.AppImage` and `NMSE-<version>-Release-x64.dmg`.
- CI: `build-nmse.yml` runs on `version.json` changes to `main` (build, tests, AOT publish,
  zip, GitHub Release); `validate-pr.yml` and `static.yml` are manual/disabled.

## Documentation Index

### [Core Logic](core-logic.md)

Logic classes that encapsulate game rules, data transformation, and domain operations.
Most are `internal static` with no mutable state; `CatalogueItemCatalog` is a scoped
instance model for the unified catalogue item lists.

- AccountLogic, BaseLogic, CatalogueLogic, CatalogueCompletionLogic (partials), CatalogueItemCatalog, CompanionLogic
- DatabaseSearchLogic, ExocraftLogic, ExosuitLogic, FreighterLogic, FrigateLogic
- InventoryBulkActions, KnowledgeCatalogue, MainStatsLogic, MilestoneLogic, MultitoolLogic
- OutfitLogic, RawJsonLogic, SaveContext, SettlementLogic, SpaceStationLogic
- SpacePoiSlotInference, SpacePoiSlotTokens, SpacePoiLayoutsFile, DiscoveriesFile
- SquadronLogic, StarshipLogic, ThemeManager, ThemeColors, UpdateService, AppJsonContext
- ExportConfig; `Core/Utilities/`: CoordinateHelper, InventoryImportHelper, InventorySlotHelper
  MathHelper, NmsColourPalette, NumericParseHelper, ProceduralSeedHelper, RawNumberGuard
  SeedHelper, StatHelper, StringHelper

### [Data Layer](data-layer.md)

Databases and helper classes that load, store, and query game reference data.

- BaseStatLimits, CatalogueDatabase, CompanionDatabase, ElementDatabase
- FrigateTraitDatabase, GalaxyDatabase, GameItemDatabase, GameItem
- IconManager, InventoryStackDatabase, JsonNameMapper, LeveledStatDatabase
- LocalisationService, ProceduralStubs, RecipeDatabase, RewardDatabase
- SettlementDatabase, SpacePoiLayoutCache, SpacePoiTableDatabase, StarshipDatabase
- TechAdjacencyDatabase, TechPackDatabase, TitleDatabase, UiStrings
- WikiGuideDatabase, WordDatabase

### [IO Layer](io-layer.md)

Save file reading, writing, compression, encryption, and platform abstraction.

- SaveFileManager, SaveSlotManager, ContainersIndexManager, MemoryDatManager
- MetaCrypto, MetaFileWriter, BinaryIO
- Lz4Compressor, Lz4CompressorStream, Lz4BufferedCompressorStream
- Lz4ChunkedCompressorStream, Lz4DecompressorStream

### [Models](models.md)

Domain model classes, the custom JSON tree, and value types.

- JsonObject, JsonArray, JsonParser, JsonReader, JsonException
- BinaryData, RawDouble, IPropertyChangeListener
- Ship, ShipType, ShipClass, Multitool, MultitoolType
- Frigate, Companion, Inventory, InventoryType
- Recipe, DifficultyLevel, SaveFileMetadata

> Note: the entity wrapper classes (`Ship`, `Multitool`, `Frigate`, `Companion`, `Inventory`,
> `Recipe`), `SaveFileMetadata`, `InventoryType`, `MultitoolType` and `IPropertyChangeListener`
> are currently unreferenced by application code; the JSON tree is used directly by panels.

### [UI Layer](ui-panels.md)

WinForms panels, controls, and visual infrastructure.

- MainForm (tab orchestrator, `MainFormResources`)
- Panels: Account, AccountCatalogue, Base (Systems: Space Stations and Space POIs), ByteBeat
  Catalogue, Companion, DatabaseSearch, DiscoveryStats, Exocraft, Exosuit, ExportConfig
  Fleet, Freighter, Frigate, InventoryGrid, KnowledgeCompletion, MainStats, Milestone
  Multiplayer (experimental), Multitool, RawJson, Recipe, Settlement, SpaceStation
  Squadron, Starship, WondersCompletion
- Controls: CompletionGridPanel, InvariantNumericTextBox, JsonSyntaxTextBox, NoSaveOverlay
  ColorEmojiLabel; Dialogs: BackupPickerDialog, ConsistencyDialog, ItemPickerDialog
- Infrastructure: FontManager, GalaxyDisplayHelper, RedrawHelper, ThemeApplicator, GoToJsonEventArgs

### [Localisation](ui-localisation.md)

UI string localisation drives menus, tabs, dialog messages, status bar text,
grid headers, and other user-visible labels. The system loads per-language
JSON files from `Resources/ui/lang/{bcp47}.json`, falls back to English,
and updates all panels via their `ApplyUiLocalisation()` methods.

- UiStrings handles loading, lookup, formatting, and fallback behaviour
- Language menu triggers reloads and updates every UI panel
- Supports 16 languages (en-GB source + 15 translations)

### [NMSE.Extractor](extractor.md)

The data extraction pipeline that converts NMS game archives into editor databases.

- Program (multi-stage pipeline), `Config/ExtractorConfig`
- `Data/`: CataloguePackBuilder, Categorizer, CuratedItemNames, ImageExtractor, JsonWriter
  LocalisationBuilder, MbinConverter, MxmlParser, PakExtractor, Parsers, ProductLookup, TeeTextWriter
- `Util/`: SteamLocator, ToolManager

### Cross-Platform (Linux & macOS)

NMSE is a Windows WinForms application, but runs on Linux and macOS via Wine
compatibility layers. NMSE ships as a Native AOT build, so no .NET runtime is installed
under Wine; what matters is a current Wine package. On macOS, use the free Gcenx Wine
Builds (recommended) or the paid CrossOver 26 or later. Whisky and Homebrew Wine builds
are not supported because they lag behind the required Wine version. Options for a native
port are under review; there is no current native cross-platform plan.

**Linux:**
- [Wine Linux Guide](wine-linux-guide.md) - run NMSE via Wine (launch script, AppImage, or manual)
- [Bottles Linux Guide](bottles-linux-guide.md) - run NMSE via Bottles (GUI Wine manager)

**macOS:**
- [Gcenx Wine Builds Guide](gcenx-macos-guide.md) - free, recommended (Apple Silicon and Intel)
- [CrossOver macOS Guide](crossover-macos-guide.md) - paid, supported commercial alternative

**Packaging scripts:** `scripts/linux/` (launch script, AppImage builder, Bottles config), `scripts/macos/` (DMG builder).

## Key Architectural Decisions

| # | Decision | Rationale |
|---|----------|-----------|
| 1 | Logic/Panel separation | Logic classes are static and testable without a UI; panels delegate all game knowledge to them |
| 2 | Custom JSON model | `JsonObject`/`JsonArray` preserve field order, support binary data, round-trip `RawDouble` values, and integrate the name mapper -- things `System.Text.Json` does not do out of the box |
| 3 | Save pipeline: containers.index / memory.dat -> LZ4 -> JSON | Each platform wraps the same JSON payload differently; the IO layer normalizes everything to a single `JsonObject` |
| 4 | Name mapper (obfuscated keys) | NMS obfuscates JSON keys to 3-character codes; the mapper translates both ways so the editor can use human-readable names internally |
| 5 | Context transforms | `PlayerStateData` resolves to `BaseContext` or `ExpeditionContext` at runtime by inspecting the save's season/context data; registered transforms keep panel reads context-aware |
| 6 | version.json -> BuildInfo.g.cs | `version.json` is the single source of truth for major/minor/patch; MSBuild reads it and generates `BuildInfo.g.cs` at build time so the version flows into the app title, About dialog, and zip filename |
| 7 | IconManager + ColorEmojiLabel | Icons are downscaled to 128 px max and cached; `ColorEmojiLabel` renders NMS glyphs via GDI+ |
| 8 | InventoryGrid as reusable control | One grid control handles every inventory type (suit, ship, weapon, freighter, vehicle) with owner-type configuration |
| 9 | Multi-format import/export | `InventoryImportHelper` detects and unwraps NomNom and NMSSaveEditor wrappers so users can share inventories across tools |
| 10 | Extractor pipeline (MBIN -> MXML -> JSON) | Game data is compiled into MBIN binary; the extractor decompiles to MXML, parses to dictionaries, then categorizes into JSON database files |
| 11 | Multi-language localisation | Game strings load from `Resources/json/lang/` (16 languages, BCP 47 tags) and UI strings from `Resources/ui/lang/`; items store `_LocStr` keys for runtime lookup. The language menu switches display language; internal logic stays English |
| 12 | Discoveries store | Long-term editor data (for example space POI layouts) lives in `<AppDir>/Discoveries/` via `DiscoveriesStore`/`DiscoveriesFile` (versioned JSON, atomic writes, legacy migration from `NMSE.conf`) |
| 13 | Source-generated JSON context | `AppJsonContext` uses System.Text.Json source generation so the trimmed Native AOT build can still (de)serialize its data files |
| 14 | Tests link source files | `NMSE.Tests` targets `net10.0` (no WinForms) and compiles linked copies of pure-logic sources; new pure-logic files must be added to `NMSE.Tests.csproj` |
| 15 | No external packages | Only xUnit (tests) and System.Drawing.Common; LZ4, TEA and SpookyHash are implemented natively in C# with no p/invoke, for cross-platform portability |
| 16 | Catalogue completion | `ItemUnlockRules` defines the canonical known/seen arrays; `CatalogueItemCatalog` builds the item lists with separate Known and Seen state; Add All/Remove All act on the visible filtered Known rows; `CatalogueCompletionLogic` retains the Complete Catalogue action and the completion sub-tabs |
| 17 | Space POI inference | `SpacePoiSlotInference` labels slot types only when every consistent layout agrees; per-system layouts persist in the Discoveries store |
| 18 | Backups | Every save writes a zip backup first (configured folder, else EXE-relative, else TEMP); restore uses `BackupPickerDialog` for all-or-single restore |
| 19 | Self-update | `UpdateService` queries GitHub Releases, parses version/assets and offers in-app self-update with cloud-sync advisories |

### Reversed game code

Some Core logic reimplements algorithms reversed from the game. These areas are deliberately not documented here: they are game-derived reference code, provided for save interoperability only, and are not covered by the project's licence. Do not add algorithm documentation for them.


[githubOwner]: https://github.com/vectorcmdr
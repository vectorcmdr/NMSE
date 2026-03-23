# NMSE - No Man's Save Editor

## Project Overview

NMSE (No Man's Save Editor) is an open-source .NET WinForms desktop application for
viewing and editing No Man's Sky save files, originally designed and developed by [**vectorcmdr**][githubOwner].

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
The solution file is `NMSE.slnx` (modern XML format).

### Build System

The project targets .NET 10.0 (Windows) with Native AOT and trimming enabled for release.

- **Development**: `dotnet build` produces managed IL (fast, supports debugging).
- **Release**: `dotnet publish -c Release` produces a self-contained, trimmed, Native AOT
  executable. Users do not need .NET installed. The CI workflow uses this for releases.
- Tiered PGO is enabled for development builds (`dotnet run`); it has no effect on AOT output.

## Documentation Index

### [Core Logic](core-logic.md)

Logic classes that encapsulate game rules, data transformation, and domain operations.
All classes are `internal static` with no mutable state.

- AccountLogic, BaseLogic, CompanionLogic, DiscoveryLogic, ExosuitLogic
- FreighterLogic, FrigateLogic, MainStatsLogic, MilestoneLogic, MultitoolLogic
- RawJsonLogic, SettlementLogic, SquadronLogic, StarshipLogic, ExocraftLogic
- ExportConfig, FileNameHelper, InventoryImportHelper, MxmlRewardEditor, SeedHelper, StatHelper

### [Data Layer](data-layer.md)

Databases and helper classes that load, store, and query game reference data.

- CompanionDatabase, CreaturePartDatabase, ElementDatabase, FrigateTraitDatabase
- GalaxyDatabase, GameItemDatabase, GameItem, IconManager
- InventoryStackDatabase, JsonNameMapper, LeveledStatDatabase, BaseStatLimits
- ProceduralStubs, RecipeDatabase, RewardDatabase, SettlementPerkDatabase
- TechAdjacencyDatabase, TechPackDatabase, TitleDatabase, WikiGuideDatabase
- WordDatabase, CoordinateHelper, LocalisationService, UiStrings

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

### [UI Layer](ui-panels.md)

WinForms panels, controls, and visual infrastructure.

- MainForm (tab orchestrator)
- Panels: Account, Base, ByteBeat, Companion, Discovery, Exocraft, Exosuit
  ExportConfig, Fleet, Freighter, Frigate, InventoryGrid, MainStats, Milestone
  Multitool, RawJson, Recipe, Settlement, Squadron, Starship
- ItemPickerDialog, FontManager, RedrawHelper, ColorEmojiLabel

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

- Program (12-stage pipeline), ExtractorConfig, Categorizer, ImageExtractor
- JsonWriter, LocalisationBuilder, MbinConverter, MxmlParser, PakExtractor
- Parsers, ProductLookup, TeeTextWriter, SteamLocator, ToolManager

### Cross-Platform (Linux & macOS)

NMSE is a Windows WinForms application, but can run on Linux and macOS via Wine
compatibility layers. A native cross-platform version using Eto.Forms is planned.

**Linux:**
- [Wine Linux Guide](wine-linux-guide.md) - run NMSE via Wine (launch script, AppImage, or manual)
- [Bottles Linux Guide](bottles-linux-guide.md) - run NMSE via Bottles (GUI Wine manager)

**macOS:**
- [Whisky macOS Guide](whisky-macos-guide.md) - run NMSE via Whisky (free, Apple Silicon supported)
- [CrossOver macOS Guide](crossover-macos-guide.md) - run NMSE via CrossOver (paid, best Apple Silicon)

**Packaging scripts:** `scripts/linux/` (launch script, AppImage builder, Bottles config), `scripts/macos/` (Homebrew Cask formula)

**Work plan:** See [Cross-Platform Work Plan](cross-platform-workplan.md) for the full migration roadmap.

## Key Architectural Decisions

| # | Decision | Rationale |
|---|----------|-----------|
| 1 | Logic/Panel separation | Logic classes are static and testable without a UI; panels delegate all game knowledge to them |
| 2 | Custom JSON model | `JsonObject`/`JsonArray` preserve field order, support binary data, round-trip `RawDouble` values, and integrate the name mapper -- things `System.Text.Json` does not do out of the box |
| 3 | Save pipeline: containers.index / memory.dat -> LZ4 -> JSON | Each platform wraps the same JSON payload differently; the IO layer normalizes everything to a single `JsonObject` |
| 4 | Name mapper (obfuscated keys) | NMS obfuscates JSON keys to 3-character codes; the mapper translates both ways so the editor can use human-readable names internally |
| 5 | Context transforms | `PlayerStateData` resolves to either `BaseContext` or `ExpeditionContext` at runtime via registered transforms on the root `JsonObject` |
| 6 | version.json -> BuildInfo.g.cs | `version.json` is the single source of truth for major/minor/patch; MSBuild reads it and generates `BuildInfo.g.cs` at build time so the version flows into the app title, About dialog, and zip filename |
| 7 | IconManager + ColorEmojiLabel | Icons are downscaled to 128 px max and cached; `ColorEmojiLabel` renders NMS glyphs via GDI+ |
| 8 | InventoryGrid as reusable control | One grid control handles every inventory type (suit, ship, weapon, freighter, vehicle) with owner-type configuration |
| 9 | Multi-format import/export | `InventoryImportHelper` detects and unwraps NomNom and NMSSaveEditor wrappers so users can share inventories across tools |
| 10 | Extractor pipeline (MBIN -> MXML -> JSON) | Game data is compiled into MBIN binary; the extractor decompiles to MXML, parses to dictionaries, then categorizes into JSON database files |
| 11 | Multi-language localisation | Per-language JSON files in `Resources/json/lang/` (16 languages, BCP 47 tags). Items store `_LocStr` keys for runtime localisation lookup. Language menu in MainForm switches display language; all internal logic uses English defaults but has a translation service for custom per-language UI strings in `Resources/ui/lang/` |


[githubOwner]: https://github.com/vectorcmdr
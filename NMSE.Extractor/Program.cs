using System.Collections.Concurrent;
using NMSE.Extractor.Config;
using NMSE.Extractor.Data;
using NMSE.Extractor.Util;
using System.Text.RegularExpressions;

namespace NMSE.Extractor;

/// <summary>
/// Entry point for the NMSE.Extractor application.
/// Orchestrates the extraction, conversion, categorization, and enrichment of No Man's Sky
/// game data into structured JSON files for the NMSE (NO MAN's SAVE EDITOR) database.
/// Handles setup, logging, tool management, resource cleanup, and the main extraction workflow.
/// </summary>
public class Program
{
    public static async Task<int> Main()
    {
        Console.Title = "NMSE.Extractor: NMSE DB Extractor";
        // Set up logging: mirror all console output to log.txt alongside the executable
        string logPath = Path.Combine(AppContext.BaseDirectory, "log.txt");
        var logStream = new StreamWriter(logPath, append: false) { AutoFlush = true };
        var originalOut = Console.Out;
        var originalErr = Console.Error;
        var teeOut = new TeeTextWriter(originalOut, logStream);
        var teeErr = new TeeTextWriter(originalErr, logStream);
        Console.SetOut(teeOut);
        Console.SetError(teeErr);

        try
        {
            Console.WriteLine("=".PadRight(50, '='));
            Console.WriteLine("NMSE (NO MAN'S SAVE EDITOR) DB Extractor");
            Console.WriteLine("=".PadRight(50, '='));

            // Resolve paths
            string baseDir = AppContext.BaseDirectory;
            string resourcesDir = Path.Combine(baseDir, ExtractorConfig.ResourcesFolder);
            string toolsDir = Path.Combine(baseDir, ExtractorConfig.ToolsFolder);
            string banksDir = Path.Combine(baseDir, ExtractorConfig.BanksFolder);
            string banksExtractedDir = Path.Combine(banksDir, "extracted");
            string mbinDir = Path.Combine(resourcesDir, ExtractorConfig.MbinSubfolder);
            string jsonDir = Path.Combine(resourcesDir, ExtractorConfig.JsonSubfolder);
            string imagesDir = Path.Combine(resourcesDir, ExtractorConfig.ImagesSubfolder);
            string mapDir = Path.Combine(resourcesDir, ExtractorConfig.MapSubfolder);

            // Step 0: Find Steam / PCBANKS
            Console.WriteLine("\n--- Step 0: Locating game files ---");
            string pcbanks = SteamLocator.FindPcBanksPath();
            Console.WriteLine($"[OK] PCBANKS: {pcbanks}");

            // Step 1: Estimate storage and prompt user
            Console.WriteLine("\n--- Step 1: Storage estimate ---");
            if (!PromptStorageConfirmation(pcbanks, baseDir))
            {
                Console.WriteLine("Extraction cancelled by user.");
                return 0;
            }

            // Step 2: Ensure tools
            Console.WriteLine("\n--- Step 2: Ensuring tools are up to date ---");
            await ToolManager.EnsureHgPakToolAsync(toolsDir);
            await ToolManager.EnsureMbinCompilerAsync(toolsDir);
            await ToolManager.EnsureImageMagickAsync(toolsDir);

            string hgpaktoolPath = Path.Combine(toolsDir, "hgpaktool.exe");
            string mbinCompilerPath = Path.Combine(toolsDir, "MBINCompiler.exe");

            // Step 3: Clean resources
            Console.WriteLine("\n--- Step 3: Cleaning resources ---");
            CleanResources(resourcesDir);

            // Clean banks/extracted/ to prevent stale data from previous runs masking failures
            string banksExtracted = Path.Combine(banksDir, ExtractorConfig.ExtractedSubfolder);
            if (Directory.Exists(banksExtracted))
            {
                Directory.Delete(banksExtracted, recursive: true);
                Console.WriteLine("  [OK] Removed banks/extracted/ (stale data cleanup)");
            }

            try
            {
                // Step 4: Per-pak filtered extraction - copies one pak at a time, extracts, removes it
                Console.WriteLine("\n--- Step 4: Extracting game data from .pak files ---");
                PakExtractor.ExtractPerPak(hgpaktoolPath, pcbanks, banksDir,
                    ExtractorConfig.GetFiltersForPak);

                // Step 5: Consolidate MBINs from extracted data (uses Move, not Copy)
                Console.WriteLine("\n--- Step 5: Consolidating MBINs ---");
                MbinConverter.ConsolidateMbins(resourcesDir, banksDir);

                Console.WriteLine("\n--- Step 6: Converting MBIN -> MXML ---");
                MbinConverter.ConvertMbinsToMxml(mbinCompilerPath, mbinDir);

                // Step 7: Build localisation
                Console.WriteLine("\n--- Step 7: Building localisation ---");
                MxmlParser.ClearLocalisationCache();
                MxmlParser.ClearXmlCache();
                LocalisationBuilder.BuildLocalisationJson(resourcesDir);

                // Step 8: Parse all data
                Console.WriteLine("\n--- Step 8: Parsing game data ---");
                var baseData = RunParsers(mbinDir);

                // Step 9: Categorize and output JSON
                Console.WriteLine("\n--- Step 9: Categorizing and saving JSON ---");
                CategorizeAndSave(baseData, jsonDir, mbinDir);

                // Step 10: Generate TechPack partial class from technology data
                Console.WriteLine("\n--- Step 10: Generating TechPackDatabase.Generated.cs ---");
                GenerateTechPackPartialClass(baseData, resourcesDir);

                // Step 11: Convert images (textures already extracted in Step 4, in banks/extracted/)
                Console.WriteLine("\n--- Step 11: Converting images ---");
                RunImageExtraction(banksExtractedDir, jsonDir, imagesDir, toolsDir);

                // Step 12: Download mapping.json
                Console.WriteLine("\n--- Step 12: Downloading mapping.json ---");
                await ToolManager.DownloadMappingJsonAsync(mapDir);
            }
            finally
            {
                // Always clean up banks directory (paks + extracted/) and mbin folder
                Console.WriteLine("\n--- Cleanup ---");
                PakExtractor.CleanupBanksDir(banksDir);
                CleanupMbinFolder(mbinDir);
            }

            Console.WriteLine("\n" + "=".PadRight(70, '='));
            Console.WriteLine("EXTRACTION COMPLETE!");
            Console.WriteLine("=".PadRight(70, '='));
            Console.WriteLine($"Output: {resourcesDir}");
            return 0;
        }
        catch (Exception ex)
        {
            Console.Error.WriteLine($"\n[FATAL] {ex.Message}");
            Console.Error.WriteLine(ex.StackTrace);
            return 1;
        }
        finally
        {
            // Restore original console writers and close the log file
            Console.SetOut(originalOut);
            Console.SetError(originalErr);
            logStream.Dispose();
        }
    }

    /// <summary>
    /// Estimate peak storage consumption, display to user, and prompt for confirmation.
    /// Returns true if user confirms, false to cancel.
    /// </summary>
    internal static bool PromptStorageConfirmation(string pcbanksPath, string baseDir)
    {
        long pakSize = PakExtractor.GetPakFilesSize(pcbanksPath, PakExtractor.IsPakRelevant);
        long largestPak = PakExtractor.GetLargestPakFileSize(pcbanksPath, PakExtractor.IsPakRelevant);
        long estimatedMax = PakExtractor.EstimateMaxStorageBytes(pakSize, largestPak);

        long totalPakSize = PakExtractor.GetPakFilesSize(pcbanksPath);
        double totalPakGB = totalPakSize / (1024.0 * 1024.0 * 1024.0);
        double pakSizeGB = pakSize / (1024.0 * 1024.0 * 1024.0);
        double estimatedGB = estimatedMax / (1024.0 * 1024.0 * 1024.0);

        Console.WriteLine($"  Total game .pak files: {totalPakGB:F2} GB");
        Console.WriteLine($"  Relevant .pak files: {pakSizeGB:F2} GB");
        Console.WriteLine($"  Estimated peak storage: {estimatedGB:F2} GB");
        Console.WriteLine($"  Working directory: {baseDir}");

        // Check available space on the drive
        try
        {
            string? root = Path.GetPathRoot(Path.GetFullPath(baseDir));
            if (root != null)
            {
                var driveInfo = new DriveInfo(root);
                double freeGB = driveInfo.AvailableFreeSpace / (1024.0 * 1024.0 * 1024.0);
                Console.WriteLine($"  Available disk space: {freeGB:F2} GB");

                if (driveInfo.AvailableFreeSpace < estimatedMax)
                {
                    Console.WriteLine($"\n[WARN] Insufficient disk space! Need ~{estimatedGB:F2} GB but only {freeGB:F2} GB available.");
                }
            }
        }
        catch { /* drive info not available on all platforms */ }

        Console.Write($"\nProceed with extraction? (Y/n): ");
        string? response = Console.ReadLine()?.Trim().ToLowerInvariant();
        return response is "" or "y" or "yes";
    }

    private static void CleanResources(string resourcesDir)
    {
        if (Directory.Exists(resourcesDir))
        {
            foreach (string dir in Directory.GetDirectories(resourcesDir))
            {
                Directory.Delete(dir, recursive: true);
                Console.WriteLine($"  Removed {Path.GetFileName(dir)}/");
            }
            foreach (string file in Directory.GetFiles(resourcesDir))
            {
                File.Delete(file);
                Console.WriteLine($"  Removed {Path.GetFileName(file)}");
            }
        }
        string jsonDir = Path.Combine(resourcesDir, ExtractorConfig.JsonSubfolder);
        Directory.CreateDirectory(jsonDir);
        Console.WriteLine("[OK] Resources cleaned.");
    }

    /// <summary>
    /// Clean up the mbin/ folder and its contents after extraction is complete.
    /// </summary>
    private static void CleanupMbinFolder(string mbinDir)
    {
        if (Directory.Exists(mbinDir))
        {
            try
            {
                Directory.Delete(mbinDir, recursive: true);
                Console.WriteLine("[OK] Cleaned up mbin/ folder");
            }
            catch (Exception ex)
            {
                Console.WriteLine($"[WARN] Could not clean up mbin/ folder: {ex.Message}");
            }
        }
    }

    private static Dictionary<string, List<Dictionary<string, object?>>> RunParsers(string mbinDir)
    {
        var baseData = new ConcurrentDictionary<string, List<Dictionary<string, object?>>>();

        var parserConfigs = new (string Name, string MxmlFile, Func<string, List<Dictionary<string, object?>>> Parser)[]
        {
            ("Refinery", "nms_reality_gcrecipetable.MXML", p => Parsers.ParseRefinery(p, onlyRefinery: true)),
            ("Nutrient Processor", "nms_reality_gcrecipetable.MXML", Parsers.ParseNutrientProcessor),
            ("Products", "nms_reality_gcproducttable.MXML", p => Parsers.ParseProducts(p)),
            ("Raw Materials", "nms_reality_gcsubstancetable.MXML", Parsers.ParseRawMaterials),
            ("Technology", "nms_reality_gctechnologytable.MXML", Parsers.ParseTechnology),
            ("Cooking", "consumableitemtable.MXML", Parsers.ParseCooking),
            ("Fish", "fishdatatable.MXML", Parsers.ParseFish),
            ("Trade", "nms_reality_gcproducttable.MXML", Parsers.ParseTrade),
            ("ShipComponents", "nms_modularcustomisationproducts.MXML", Parsers.ParseShipComponents),
            ("BaseParts", "nms_basepartproducts.MXML", Parsers.ParseBaseParts),
            ("ProceduralTech", "nms_reality_gcproceduraltechnologytable.MXML", Parsers.ParseProceduralTech),
            ("Egg Modifiers", "peteggtraitmodifieroverridetable.MXML", Parsers.ParsePetEggTraitModifiers),
            ("Recipes", "nms_reality_gcrecipetable.MXML", Parsers.ParseAllRecipes),
            ("Words", "nms_dialog_gcalienspeechtable.MXML", Parsers.ParseWords),
            ("Rewards", "UNLOCKABLESEASONREWARDS.MXML", Parsers.ParseRewards),
            ("Titles", "PLAYERTITLEDATA.MXML", Parsers.ParseTitles),
            ("FrigateTraits", "FRIGATETRAITTABLE.MXML", Parsers.ParseFrigateTraits),
            ("SettlementPerks", "SETTLEMENTPERKSTABLE.MXML", Parsers.ParseSettlementPerks),
            ("WikiGuide", "WIKI.MXML", Parsers.ParseWikiGuide),
            ("CompanionAccessories", "CHARACTERCUSTOMISATIONDESCRIPTORGROUPSDATA.MXML", Parsers.ParsePetAccessories),
            ("PetBattleMoves", "PETBATTLERMOVESTABLE.MXML", Parsers.ParsePetBattleMoves),
            ("PetBattleMovesets", "PETBATTLERMOVESETSTABLE.MXML", Parsers.ParsePetBattleMovesets),
            ("GameTableGlobals", "GCGAMETABLEGLOBALS.MXML", Parsers.ParseGameTableGlobals),
            ("CreatureSpecies", "creaturedatatable.MXML", Parsers.ParseCreatureSpecies),
            ("CreatureDescriptors", "creaturefilenametable.MXML",
                p => Parsers.ParseCreatureDescriptors(p, mbinDir)),
        };

        // Pre-warm XML cache by loading shared MXML files on the main thread.
        // This prevents redundant file I/O when multiple parsers reference the same file.
        var distinctMxmlFiles = parserConfigs.Select(c => c.MxmlFile).Distinct().ToArray();
        foreach (string mxmlFile in distinctMxmlFiles)
        {
            string mxmlPath = Path.Combine(mbinDir, mxmlFile);
            if (File.Exists(mxmlPath))
            {
                try { MxmlParser.LoadXml(mxmlPath); }
                catch (Exception ex) { Console.WriteLine($"  [WARN] Cache warm failed for {mxmlFile}: {ex.Message}"); }
            }
        }

        Console.WriteLine($"[INFO] Running {parserConfigs.Length} parsers in parallel...");

        Parallel.ForEach(parserConfigs, config =>
        {
            var (name, mxmlFile, parser) = config;
            string mxmlPath = Path.Combine(mbinDir, mxmlFile);

            if (!File.Exists(mxmlPath))
            {
                Console.WriteLine($"  [SKIP] {name}: {mxmlFile} not found");
                return;
            }
            try
            {
                var data = parser(mxmlPath);
                baseData[name] = data;
                Console.WriteLine($"  [OK] {name}: {data.Count} items extracted");
            }
            catch (Exception ex)
            {
                Console.WriteLine($"  [ERROR] {name}: {ex.Message}");
            }
        });

        Console.WriteLine($"[OK] Parsed {baseData.Count} data sets");
        return new Dictionary<string, List<Dictionary<string, object?>>>(baseData);
    }

    private static void CategorizeAndSave(
        Dictionary<string, List<Dictionary<string, object?>>> baseData,
        string jsonDir, string mbinDir)
    {
        // Pre-seeded files (bypass categorization)
        var finalFiles = new Dictionary<string, List<Dictionary<string, object?>>>
        {
            ["Fish.json"] = baseData.GetValueOrDefault("Fish") ?? new(),
            ["Trade.json"] = baseData.GetValueOrDefault("Trade") ?? new(),
            ["Raw Materials.json"] = baseData.GetValueOrDefault("Raw Materials") ?? new(),
        };

        if (baseData.ContainsKey("Egg Modifiers"))
            finalFiles["Egg Modifiers.json.tbc"] = baseData["Egg Modifiers"];

        // Standalone data files (not categorized, written directly)
        if (baseData.ContainsKey("Recipes"))
            finalFiles["Recipes.json"] = baseData["Recipes"];
        if (baseData.ContainsKey("Rewards"))
            finalFiles["Rewards.json"] = baseData["Rewards"];
        if (baseData.ContainsKey("Words"))
            finalFiles["Words.json"] = baseData["Words"];
        if (baseData.ContainsKey("Titles"))
            finalFiles["Titles.json"] = baseData["Titles"];
        if (baseData.ContainsKey("FrigateTraits"))
            finalFiles["Frigate Traits.json"] = baseData["FrigateTraits"];
        if (baseData.ContainsKey("SettlementPerks"))
            finalFiles["Settlement Perks.json"] = baseData["SettlementPerks"];
        if (baseData.ContainsKey("WikiGuide"))
            finalFiles["Wiki Guide.json"] = baseData["WikiGuide"];
        if (baseData.ContainsKey("CompanionAccessories"))
            finalFiles["Companion Accessories.json"] = baseData["CompanionAccessories"];
        if (baseData.ContainsKey("PetBattleMoves"))
            finalFiles["Pet Battle Moves.json"] = baseData["PetBattleMoves"];
        if (baseData.ContainsKey("PetBattleMovesets"))
            finalFiles["Pet Battle Movesets.json"] = baseData["PetBattleMovesets"];
        if (baseData.ContainsKey("GameTableGlobals"))
            finalFiles["Game Table Globals.json"] = baseData["GameTableGlobals"];
        if (baseData.ContainsKey("CreatureSpecies"))
            finalFiles["Creature Species.json"] = baseData["CreatureSpecies"];
        if (baseData.ContainsKey("CreatureDescriptors"))
            finalFiles["Creature Descriptors.json"] = baseData["CreatureDescriptors"];

        // Categorized files
        var categorized = new Dictionary<string, List<Dictionary<string, object?>>>
        {
            ["Buildings.json"] = new(), ["Constructed Technology.json"] = new(), ["Food.json"] = new(),
            ["Corvette.json"] = new(), ["Curiosities.json"] = new(), ["Exocraft.json"] = new(),
            ["Starships.json"] = new(), ["Others.json"] = new(), ["Products.json"] = new(),
            ["Technology.json"] = new(), ["Technology Module.json"] = new(), ["Upgrades.json"] = new(),
        };

        // Items to categorize
        // Products first so its items establish ordering, then Cooking to enrich/replace Food items.
        var itemsToCategorize = new List<Dictionary<string, object?>>();
        foreach (string key in new[] { "Products", "Technology", "Cooking", "ShipComponents", "BaseParts", "ProceduralTech" })
            if (baseData.TryGetValue(key, out var data))
                itemsToCategorize.AddRange(data);

        int totalCategorized = 0, totalSkipped = 0;
        var uncategorized = new List<Dictionary<string, object?>>();

        // Track pre-seeded IDs to avoid duplicates
        var preseededIds = new Dictionary<string, HashSet<string>>();
        foreach (var (filename, data) in finalFiles)
        {
            var ids = new HashSet<string>();
            foreach (var item in data)
                if (item.TryGetValue("Id", out var id) && id is string idStr)
                    ids.Add(idStr);
            preseededIds[filename] = ids;
        }

        foreach (var item in itemsToCategorize)
        {
            string? targetFile = Categorizer.CategorizeItem(item);
            if (targetFile == null)
            {
                totalSkipped++;
                uncategorized.Add(item);
                continue;
            }

            if (categorized.TryGetValue(targetFile, out var catList))
            {
                catList.Add(item);
                totalCategorized++;
            }
            else if (finalFiles.TryGetValue(targetFile, out var seedList))
            {
                string? itemId = item.GetValueOrDefault("Id")?.ToString();
                if (itemId != null && preseededIds.TryGetValue(targetFile, out var seedIds) && seedIds.Contains(itemId))
                    continue; // Already in pre-seeded data
                seedList.Add(item);
                totalCategorized++;
            }
            else
            {
                totalSkipped++;
                uncategorized.Add(item);
            }
        }

        Console.WriteLine($"Categorized {totalCategorized} items, skipped {totalSkipped}");

        // Merge categorized into final
        foreach (var (filename, data) in categorized)
            finalFiles[filename] = data;

        // Reclassify items between Upgrades.json and Technology Module.json based on DeploysInto.
        // Items in Upgrades.json WITH DeploysInto are cargo-holdable tech module fragments
        // that unpack into technology slots — they belong in Technology Module.json.
        // Items in Technology Module.json WITHOUT DeploysInto are actual tech upgrades
        // meant to be installed directly — they belong in Upgrades.json.
        ReclassifyByDeploysInto(finalFiles);

        // Re-route Raw Materials items that belong elsewhere (e.g. Reward Item -> Others.json).
        // Raw Materials is pre-seeded from the substance table and normally bypasses categorization.
        var rawMaterials = finalFiles.GetValueOrDefault("Raw Materials.json");
        if (rawMaterials != null)
        {
            var toKeep = new List<Dictionary<string, object?>>();
            int rerouted = 0;
            foreach (var item in rawMaterials)
            {
                string? targetFile = Categorizer.CategorizeItem(item);
                if (targetFile != null && targetFile != "Raw Materials.json"
                    && finalFiles.TryGetValue(targetFile, out var targetList))
                {
                    targetList.Add(item);
                    rerouted++;
                }
                else
                {
                    toKeep.Add(item);
                }
            }
            if (rerouted > 0)
            {
                finalFiles["Raw Materials.json"] = toKeep;
                Console.WriteLine($"  [NORMALIZE] Moved {rerouted} Raw Materials items to other files");
            }
        }

        // Move exocraft upgrades from Exocraft.json to Upgrades.json
        MoveExocraftUpgrades(finalFiles);

        // For qualified upgrade groups (class tier or quality prefix):
        // strip item name from group -> short group, set Name = "DisplayName ShortGroup"
        NormalizeUpgradeDisplayNames(finalFiles);

        // Correct rarity for upgrade items based on group/description keywords
        CorrectUpgradeRarities(finalFiles);

        // Enrich placeholder upgrade descriptions
        EnrichUpgradeDescriptions(finalFiles);

        // Enrich fish items with CdnUrl from Products data, and cooking data for specific items
        EnrichFishWithCookingData(finalFiles, baseData);

        // Apply slugs (before enrichment steps that add trailing fields)
        ApplySlugs(finalFiles);

        // Enrich items with Category (maps to GameItem.TechnologyCategory) from Technology / ProceduralTech base data.
        // Product-parsed items that got categorized (e.g. to Upgrades.json, Technology.json)
        // lack the Category field that identifies which entity type the tech belongs to.
        EnrichTechnologyCategory(finalFiles, baseData);

        // Enrich upgrade stats from Technology / ProceduralTech base data
        // (adds StatLevels, NumStatsMin, NumStatsMax, WeightingCurve after Slug)
        EnrichUpgradeStats(finalFiles, baseData);

        // Enrich corvette items with product metadata (adds fields after Slug)
        EnrichCorvetteMetadata(finalFiles, mbinDir);

        // Link buildable tech labels to corvette items (adds fields after Slug)
        EnrichCorvetteBuildableTechLabels(finalFiles);

        // Enrich exocraft items with product metadata (adds fields after Slug)
        EnrichExocraftMetadata(finalFiles, mbinDir);

        // Enrich buildings with metadata from basebuildingobjectstable (adds fields after Slug)
        EnrichBuildingsMetadata(finalFiles, mbinDir);

        // Deduplicate
        DeduplicateAll(finalFiles);

        // Strip cooking-specific fields from non-Food items.
        // Items from the Cooking parser that get categorized to non-Food files carry
        // extra fields (NameLower, CookingValue, RewardID, EffectCategory, RewardEffectStats)
        // that should only appear in Food.json.
        StripCookingFieldsFromNonFood(finalFiles);

        // Remove T_BOBBLE_* tech variants when BOBBLE_* product versions exist
        DedupeStarshipAdornmentDisplayDuplicates(finalFiles);

        // Save all files
        Console.WriteLine("\nSaving final files:");
        int totalItems = 0;
        double totalSize = 0;
        foreach (var (filename, data) in finalFiles.OrderBy(kv => kv.Key))
        {
            double size = JsonWriter.SaveJson(data, jsonDir, filename);
            totalItems += data.Count;
            totalSize += size;
            Console.WriteLine($"  {filename,-30} {data.Count,4} items  {size,8:F1} KB");
        }

        // Save uncategorized for review
        if (uncategorized.Count > 0)
            JsonWriter.SaveJson(uncategorized, jsonDir, "none.json", useSpaceIndent: true);

        Console.WriteLine($"\n  TOTAL: {totalItems} items  {totalSize:F1} KB");
    }

    /// <summary>
    /// Enrich product-parsed items with TechnologyCategory (Category field) and tech flags
    /// (Upgrade, Core, Procedural) from Technology and ProceduralTech base data.
    /// Items from the Products parser get categorized to files like Upgrades.json,
    /// Technology.json, etc. but lack these tech-specific fields. This method resolves
    /// them by matching on Id or DeploysInto.
    /// </summary>
    private static void EnrichTechnologyCategory(
        Dictionary<string, List<Dictionary<string, object?>>> finalFiles,
        Dictionary<string, List<Dictionary<string, object?>>> baseData)
    {
        // Build lookup: Id -> source tech item (ALL Technology and ProceduralTech items)
        var techById = new Dictionary<string, Dictionary<string, object?>>(StringComparer.OrdinalIgnoreCase);
        foreach (string key in new[] { "Technology", "ProceduralTech" })
        {
            if (!baseData.TryGetValue(key, out var items)) continue;
            foreach (var item in items)
            {
                if (item.TryGetValue("Id", out var idObj) && idObj is string id
                    && !string.IsNullOrEmpty(id))
                {
                    techById.TryAdd(id, item);
                }
            }
        }

        if (techById.Count == 0) return;

        // Enrichment target files: any file that may contain product-parsed items
        // that should have tech metadata
        string[] targetFiles = { "Upgrades.json", "Technology.json", "Technology Module.json",
                                  "Exocraft.json", "Starships.json", "Constructed Technology.json" };
        int totalEnriched = 0;

        foreach (string fileName in targetFiles)
        {
            if (!finalFiles.TryGetValue(fileName, out var items)) continue;

            foreach (var item in items)
            {
                // Skip items that already have Category from the Technology parser
                if (item.TryGetValue("Category", out var existingCat)
                    && existingCat is string catStr && !string.IsNullOrEmpty(catStr))
                    continue;

                string? itemId = item.GetValueOrDefault("Id")?.ToString();
                Dictionary<string, object?>? source = null;

                // Try matching by Id
                if (itemId != null)
                    techById.TryGetValue(itemId, out source);

                // Try matching by DeploysInto target
                if (source == null)
                {
                    string? deployTarget = item.GetValueOrDefault("DeploysInto")?.ToString();
                    if (!string.IsNullOrEmpty(deployTarget))
                        techById.TryGetValue(deployTarget, out source);
                }

                if (source == null) continue;

                // Copy tech-specific fields (including charge fields for correct Amount/MaxAmount)
                bool enriched = false;
                foreach (string field in new[] { "Category", "Upgrade", "Core", "Procedural",
                                                  "Chargeable", "ChargeAmount", "BuildFullyCharged" })
                {
                    if (source.TryGetValue(field, out var srcVal) && srcVal != null)
                    {
                        if (!item.TryGetValue(field, out var tgtVal) || tgtVal == null
                            || (tgtVal is string ts && string.IsNullOrEmpty(ts)))
                        {
                            item[field] = srcVal;
                            enriched = true;
                        }
                    }
                }
                if (enriched) totalEnriched++;
            }
        }

        if (totalEnriched > 0)
            Console.WriteLine($"  [ENRICH] Enriched {totalEnriched} items with TechnologyCategory from Technology/ProceduralTech");
    }

    /// <summary>
    /// For Upgrades.json items missing stat data, copy stats from Technology or ProceduralTech source items
    /// matched by Id or DeploysInto target.
    /// </summary>
    private static void EnrichUpgradeStats(
        Dictionary<string, List<Dictionary<string, object?>>> finalFiles,
        Dictionary<string, List<Dictionary<string, object?>>> baseData)
    {
        if (!finalFiles.TryGetValue("Upgrades.json", out var upgrades))
            return;

        // Build lookup: Id -> source item (Technology and ProceduralTech items that have stats)
        var sourceById = new Dictionary<string, Dictionary<string, object?>>();
        foreach (string key in new[] { "Technology", "ProceduralTech" })
        {
            if (!baseData.TryGetValue(key, out var items)) continue;
            foreach (var item in items)
            {
                if (item.TryGetValue("Id", out var idObj) && idObj is string id
                    && !string.IsNullOrEmpty(id) && HasStats(item))
                {
                    sourceById[id] = item;
                }
            }
        }

        int enriched = 0;
        foreach (var item in upgrades)
        {
            if (HasStats(item))
                continue;

            string? itemId = item.GetValueOrDefault("Id")?.ToString();

            // Try matching by Id
            if (itemId != null && sourceById.TryGetValue(itemId, out var source)
                && CopyStatsFields(item, source))
            {
                enriched++;
                continue;
            }

            // Try matching by DeploysInto target
            string? deployTarget = item.GetValueOrDefault("DeploysInto")?.ToString();
            if (!string.IsNullOrEmpty(deployTarget)
                && sourceById.TryGetValue(deployTarget, out source)
                && CopyStatsFields(item, source))
            {
                enriched++;
            }
        }

        if (enriched > 0)
            Console.WriteLine($"  [ENRICH] Enriched {enriched} Upgrades items with stats from Technology/ProceduralTech");
    }

    /// <summary>
    /// Returns true if the item already has stat data (StatBonuses, StatLevels, or NumStatsMin/Max).
    /// </summary>
    private static bool HasStats(Dictionary<string, object?> item)
    {
        if (item.TryGetValue("StatBonuses", out var sb) && sb is System.Collections.IList sbList && sbList.Count > 0)
            return true;
        if (item.TryGetValue("StatLevels", out var sl) && sl is System.Collections.IList slList && slList.Count > 0)
            return true;
        if (item.TryGetValue("NumStatsMin", out var nmin) && nmin != null
            && item.TryGetValue("NumStatsMax", out var nmax) && nmax != null)
            return true;
        return false;
    }

    /// <summary>
    /// Copy stat-related fields from source to target if the target field is empty/null.
    /// Returns true if at least one field was copied.
    /// </summary>
    private static bool CopyStatsFields(Dictionary<string, object?> target, Dictionary<string, object?> source)
    {
        bool copied = false;
        foreach (string field in new[] { "StatBonuses", "StatLevels", "NumStatsMin", "NumStatsMax", "WeightingCurve" })
        {
            if (!source.TryGetValue(field, out var srcVal) || IsEmptyValue(srcVal))
                continue;
            if (!target.TryGetValue(field, out var tgtVal) || IsEmptyValue(tgtVal))
            {
                target[field] = srcVal;
                copied = true;
            }
        }
        return copied;
    }

    private static bool IsEmptyValue(object? value)
    {
        if (value == null) return true;
        if (value is string s) return s.Length == 0;
        if (value is System.Collections.IList list) return list.Count == 0;
        return false;
    }

    private static string? NullIfEmpty(string s) => string.IsNullOrEmpty(s) ? null : s;

    /// <summary>
    /// Enrich Buildings.json items with building-specific metadata from basebuildingobjectstable.MXML.
    /// </summary>
    private static void EnrichBuildingsMetadata(
        Dictionary<string, List<Dictionary<string, object?>>> finalFiles,
        string mbinDir)
    {
        if (!finalFiles.TryGetValue("Buildings.json", out var buildings) || buildings.Count == 0)
            return;

        string sourcePath = Path.Combine(mbinDir, "basebuildingobjectstable.MXML");
        if (!File.Exists(sourcePath))
        {
            Console.WriteLine("  [SKIP] basebuildingobjectstable.MXML not found");
            return;
        }

        var root = MxmlParser.LoadXml(sourcePath);
        var objectsProp = root.Descendants("Property")
            .FirstOrDefault(e => e.Attribute("name")?.Value == "Objects");
        if (objectsProp == null) return;

        var metadataById = new Dictionary<string, Dictionary<string, object?>>();
        foreach (var elem in objectsProp.Elements("Property")
            .Where(e => e.Attribute("name")?.Value == "Objects"))
        {
            string itemId = MxmlParser.GetPropertyValue(elem, "ID");
            if (string.IsNullOrEmpty(itemId)) continue;

            string iconOverride = MxmlParser.GetPropertyValue(elem, "IconOverrideProductID");
            bool buildableOnPlanet = MxmlParser.ParseValue(
                MxmlParser.GetPropertyValue(elem, "BuildableOnPlanetBase", "true")) is true;
            bool buildableOnSpace = MxmlParser.ParseValue(
                MxmlParser.GetPropertyValue(elem, "BuildableOnSpaceBase", "false")) is true;
            bool buildableOnFreighter = MxmlParser.ParseValue(
                MxmlParser.GetPropertyValue(elem, "BuildableOnFreighter", "false")) is true;

            // CanPickUp / IsTemporary flags (BaseBuildingData)
            bool canPickUp = MxmlParser.ParseValue(
                MxmlParser.GetPropertyValue(elem, "CanPickUp", "false")) is true;
            bool isTemporary = MxmlParser.ParseValue(
                MxmlParser.GetPropertyValue(elem, "IsTemporary", "false")) is true;

            // Groups list
            var groupsList = new List<Dictionary<string, object?>>();
            var groupsProp = elem.Descendants("Property")
                .FirstOrDefault(e => e.Attribute("name")?.Value == "Groups");
            if (groupsProp != null)
            {
                foreach (var grpElem in groupsProp.Elements("Property")
                    .Where(e => e.Attribute("name")?.Value == "Groups"))
                {
                    string g = MxmlParser.GetPropertyValue(grpElem, "Group");
                    string sub = MxmlParser.GetPropertyValue(grpElem, "SubGroupName");
                    if (!string.IsNullOrEmpty(g))
                        groupsList.Add(new Dictionary<string, object?>
                        {
                            ["Group"] = g,
                            ["SubGroupName"] = NullIfEmpty(sub)
                        });
                }
            }

            // LinkGridData
            Dictionary<string, object?>? linkGridData = null;
            var linkElem = elem.Descendants("Property")
                .FirstOrDefault(e => e.Attribute("name")?.Value == "LinkGridData");
            if (linkElem != null)
            {
                var networkElem = linkElem.Descendants("Property")
                    .FirstOrDefault(e => e.Attribute("name")?.Value == "Network");
                string linkType = networkElem != null
                    ? MxmlParser.GetNestedEnum(networkElem, "LinkNetworkType", "LinkNetworkType")
                    : "";
                object rate = MxmlParser.ParseValue(
                    MxmlParser.GetPropertyValue(linkElem, "Rate", "0"));
                object storage = MxmlParser.ParseValue(
                    MxmlParser.GetPropertyValue(linkElem, "Storage", "0"));
                bool hasValues = !string.IsNullOrEmpty(linkType)
                    || (rate is int ri && ri != 0) || (rate is double rd && rd != 0)
                    || (storage is int si && si != 0) || (storage is double sd && sd != 0);
                if (hasValues)
                    linkGridData = new Dictionary<string, object?>
                    {
                        ["Network"] = NullIfEmpty(linkType),
                        ["Rate"] = rate,
                        ["Storage"] = storage
                    };
            }

            metadataById[itemId] = new Dictionary<string, object?>
            {
                ["IconOverrideProductID"] = NullIfEmpty(iconOverride),
                ["BuildableOnPlanetBase"] = buildableOnPlanet,
                ["BuildableOnSpaceBase"] = buildableOnSpace,
                ["BuildableOnFreighter"] = buildableOnFreighter,
                ["CanPickUp"] = canPickUp,
                ["IsTemporary"] = isTemporary,
                ["Groups"] = groupsList.Count > 0 ? groupsList : null,
                ["LinkGridData"] = linkGridData,
            };
        }

        int enriched = 0;
        foreach (var item in buildings)
        {
            string? itemId = item.GetValueOrDefault("Id")?.ToString();
            if (itemId != null && metadataById.TryGetValue(itemId, out var extra))
            {
                foreach (var kv in extra)
                    item[kv.Key] = kv.Value;
                enriched++;
            }
        }

        if (enriched > 0)
            Console.WriteLine($"  [ENRICH] Enriched {enriched} Buildings items with metadata from basebuildingobjectstable");
    }

    /// <summary>
    /// Extract product metadata from a product-style MXML table into a lookup dictionary keyed by ID.
    /// Shared helper for Corvette and Exocraft enrichment.
    /// </summary>
    private static void CollectProductMetadata(
        string mxmlPath,
        Dictionary<string, Dictionary<string, object?>> metadataById,
        Func<System.Xml.Linq.XElement, Dictionary<string, object?>> buildMetadata)
    {
        if (!File.Exists(mxmlPath)) return;

        var root = MxmlParser.LoadXml(mxmlPath);
        var tableProp = root.Descendants("Property")
            .FirstOrDefault(e => e.Attribute("name")?.Value == "Table");
        if (tableProp == null) return;

        foreach (var elem in tableProp.Elements("Property")
            .Where(e => e.Attribute("name")?.Value == "Table"))
        {
            string itemId = MxmlParser.GetPropertyValue(elem, "ID");
            if (string.IsNullOrEmpty(itemId)) continue;

            metadataById[itemId] = buildMetadata(elem);
        }
    }

    /// <summary>
    /// Extract HeroIconPath from a product element.
    /// </summary>
    private static string? ExtractHeroIconPath(System.Xml.Linq.XElement elem)
    {
        var heroIconProp = elem.Descendants("Property")
            .FirstOrDefault(e => e.Attribute("name")?.Value == "HeroIcon");
        string heroIconFilename = heroIconProp != null
            ? MxmlParser.GetPropertyValue(heroIconProp, "Filename") : "";
        string heroIconPath = !string.IsNullOrEmpty(heroIconFilename)
            ? MxmlParser.NormalizeGameIconPath(heroIconFilename) : "";
        return !string.IsNullOrEmpty(heroIconPath) ? heroIconPath : null;
    }

    /// <summary>
    /// Enrich Corvette.json items with product metadata from basepartproducts and modularcustomisationproducts.
    /// </summary>
    private static void EnrichCorvetteMetadata(
        Dictionary<string, List<Dictionary<string, object?>>> finalFiles, string mbinDir)
    {
        if (!finalFiles.TryGetValue("Corvette.json", out var corvettes) || corvettes.Count == 0)
            return;

        var metadataById = new Dictionary<string, Dictionary<string, object?>>();

        Dictionary<string, object?> BuildCorvetteMetadata(System.Xml.Linq.XElement elem) => new()
        {
            ["HeroIconPath"] = ExtractHeroIconPath(elem),
            ["BuildableShipTechID"] = NullIfEmpty(MxmlParser.GetPropertyValue(elem, "BuildableShipTechID")),
            ["GroupID"] = NullIfEmpty(MxmlParser.GetPropertyValue(elem, "GroupID")),
            ["SubstanceCategory"] = NullIfEmpty(MxmlParser.GetNestedEnum(elem, "Category", "SubstanceCategory")),
            ["ProductCategory"] = NullIfEmpty(MxmlParser.GetNestedEnum(elem, "Type", "ProductCategory")),
            ["Level"] = MxmlParser.ParseValue(MxmlParser.GetPropertyValue(elem, "Level", "0")),
            ["ChargeValue"] = MxmlParser.ParseValue(MxmlParser.GetPropertyValue(elem, "ChargeValue", "0")),
            ["DefaultCraftAmount"] = MxmlParser.ParseValue(MxmlParser.GetPropertyValue(elem, "DefaultCraftAmount", "1")),
            ["CraftAmountStepSize"] = MxmlParser.ParseValue(MxmlParser.GetPropertyValue(elem, "CraftAmountStepSize", "1")),
            ["CraftAmountMultiplier"] = MxmlParser.ParseValue(MxmlParser.GetPropertyValue(elem, "CraftAmountMultiplier", "1")),
            ["SpecificChargeOnly"] = MxmlParser.ParseValue(MxmlParser.GetPropertyValue(elem, "SpecificChargeOnly", "false")),
            ["NormalisedValueOnWorld"] = ProductLookup.AsDouble(MxmlParser.ParseValue(MxmlParser.GetPropertyValue(elem, "NormalisedValueOnWorld", "0"))),
            ["NormalisedValueOffWorld"] = ProductLookup.AsDouble(MxmlParser.ParseValue(MxmlParser.GetPropertyValue(elem, "NormalisedValueOffWorld", "0"))),
            ["CorvettePartCategory"] = NullIfEmpty(MxmlParser.GetNestedEnum(elem, "CorvettePartCategory", "CorvettePartCategory")),
            ["CorvetteRewardFrequency"] = ProductLookup.AsDouble(MxmlParser.ParseValue(MxmlParser.GetPropertyValue(elem, "CorvetteRewardFrequency", "0"))),
            ["IsCraftable"] = MxmlParser.ParseValue(MxmlParser.GetPropertyValue(elem, "IsCraftable", "false")),
            ["PinObjective"] = NullIfEmpty(MxmlParser.GetPropertyValue(elem, "PinObjective")),
            ["PinObjectiveTip"] = NullIfEmpty(MxmlParser.GetPropertyValue(elem, "PinObjectiveTip")),
            ["PinObjectiveMessage"] = NullIfEmpty(MxmlParser.GetPropertyValue(elem, "PinObjectiveMessage")),
            ["PinObjectiveScannableType"] = NullIfEmpty(MxmlParser.GetNestedEnum(elem, "PinObjectiveScannableType", "ScanIconType")),
            ["PinObjectiveEasyToRefine"] = MxmlParser.ParseValue(MxmlParser.GetPropertyValue(elem, "PinObjectiveEasyToRefine", "false")),
            ["NeverPinnable"] = MxmlParser.ParseValue(MxmlParser.GetPropertyValue(elem, "NeverPinnable", "false")),
            ["CanSendToOtherPlayers"] = MxmlParser.ParseValue(MxmlParser.GetPropertyValue(elem, "CanSendToOtherPlayers", "true")),
        };

        // Load from basepartproducts first, then modularcustomisationproducts (later overrides)
        CollectProductMetadata(
            Path.Combine(mbinDir, "nms_basepartproducts.MXML"), metadataById, BuildCorvetteMetadata);
        CollectProductMetadata(
            Path.Combine(mbinDir, "nms_modularcustomisationproducts.MXML"), metadataById, BuildCorvetteMetadata);

        int enriched = 0;
        foreach (var item in corvettes)
        {
            string? itemId = item.GetValueOrDefault("Id")?.ToString();
            if (itemId != null && metadataById.TryGetValue(itemId, out var extra))
            {
                foreach (var kv in extra)
                    item[kv.Key] = kv.Value;
                enriched++;
            }
        }

        if (enriched > 0)
            Console.WriteLine($"  [ENRICH] Enriched {enriched} Corvette items with product metadata");
    }

    /// <summary>
    /// Enrich Exocraft.json items with product metadata from gcproducttable and basepartproducts.
    /// </summary>
    private static void EnrichExocraftMetadata(
        Dictionary<string, List<Dictionary<string, object?>>> finalFiles, string mbinDir)
    {
        if (!finalFiles.TryGetValue("Exocraft.json", out var exocrafts) || exocrafts.Count == 0)
            return;

        var metadataById = new Dictionary<string, Dictionary<string, object?>>();

        Dictionary<string, object?> BuildExocraftMetadata(System.Xml.Linq.XElement elem)
        {
            // PriceModifiers from Cost element
            var costProp = elem.Descendants("Property")
                .FirstOrDefault(e => e.Attribute("name")?.Value == "Cost");
            Dictionary<string, object>? priceModifiers = null;
            if (costProp != null)
            {
                priceModifiers = new Dictionary<string, object>
                {
                    ["SpaceStationMarkup"] = ProductLookup.AsDouble(MxmlParser.ParseValue(MxmlParser.GetPropertyValue(costProp, "SpaceStationMarkup", "0"))),
                    ["LowPriceMod"] = ProductLookup.AsDouble(MxmlParser.ParseValue(MxmlParser.GetPropertyValue(costProp, "LowPriceMod", "0"))),
                    ["HighPriceMod"] = ProductLookup.AsDouble(MxmlParser.ParseValue(MxmlParser.GetPropertyValue(costProp, "HighPriceMod", "0"))),
                    ["BuyBaseMarkup"] = ProductLookup.AsDouble(MxmlParser.ParseValue(MxmlParser.GetPropertyValue(costProp, "BuyBaseMarkup", "0"))),
                    ["BuyMarkupMod"] = ProductLookup.AsDouble(MxmlParser.ParseValue(MxmlParser.GetPropertyValue(costProp, "BuyMarkupMod", "0"))),
                };
            }

            return new Dictionary<string, object?>
            {
                ["HeroIconPath"] = ExtractHeroIconPath(elem),
                ["BuildableShipTechID"] = NullIfEmpty(MxmlParser.GetPropertyValue(elem, "BuildableShipTechID")),
                ["GroupID"] = NullIfEmpty(MxmlParser.GetPropertyValue(elem, "GroupID")),
                ["SubstanceCategory"] = NullIfEmpty(MxmlParser.GetNestedEnum(elem, "Category", "SubstanceCategory")),
                ["ProductCategory"] = NullIfEmpty(MxmlParser.GetNestedEnum(elem, "Type", "ProductCategory")),
                ["Level"] = MxmlParser.ParseValue(MxmlParser.GetPropertyValue(elem, "Level", "0")),
                ["ChargeValue"] = MxmlParser.ParseValue(MxmlParser.GetPropertyValue(elem, "ChargeValue", "0")),
                ["DefaultCraftAmount"] = MxmlParser.ParseValue(MxmlParser.GetPropertyValue(elem, "DefaultCraftAmount", "1")),
                ["CraftAmountStepSize"] = MxmlParser.ParseValue(MxmlParser.GetPropertyValue(elem, "CraftAmountStepSize", "1")),
                ["CraftAmountMultiplier"] = MxmlParser.ParseValue(MxmlParser.GetPropertyValue(elem, "CraftAmountMultiplier", "1")),
                ["SpecificChargeOnly"] = MxmlParser.ParseValue(MxmlParser.GetPropertyValue(elem, "SpecificChargeOnly", "false")),
                ["NormalisedValueOnWorld"] = ProductLookup.AsDouble(MxmlParser.ParseValue(MxmlParser.GetPropertyValue(elem, "NormalisedValueOnWorld", "0"))),
                ["NormalisedValueOffWorld"] = ProductLookup.AsDouble(MxmlParser.ParseValue(MxmlParser.GetPropertyValue(elem, "NormalisedValueOffWorld", "0"))),
                ["EconomyInfluenceMultiplier"] = ProductLookup.AsDouble(MxmlParser.ParseValue(MxmlParser.GetPropertyValue(elem, "EconomyInfluenceMultiplier", "0"))),
                ["IsCraftable"] = MxmlParser.ParseValue(MxmlParser.GetPropertyValue(elem, "IsCraftable", "false")),
                ["PinObjective"] = NullIfEmpty(MxmlParser.GetPropertyValue(elem, "PinObjective")),
                ["PinObjectiveTip"] = NullIfEmpty(MxmlParser.GetPropertyValue(elem, "PinObjectiveTip")),
                ["PinObjectiveMessage"] = NullIfEmpty(MxmlParser.GetPropertyValue(elem, "PinObjectiveMessage")),
                ["PinObjectiveScannableType"] = NullIfEmpty(MxmlParser.GetNestedEnum(elem, "PinObjectiveScannableType", "ScanIconType")),
                ["PinObjectiveEasyToRefine"] = MxmlParser.ParseValue(MxmlParser.GetPropertyValue(elem, "PinObjectiveEasyToRefine", "false")),
                ["NeverPinnable"] = MxmlParser.ParseValue(MxmlParser.GetPropertyValue(elem, "NeverPinnable", "false")),
                ["CanSendToOtherPlayers"] = MxmlParser.ParseValue(MxmlParser.GetPropertyValue(elem, "CanSendToOtherPlayers", "true")),
                ["IsTechbox"] = MxmlParser.ParseValue(MxmlParser.GetPropertyValue(elem, "IsTechbox", "false")),
                ["GiveRewardOnSpecialPurchase"] = NullIfEmpty(MxmlParser.GetPropertyValue(elem, "GiveRewardOnSpecialPurchase")),
                ["PriceModifiers"] = priceModifiers,
            };
        }

        // Load from gcproducttable first, then basepartproducts (later overrides)
        CollectProductMetadata(
            Path.Combine(mbinDir, "nms_reality_gcproducttable.MXML"), metadataById, BuildExocraftMetadata);
        CollectProductMetadata(
            Path.Combine(mbinDir, "nms_basepartproducts.MXML"), metadataById, BuildExocraftMetadata);

        int enriched = 0;
        foreach (var item in exocrafts)
        {
            string? itemId = item.GetValueOrDefault("Id")?.ToString();
            if (itemId != null && metadataById.TryGetValue(itemId, out var extra))
            {
                foreach (var kv in extra)
                    item[kv.Key] = kv.Value;
                enriched++;
            }
        }

        if (enriched > 0)
            Console.WriteLine($"  [ENRICH] Enriched {enriched} Exocraft items with product metadata");
    }

    /// <summary>
    /// Enrich Fish items: add CdnUrl from Products parser output for fish items that
    /// also exist as products. Only F_BOSS_JELLY gets RewardID/EffectCategory/RewardEffectStats
    /// from the Cooking data.
    /// </summary>
    private static void EnrichFishWithCookingData(
        Dictionary<string, List<Dictionary<string, object?>>> finalFiles,
        Dictionary<string, List<Dictionary<string, object?>>> baseData)
    {
        if (!finalFiles.TryGetValue("Fish.json", out var fishList)) return;

        // Build lookup of product IDs from the Products parser output (these have CdnUrl)
        var productsById = new HashSet<string>();
        if (baseData.TryGetValue("Products", out var productsList))
        {
            foreach (var item in productsList)
                if (item.TryGetValue("Id", out var id) && id is string idStr)
                    productsById.Add(idStr);
        }

        // Build lookup of cooking items for RewardID enrichment
        var cookingById = new Dictionary<string, Dictionary<string, object?>>();
        if (baseData.TryGetValue("Cooking", out var cookingList))
        {
            foreach (var item in cookingList)
                if (item.TryGetValue("Id", out var id) && id is string idStr)
                    cookingById[idStr] = item;
        }

        int cdnEnriched = 0, rewardEnriched = 0;
        foreach (var fish in fishList)
        {
            if (!fish.TryGetValue("Id", out var id) || id is not string fishId) continue;

            // Add CdnUrl for fish items that also exist in the Products parser output
            if (productsById.Contains(fishId))
            {
                fish["CdnUrl"] = "";
                cdnEnriched++;
            }

            // Add RewardID/EffectCategory/RewardEffectStats from cooking data
            // Only for fish that are also in the Products output (have CdnUrl) and
            // have a non-empty RewardID in the consumable table
            if (productsById.Contains(fishId) && cookingById.TryGetValue(fishId, out var cooking))
            {
                var rewardId = cooking.GetValueOrDefault("RewardID");
                if (rewardId is string rid && !string.IsNullOrEmpty(rid))
                {
                    fish["RewardID"] = rewardId;
                    fish["EffectCategory"] = cooking.GetValueOrDefault("EffectCategory");
                    fish["RewardEffectStats"] = cooking.GetValueOrDefault("RewardEffectStats");
                    rewardEnriched++;
                }
            }
        }
        if (cdnEnriched > 0)
            Console.WriteLine($"  [ENRICH] Fish.json: added CdnUrl to {cdnEnriched} items from Products data");
        if (rewardEnriched > 0)
            Console.WriteLine($"  [ENRICH] Fish.json: added reward data to {rewardEnriched} items from Cooking data");
    }

    /// <summary>
    /// Strip cooking-specific fields from items in non-Food category files.
    /// Items from the Cooking parser carry extra fields (CookingValue, RewardID,
    /// EffectCategory, RewardEffectStats) that should only appear in Food.json.
    /// Items that came from the Cooking parser (identified by having these fields)
    /// also have their NameLower stripped, since it was inherited from the cooking
    /// product lookup rather than the Products parser.
    /// </summary>
    private static void StripCookingFieldsFromNonFood(
        Dictionary<string, List<Dictionary<string, object?>>> finalFiles)
    {
        string[] cookingOnlyFields = { "CookingValue", "RewardID", "EffectCategory", "RewardEffectStats" };
        string[] exemptFiles = { "Food.json", "Fish.json" };

        int totalStripped = 0;
        foreach (var (filename, data) in finalFiles)
        {
            if (exemptFiles.Contains(filename)) continue;

            int fileStripped = 0;
            foreach (var item in data)
            {
                bool hadCookingField = false;
                foreach (var field in cookingOnlyFields)
                {
                    if (item.Remove(field))
                        hadCookingField = true;
                }
                // Also strip NameLower from items that came from the Cooking parser
                if (hadCookingField)
                {
                    item.Remove("NameLower");
                    fileStripped++;
                }
            }
            if (fileStripped > 0)
            {
                Console.WriteLine($"  [NORMALIZE] {filename}: stripped cooking fields from {fileStripped} items");
                totalStripped += fileStripped;
            }
        }
        if (totalStripped > 0)
            Console.WriteLine($"  [NORMALIZE] Stripped cooking fields from {totalStripped} total non-Food items");
    }

    private static void ApplySlugs(Dictionary<string, List<Dictionary<string, object?>>> finalFiles)
    {
        var slugs = new Dictionary<string, string>
        {
            ["Raw Materials.json"] = "raw/", ["Products.json"] = "products/",
            ["Food.json"] = "food/", ["Curiosities.json"] = "curiosities/",
            ["Corvette.json"] = "corvette/", ["Fish.json"] = "fish/",
            ["Constructed Technology.json"] = "technology/", ["Technology.json"] = "technology/",
            ["Technology Module.json"] = "technology/", ["Others.json"] = "other/",
            ["Buildings.json"] = "buildings/", ["Trade.json"] = "other/",
            ["Exocraft.json"] = "exocraft/", ["Starships.json"] = "starships/",
            ["Upgrades.json"] = "upgrades/", ["Egg Modifiers.json.tbc"] = "pet-eggs/",
        };

        foreach (var (filename, data) in finalFiles)
        {
            if (!slugs.TryGetValue(filename, out string? prefix)) continue;
            foreach (var item in data)
            {
                string? itemId = item.GetValueOrDefault("Id")?.ToString();
                if (!string.IsNullOrEmpty(itemId))
                    item["Slug"] = $"{prefix}{itemId}";
            }
        }
    }

    private static void DeduplicateAll(Dictionary<string, List<Dictionary<string, object?>>> finalFiles)
    {
        // Per-file dedup
        foreach (var (filename, data) in finalFiles.ToList())
        {
            // For Food.json, keep the LAST value but at the FIRST-seen position.
            if (filename == "Food.json")
            {
                // Build map: id -> first index, and id -> last (latest) item data
                var firstPos = new Dictionary<string, int>();
                var latestItem = new Dictionary<string, Dictionary<string, object?>>();
                for (int i = 0; i < data.Count; i++)
                {
                    string? id = data[i].GetValueOrDefault("Id")?.ToString();
                    if (id == null) continue;
                    if (!firstPos.ContainsKey(id))
                        firstPos[id] = i;
                    latestItem[id] = data[i]; // last write wins
                }
                var deduped = new List<Dictionary<string, object?>>();
                var seen = new HashSet<string>();
                for (int i = 0; i < data.Count; i++)
                {
                    string? id = data[i].GetValueOrDefault("Id")?.ToString();
                    if (id == null)
                    {
                        deduped.Add(data[i]);
                        continue;
                    }
                    // Only emit at the first-seen position, using the latest item data
                    if (firstPos.TryGetValue(id, out int fp) && fp == i && seen.Add(id))
                        deduped.Add(latestItem[id]);
                }
                if (deduped.Count < data.Count)
                {
                    int removed = data.Count - deduped.Count;
                    finalFiles[filename] = deduped;
                    Console.WriteLine($"  [NORMALIZE] {filename}: removed {removed} duplicate IDs (kept last value at first position)");
                }
            }
            else
            {
                var seen = new HashSet<string>();
                var deduped = new List<Dictionary<string, object?>>();
                foreach (var item in data)
                {
                    string? id = item.GetValueOrDefault("Id")?.ToString();
                    if (id == null || seen.Add(id))
                        deduped.Add(item);
                }
                if (deduped.Count < data.Count)
                {
                    int removed = data.Count - deduped.Count;
                    finalFiles[filename] = deduped;
                    Console.WriteLine($"  [NORMALIZE] {filename}: removed {removed} duplicate IDs");
                }
            }
        }

        // Cross-file dedup
        // Recipes.json is excluded because recipe IDs intentionally match product/material IDs
        // (they represent what the recipe produces) but are structurally different data.
        var globalSeen = new HashSet<string>();
        foreach (var (filename, data) in finalFiles.ToList())
        {
            if (filename == "Recipes.json") continue;

            var keep = new List<Dictionary<string, object?>>();
            int removed = 0;
            foreach (var item in data)
            {
                string? id = item.GetValueOrDefault("Id")?.ToString();
                if (id == null || globalSeen.Add(id))
                    keep.Add(item);
                else
                    removed++;
            }
            if (removed > 0)
            {
                finalFiles[filename] = keep;
                Console.WriteLine($"  [NORMALIZE] {filename}: removed {removed} cross-file duplicates");
            }
        }
    }

    private static void RunImageExtraction(
        string extractedDir, string jsonDir, string imagesDir, string toolsDir)
    {
        try
        {
            // Normalize texture paths (uses data already extracted in Step 5)
            ImageExtractor.NormalizeExtracted(extractedDir);

            // Convert DDS icons to PNG using ImageMagick magick.exe
            var (success, skipped) = ImageExtractor.ExtractIcons(
                jsonDir, extractedDir, imagesDir, toolsDir);
            PakExtractor.FinishProgress();
            Console.WriteLine($"[OK] Extracted: {success}  Skipped: {skipped}");
        }
        catch (Exception ex)
        {
            Console.WriteLine($"[WARN] Image extraction failed: {ex.Message}");
        }
    }

    /// <summary>
    /// Remove T_BOBBLE_* tech variants from Others.json when BOBBLE_* product versions exist.
    /// Both share the same Name/Group (Starship Interior Adornment) and display as duplicates.
    /// </summary>
    private static void DedupeStarshipAdornmentDisplayDuplicates(
        Dictionary<string, List<Dictionary<string, object?>>> finalFiles)
    {
        if (!finalFiles.TryGetValue("Others.json", out var others))
            return;

        var productIds = new HashSet<string>();
        foreach (var item in others)
        {
            string? id = item.GetValueOrDefault("Id")?.ToString();
            if (id != null && id.StartsWith("BOBBLE_"))
                productIds.Add(id);
        }
        if (productIds.Count == 0) return;

        var dropIds = new HashSet<string>(productIds.Select(pid => $"T_{pid}"));
        var keep = others.Where(item =>
        {
            string? id = item.GetValueOrDefault("Id")?.ToString();
            return id == null || !dropIds.Contains(id);
        }).ToList();

        int removed = others.Count - keep.Count;
        if (removed > 0)
        {
            finalFiles["Others.json"] = keep;
            Console.WriteLine($"  [NORMALIZE] Others.json: removed {removed} Starship Interior Adornment display duplicates");
        }
    }

    /// <summary>
    /// Reclassify items between Upgrades.json and Technology Module.json based on DeploysInto.
    /// <para>
    /// Items in Upgrades.json that have a DeploysInto value are cargo-holdable tech module
    /// fragments (e.g. U_SHIPSHIELD3, U_HYPER3, U_SCANNER4) that unpack into technology
    /// slots when used. These belong in Technology Module.json because they are products
    /// stored in cargo inventory, not direct tech installs.
    /// </para>
    /// <para>
    /// Conversely, items in Technology Module.json that do NOT have DeploysInto are actual
    /// technology upgrades (e.g. UA_PULSE1, UA_LAUN1, UA_HYP1) that install directly into
    /// technology slots. These belong in Upgrades.json.
    /// </para>
    /// </summary>
    private static void ReclassifyByDeploysInto(
        Dictionary<string, List<Dictionary<string, object?>>> finalFiles)
    {
        if (!finalFiles.TryGetValue("Upgrades.json", out var upgrades)
            || !finalFiles.TryGetValue("Technology Module.json", out var techModules))
            return;

        // Upgrades WITH DeploysInto → move to Technology Module
        var toTechModule = new List<Dictionary<string, object?>>();
        var keepInUpgrades = new List<Dictionary<string, object?>>();
        foreach (var item in upgrades)
        {
            string deploysInto = item.GetValueOrDefault("DeploysInto")?.ToString() ?? "";
            if (!string.IsNullOrEmpty(deploysInto) && deploysInto != "^")
                toTechModule.Add(item);
            else
                keepInUpgrades.Add(item);
        }

        // Technology Module WITHOUT DeploysInto → move to Upgrades
        var toUpgrades = new List<Dictionary<string, object?>>();
        var keepInTechModule = new List<Dictionary<string, object?>>();
        foreach (var item in techModules)
        {
            string deploysInto = item.GetValueOrDefault("DeploysInto")?.ToString() ?? "";
            if (string.IsNullOrEmpty(deploysInto) || deploysInto == "^")
                toUpgrades.Add(item);
            else
                keepInTechModule.Add(item);
        }

        if (toTechModule.Count == 0 && toUpgrades.Count == 0)
            return;

        // Apply the reclassification
        keepInUpgrades.AddRange(toUpgrades);
        keepInTechModule.AddRange(toTechModule);
        finalFiles["Upgrades.json"] = keepInUpgrades;
        finalFiles["Technology Module.json"] = keepInTechModule;

        if (toTechModule.Count > 0)
            Console.WriteLine($"  [RECLASSIFY] Moved {toTechModule.Count} items from Upgrades → Technology Module (have DeploysInto)");
        if (toUpgrades.Count > 0)
            Console.WriteLine($"  [RECLASSIFY] Moved {toUpgrades.Count} items from Technology Module → Upgrades (no DeploysInto)");
    }

    /// <summary>
    /// Move upgrade items from Exocraft.json to Upgrades.json.
    /// </summary>
    private static void MoveExocraftUpgrades(
        Dictionary<string, List<Dictionary<string, object?>>> finalFiles)
    {
        if (!finalFiles.TryGetValue("Exocraft.json", out var exocraft)
            || !finalFiles.TryGetValue("Upgrades.json", out var upgrades))
            return;

        var keep = new List<Dictionary<string, object?>>();
        int moved = 0;
        foreach (var item in exocraft)
        {
            string name = (item.GetValueOrDefault("Name")?.ToString() ?? "").ToLower();
            string group = (item.GetValueOrDefault("Group")?.ToString() ?? "").ToLower();
            if (name.Contains("upgrade") || group.Contains("upgrade"))
            {
                upgrades.Add(item);
                moved++;
            }
            else
            {
                keep.Add(item);
            }
        }
        if (moved > 0)
        {
            finalFiles["Exocraft.json"] = keep;
            Console.WriteLine($"  [NORMALIZE] Moved {moved} upgrade items from Exocraft.json to Upgrades.json");
        }
    }

    /// <summary>
    /// Corrects the Rarity field for upgrade items (U_, SHIP_) based on their
    /// Group name or Description keywords. The game data assigns a generic
    /// "Rare" rarity to all upgrades, but the actual class/rarity can be
    /// inferred from the group name:
    ///   "Banned"     -> Illegal  (X class)
    ///   "Sentinel"   -> Sentinel (? class)
    ///   "Supreme"    -> Legendary (S class)
    ///   "Powerful"   -> Epic     (A class)
    ///   "Significant"-> Rare     (B class)
    ///   "moderate" in description -> Normal (C class)
    ///   "Starship Core Component" with _C/_B/_A/_S suffix -> matching rarity
    /// </summary>
    private static void CorrectUpgradeRarities(
        Dictionary<string, List<Dictionary<string, object?>>> finalFiles)
    {
        // Process both Upgrades.json and Technology Module.json for rarity corrections
        string[] targetFiles = ["Upgrades.json", "Technology Module.json"];
        int corrected = 0;

        foreach (string fileName in targetFiles)
        {
            if (!finalFiles.TryGetValue(fileName, out var items))
                continue;

            foreach (var item in items)
            {
                string id = item.GetValueOrDefault("Id")?.ToString() ?? "";
                string group = item.GetValueOrDefault("Group")?.ToString() ?? "";
                string name = item.GetValueOrDefault("Name")?.ToString() ?? "";
                string description = item.GetValueOrDefault("Description")?.ToString() ?? "";
                string groupLower = group.ToLowerInvariant();
                string nameLower = name.ToLowerInvariant();
                string descLower = description.ToLowerInvariant();

                string? newRarity = null;

                if (groupLower.Contains("banned"))
                    newRarity = "Illegal";
                else if (groupLower.Contains("sentinel") || nameLower.Contains("sentinel"))
                    newRarity = "Sentinel";
                else if (groupLower.Contains("supreme"))
                    newRarity = "Legendary";
                else if (groupLower.Contains("powerful"))
                    newRarity = "Epic";
                else if (groupLower.Contains("significant"))
                    newRarity = "Rare";
                else if (descLower.Contains("moderate"))
                    newRarity = "Normal";
                else if (group == "Starship Core Component")
                {
                    // Infer rarity from ID suffix: SHIP_CORE_C, SHIP_CORE_B, etc.
                    if (id.EndsWith("_S", StringComparison.OrdinalIgnoreCase))
                        newRarity = "Legendary";
                    else if (id.EndsWith("_A", StringComparison.OrdinalIgnoreCase))
                        newRarity = "Epic";
                    else if (id.EndsWith("_B", StringComparison.OrdinalIgnoreCase))
                        newRarity = "Rare";
                    else if (id.EndsWith("_C", StringComparison.OrdinalIgnoreCase))
                        newRarity = "Normal";
                }
                else if (groupLower.Contains("deployable salvage"))
                {
                    // Infer rarity from group prefix: S-Class, A-Class, etc.
                    if (group.StartsWith("S-Class", StringComparison.OrdinalIgnoreCase))
                        newRarity = "Legendary";
                    else if (group.StartsWith("A-Class", StringComparison.OrdinalIgnoreCase))
                        newRarity = "Epic";
                    else if (group.StartsWith("B-Class", StringComparison.OrdinalIgnoreCase))
                        newRarity = "Rare";
                    else if (group.StartsWith("C-Class", StringComparison.OrdinalIgnoreCase))
                        newRarity = "Normal";
                }

                if (newRarity != null)
                {
                    string oldRarity = item.GetValueOrDefault("Rarity")?.ToString() ?? "";
                    if (oldRarity != newRarity)
                    {
                        item["Rarity"] = newRarity;
                        corrected++;
                    }
                }
            }
        }
        if (corrected > 0)
            Console.WriteLine($"  [NORMALIZE] Corrected Rarity for {corrected} upgrade items");
    }

    private static readonly Regex QualifiedUpgradeRe = new(
        @"^([CBSA]-Class|Significant|Powerful|Supreme|Banned|Illegal|SeaTrash)\s+.+\s+(Upgrade)$",
        RegexOptions.IgnoreCase | RegexOptions.Compiled);

    /// <summary>
    /// For Upgrades items whose Group contains "Upgrade" or "Node",
    /// override Name with the Group value to match the reference output.
    /// Items like Deployable Salvage and Starship Core Components whose
    /// Group does NOT contain these keywords keep their original Name.
    /// </summary>
    private static void NormalizeUpgradeNames(
        Dictionary<string, List<Dictionary<string, object?>>> finalFiles)
    {
        if (!finalFiles.TryGetValue("Upgrades.json", out var upgrades))
            return;

        int changed = 0;
        foreach (var item in upgrades)
        {
            string? group = item.GetValueOrDefault("Group")?.ToString();
            if (string.IsNullOrEmpty(group)) continue;

            bool hasKeyword = group.Contains("Upgrade") || group.Contains("Node");
            if (!hasKeyword) continue;

            string? name = item.GetValueOrDefault("Name")?.ToString();
            if (name != group)
            {
                item["Name"] = group;
                changed++;
            }
        }
        if (changed > 0)
            Console.WriteLine($"  [NORMALIZE] Upgrades.json: set Name = Group for {changed} upgrade items");
    }

    /// <summary>
    /// For qualified upgrade groups (class tier or quality prefix):
    /// - Strip the item name from the group to form a short group, e.g. "A-Class Upgrade"
    /// - Set Name to "original Name short group", e.g. "Blaze Javelin A-Class Upgrade"
    /// - Set Group to the short group, e.g. "A-Class Upgrade"
    /// </summary>
    private static void NormalizeUpgradeDisplayNames(
        Dictionary<string, List<Dictionary<string, object?>>> finalFiles)
    {
        if (!finalFiles.TryGetValue("Upgrades.json", out var upgrades))
            return;

        int changed = 0;
        foreach (var item in upgrades)
        {
            string? group = item.GetValueOrDefault("Group")?.ToString();
            if (string.IsNullOrEmpty(group)) continue;

            var m = QualifiedUpgradeRe.Match(group);
            if (!m.Success) continue;

            string qualifier = m.Groups[1].Value;
            string shortGroup = $"{qualifier} Upgrade";
            string displayName = (item.GetValueOrDefault("Name")?.ToString() ?? "").Trim();

            // Title-case lowercase names (e.g. corvette upgrades)
            if (displayName.Length > 0 && char.IsLower(displayName[0]))
                displayName = System.Globalization.CultureInfo.InvariantCulture.TextInfo.ToTitleCase(displayName);

            string newName = !string.IsNullOrEmpty(displayName) ? $"{displayName} {shortGroup}" : shortGroup;
            if (item.GetValueOrDefault("Name")?.ToString() != newName || item.GetValueOrDefault("Group")?.ToString() != shortGroup)
            {
                item["Name"] = newName;
                item["Group"] = shortGroup;
                changed++;
            }
        }
        if (changed > 0)
            Console.WriteLine($"  [NORMALIZE] Upgrades.json: set Name from Group for {changed} upgrade items");
    }

    private static readonly Regex PlaceholderUpRe = new(@"^Up [A-Za-z0-9_]+$", RegexOptions.Compiled);
    private static readonly Regex PlaceholderUtCrRe = new(@"^Ut Cr [A-Za-z0-9_]+$", RegexOptions.Compiled);
    private static readonly Regex ClassPrefixRe = new(@"^[CBSA]-Class\s+", RegexOptions.IgnoreCase | RegexOptions.Compiled);
    private static readonly Regex UpgradeSuffixRe = new(@"\s+Upgrade$", RegexOptions.IgnoreCase | RegexOptions.Compiled);

    private static readonly Dictionary<string, string> StrengthByQuality = new()
    {
        ["Normal"] = "moderate", ["Rare"] = "significant", ["Epic"] = "extremely powerful",
        ["Legendary"] = "supremely powerful", ["Illegal"] = "highly unstable",
    };

    private static bool IsPlaceholderUpgradeDescription(string? text)
    {
        if (string.IsNullOrWhiteSpace(text)) return false;
        string value = text.Trim();
        return PlaceholderUpRe.IsMatch(value) || PlaceholderUtCrRe.IsMatch(value);
    }

    private static string BuildUpgradeDescriptionFromGroup(Dictionary<string, object?> item)
    {
        string group = (item.GetValueOrDefault("Group")?.ToString() ?? "").Trim();
        string quality = (item.GetValueOrDefault("Quality")?.ToString() ?? "").Trim();
        if (string.IsNullOrEmpty(group)) return "";

        string target = ClassPrefixRe.Replace(group, "");
        target = UpgradeSuffixRe.Replace(target, "").Trim();
        if (string.IsNullOrEmpty(target)) target = group;

        string strength = StrengthByQuality.GetValueOrDefault(quality, "powerful");

        return $"A {strength} upgrade for the {target}. Use [E] to begin upgrade installation process.\n\n"
             + "The module is flexible, and exact upgrade statistics are unknown until installation is complete.";
    }

    /// <summary>
    /// Replace placeholder upgrade descriptions with meaningful text.
    /// </summary>
    private static void EnrichUpgradeDescriptions(
        Dictionary<string, List<Dictionary<string, object?>>> finalFiles)
    {
        if (!finalFiles.TryGetValue("Upgrades.json", out var upgrades))
            return;

        var byId = new Dictionary<string, Dictionary<string, object?>>();
        var wrappersByTarget = new Dictionary<string, List<string>>();
        foreach (var item in upgrades)
        {
            string? itemId = item.GetValueOrDefault("Id")?.ToString();
            if (!string.IsNullOrEmpty(itemId))
            {
                byId[itemId] = item;
                string? deploy = item.GetValueOrDefault("DeploysInto")?.ToString();
                if (!string.IsNullOrEmpty(deploy))
                {
                    if (!wrappersByTarget.TryGetValue(deploy, out var list))
                    {
                        list = new List<string>();
                        wrappersByTarget[deploy] = list;
                    }
                    list.Add(itemId);
                }
            }
        }

        int updated = 0;

        // First pass: enrich targets via wrappers
        foreach (var (targetId, wrapperIds) in wrappersByTarget)
        {
            if (!byId.TryGetValue(targetId, out var target)) continue;
            if (!IsPlaceholderUpgradeDescription(target.GetValueOrDefault("Description")?.ToString())) continue;

            string? replacement = null;
            foreach (string wrapperId in wrapperIds)
            {
                if (!byId.TryGetValue(wrapperId, out var wrapper)) continue;
                string? wrapperDesc = wrapper.GetValueOrDefault("Description")?.ToString();
                if (!string.IsNullOrEmpty(wrapperDesc?.Trim()) && !IsPlaceholderUpgradeDescription(wrapperDesc))
                {
                    replacement = wrapperDesc;
                    break;
                }
            }

            if (replacement != null)
            {
                target["Description"] = replacement;
                updated++;
            }
            else
            {
                string generated = BuildUpgradeDescriptionFromGroup(target);
                if (!string.IsNullOrEmpty(generated))
                {
                    target["Description"] = generated;
                    updated++;
                }
            }
        }

        // Second pass: remaining placeholders
        foreach (var item in upgrades)
        {
            if (!IsPlaceholderUpgradeDescription(item.GetValueOrDefault("Description")?.ToString())) continue;
            string generated = BuildUpgradeDescriptionFromGroup(item);
            if (!string.IsNullOrEmpty(generated))
            {
                item["Description"] = generated;
                updated++;
            }
        }

        if (updated > 0)
            Console.WriteLine($"  [ENRICH] Upgrades.json: replaced {updated} placeholder descriptions");
    }

    /// <summary>
    /// Link buildable tech labels to Corvette items by matching BuildableShipTechID to Upgrades items.
    /// </summary>
    private static void EnrichCorvetteBuildableTechLabels(
        Dictionary<string, List<Dictionary<string, object?>>> finalFiles)
    {
        if (!finalFiles.TryGetValue("Corvette.json", out var corvetteItems)
            || !finalFiles.TryGetValue("Upgrades.json", out var upgrades))
            return;

        var upgradesById = new Dictionary<string, Dictionary<string, object?>>();
        foreach (var item in upgrades)
        {
            string? id = item.GetValueOrDefault("Id")?.ToString();
            if (!string.IsNullOrEmpty(id))
                upgradesById[id] = item;
        }

        int enriched = 0;
        foreach (var item in corvetteItems)
        {
            string? techId = item.GetValueOrDefault("BuildableShipTechID")?.ToString();
            if (string.IsNullOrEmpty(techId)) continue;

            if (!upgradesById.TryGetValue(techId, out var linked)) continue;

            item["BuildableShipTechName"] = linked.GetValueOrDefault("Name");
            item["BuildableShipTechGroup"] = linked.GetValueOrDefault("Group");
            item["BuildableShipTechDescription"] = linked.GetValueOrDefault("Description");
            enriched++;
        }

        if (enriched > 0)
            Console.WriteLine($"  [ENRICH] Corvette.json: linked buildable tech labels for {enriched} items");
    }

    /// <summary>
    /// Generate TechPackDatabase.Generated.cs from technology data.
    /// Produces a tech catalog mapping tech IDs to their game-data metadata (icon, category, class).
    /// The _generatedPacks dictionary is populated with entries from the game data tech table,
    /// enabling runtime lookup of tech metadata by ID.
    /// </summary>
    private static void GenerateTechPackPartialClass(
        Dictionary<string, List<Dictionary<string, object?>>> baseData,
        string resourcesDir)
    {
        if (!baseData.TryGetValue("Technology", out var techItems) || techItems.Count == 0)
        {
            Console.WriteLine("[WARN] No Technology data available - skipping TechPackDatabase.Generated.cs");
            return;
        }

        // Build the tech catalog from parsed technology data
        var catalog = new List<(string Id, string IconPath, string Category, bool IsUpgrade, bool IsCore, bool IsProcedural)>();
        foreach (var item in techItems)
        {
            string? id = item.GetValueOrDefault("Id")?.ToString();
            if (string.IsNullOrEmpty(id)) continue;

            string iconPath = item.GetValueOrDefault("IconPath")?.ToString() ?? "";
            string category = item.GetValueOrDefault("Category")?.ToString() ?? "";
            bool isUpgrade = item.GetValueOrDefault("Upgrade") is true;
            bool isCore = item.GetValueOrDefault("Core") is true;
            bool isProcedural = item.GetValueOrDefault("Procedural") is true;

            catalog.Add((id, iconPath, category, isUpgrade, isCore, isProcedural));
        }

        // Determine class suffix from Id pattern: UP_XXX1=C, UP_XXX2=B, UP_XXX3=A, UP_XXX4=S
        static string InferClass(string id, bool isUpgrade)
        {
            if (!isUpgrade) return "NONE";
            if (id.EndsWith("1")) return "C";
            if (id.EndsWith("2")) return "B";
            if (id.EndsWith("3")) return "A";
            if (id.EndsWith("4")) return "S";
            // X-class (alien/sentinel/robot/royal upgrades)
            if (id.Contains("_ALIEN") || id.Contains("_SENT") || id.Contains("_ROBO")
                || id.Contains("_ROYAL")) return "X";
            return "?";
        }

        // Generate the .cs source file
        var sb = new System.Text.StringBuilder();
        sb.AppendLine("// <auto-generated>");
        sb.AppendLine("// This file is produced by the NMSE.Extractor from NMS game data.");
        sb.AppendLine("// It provides a tech catalog for enriching TechPackDatabase entries.");
        sb.AppendLine("// Regenerate by running the extractor against updated game files.");
        sb.AppendLine($"// NMS Version: 6.24 REMNANT (27 February 2026)");
        sb.AppendLine($"// Generated: {DateTime.UtcNow:yyyy-MM-dd HH:mm:ss} UTC");
        sb.AppendLine($"// Total technologies catalogued: {catalog.Count}");
        sb.AppendLine("// </auto-generated>");
        sb.AppendLine();
        sb.AppendLine("using System.Collections.Generic;");
        sb.AppendLine();
        sb.AppendLine("namespace NMSE.Data;");
        sb.AppendLine();
        sb.AppendLine("public static partial class TechPacks");
        sb.AppendLine("{");

        // _generatedPacks dictionary - empty since hashes can only come from save data
        sb.AppendLine("    /// <summary>");
        sb.AppendLine("    /// Additional techpacks discovered by the extractor from MBIN data.");
        sb.AppendLine("    /// Populate hash entries here when hashes are discovered from save files.");
        sb.AppendLine("    /// Use the tech catalog below (via <see cref=\"GetTechCatalogEntry\"/>) to look up");
        sb.AppendLine("    /// icon paths and class info for new entries.");
        sb.AppendLine("    /// </summary>");
        sb.AppendLine("    private static readonly Dictionary<string, TechPack> _generatedPacks = new()");
        sb.AppendLine("    {");
        sb.AppendLine("    };");
        sb.AppendLine();

        // RegisterGeneratedPacks
        sb.AppendLine("    /// <summary>");
        sb.AppendLine("    /// Registers extractor-generated techpacks into the main dictionary.");
        sb.AppendLine("    /// Call this once during application startup after the main dictionary is initialized.");
        sb.AppendLine("    /// Only adds packs whose hashes are not already in the dictionary.");
        sb.AppendLine("    /// </summary>");
        sb.AppendLine("    public static void RegisterGeneratedPacks()");
        sb.AppendLine("    {");
        sb.AppendLine("        foreach (var kvp in _generatedPacks)");
        sb.AppendLine("            Dictionary.TryAdd(kvp.Key, kvp.Value);");
        sb.AppendLine("    }");
        sb.AppendLine();

        // Tech catalog - the real value: a dictionary mapping tech ID -> metadata from game data
        sb.AppendLine("    /// <summary>");
        sb.AppendLine("    /// Game-data tech catalog extracted from NMS_REALITY_GCTECHNOLOGYTABLE.");
        sb.AppendLine($"    /// Contains {catalog.Count} technology entries with icon paths and category info.");
        sb.AppendLine("    /// </summary>");
        sb.AppendLine("    public sealed class TechCatalogEntry");
        sb.AppendLine("    {");
        sb.AppendLine("        public required string Id { get; init; }");
        sb.AppendLine("        public required string IconPath { get; init; }");
        sb.AppendLine("        public required string Category { get; init; }");
        sb.AppendLine("        public required string InferredClass { get; init; }");
        sb.AppendLine("        public required bool IsUpgrade { get; init; }");
        sb.AppendLine("        public required bool IsCore { get; init; }");
        sb.AppendLine("        public required bool IsProcedural { get; init; }");
        sb.AppendLine("    }");
        sb.AppendLine();
        sb.AppendLine("    private static readonly Dictionary<string, TechCatalogEntry> _techCatalog = new(System.StringComparer.OrdinalIgnoreCase)");
        sb.AppendLine("    {");

        foreach (var (id, iconPath, category, isUpgrade, isCore, isProcedural) in catalog.OrderBy(c => c.Id, StringComparer.OrdinalIgnoreCase))
        {
            string cls = InferClass(id, isUpgrade);
            // Normalize all path separators to forward slashes for the generated C# string literal
            string escapedIcon = iconPath.Replace('\\', '/');
            sb.AppendLine($"        [\"{id}\"] = new TechCatalogEntry {{ Id = \"{id}\", IconPath = \"{escapedIcon}\", Category = \"{category}\", InferredClass = \"{cls}\", IsUpgrade = {(isUpgrade ? "true" : "false")}, IsCore = {(isCore ? "true" : "false")}, IsProcedural = {(isProcedural ? "true" : "false")} }},");
        }

        sb.AppendLine("    };");
        sb.AppendLine();

        // Lookup method
        sb.AppendLine("    /// <summary>");
        sb.AppendLine("    /// Look up a technology's game-data metadata by its ID.");
        sb.AppendLine("    /// Returns null if the tech ID is not in the catalog.");
        sb.AppendLine("    /// </summary>");
        sb.AppendLine("    public static TechCatalogEntry? GetTechCatalogEntry(string techId)");
        sb.AppendLine("    {");
        sb.AppendLine("        if (_techCatalog.TryGetValue(techId, out var entry)) return entry;");
        sb.AppendLine("        // Try with/without ^ prefix");
        sb.AppendLine("        if (techId.StartsWith(\"^\") && _techCatalog.TryGetValue(techId[1..], out entry)) return entry;");
        sb.AppendLine("        if (!techId.StartsWith(\"^\") && _techCatalog.TryGetValue(\"^\" + techId, out entry)) return entry;");
        sb.AppendLine("        return null;");
        sb.AppendLine("    }");
        sb.AppendLine("}");

        // Write the generated file
        // Place it alongside the main project's Data directory
        string outputPath = Path.Combine(resourcesDir, "..", "Data", "TechPackDatabase.Generated.cs");
        outputPath = Path.GetFullPath(outputPath);

        // Ensure directory exists
        string? dir = Path.GetDirectoryName(outputPath);
        if (dir != null) Directory.CreateDirectory(dir);

        File.WriteAllText(outputPath, sb.ToString());
        Console.WriteLine($"[OK] Generated TechPackDatabase.Generated.cs with {catalog.Count} tech catalog entries");

        // Summary statistics
        int upgradeCount = catalog.Count(c => c.IsUpgrade);
        int coreCount = catalog.Count(c => c.IsCore);
        int proceduralCount = catalog.Count(c => c.IsProcedural);
        var categories = catalog.GroupBy(c => c.Category).OrderByDescending(g => g.Count());
        Console.WriteLine($"  Upgrades: {upgradeCount}, Core: {coreCount}, Procedural: {proceduralCount}");
        foreach (var g in categories.Take(8))
            Console.WriteLine($"  Category {g.Key}: {g.Count()} techs");
    }
}

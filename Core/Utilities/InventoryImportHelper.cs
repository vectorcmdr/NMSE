using NMSE.Models;

namespace NMSE.Core.Utilities;

/// <summary>
/// Helper for locating and unwrapping imported JSON files from various editors.
/// Supports raw exports as well as NomNom's {"Data": {...}} wrapper format
/// and NMSSaveEditor formats.
/// </summary>
internal static class InventoryImportHelper
{
    /// <summary>
    /// Known wrapper paths where inventory data may be nested, checked in order.
    /// More specific paths (e.g. Data -> Multitool -> Store) are listed after
    /// general ones (e.g. Store) because the first match wins and the general
    /// paths handle NMSSaveEditor format which wraps inventory at a shallower level.
    /// Covers NMSSaveEditor and NomNom formats (keys auto-deobfuscated by JsonParser).
    /// </summary>
    internal static readonly string[][] KnownInventoryPaths =
    [
        // NMSSaveEditor multitool: { Store: { Slots: [...] } }
        ["Store"],
        // NomNom Data envelope paths
        ["Data", "Multitool", "Store"],
        ["Data", "Vehicle", "Inventory"],
        ["Data", "Vehicle", "Inventory_TechOnly"],
        ["Data", "Inventory"],
        ["Data", "Inventory_TechOnly"],
        ["Data", "Freighter", "Inventory"],
        ["Data", "Freighter", "Inventory_TechOnly"],
        ["Data", "Starship", "Inventory"],
        ["Data", "Starship", "Inventory_TechOnly"],
    ];

    /// <summary>
    /// Searches a parsed JSON object for inventory data (an object containing a "Slots" array).
    /// Handles multiple formats:
    /// <list type="bullet">
    ///   <item>Raw inventory: Slots array at the top level.</item>
    ///   <item>NMSSaveEditor wrappers: e.g. Multitool.wp0 has Store -> {Slots, ...}.</item>
    ///   <item>NomNom wrappers: e.g. .mlt has Data -> Multitool -> Store -> {Slots, ...} (keys auto-deobfuscated by parser).</item>
    /// </list>
    /// </summary>
    /// <returns>The JsonObject containing Slots, or null if not found.</returns>
    internal static JsonObject? FindInventoryObject(JsonObject root)
    {
        // 1. Direct: Slots at top level (our own export format)
        if (root.GetArray("Slots") != null)
            return root;

        // 2. Check known wrapper paths
        foreach (var path in KnownInventoryPaths)
        {
            var current = root;
            bool found = true;
            foreach (var segment in path)
            {
                var next = current.GetObject(segment);
                if (next == null) { found = false; break; }
                current = next;
            }
            if (found && current.GetArray("Slots") != null)
                return current;
        }

        // 3. Fallback: breadth-first search for any nested object containing Slots.
        //    Limited depth to avoid traversing huge save files.
        return FindInventoryBfs(root, maxDepth: 4);
    }

    /// <summary>
    /// Breadth-first search for a nested JsonObject containing a "Slots" array.
    /// </summary>
    internal static JsonObject? FindInventoryBfs(JsonObject root, int maxDepth)
    {
        var queue = new Queue<(JsonObject obj, int depth)>();
        queue.Enqueue((root, 0));

        while (queue.Count > 0)
        {
            var (current, depth) = queue.Dequeue();
            if (current.GetArray("Slots") != null)
                return current;

            if (depth >= maxDepth)
                continue;

            foreach (var key in current.Names())
            {
                var child = current.GetObject(key);
                if (child != null)
                    queue.Enqueue((child, depth + 1));
            }
        }

        return null;
    }

    // --- NomNom Wrapper Detection & Unwrapping ----------------------

    /// <summary>
    /// Detects if the imported object uses NomNom's wrapper format
    /// (has "Data" and "FileVersion" keys).
    /// </summary>
    internal static bool IsNomNomWrapper(JsonObject imported)
    {
        return imported.Contains("Data") && imported.Contains("FileVersion");
    }

    /// <summary>
    /// Unwraps a NomNom export by extracting the entity object from inside
    /// Data -> {typeKey}. If the file is not a NomNom wrapper, returns the
    /// original object unchanged.
    /// </summary>
    /// <param name="imported">The parsed JSON object.</param>
    /// <param name="typeKey">The NomNom type key inside Data (e.g. "Multitool", "Vehicle", "Settlement").</param>
    /// <returns>The unwrapped entity object, or the original if not a NomNom wrapper.</returns>
    internal static JsonObject UnwrapNomNom(JsonObject imported, string typeKey)
    {
        if (!IsNomNomWrapper(imported))
            return imported;

        var data = imported.GetObject("Data");
        if (data == null)
            return imported;

        var entity = data.GetObject(typeKey);
        return entity ?? imported;
    }

    /// <summary>
    /// Unwraps a NomNom companion export. Companions have a special structure
    /// where Pet and AccessoryCustomisation are separate keys under Data.
    /// Merges them into a single object matching the save format.
    /// </summary>
    internal static JsonObject UnwrapNomNomCompanion(JsonObject imported)
    {
        if (!IsNomNomWrapper(imported))
            return imported;

        var data = imported.GetObject("Data");
        if (data == null)
            return imported;

        var pet = data.GetObject("Pet");
        if (pet == null)
            return imported;

        // Merge AccessoryCustomisation into the pet object if present
        var accessory = data.GetObject("AccessoryCustomisation");
        if (accessory != null && !pet.Contains("AccessoryCustomisation"))
            pet.Set("AccessoryCustomisation", accessory);

        return pet;
    }

    /// <summary>
    /// Unwraps a NomNom frigate export. The frigate data is under Data -> Frigate.
    /// </summary>
    internal static JsonObject UnwrapNomNomFrigate(JsonObject imported)
    {
        return UnwrapNomNom(imported, "Frigate");
    }

    /// <summary>
    /// Unwraps a NomNom squadron pilot export. The pilot data is under Data -> Pilot.
    /// </summary>
    internal static JsonObject UnwrapNomNomPilot(JsonObject imported)
    {
        return UnwrapNomNom(imported, "Pilot");
    }
}

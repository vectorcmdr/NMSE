using NMSE.Core.Utilities;
using NMSE.Models;
using static NMSE.Core.Utilities.MathHelper;

namespace NMSE.Core;

/// <summary>
/// Provides base-building operations and defines storage inventory keys for chest and special inventories.
/// </summary>
internal static class BaseLogic
{
    // ---- Move Base Computer ----

    // Algorithm summary:
    // 1. Build the old local coordinate system from the base's current Position and Forward.
    //    The Y-axis points radially away from the planet centre (normalised Position),
    //    the Z-axis is the Forward component perpendicular to Y (Gram-Schmidt), and
    //    X is the cross product completing the right-hand system.
    // 2. Compute the target object's world-space position by applying the old system.
    // 3. Build a new coordinate system centred at the target's world position with Y pointing
    //    radially outward from the planet and Z derived from the old Forward via Gram-Schmidt.
    // 4. Transform every object's Position, Up, and At vectors from old local to world and
    //    then from world to new local, preserving their absolute world positions.
    // 5. Update the base's root Position and Forward to the new values.
    // 6. Swap the base computer and target object's local vectors so the base computer sits
    //    at the new centre.

    /// <summary>
    /// Moves the base computer to the location of a target object by performing a full
    /// coordinate system transformation.
    /// </summary>
    /// <param name="baseData">The PersistentPlayerBases entry (has Position, Forward, Objects).</param>
    /// <param name="baseFlag">The ^BASE_FLAG object within this base.</param>
    /// <param name="target">The target object to move the base computer to.</param>
    internal static void MoveBaseComputer(JsonObject baseData, JsonObject baseFlag, JsonObject target)
    {
        // Read base position/forward and build old coordinate system
        var basePos = Vec3.FromArray(baseData.GetArray("Position")!);
        var baseFwd = Vec3.FromArray(baseData.GetArray("Forward")!);
        Vec3 basePosN = basePos.Normalized();
        Vec3 baseFwdN = baseFwd.Normalized();

        // Y-axis = radial "up" direction (normalised position vector from planet centre)
        // Z-axis = forward component perpendicular to Y (Gram-Schmidt)
        // X-axis = completes the right-hand system
        Vec3 oldZ = baseFwdN;
        Vec3 oldY = Vec3.GramSchmidt(basePosN, baseFwdN);
        Vec3 oldX = Vec3.Cross(oldY, oldZ).Normalized();
        var oldSystem = new CoordSystem(basePos, oldX, oldY, oldZ);

        // Compute target's world-space position
        Vec3 targetLocal = Vec3.FromArray(target.GetArray("Position")!);
        Vec3 targetWorld = oldSystem.Apply(targetLocal);

        // Build new coordinate system centred at the target
        Vec3 newY = targetWorld.Normalized();
        // Derive a new forward (Z) that is perpendicular to the new radial up (Y)
        // using the old forward direction as a guide (Gram-Schmidt).
        Vec3 newZ = Vec3.GramSchmidt(baseFwd, newY);
        Vec3 newX = Vec3.Cross(newY, newZ).Normalized();
        var newSystem = new CoordSystem(targetWorld, newX, newY, newZ);

        // Transform every object from old local to new local
        var objects = baseData.GetArray("Objects");
        if (objects != null)
        {
            // Direction vectors (Up, At) are transformed by converting them to a far away
            // point offset, transforming the point, then recovering the direction.
            // This is equivalent to applying the rotation part of the transform
            const double scale = 100.0;

            for (int i = 0; i < objects.Length; i++)
            {
                var obj = objects.GetObject(i);

                Vec3 pos = Vec3.FromArray(obj.GetArray("Position")!);
                Vec3 up  = Vec3.FromArray(obj.GetArray("Up")!);
                Vec3 at  = Vec3.FromArray(obj.GetArray("At")!);

                // Convert position and direction endpoints to world space
                Vec3 p1 = pos;
                Vec3 p2 = pos + scale * up;
                Vec3 p3 = pos + scale * at;

                Vec3 w1 = oldSystem.Apply(p1);
                Vec3 w2 = oldSystem.Apply(p2);
                Vec3 w3 = oldSystem.Apply(p3);

                // Convert from world space to new local space
                Vec3 n1 = newSystem.Solve(w1);
                Vec3 n2 = newSystem.Solve(w2);
                Vec3 n3 = newSystem.Solve(w3);

                Vec3 newPos = n1;
                Vec3 newUp  = (n2 - n1) / scale;
                Vec3 newAt  = (n3 - n1) / scale;

                newPos.WriteToArray(obj.GetArray("Position")!);
                newUp.WriteToArray(obj.GetArray("Up")!);
                newAt.WriteToArray(obj.GetArray("At")!);
            }
        }

        // Update base Position and Forward
        targetWorld.WriteToArray(baseData.GetArray("Position")!);
        newZ.WriteToArray(baseData.GetArray("Forward")!);

        // Swap base computer and target local positions
        SwapPositions(baseFlag, target);
    }

    /// <summary>
    /// Swaps the Position, Up, and At fields between two base objects.
    /// Used as the final step of <see cref="MoveBaseComputer"/> to exchange the base computer
    /// and target object's local positions after the coordinate system transformation.
    /// </summary>
    /// <param name="a">The first base JSON object.</param>
    /// <param name="b">The second base JSON object.</param>
    internal static void SwapPositions(JsonObject a, JsonObject b)
    {
        foreach (string field in new[] { "Position", "Up", "At" })
        {
            var aVal = a.Get(field);
            var bVal = b.Get(field);
            a.Set(field, bVal);
            b.Set(field, aVal);
        }
    }

    /// <summary>
    /// Clears all terrain edits associated with a specific base.
    /// Terrain edits are stored in <c>PlayerStateData.TerrainEditData</c> and indexed by
    /// the base's GalacticAddress. Each matching address has corresponding entries in the
    /// parallel arrays: BufferSizes, BufferAges, BufferAnchors, BufferProtected, and Edits.
    /// </summary>
    /// <param name="playerState">The PlayerStateData JSON object.</param>
    /// <param name="baseObj">The base JSON object from PersistentPlayerBases.</param>
    /// <returns>The number of terrain edit buffers removed, or 0 if none were found.</returns>
    internal static int ClearTerrainEdits(JsonObject playerState, JsonObject baseObj)
    {
        string baseAddress = CoordinateHelper.NormalizeGalacticAddress(baseObj.Get("GalacticAddress"));
        if (string.IsNullOrEmpty(baseAddress))
            return 0;

        var terrainData = playerState.GetObject("TerrainEditData");
        if (terrainData == null)
            return 0;

        var galacticAddresses = terrainData.GetArray("GalacticAddresses");
        var bufferSizes = terrainData.GetArray("BufferSizes");
        var edits = terrainData.GetArray("Edits");
        if (galacticAddresses == null || bufferSizes == null || edits == null)
            return 0;

        // Optional arrays that may be present depending on save version
        var bufferAges = terrainData.GetArray("BufferAges");
        var bufferAnchors = terrainData.GetArray("BufferAnchors");
        var bufferProtected = terrainData.GetArray("BufferProtected");

        // Find all indices in GalacticAddresses that match the base's address.
        // Collect in reverse order so removal doesn't shift later indices.
        var matchIndices = new List<int>();
        for (int i = 0; i < galacticAddresses.Length; i++)
        {
            string terrainAddress = CoordinateHelper.NormalizeGalacticAddress(galacticAddresses.Get(i));
            if (terrainAddress == baseAddress)
                matchIndices.Add(i);
        }

        if (matchIndices.Count == 0)
            return 0;

        // Calculate the cumulative edit offset for each buffer index.
        // editOffset[i] = sum of BufferSizes[0..i-1], i.e. starting position in Edits array.
        int totalBuffers = bufferSizes.Length;
        var editOffsets = new int[totalBuffers];
        int runningOffset = 0;
        for (int i = 0; i < totalBuffers; i++)
        {
            editOffsets[i] = runningOffset;
            runningOffset += bufferSizes.GetInt(i);
        }

        // Process matches in reverse order to preserve earlier indices during removal.
        int removed = 0;
        for (int m = matchIndices.Count - 1; m >= 0; m--)
        {
            int idx = matchIndices[m];
            int editCount = bufferSizes.GetInt(idx);
            int editStart = editOffsets[idx];

            // Remove the edit entries for this buffer (in reverse to preserve indices)
            for (int e = editCount - 1; e >= 0; e--)
                edits.RemoveAt(editStart + e);

            // Remove from parallel arrays
            galacticAddresses.RemoveAt(idx);
            bufferSizes.RemoveAt(idx);
            if (bufferAges != null && idx < bufferAges.Length)
                bufferAges.RemoveAt(idx);
            if (bufferAnchors != null && idx < bufferAnchors.Length)
                bufferAnchors.RemoveAt(idx);
            if (bufferProtected != null && idx < bufferProtected.Length)
                bufferProtected.RemoveAt(idx);

            removed++;
        }

        return removed;
    }

    /// <summary>
    /// JSON keys for the 10 standard storage container chest inventories.
    /// </summary>
    internal static readonly string[] ChestInventoryKeys =
    {
        "Chest1Inventory", "Chest2Inventory", "Chest3Inventory", "Chest4Inventory", "Chest5Inventory",
        "Chest6Inventory", "Chest7Inventory", "Chest8Inventory", "Chest9Inventory", "Chest10Inventory"
    };

    /// <summary>
    /// Definitions for special storage inventories, including their JSON key, display name, and export filename.
    /// </summary>
    internal static readonly (string Key, string DisplayName, string ExportFileName)[] StorageInventories =
    {
        ("CookingIngredientsInventory", "Ingredient Storage", "Ingredient_Storage.json"),
        ("CorvetteStorageInventory", "Corvette Parts Cache", "Corvette_Parts_Cache.json"),
        ("ChestMagicInventory", "Base Salvage Capsule", "Base_Salvage_Capsule.json"),
        ("RocketLockerInventory", "Rocket", "Rocket.json"),
        ("FishPlatformInventory", "Fishing Platform", "Fishing_Platform.json"),
        ("FishBaitBoxInventory", "Fish Bait", "Fish_Bait.json"),
        ("FoodUnitInventory", "Food Unit", "Food_Unit.json"),
        ("ChestMagic2Inventory", "Freighter Refund (unused)", "Freighter_Refund.json"),
    };

    /// <summary>
    /// Swaps two entries in the <c>PersistentPlayerBases</c> array by their raw array indices.
    /// Uses remove-then-insert so that <see cref="JsonArray"/> parent tracking remains
    /// consistent.  The higher-indexed element is processed first so that removal does not
    /// shift the lower index.
    /// </summary>
    /// <param name="bases">The <c>PersistentPlayerBases</c> JSON array.</param>
    /// <param name="indexA">Raw array index of the first element.</param>
    /// <param name="indexB">Raw array index of the second element.</param>
    internal static void SwapPlayerBases(JsonArray bases, int indexA, int indexB)
    {
        if (indexA == indexB) return;
        if (indexA < 0 || indexB < 0 || indexA >= bases.Length || indexB >= bases.Length) return;

        int lo = Math.Min(indexA, indexB);
        int hi = Math.Max(indexA, indexB);

        var loVal = bases.Get(lo);
        var hiVal = bases.Get(hi);

        // Remove the higher index first so lo's index is not affected.
        bases.RemoveAt(hi);
        bases.Insert(hi, loVal);
        bases.RemoveAt(lo);
        bases.Insert(lo, hiVal);
    }

    /// <summary>
    /// The default internal name for unnamed storage containers in NMS save data.
    /// When a chest has this name, the player has not assigned a custom name.
    /// </summary>
    internal const string DefaultChestName = "BLD_STORAGE_NAME";

    /// <summary>
    /// Reads the display name from a chest inventory JSON object.
    /// Returns an empty string if the name is the default <see cref="DefaultChestName"/> or absent.
    /// </summary>
    /// <param name="chestInventory">The chest inventory JSON object (e.g. <c>Chest1Inventory</c>).</param>
    /// <returns>The custom name, or an empty string if unnamed.</returns>
    internal static string GetChestName(JsonObject? chestInventory)
    {
        if (chestInventory == null) return "";
        string? name = chestInventory.GetString("Name");
        if (string.IsNullOrEmpty(name) || name == DefaultChestName)
            return "";
        return name;
    }

    /// <summary>
    /// Sets the display name on a chest inventory JSON object.
    /// If the new name is null or empty, resets to <see cref="DefaultChestName"/>.
    /// </summary>
    /// <param name="chestInventory">The chest inventory JSON object to modify.</param>
    /// <param name="newName">The new name to set, or null/empty to reset.</param>
    internal static void SetChestName(JsonObject? chestInventory, string? newName)
    {
        if (chestInventory == null) return;
        string value = string.IsNullOrWhiteSpace(newName) ? DefaultChestName : newName.Trim();
        chestInventory.Set("Name", value);
    }

    /// <summary>
    /// Formats a chest tab title. If the chest has a custom name, appends it
    /// after the tab label (e.g. "Chest 0: Cooking Items").
    /// </summary>
    /// <param name="tabLabel">The base tab label (e.g. "Chest 0").</param>
    /// <param name="chestName">The custom name, or empty/null if unnamed.</param>
    /// <returns>The formatted tab title.</returns>
    internal static string FormatChestTabTitle(string tabLabel, string? chestName)
    {
        if (string.IsNullOrEmpty(chestName))
            return tabLabel;
        return $"{tabLabel}: {chestName}";
    }
}

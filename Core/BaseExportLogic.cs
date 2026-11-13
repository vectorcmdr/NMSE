using NMSE.Core.Utilities;
using NMSE.Models;

namespace NMSE.Core;

/// <summary>
/// Reads and writes the NMSE base export envelope and moves the terrain edit
/// buffers that belong to a base's galactic address.
/// No UI dependencies.
/// </summary>
internal static class BaseExportLogic
{
    /// <summary>Marker written in a terrain sidecar file.</summary>
    internal const string TerrainFormatName = "NMSETerrain";

    /// <summary>Current version of the terrain sidecar format.</summary>
    internal const int TerrainFormatVersion = 1;

    /// <summary>File-name suffix appended to the base file for its terrain sidecar.</summary>
    internal const string TerrainSidecarSuffix = ".terrain.json";

    /// <summary>The result of reading a base export file.</summary>
    internal enum ReadStatus
    {
        /// <summary>The file contains a base object.</summary>
        Ok,
        /// <summary>The file is valid JSON but does not contain a base.</summary>
        NotABase
    }

    /// <summary>The result of looking for a base's terrain sidecar.</summary>
    internal enum TerrainSidecarStatus
    {
        /// <summary>No sidecar exists; the base can still be imported without terrain.</summary>
        Missing,
        /// <summary>The sidecar was read and matches the base.</summary>
        Ok,
        /// <summary>The sidecar exists but is malformed or belongs to another base.</summary>
        Invalid
    }

    /// <summary>A decoded base export, ready to apply to a save.</summary>
    internal sealed class ExportPayload
    {
        /// <summary>Creates a payload from a base entry and optional terrain data.</summary>
        public ExportPayload(JsonObject baseObject, JsonObject? terrainEditData)
        {
            Base = baseObject;
            TerrainEditData = terrainEditData;
        }

        /// <summary>Gets the full PersistentPlayerBases entry.</summary>
        public JsonObject Base { get; }

        /// <summary>Gets the terrain buffers matching the base, or null when none were exported.</summary>
        public JsonObject? TerrainEditData { get; }
    }

    /// <summary>The result of decoding an export file.</summary>
    internal sealed class ReadResult
    {
        /// <summary>Creates a read result.</summary>
        public ReadResult(ReadStatus status, ExportPayload? payload = null)
        {
            Status = status;
            Payload = payload;
        }

        /// <summary>Gets the decode status.</summary>
        public ReadStatus Status { get; }

        /// <summary>Gets the decoded payload when the status is Ok.</summary>
        public ExportPayload? Payload { get; }
    }

    /// <summary>The result of applying a base import.</summary>
    internal sealed class ImportResult
    {
        /// <summary>Creates an import result.</summary>
        public ImportResult(int baseIndex, int terrainBuffers)
        {
            BaseIndex = baseIndex;
            TerrainBuffers = terrainBuffers;
        }

        /// <summary>Gets the index of the imported or replaced base.</summary>
        public int BaseIndex { get; }

        /// <summary>Gets the number of terrain buffers applied.</summary>
        public int TerrainBuffers { get; }
    }

    /// <summary>The result of writing a base export and its optional sidecar.</summary>
    internal sealed class ExportWriteResult
    {
        /// <summary>Creates an export write result.</summary>
        public ExportWriteResult(int terrainBuffers, string? terrainSidecarPath, bool removedStaleSidecar)
        {
            TerrainBuffers = terrainBuffers;
            TerrainSidecarPath = terrainSidecarPath;
            RemovedStaleSidecar = removedStaleSidecar;
        }

        /// <summary>Gets the number of terrain buffers written to the sidecar.</summary>
        public int TerrainBuffers { get; }

        /// <summary>Gets the sidecar path written, or null when the base had no terrain.</summary>
        public string? TerrainSidecarPath { get; }

        /// <summary>Gets whether an obsolete sidecar was deleted for a base with no terrain.</summary>
        public bool RemovedStaleSidecar { get; }
    }

    /// <summary>The result of reading a terrain sidecar.</summary>
    internal sealed class TerrainSidecarResult
    {
        /// <summary>Creates a sidecar result.</summary>
        public TerrainSidecarResult(TerrainSidecarStatus status, JsonObject? terrainEditData = null)
        {
            Status = status;
            TerrainEditData = terrainEditData;
        }

        /// <summary>Gets the sidecar decode status.</summary>
        public TerrainSidecarStatus Status { get; }

        /// <summary>Gets the decoded terrain payload when the status is Ok.</summary>
        public JsonObject? TerrainEditData { get; }
    }

    /// <summary>
    /// Writes a base as a raw <c>.nmsbase</c> file and writes the matching terrain
    /// buffers to a sibling <c>.terrain.json</c> sidecar. A base without terrain
    /// removes any stale sidecar so it cannot be applied later.
    /// </summary>
    /// <param name="playerState">The PlayerStateData object, or null when terrain is unavailable.</param>
    /// <param name="baseObject">The PersistentPlayerBases entry to export.</param>
    /// <param name="basePath">The chosen base file path.</param>
    /// <returns>The written sidecar details.</returns>
    internal static ExportWriteResult WriteExport(
        JsonObject? playerState,
        JsonObject baseObject,
        string basePath)
    {
        string sidecarPath = GetTerrainSidecarPath(basePath);
        baseObject.ExportToFile(basePath);

        var terrain = playerState == null ? null : ExtractTerrainForBase(playerState, baseObject);
        if (terrain == null)
        {
            bool removed = File.Exists(sidecarPath);
            if (removed)
                File.Delete(sidecarPath);
            return new ExportWriteResult(0, null, removed);
        }

        if (File.Exists(sidecarPath))
            File.Delete(sidecarPath);

        var sidecar = new JsonObject();
        sidecar.Set("Format", TerrainFormatName);
        sidecar.Set("FormatVersion", TerrainFormatVersion);
        sidecar.Set("TerrainEditData", terrain);
        sidecar.ExportToFile(sidecarPath);

        int bufferCount = terrain.GetArray("GalacticAddresses")?.Length ?? 0;
        return new ExportWriteResult(bufferCount, sidecarPath, false);
    }

    /// <summary>Gets the sidecar path used for a base file.</summary>
    /// <param name="basePath">The base file path.</param>
    internal static string GetTerrainSidecarPath(string basePath) =>
        basePath + TerrainSidecarSuffix;

    /// <summary>
    /// Resolves a selected base or sidecar path to the base file path.
    /// If the sidecar was selected, its base file must exist.
    /// </summary>
    /// <param name="selectedPath">The path chosen in the import dialog.</param>
    internal static string ResolveBaseFilePath(string selectedPath)
    {
        if (selectedPath.EndsWith(TerrainSidecarSuffix, StringComparison.OrdinalIgnoreCase))
        {
            string candidate = selectedPath[..^TerrainSidecarSuffix.Length];
            if (File.Exists(candidate))
                return candidate;
        }
        return selectedPath;
    }

    /// <summary>
    /// Reads the raw base file. A raw base never contains terrain; the sidecar is
    /// read separately so the base file remains compatible with older NMSE builds.
    /// </summary>
    /// <param name="file">The parsed base file.</param>
    /// <returns>The decoded result.</returns>
    internal static ReadResult ReadExport(JsonObject file)
    {
        if (!LooksLikeBase(file))
            return new ReadResult(ReadStatus.NotABase);

        return new ReadResult(ReadStatus.Ok, new ExportPayload(file, null));
    }

    /// <summary>
    /// Reads and validates a terrain sidecar for a base. Missing sidecars are not
    /// errors: the base can be imported without terrain.
    /// </summary>
    /// <param name="basePath">The base file path.</param>
    /// <param name="baseObject">The decoded base object.</param>
    /// <returns>The sidecar decode result.</returns>
    internal static TerrainSidecarResult ReadTerrainSidecar(string basePath, JsonObject baseObject)
    {
        string sidecarPath = GetTerrainSidecarPath(basePath);
        if (!File.Exists(sidecarPath))
            return new TerrainSidecarResult(TerrainSidecarStatus.Missing);

        try
        {
            var sidecar = JsonObject.ImportFromFile(sidecarPath);
            if (!string.Equals(sidecar.GetString("Format"), TerrainFormatName, StringComparison.Ordinal)
                || !TryGetInt(sidecar, "FormatVersion", out int version)
                || version != TerrainFormatVersion)
            {
                return new TerrainSidecarResult(TerrainSidecarStatus.Invalid);
            }

            var terrain = sidecar.GetObject("TerrainEditData");
            if (terrain == null)
                return new TerrainSidecarResult(TerrainSidecarStatus.Invalid);

            ValidateTerrain(terrain);
            if (!TerrainMatchesBase(terrain, baseObject))
                return new TerrainSidecarResult(TerrainSidecarStatus.Invalid);

            return new TerrainSidecarResult(TerrainSidecarStatus.Ok, terrain);
        }
        catch (Exception)
        {
            return new TerrainSidecarResult(TerrainSidecarStatus.Invalid);
        }
    }

    /// <summary>Gets whether the base kind is one that can be imported as a base.</summary>
    internal static bool IsSupportedKind(JsonObject baseObject) =>
        BaseListLogic.Classify(baseObject) != BaseListLogic.BaseListKind.Other;

    /// <summary>Gets whether two bases have the same classified type.</summary>
    internal static bool IsSameKind(JsonObject first, JsonObject second) =>
        BaseListLogic.Classify(first) == BaseListLogic.Classify(second);

    /// <summary>
    /// Gets whether two bases have both the same classified kind and the same raw
    /// PersistentBaseTypes value. Used for the type-for-type replace rule.
    /// </summary>
    internal static bool IsSameType(JsonObject first, JsonObject second) =>
        IsSameKind(first, second)
        && string.Equals(
            BaseListLogic.GetTypeName(first),
            BaseListLogic.GetTypeName(second),
            StringComparison.OrdinalIgnoreCase);

    /// <summary>Gets a human-readable display name for a base entry.</summary>
    internal static string GetDisplayName(JsonObject baseObject)
    {
        string? name = baseObject.GetString("Name");
        if (!string.IsNullOrWhiteSpace(name))
            return name.Trim();

        string typeName = BaseListLogic.GetTypeName(baseObject);
        return string.IsNullOrWhiteSpace(typeName) ? "Base" : typeName;
    }

    /// <summary>
    /// Extracts the terrain edit buffers matching a base's galactic address.
    /// The returned object contains the parallel arrays for those buffers only.
    /// </summary>
    /// <param name="playerState">The PlayerStateData object.</param>
    /// <param name="baseObject">The base whose terrain should be extracted.</param>
    /// <returns>The sliced terrain object, or null when no buffers match.</returns>
    internal static JsonObject? ExtractTerrainForBase(JsonObject playerState, JsonObject baseObject)
    {
        string address = CoordinateHelper.NormalizeGalacticAddress(baseObject.Get("GalacticAddress"));
        if (string.IsNullOrEmpty(address))
            return null;

        var terrain = playerState.GetObject("TerrainEditData");
        if (terrain == null)
            return null;

        var addresses = terrain.GetArray("GalacticAddresses");
        var sizes = terrain.GetArray("BufferSizes");
        var edits = terrain.GetArray("Edits");
        if (addresses == null || sizes == null || edits == null)
            return null;
        if (addresses.Length != sizes.Length)
            throw new InvalidDataException("Terrain address and buffer-size arrays have different lengths.");

        var ages = terrain.GetArray("BufferAges");
        var anchors = terrain.GetArray("BufferAnchors");
        var protectedBuffers = terrain.GetArray("BufferProtected");
        ValidateOptionalLength(ages, addresses.Length, "BufferAges");
        ValidateOptionalLength(anchors, addresses.Length, "BufferAnchors");
        ValidateOptionalLength(protectedBuffers, addresses.Length, "BufferProtected");

        var offsets = BuildEditOffsets(sizes, edits.Length);

        var matches = new List<int>();
        for (int i = 0; i < addresses.Length; i++)
        {
            if (string.Equals(
                    CoordinateHelper.NormalizeGalacticAddress(addresses.Get(i)),
                    address,
                    StringComparison.Ordinal))
            {
                matches.Add(i);
            }
        }

        if (matches.Count == 0)
            return null;

        var result = new JsonObject();
        var resultAddresses = new JsonArray();
        var resultSizes = new JsonArray();
        var resultEdits = new JsonArray();
        JsonArray? resultAges = ages == null ? null : new JsonArray();
        JsonArray? resultAnchors = anchors == null ? null : new JsonArray();
        JsonArray? resultProtected = protectedBuffers == null ? null : new JsonArray();

        foreach (int index in matches)
        {
            resultAddresses.Add(addresses.Get(index));
            int count = sizes.GetInt(index);
            resultSizes.Add(count);
            for (int i = 0; i < count; i++)
                resultEdits.Add(edits.Get(offsets[index] + i));

            resultAges?.Add(ages!.Get(index));
            resultAnchors?.Add(anchors!.Get(index));
            resultProtected?.Add(protectedBuffers!.Get(index));
        }

        result.Set("GalacticAddresses", resultAddresses);
        result.Set("BufferSizes", resultSizes);
        result.Set("Edits", resultEdits);
        if (resultAges != null) result.Set("BufferAges", resultAges);
        if (resultAnchors != null) result.Set("BufferAnchors", resultAnchors);
        if (resultProtected != null) result.Set("BufferProtected", resultProtected);
        return result;
    }

    /// <summary>
    /// Appends the supplied terrain buffers to a save. Existing buffers for the
    /// base address are removed first when <paramref name="replaceExisting"/> is true.
    /// </summary>
    /// <param name="playerState">The PlayerStateData object.</param>
    /// <param name="baseObject">The base the terrain belongs to.</param>
    /// <param name="terrain">The exported terrain payload, or null.</param>
    /// <param name="replaceExisting">True to clear existing terrain at the base address first.</param>
    /// <returns>The number of terrain buffers appended.</returns>
    internal static int ApplyTerrainForBase(
        JsonObject playerState,
        JsonObject baseObject,
        JsonObject? terrain,
        bool replaceExisting)
    {
        if (terrain == null)
            return 0;

        ValidateTerrain(terrain);

        string address = CoordinateHelper.NormalizeGalacticAddress(baseObject.Get("GalacticAddress"));
        if (string.IsNullOrEmpty(address))
            throw new InvalidDataException("The imported base has no galactic address.");

        if (replaceExisting)
            RemoveTerrainAtAddress(playerState, address);

        var target = playerState.GetObject("TerrainEditData");
        if (target == null)
        {
            target = new JsonObject();
            playerState.Set("TerrainEditData", target);
        }

        var targetAddresses = EnsureArray(target, "GalacticAddresses");
        var targetSizes = EnsureArray(target, "BufferSizes");
        var targetEdits = EnsureArray(target, "Edits");

        var sourceAddresses = terrain.GetArray("GalacticAddresses")!;
        var sourceSizes = terrain.GetArray("BufferSizes")!;
        var sourceEdits = terrain.GetArray("Edits")!;
        var sourceAges = terrain.GetArray("BufferAges");
        var sourceAnchors = terrain.GetArray("BufferAnchors");
        var sourceProtected = terrain.GetArray("BufferProtected");
        var offsets = BuildEditOffsets(sourceSizes, sourceEdits.Length);

        int existingBuffers = targetAddresses.Length;
        var targetAges = target.GetArray("BufferAges");
        var targetAnchors = target.GetArray("BufferAnchors");
        var targetProtected = target.GetArray("BufferProtected");

        if (sourceAges != null)
        {
            targetAges ??= EnsureArray(target, "BufferAges", existingBuffers);
            if (targetAges.Length < existingBuffers)
                PadArray(targetAges, existingBuffers - targetAges.Length, 0);
        }
        if (sourceAnchors != null)
        {
            targetAnchors ??= EnsureArray(target, "BufferAnchors", existingBuffers);
            if (targetAnchors.Length < existingBuffers)
                PadArray(targetAnchors, existingBuffers - targetAnchors.Length, 0);
        }
        if (sourceProtected != null)
        {
            targetProtected ??= EnsureArray(target, "BufferProtected", existingBuffers);
            if (targetProtected.Length < existingBuffers)
                PadArray(targetProtected, existingBuffers - targetProtected.Length, 0);
        }

        for (int i = 0; i < sourceAddresses.Length; i++)
        {
            targetAddresses.Add(sourceAddresses.Get(i));
            int count = sourceSizes.GetInt(i);
            targetSizes.Add(count);
            for (int e = 0; e < count; e++)
                targetEdits.Add(sourceEdits.Get(offsets[i] + e));

            if (targetAges != null)
                targetAges.Add(sourceAges != null ? sourceAges.Get(i) : 0);
            if (targetAnchors != null)
                targetAnchors.Add(sourceAnchors != null ? sourceAnchors.Get(i) : 0);
            if (targetProtected != null)
                targetProtected.Add(sourceProtected != null ? sourceProtected.Get(i) : 0);
        }

        return sourceAddresses.Length;
    }

    /// <summary>
    /// Appends an imported base as a new entry, preserving its address, position,
    /// type, objects and name, and applying its terrain payload.
    /// </summary>
    /// <param name="playerState">The PlayerStateData object.</param>
    /// <param name="payload">The decoded export.</param>
    /// <returns>The new base index and applied terrain-buffer count.</returns>
    internal static ImportResult ImportAsNew(JsonObject playerState, ExportPayload payload)
    {
        ValidatePayloadTerrain(payload);

        var bases = playerState.GetArray("PersistentPlayerBases");
        if (bases == null)
        {
            bases = new JsonArray();
            playerState.Set("PersistentPlayerBases", bases);
        }

        var imported = payload.Base.DeepClone();
        int index = bases.Length;
        bases.Add(imported);

        int terrainBuffers = payload.TerrainEditData == null
            ? 0
            : ApplyTerrainForBase(playerState, imported, payload.TerrainEditData, replaceExisting: true);

        return new ImportResult(index, terrainBuffers);
    }

    /// <summary>
    /// Replaces an existing base with the imported entry. The caller is responsible
    /// for checking that the two bases have the same type.
    /// </summary>
    /// <param name="playerState">The PlayerStateData object.</param>
    /// <param name="targetIndex">The index of the selected base.</param>
    /// <param name="payload">The decoded export.</param>
    /// <returns>The replaced index and applied terrain-buffer count.</returns>
    internal static ImportResult ReplaceBase(JsonObject playerState, int targetIndex, ExportPayload payload)
    {
        ValidatePayloadTerrain(payload);

        var bases = playerState.GetArray("PersistentPlayerBases")
            ?? throw new InvalidDataException("This save has no PersistentPlayerBases array.");
        if (targetIndex < 0 || targetIndex >= bases.Length)
            throw new ArgumentOutOfRangeException(nameof(targetIndex));

        var oldBase = bases.GetObject(targetIndex)
            ?? throw new InvalidDataException("The selected base entry could not be read.");
        string oldAddress = CoordinateHelper.NormalizeGalacticAddress(oldBase.Get("GalacticAddress"));
        string newAddress = CoordinateHelper.NormalizeGalacticAddress(payload.Base.Get("GalacticAddress"));
        bool sameAddress = !string.IsNullOrEmpty(oldAddress)
            && string.Equals(oldAddress, newAddress, StringComparison.Ordinal);

        var imported = payload.Base.DeepClone();
        bases.Set(targetIndex, imported);

        if (!sameAddress)
        {
            // Moving the base means the old terrain no longer belongs to it.
            RemoveTerrainAtAddress(playerState, oldAddress);
        }

        int terrainBuffers = payload.TerrainEditData == null
            ? 0
            : ApplyTerrainForBase(playerState, imported, payload.TerrainEditData, replaceExisting: true);

        return new ImportResult(targetIndex, terrainBuffers);
    }

    /// <summary>Counts bases at an address, optionally excluding one array index.</summary>
    internal static int CountBasesAtAddress(JsonObject playerState, object? address, int excludeIndex = -1)
    {
        string normalized = CoordinateHelper.NormalizeGalacticAddress(address);
        if (string.IsNullOrEmpty(normalized))
            return 0;

        var bases = playerState.GetArray("PersistentPlayerBases");
        if (bases == null)
            return 0;

        int count = 0;
        for (int i = 0; i < bases.Length; i++)
        {
            if (i == excludeIndex)
                continue;
            JsonObject? baseObject;
            try
            {
                baseObject = bases.GetObject(i);
            }
            catch
            {
                continue;
            }
            if (baseObject == null)
                continue;
            if (string.Equals(
                    CoordinateHelper.NormalizeGalacticAddress(baseObject.Get("GalacticAddress")),
                    normalized,
                    StringComparison.Ordinal))
            {
                count++;
            }
        }
        return count;
    }

    /// <summary>Gets whether the save has terrain buffers for an address.</summary>
    internal static bool HasTerrainAtAddress(JsonObject playerState, object? address)
    {
        string normalized = CoordinateHelper.NormalizeGalacticAddress(address);
        if (string.IsNullOrEmpty(normalized))
            return false;

        var addresses = playerState.GetObject("TerrainEditData")?.GetArray("GalacticAddresses");
        if (addresses == null)
            return false;

        for (int i = 0; i < addresses.Length; i++)
        {
            if (string.Equals(
                    CoordinateHelper.NormalizeGalacticAddress(addresses.Get(i)),
                    normalized,
                    StringComparison.Ordinal))
            {
                return true;
            }
        }
        return false;
    }

    /// <summary>
    /// Gets whether applying the imported terrain could affect another base or
    /// existing terrain at the destination.
    /// </summary>
    /// <param name="playerState">The target PlayerStateData.</param>
    /// <param name="payload">The decoded export.</param>
    /// <param name="targetIndex">The selected replace target, or -1 for a new base.</param>
    internal static bool HasTerrainConflict(JsonObject playerState, ExportPayload payload, int targetIndex)
    {
        string importedAddress = CoordinateHelper.NormalizeGalacticAddress(
            payload.Base.Get("GalacticAddress"));

        JsonObject? target = targetIndex >= 0
            ? playerState.GetArray("PersistentPlayerBases")?.GetObject(targetIndex)
            : null;

        // Replacing a base at an address shared with another base can remove that
        // other base's terrain even when the legacy export carries no terrain payload.
        if (target != null)
        {
            string targetAddress = CoordinateHelper.NormalizeGalacticAddress(target.Get("GalacticAddress"));
            if (CountBasesAtAddress(playerState, targetAddress, targetIndex) > 0)
                return true;
        }

        if (payload.TerrainEditData == null || string.IsNullOrEmpty(importedAddress))
            return false;

        if (CountBasesAtAddress(playerState, importedAddress, targetIndex) > 0)
            return true;

        if (targetIndex < 0)
            return HasTerrainAtAddress(playerState, importedAddress);

        if (target == null)
            return HasTerrainAtAddress(playerState, importedAddress);

        string targetAddressForTerrain = CoordinateHelper.NormalizeGalacticAddress(target.Get("GalacticAddress"));
        return !string.Equals(targetAddressForTerrain, importedAddress, StringComparison.Ordinal)
               && HasTerrainAtAddress(playerState, importedAddress);
    }

    /// <summary>
    /// Gets whether the imported base's owner or game mode differs from a base
    /// already in the target save.
    /// </summary>
    internal static bool HasMetadataMismatch(JsonObject playerState, JsonObject imported, int targetIndex)
    {
        var bases = playerState.GetArray("PersistentPlayerBases");
        if (bases == null || bases.Length == 0)
            return false;

        JsonObject? reference = null;
        if (targetIndex >= 0 && targetIndex < bases.Length)
            reference = bases.GetObject(targetIndex);
        if (reference == null)
        {
            for (int i = 0; i < bases.Length; i++)
            {
                reference = bases.GetObject(i);
                if (reference != null)
                    break;
            }
        }
        if (reference == null)
            return false;

        string importedOwner = GetOwnerFingerprint(imported);
        string referenceOwner = GetOwnerFingerprint(reference);
        if (importedOwner.Length > 0 && referenceOwner.Length > 0
            && !string.Equals(importedOwner, referenceOwner, StringComparison.Ordinal))
        {
            return true;
        }

        return Differs(imported, reference, "GameMode")
            || Differs(imported, reference, "Difficulty");
    }

    private static bool Differs(JsonObject first, JsonObject second, string key)
    {
        string? a = first.GetString(key);
        string? b = second.GetString(key);
        return !string.IsNullOrWhiteSpace(a)
            && !string.IsNullOrWhiteSpace(b)
            && !string.Equals(a.Trim(), b.Trim(), StringComparison.OrdinalIgnoreCase);
    }

    private static string GetOwnerFingerprint(JsonObject baseObject)
    {
        var owner = baseObject.GetObject("Owner");
        return owner?.ToString() ?? "";
    }

    private static bool LooksLikeBase(JsonObject obj) =>
        obj.Contains("Objects")
        || obj.Contains("BaseType")
        || obj.Contains("BaseType.PersistentBaseTypes")
        || obj.Contains("GalacticAddress");

    private static bool TryGetInt(JsonObject obj, string key, out int value)
    {
        value = 0;
        object? raw = obj.Get(key);
        try
        {
            if (raw is RawDouble rawDouble)
            {
                value = (int)rawDouble.Value;
                return true;
            }
            if (raw is double d)
            {
                value = (int)d;
                return true;
            }
            if (raw is int i)
            {
                value = i;
                return true;
            }
            if (raw is long l)
            {
                value = (int)l;
                return true;
            }
            if (raw is string s)
                return int.TryParse(s, System.Globalization.NumberStyles.Integer,
                    System.Globalization.CultureInfo.InvariantCulture, out value);
        }
        catch
        {
        }
        return false;
    }

    private static int[] BuildEditOffsets(JsonArray sizes, int editCount)
    {
        var offsets = new int[sizes.Length];
        long total = 0;
        for (int i = 0; i < sizes.Length; i++)
        {
            offsets[i] = checked((int)total);
            int count = sizes.GetInt(i);
            if (count < 0)
                throw new InvalidDataException("Terrain buffer size is negative.");
            total += count;
        }
        // TerrainEditData.Edits is a fixed-capacity 30,000-entry pool; the
        // buffer size sum only covers the entries currently in use.
        // Unused entries at the end are zero and must not fail validation.
        if (total > editCount)
            throw new InvalidDataException("Terrain edit data is shorter than the buffer sizes require.");
        return offsets;
    }

    private static void ValidatePayloadTerrain(ExportPayload payload)
    {
        if (payload.TerrainEditData == null)
            return;
        ValidateTerrain(payload.TerrainEditData);
        if (string.IsNullOrEmpty(CoordinateHelper.NormalizeGalacticAddress(
                payload.Base.Get("GalacticAddress"))))
        {
            throw new InvalidDataException("The imported base has terrain but no galactic address.");
        }
    }

    private static bool TerrainMatchesBase(JsonObject terrain, JsonObject baseObject)
    {
        string baseAddress = CoordinateHelper.NormalizeGalacticAddress(baseObject.Get("GalacticAddress"));
        if (string.IsNullOrEmpty(baseAddress))
            return false;

        var addresses = terrain.GetArray("GalacticAddresses");
        if (addresses == null || addresses.Length == 0)
            return false;

        for (int i = 0; i < addresses.Length; i++)
        {
            if (!string.Equals(
                    CoordinateHelper.NormalizeGalacticAddress(addresses.Get(i)),
                    baseAddress,
                    StringComparison.Ordinal))
            {
                return false;
            }
        }
        return true;
    }

    private static void ValidateTerrain(JsonObject? terrain)
    {
        if (terrain == null)
            return;

        var addresses = terrain.GetArray("GalacticAddresses");
        var sizes = terrain.GetArray("BufferSizes");
        var edits = terrain.GetArray("Edits");
        if (addresses == null || sizes == null || edits == null)
            throw new InvalidDataException("Terrain edit data is missing required arrays.");
        if (addresses.Length != sizes.Length)
            throw new InvalidDataException("Terrain address and buffer-size arrays have different lengths.");

        ValidateOptionalLength(terrain.GetArray("BufferAges"), addresses.Length, "BufferAges");
        ValidateOptionalLength(terrain.GetArray("BufferAnchors"), addresses.Length, "BufferAnchors");
        ValidateOptionalLength(terrain.GetArray("BufferProtected"), addresses.Length, "BufferProtected");
        BuildEditOffsets(sizes, edits.Length);
    }

    private static void ValidateOptionalLength(JsonArray? array, int expected, string name)
    {
        if (array != null && array.Length != expected)
            throw new InvalidDataException($"Terrain {name} length does not match the buffer count.");
    }

    private static JsonArray EnsureArray(JsonObject parent, string name)
    {
        var array = parent.GetArray(name);
        if (array == null)
        {
            array = new JsonArray();
            parent.Set(name, array);
        }
        return array;
    }

    private static JsonArray EnsureArray(JsonObject parent, string name, int padTo)
    {
        var array = EnsureArray(parent, name);
        if (array.Length < padTo)
            PadArray(array, padTo - array.Length, 0);
        return array;
    }

    private static void PadArray(JsonArray array, int count, object? value)
    {
        for (int i = 0; i < count; i++)
            array.Add(value);
    }

    private static void RemoveTerrainAtAddress(JsonObject playerState, string address)
    {
        if (string.IsNullOrEmpty(address))
            return;
        var temp = new JsonObject();
        temp.Set("GalacticAddress", address);
        BaseLogic.ClearTerrainEdits(playerState, temp);
    }
}

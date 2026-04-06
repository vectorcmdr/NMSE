using System;
using System.IO;
using Avalonia.Media.Imaging;

namespace NMSE.Data;

/// <summary>NMS galactic coordinate conversion utilities.</summary>
public static class CoordinateHelper
{
    /// <summary>Convert voxel coordinates to a 12-character portal code.</summary>
    public static string VoxelToPortalCode(int voxelX, int voxelY, int voxelZ, int systemIndex, int planetIndex)
    {
        int x = ConvertVoxelForAddress(voxelX, 3);
        int y = ConvertVoxelForAddress(voxelY, 2);
        int z = ConvertVoxelForAddress(voxelZ, 3);
        return $"{planetIndex:X1}{systemIndex:X3}{y:X2}{z:X3}{x:X3}";
    }

    /// <summary>Convert voxel coordinates to signal booster format (XXXX:YYYY:ZZZZ:SSSS).</summary>
    public static string VoxelToSignalBooster(int voxelX, int voxelY, int voxelZ, int systemIndex)
    {
        int x = voxelX + GetShiftValue(3);
        int y = voxelY + GetShiftValue(2);
        int z = voxelZ + GetShiftValue(3);
        return $"{x:X4}:{y:X4}:{z:X4}:{systemIndex:X4}";
    }

    /// <summary>Compute straight-line distance to galaxy center in light-years.</summary>
    public static double GetDistanceToCenter(int voxelX, int voxelY, int voxelZ)
    {
        return Math.Sqrt(voxelX * voxelX + voxelY * voxelY + voxelZ * voxelZ) * 100.0;
    }

    /// <summary>Compute approximate number of jumps to reach the center at the given ly per jump.</summary>
    public static int GetJumpsToCenter(double distanceToCenter, double distancePerJump)
    {
        if (distancePerJump <= 0) return 0;
        return (int)Math.Ceiling(distanceToCenter / distancePerJump);
    }

    /// <summary>Default hyperdrive range used when calculation isn't available.</summary>
    public const double DefaultHyperdriveRange = 100.0;

    /// <summary>Seconds between space battles (game mechanic: 3 hours real-time).</summary>
    public const int SpaceBattleIntervalSeconds = 10800;

    /// <summary>Warps between space battles (game mechanic: every 5 warps).</summary>
    public const int SpaceBattleIntervalWarps = 5;

    /// <summary>Player state values.</summary>
    public static readonly string[] PlayerStates =
    {
        "OnFoot", "InShip", "InStation", "AboardFleet", "InNexus",
        "AbandonedFreighter", "InShipLanded", "InVehicle",
        "OnFootInCorvette", "OnFootInCorvetteLanded"
    };

    /// <summary>Localisation keys corresponding to PlayerStates, for display in combo boxes.</summary>
    public static readonly string[] PlayerStateLocKeys =
    {
        "player.state_on_foot", "player.state_in_ship", "player.state_in_station",
        "player.state_aboard_fleet", "player.state_in_nexus", "player.state_abandoned_freighter",
        "player.state_in_ship_landed", "player.state_in_vehicle",
        "player.state_on_foot_corvette", "player.state_on_foot_corvette_landed"
    };

    /// <summary>
    /// Convert a hex portal code string to a decimal string where each hex digit
    /// is converted to its decimal value + 1 (0->1, 1->2, ..., F->16), comma-separated.
    /// </summary>
    public static string PortalHexToDec(string portalCode)
    {
        if (string.IsNullOrEmpty(portalCode)) return "";
        var parts = new List<string>(portalCode.Length);
        foreach (char c in portalCode)
        {
            int val;
            if (c >= '0' && c <= '9') val = c - '0';
            else if (c >= 'A' && c <= 'F') val = c - 'A' + 10;
            else if (c >= 'a' && c <= 'f') val = c - 'a' + 10;
            else continue;
            parts.Add((val + 1).ToString());
        }
        return string.Join(",", parts);
    }

    private static int ConvertVoxelForAddress(int value, int byteLength)
    {
        int signValue = (int)Math.Pow(16, byteLength);
        int num = value % signValue;
        return num >= 0 ? num : num + signValue;
    }

    private static int GetShiftValue(int byteLength)
    {
        return (int)(0.5 * Math.Pow(16, byteLength) - 1);
    }

    /// <summary>
    /// Parse a 12-character portal code (hex) back into voxel coordinates.
    /// Format: {planetIndex:1}{systemIndex:3}{y:2}{z:3}{x:3}
    /// </summary>
    public static bool PortalCodeToVoxel(string portalCode, out int voxelX, out int voxelY, out int voxelZ, out int systemIndex, out int planetIndex)
    {
        voxelX = voxelY = voxelZ = systemIndex = planetIndex = 0;

        if (string.IsNullOrEmpty(portalCode) || portalCode.Length != 12)
            return false;

        foreach (char c in portalCode)
        {
            if (!((c >= '0' && c <= '9') || (c >= 'A' && c <= 'F') || (c >= 'a' && c <= 'f')))
                return false;
        }

        planetIndex = Convert.ToInt32(portalCode[..1], 16);
        systemIndex = Convert.ToInt32(portalCode[1..4], 16);
        int rawY = Convert.ToInt32(portalCode[4..6], 16);
        int rawZ = Convert.ToInt32(portalCode[6..9], 16);
        int rawX = Convert.ToInt32(portalCode[9..12], 16);

        voxelX = ConvertAddressToVoxel(rawX, 3);
        voxelY = ConvertAddressToVoxel(rawY, 2);
        voxelZ = ConvertAddressToVoxel(rawZ, 3);
        return true;
    }

    private static int ConvertAddressToVoxel(int address, int byteLength)
    {
        int signValue = (int)Math.Pow(16, byteLength);
        int halfSign = signValue / 2;
        return address >= halfSign ? address - signValue : address;
    }

    /// <summary>Glyph image cache, indexed by hex digit 0-F.</summary>
    private static readonly Dictionary<char, Bitmap?> _glyphCache = new();
    private static string? _glyphBasePath;

    /// <summary>Set the base path where glyph images (UI-GLYPH1.PNG etc.) are located.</summary>
    public static void SetGlyphBasePath(string basePath)
    {
        _glyphBasePath = basePath;
        _glyphCache.Clear();
    }

    /// <summary>Get the glyph image for a hex digit (0-9, A-F). Returns null if not found.</summary>
    public static Bitmap? GetGlyphImage(char hexDigit)
    {
        hexDigit = char.ToUpperInvariant(hexDigit);
        if (_glyphCache.TryGetValue(hexDigit, out var cached))
            return cached;

        Bitmap? img = LoadGlyphImage(hexDigit);
        _glyphCache[hexDigit] = img;
        return img;
    }

    private static Bitmap? LoadGlyphImage(char hexDigit)
    {
        if (string.IsNullOrEmpty(_glyphBasePath)) return null;

        int index = hexDigit >= '0' && hexDigit <= '9'
            ? hexDigit - '0' + 1
            : hexDigit >= 'A' && hexDigit <= 'F'
                ? hexDigit - 'A' + 11
                : -1;
        if (index < 1) return null;

        string path = Path.Combine(_glyphBasePath, $"UI-GLYPH{index}.PNG");
        if (!File.Exists(path)) return null;

        try { return new Bitmap(path); }
        catch { return null; }
    }
}

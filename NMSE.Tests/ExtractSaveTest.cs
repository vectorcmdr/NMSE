using NMSE.IO;
using NMSE.Models;
using System.Globalization;
using System.Text;
using Xunit;

namespace NMSE.Tests;

/// <summary>
/// Tests that binary data (e.g. procedural tech pack item IDs) is serialized
/// with raw high bytes (0x80-0xFF) in the Latin-1 output, matching the format
/// the NMS game engine writes.  Control characters (0x00-0x1F) are JSON-escaped.
/// </summary>
public class BinaryDataSerializationTests
{
    [Fact]
    public void BinaryData_HighBytes_AreRawLatin1()
    {
        // Bytes representing item ID "^8080E247A50D#42255"
        byte[] testData = { 0x5E, 0x80, 0x80, 0xE2, 0x47, 0xA5, 0x0D, 0x23, 0x34, 0x32, 0x32, 0x35, 0x35 };
        var binaryData = new BinaryData(testData);

        var obj = new JsonObject();
        obj.Add("Id", binaryData);

        string json = obj.ToString();

        // High bytes should NOT be escaped - they appear as raw Latin-1 characters
        Assert.DoesNotContain("\\u0080", json);
        Assert.DoesNotContain("\\u00E2", json);
        Assert.DoesNotContain("\\u00A5", json);
        // Control char 0x0D uses standard \r escape
        Assert.Contains("\\r", json, StringComparison.Ordinal);
        // Printable ASCII written directly
        Assert.Contains("G", json, StringComparison.Ordinal);
        Assert.Contains("#42255", json, StringComparison.Ordinal);

        // Latin-1 encoded output SHOULD contain raw high bytes
        byte[] latin1Bytes = Encoding.Latin1.GetBytes(json);
        bool hasHighByte = false;
        foreach (byte b in latin1Bytes)
            if (b >= 0x80) { hasHighByte = true; break; }
        Assert.True(hasHighByte, "Serialized BinaryData should contain raw high bytes in Latin-1 output");
    }

    [Fact]
    public void BinaryData_RoundTrip_PreservesBytes()
    {
        // Round-trip: BinaryData -> serialize -> parse -> re-serialize
        byte[] testData = { 0x5E, 0x80, 0x80, 0xE2, 0x47, 0xA5, 0x0D, 0x23, 0x34, 0x32, 0x32, 0x35, 0x35 };
        var binaryData = new BinaryData(testData);

        var obj = new JsonObject();
        obj.Add("Id", binaryData);

        string json1 = obj.ToString();
        var parsed = JsonObject.Parse(json1);
        string json2 = parsed.ToString();

        // Re-serialized JSON should be identical
        Assert.Equal(json1, json2);
    }

    [Fact]
    public void BinaryData_SaveFileRoundTrip_PreservesBinaryIds()
    {
        string saveFile = "/home/runner/work/NMSSE_CE_Dev/NMSSE_CE_Dev/_ref/saves/original/save.hg";
        if (!File.Exists(saveFile)) return;

        var saveData = SaveFileManager.LoadSaveFile(saveFile);
        string json = saveData.ToString();

        // The serialized output should contain raw high bytes (from BinaryData values)
        byte[] latin1Bytes = Encoding.Latin1.GetBytes(json);
        bool hasHighByte = false;
        for (int i = 0; i < latin1Bytes.Length; i++)
        {
            if (latin1Bytes[i] >= 0x80) { hasHighByte = true; break; }
        }
        Assert.True(hasHighByte, "Save file with tech pack IDs should produce raw high bytes in Latin-1 output");

        // Verify re-parse round-trip
        var reparsed = JsonObject.Parse(json);
        string json2 = reparsed.ToString();
        Assert.Equal(json, json2);
    }
}

using System.Text.Json;
using NMSE.Extractor.Data;

namespace NMSE.Extractor.Tests;

public class JsonWriterTests
{
    [Fact]
    public void SaveJson_CreatesFile()
    {
        string tempDir = Path.Combine(Path.GetTempPath(), Guid.NewGuid().ToString());
        try
        {
            var data = new List<Dictionary<string, object?>>
            {
                new() { ["Id"] = "TEST", ["Name"] = "Test Item" }
            };

            double size = JsonWriter.SaveJson(data, tempDir, "test.json");
            Assert.True(size > 0);

            string path = Path.Combine(tempDir, "test.json");
            Assert.True(File.Exists(path));

            string json = File.ReadAllText(path);
            using var doc = JsonDocument.Parse(json);
            Assert.Equal(JsonValueKind.Array, doc.RootElement.ValueKind);
            Assert.Equal(1, doc.RootElement.GetArrayLength());
            Assert.Equal("TEST", doc.RootElement[0].GetProperty("Id").GetString());
        }
        finally
        {
            if (Directory.Exists(tempDir))
                Directory.Delete(tempDir, true);
        }
    }

    [Fact]
    public void SaveJson_CreatesDirectoryIfNotExists()
    {
        string tempDir = Path.Combine(Path.GetTempPath(), Guid.NewGuid().ToString(), "sub");
        try
        {
            var data = new List<Dictionary<string, object?>> { new() { ["Id"] = "X" } };
            JsonWriter.SaveJson(data, tempDir, "output.json");
            Assert.True(File.Exists(Path.Combine(tempDir, "output.json")));
        }
        finally
        {
            string parent = Path.GetDirectoryName(tempDir)!;
            if (Directory.Exists(parent))
                Directory.Delete(parent, true);
        }
    }

    [Fact]
    public void SaveJson_UsesTabIndentation()
    {
        string tempDir = Path.Combine(Path.GetTempPath(), Guid.NewGuid().ToString());
        try
        {
            var data = new List<Dictionary<string, object?>>
            {
                new() { ["Id"] = "TEST" }
            };
            JsonWriter.SaveJson(data, tempDir, "test.json");
            string json = File.ReadAllText(Path.Combine(tempDir, "test.json"));
            // Should use tab indentation, not spaces
            Assert.Contains("\t\"Id\"", json);
            Assert.DoesNotContain("  \"Id\"", json);
        }
        finally
        {
            if (Directory.Exists(tempDir))
                Directory.Delete(tempDir, true);
        }
    }

    [Fact]
    public void SaveJson_DoubleZero_WrittenAsZeroPointZero()
    {
        string tempDir = Path.Combine(Path.GetTempPath(), Guid.NewGuid().ToString());
        try
        {
            var data = new List<Dictionary<string, object?>>
            {
                new() { ["Value"] = 0.0 }
            };
            JsonWriter.SaveJson(data, tempDir, "test.json");
            string json = File.ReadAllText(Path.Combine(tempDir, "test.json"));
            // Should write "0.0" not "0" for double zero
            Assert.Contains("0.0", json);
        }
        finally
        {
            if (Directory.Exists(tempDir))
                Directory.Delete(tempDir, true);
        }
    }

    [Fact]
    public void SaveJson_ScientificNotation_UsesLowercaseE()
    {
        string tempDir = Path.Combine(Path.GetTempPath(), Guid.NewGuid().ToString());
        try
        {
            var data = new List<Dictionary<string, object?>>
            {
                new() { ["Value"] = 5.12179504e-05 }
            };
            JsonWriter.SaveJson(data, tempDir, "test.json");
            string json = File.ReadAllText(Path.Combine(tempDir, "test.json"));
            // Should use lowercase 'e' for scientific notation
            Assert.Contains("e-05", json);
            Assert.DoesNotContain("E-05", json);
        }
        finally
        {
            if (Directory.Exists(tempDir))
                Directory.Delete(tempDir, true);
        }
    }

    [Fact]
    public void SaveJson_IntegerValues_RemainIntegers()
    {
        string tempDir = Path.Combine(Path.GetTempPath(), Guid.NewGuid().ToString());
        try
        {
            var data = new List<Dictionary<string, object?>>
            {
                new() { ["Count"] = 800 }
            };
            JsonWriter.SaveJson(data, tempDir, "test.json");
            string json = File.ReadAllText(Path.Combine(tempDir, "test.json"));
            // Integer 800 should be written as "800", not "800.0"
            Assert.Contains(": 800", json);
            Assert.DoesNotContain("800.0", json);
        }
        finally
        {
            if (Directory.Exists(tempDir))
                Directory.Delete(tempDir, true);
        }
    }

    [Fact]
    public void SaveJson_PropertyOrder_PreservedFromDictionary()
    {
        string tempDir = Path.Combine(Path.GetTempPath(), Guid.NewGuid().ToString());
        try
        {
            var data = new List<Dictionary<string, object?>>
            {
                new()
                {
                    ["Id"] = "TEST",
                    ["Icon"] = "TEST.png",
                    ["Name"] = "Test Item",
                    ["Value"] = 100
                }
            };
            JsonWriter.SaveJson(data, tempDir, "test.json");
            string json = File.ReadAllText(Path.Combine(tempDir, "test.json"));

            int idPos = json.IndexOf("\"Id\"", StringComparison.Ordinal);
            int iconPos = json.IndexOf("\"Icon\"", StringComparison.Ordinal);
            int namePos = json.IndexOf("\"Name\"", StringComparison.Ordinal);
            int valuePos = json.IndexOf("\"Value\"", StringComparison.Ordinal);

            // Properties should appear in dictionary insertion order
            Assert.True(idPos < iconPos);
            Assert.True(iconPos < namePos);
            Assert.True(namePos < valuePos);
        }
        finally
        {
            if (Directory.Exists(tempDir))
                Directory.Delete(tempDir, true);
        }
    }
}

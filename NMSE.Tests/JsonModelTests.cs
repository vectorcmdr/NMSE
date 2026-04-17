using System.Globalization;
using NMSE.Models;

namespace NMSE.Tests;

/// <summary>
/// Tests for the JSON model classes: JsonObject and JsonArray.
/// </summary>
public class JsonModelTests
{
    // --- JsonObject: Parse -------------------------------------------

    [Fact]
    public void JsonObject_Parse_EmptyObject()
    {
        var obj = JsonObject.Parse("{}");
        Assert.Equal(0, obj.Size());
    }

    [Fact]
    public void JsonObject_Parse_SimpleProperties()
    {
        var obj = JsonObject.Parse("""{"name":"test","value":42,"flag":true}""");
        Assert.Equal(3, obj.Size());
        Assert.Equal("test", obj.GetString("name"));
        Assert.Equal(42, obj.GetInt("value"));
        Assert.True(obj.GetBool("flag"));
    }

    [Fact]
    public void JsonObject_Parse_NestedObject()
    {
        var obj = JsonObject.Parse("""{"outer":{"inner":"deep"}}""");
        var outer = obj.GetObject("outer");
        Assert.NotNull(outer);
        Assert.Equal("deep", outer.GetString("inner"));
    }

    [Fact]
    public void JsonObject_Parse_NestedArray()
    {
        var obj = JsonObject.Parse("""{"items":[1,2,3]}""");
        var arr = obj.GetArray("items");
        Assert.NotNull(arr);
        Assert.Equal(3, arr.Length);
    }

    // --- JsonObject: Add ---------------------------------------------

    [Fact]
    public void JsonObject_Add_IncreasesSize()
    {
        var obj = new JsonObject();
        obj.Add("key1", "value1");
        obj.Add("key2", 42);
        Assert.Equal(2, obj.Size());
    }

    [Fact]
    public void JsonObject_Add_DuplicateKey_Throws()
    {
        var obj = new JsonObject();
        obj.Add("key", "value");
        Assert.Throws<InvalidOperationException>(() => obj.Add("key", "other"));
    }

    // --- JsonObject: Set ---------------------------------------------

    [Fact]
    public void JsonObject_Set_UpdatesExistingValue()
    {
        var obj = new JsonObject();
        obj.Add("key", "original");
        obj.Set("key", "updated");
        Assert.Equal("updated", obj.GetString("key"));
        Assert.Equal(1, obj.Size());
    }

    [Fact]
    public void JsonObject_Set_AddsNewKeyIfNotPresent()
    {
        var obj = new JsonObject();
        obj.Set("newKey", "newValue");
        Assert.Equal(1, obj.Size());
        Assert.Equal("newValue", obj.GetString("newKey"));
    }

    // --- JsonObject: Get ---------------------------------------------

    [Fact]
    public void JsonObject_Get_ReturnsNullForMissingKey()
    {
        var obj = new JsonObject();
        Assert.Null(obj.Get("nonexistent"));
    }

    [Fact]
    public void JsonObject_GetString_ReturnsStringValue()
    {
        var obj = JsonObject.Parse("""{"s":"hello"}""");
        Assert.Equal("hello", obj.GetString("s"));
    }

    [Fact]
    public void JsonObject_GetInt_ReturnsIntegerValue()
    {
        var obj = JsonObject.Parse("""{"n":123}""");
        Assert.Equal(123, obj.GetInt("n"));
    }

    [Fact]
    public void JsonObject_GetBool_ReturnsBooleanValue()
    {
        var obj = JsonObject.Parse("""{"t":true,"f":false}""");
        Assert.True(obj.GetBool("t"));
        Assert.False(obj.GetBool("f"));
    }

    [Fact]
    public void JsonObject_GetDouble_ReturnsDoubleValue()
    {
        var obj = JsonObject.Parse("""{"d":3.14}""");
        Assert.Equal(3.14, obj.GetDouble("d"), 2);
    }

    [Fact]
    public void JsonObject_GetFloat_PreservesFullDoublePrecision()
    {
        // GetFloat must return full double precision, not truncate to ~7 digit float
        var obj = JsonObject.Parse("""{"FloatValue":1234567.890123456}""");
        double result = obj.GetFloat("FloatValue");
        // float precision would give ~1234567.875 or similar (only ~7 significant digits)
        // double precision preserves all digits
        Assert.Equal(1234567.890123456, result, 6);
    }

    [Fact]
    public void JsonObject_GetFloat_LargeValue_NoRounding()
    {
        // Regression: GetFloat used to cast through (float), losing precision
        var obj = JsonObject.Parse("""{"FloatValue":9876543.210987654}""");
        double result = obj.GetFloat("FloatValue");
        Assert.True(Math.Abs(result - 9876543.210987654) < 0.001,
            $"GetFloat returned {result}, expected ~9876543.210987654 (precision loss detected)");
    }

    // --- Float type rejection -----------------------------------------

    [Fact]
    public void JsonArray_ValidateType_RejectsFloat()
    {
        // After the float-to-double migration, float values must not be stored in the JSON model.
        // Callers should cast to double before adding.
        var arr = new JsonArray();
        Assert.Throws<ArgumentException>(() => arr.Add(1.5f));
    }

    [Fact]
    public void Double_PrecisionRoundTrip_ThroughJsonModel()
    {
        // Verify a high-precision double value (double precision 64-bit) round trips
		// without precision loss through parse (in) -> model (x) -> serialize (out).
        const double original = 1234567.890123456;
        var json = $$"""{"Value":{{original.ToString("R", System.Globalization.CultureInfo.InvariantCulture)}}}""";
        var obj = JsonObject.Parse(json);
        double retrieved = obj.GetDouble("Value");
        Assert.Equal(original, retrieved);

        // Serialize back and re-parse to confirm full round-trip
        string serialized = obj.ToString();
        var obj2 = JsonObject.Parse(serialized);
        Assert.Equal(original, obj2.GetDouble("Value"));
    }

    // --- JsonObject: Contains ----------------------------------------

    [Fact]
    public void JsonObject_Contains_ReturnsTrueForExistingKey()
    {
        var obj = new JsonObject();
        obj.Add("key", "value");
        Assert.True(obj.Contains("key"));
    }

    [Fact]
    public void JsonObject_Contains_ReturnsFalseForMissingKey()
    {
        var obj = new JsonObject();
        Assert.False(obj.Contains("missing"));
    }

    // --- JsonObject: Remove ------------------------------------------

    [Fact]
    public void JsonObject_Remove_DecreasesSize()
    {
        var obj = new JsonObject();
        obj.Add("a", 1);
        obj.Add("b", 2);
        obj.Add("c", 3);
        obj.Remove("b");
        Assert.Equal(2, obj.Size());
        Assert.False(obj.Contains("b"));
        Assert.True(obj.Contains("a"));
        Assert.True(obj.Contains("c"));
    }

    [Fact]
    public void JsonObject_Remove_NonexistentKey_DoesNothing()
    {
        var obj = new JsonObject();
        obj.Add("key", "value");
        obj.Remove("nonexistent");
        Assert.Equal(1, obj.Size());
    }

    // --- JsonObject: DeepClone ---------------------------------------

    [Fact]
    public void JsonObject_DeepClone_CreatesSeparateCopy()
    {
        var obj = new JsonObject();
        obj.Add("name", "original");
        obj.Add("nested", JsonObject.Parse("""{"x":1}"""));

        var clone = obj.DeepClone();
        clone.Set("name", "modified");

        Assert.Equal("original", obj.GetString("name"));
        Assert.Equal("modified", clone.GetString("name"));
    }

    [Fact]
    public void JsonObject_DeepClone_PreservesAllProperties()
    {
        var obj = JsonObject.Parse("""{"a":"str","b":42,"c":true,"d":null,"e":[1,2]}""");
        var clone = obj.DeepClone();

        Assert.Equal(obj.Size(), clone.Size());
        Assert.Equal("str", clone.GetString("a"));
        Assert.Equal(42, clone.GetInt("b"));
        Assert.True(clone.GetBool("c"));
        Assert.Null(clone.Get("d"));
        Assert.Equal(2, clone.GetArray("e")!.Length);
    }

    // --- JsonObject: Names -------------------------------------------

    [Fact]
    public void JsonObject_Names_ReturnsAllPropertyNames()
    {
        var obj = new JsonObject();
        obj.Add("alpha", 1);
        obj.Add("beta", 2);
        var names = obj.Names();
        Assert.Equal(2, names.Count);
        Assert.Equal("alpha", names[0]);
        Assert.Equal("beta", names[1]);
    }

    // --- JsonObject: Path-based access -------------------------------

    [Fact]
    public void JsonObject_GetValue_DottedPath()
    {
        var obj = JsonObject.Parse("""{"player":{"health":100,"name":"Test"}}""");
        Assert.Equal(100, Convert.ToInt32(obj.GetValue("player.health"), CultureInfo.InvariantCulture));
        Assert.Equal("Test", obj.GetValue("player.name") as string);
    }

    [Fact]
    public void JsonObject_GetValue_ArrayIndexPath()
    {
        var obj = JsonObject.Parse("""{"items":[10,20,30]}""");
        Assert.Equal(30, Convert.ToInt32(obj.GetValue("items[2]"), CultureInfo.InvariantCulture));
    }

    // --- JsonArray: Add / Length --------------------------------------

    [Fact]
    public void JsonArray_Add_IncreasesLength()
    {
        var arr = new JsonArray();
        arr.Add("first");
        arr.Add(42);
        arr.Add(true);
        Assert.Equal(3, arr.Length);
    }

    // --- JsonArray: Get ----------------------------------------------

    [Fact]
    public void JsonArray_Get_ReturnsCorrectValues()
    {
        var arr = new JsonArray();
        arr.Add("hello");
        arr.Add(99);
        Assert.Equal("hello", arr.GetString(0));
        Assert.Equal(99, arr.GetInt(1));
    }

    [Fact]
    public void JsonArray_Get_OutOfRange_Throws()
    {
        var arr = new JsonArray();
        arr.Add("only");
        Assert.Throws<IndexOutOfRangeException>(() => arr.Get(-1));
        Assert.Throws<IndexOutOfRangeException>(() => arr.Get(1));
    }

    // --- JsonArray: Set ----------------------------------------------

    [Fact]
    public void JsonArray_Set_ReplacesValue()
    {
        var arr = new JsonArray();
        arr.Add("original");
        arr.Set(0, "replaced");
        Assert.Equal("replaced", arr.GetString(0));
        Assert.Equal(1, arr.Length);
    }

    [Fact]
    public void JsonArray_Set_OutOfRange_Throws()
    {
        var arr = new JsonArray();
        Assert.Throws<IndexOutOfRangeException>(() => arr.Set(0, "value"));
    }

    // --- JsonArray: RemoveAt -----------------------------------------

    [Fact]
    public void JsonArray_RemoveAt_DecreasesLength()
    {
        var arr = new JsonArray();
        arr.Add("a");
        arr.Add("b");
        arr.Add("c");
        arr.RemoveAt(1);
        Assert.Equal(2, arr.Length);
        Assert.Equal("a", arr.GetString(0));
        Assert.Equal("c", arr.GetString(1));
    }

    [Fact]
    public void JsonArray_RemoveAt_OutOfRange_Throws()
    {
        var arr = new JsonArray();
        Assert.Throws<IndexOutOfRangeException>(() => arr.RemoveAt(0));
    }

    // --- JsonArray: Clear --------------------------------------------

    [Fact]
    public void JsonArray_Clear_SetsLengthToZero()
    {
        var arr = new JsonArray();
        arr.Add(1);
        arr.Add(2);
        arr.Add(3);
        arr.Clear();
        Assert.Equal(0, arr.Length);
    }

    // --- JsonArray: IndexOf ------------------------------------------

    [Fact]
    public void JsonArray_IndexOf_FindsExistingValue()
    {
        var arr = new JsonArray();
        arr.Add("alpha");
        arr.Add("beta");
        arr.Add("gamma");
        Assert.Equal(1, arr.IndexOf("beta"));
    }

    [Fact]
    public void JsonArray_IndexOf_ReturnsNegativeOneForMissing()
    {
        var arr = new JsonArray();
        arr.Add("alpha");
        Assert.Equal(-1, arr.IndexOf("missing"));
    }

    // --- JsonArray: DeepClone ----------------------------------------

    [Fact]
    public void JsonArray_DeepClone_CreatesSeparateCopy()
    {
        var arr = new JsonArray();
        arr.Add("original");
        arr.Add(JsonObject.Parse("""{"x":1}"""));

        var clone = arr.DeepClone();
        clone.Set(0, "modified");

        Assert.Equal("original", arr.GetString(0));
        Assert.Equal("modified", clone.GetString(0));
    }

    [Fact]
    public void JsonArray_DeepClone_PreservesLength()
    {
        var arr = new JsonArray();
        arr.Add(1);
        arr.Add(2);
        arr.Add(3);

        var clone = arr.DeepClone();
        Assert.Equal(arr.Length, clone.Length);
        Assert.Equal(1, clone.GetInt(0));
        Assert.Equal(2, clone.GetInt(1));
        Assert.Equal(3, clone.GetInt(2));
    }

    // --- JsonArray: Insert -------------------------------------------

    [Fact]
    public void JsonArray_Insert_ShiftsElements()
    {
        var arr = new JsonArray();
        arr.Add("a");
        arr.Add("c");
        arr.Insert(1, "b");
        Assert.Equal(3, arr.Length);
        Assert.Equal("a", arr.GetString(0));
        Assert.Equal("b", arr.GetString(1));
        Assert.Equal("c", arr.GetString(2));
    }

    // --- JsonArray: Type-safe getters --------------------------------

    [Fact]
    public void JsonArray_TypedGetters_ReturnCorrectTypes()
    {
        var arr = new JsonArray();
        arr.Add(42L);
        arr.Add(3.14);
        arr.Add(true);

        Assert.Equal(42, arr.GetInt(0));
        Assert.Equal(42L, arr.GetLong(0));
        Assert.Equal(3.14, arr.GetDouble(1), 2);
        Assert.True(arr.GetBool(2));
    }

    // --- JsonObject+JsonArray: Nested structures ---------------------

    [Fact]
    public void JsonObject_Parse_ComplexNestedStructure()
    {
        string json = """
        {
            "name": "TestSave",
            "version": 2,
            "players": [
                {"id": 1, "name": "Alice"},
                {"id": 2, "name": "Bob"}
            ],
            "settings": {
                "difficulty": "Normal",
                "mods": ["modA", "modB"]
            }
        }
        """;
        var obj = JsonObject.Parse(json);
        Assert.Equal("TestSave", obj.GetString("name"));
        Assert.Equal(2, obj.GetInt("version"));

        var players = obj.GetArray("players");
        Assert.NotNull(players);
        Assert.Equal(2, players.Length);
        Assert.Equal("Alice", players.GetObject(0).GetString("name"));
        Assert.Equal(2, players.GetObject(1).GetInt("id"));

        var settings = obj.GetObject("settings");
        Assert.NotNull(settings);
        Assert.Equal("Normal", settings.GetString("difficulty"));
        Assert.Equal(2, settings.GetArray("mods")!.Length);
    }

    // --- JsonObject: Null values -------------------------------------

    [Fact]
    public void JsonObject_Parse_NullValue()
    {
        var obj = JsonObject.Parse("""{"key":null}""");
        Assert.True(obj.Contains("key"));
        Assert.Null(obj.Get("key"));
    }

    [Fact]
    public void JsonObject_Add_NullValue()
    {
        var obj = new JsonObject();
        obj.Add("nullable", null);
        Assert.Equal(1, obj.Size());
        Assert.Null(obj.Get("nullable"));
    }

    // --- CultureInfo safety ---

    [Fact]
    public void JsonObject_GetDouble_CultureInvariant_WhenStringValue()
    {
        // Simulate a value stored as a string (not RawDouble) with a period decimal
        var prevCulture = System.Threading.Thread.CurrentThread.CurrentCulture;
        try
        {
            // Set a comma-decimal culture (e.g. German)
            System.Threading.Thread.CurrentThread.CurrentCulture = new System.Globalization.CultureInfo("de-DE");

            var obj = new JsonObject();
            obj.Add("val", "1.5"); // stored as string, not RawDouble
            double result = obj.GetDouble("val");
            Assert.Equal(1.5, result);
        }
        finally
        {
            System.Threading.Thread.CurrentThread.CurrentCulture = prevCulture;
        }
    }

    [Fact]
    public void JsonObject_GetDecimal_CultureInvariant_WhenStringValue()
    {
        var prevCulture = System.Threading.Thread.CurrentThread.CurrentCulture;
        try
        {
            System.Threading.Thread.CurrentThread.CurrentCulture = new System.Globalization.CultureInfo("fr-FR");

            var obj = new JsonObject();
            obj.Add("val", "2.75");
            decimal result = obj.GetDecimal("val");
            Assert.Equal(2.75m, result);
        }
        finally
        {
            System.Threading.Thread.CurrentThread.CurrentCulture = prevCulture;
        }
    }

    [Fact]
    public void JsonArray_GetDouble_CultureInvariant_WhenStringValue()
    {
        var prevCulture = System.Threading.Thread.CurrentThread.CurrentCulture;
        try
        {
            System.Threading.Thread.CurrentThread.CurrentCulture = new System.Globalization.CultureInfo("de-DE");

            var arr = new JsonArray();
            arr.Add("3.14"); // stored as string
            double result = arr.GetDouble(0);
            Assert.Equal(3.14, result);
        }
        finally
        {
            System.Threading.Thread.CurrentThread.CurrentCulture = prevCulture;
        }
    }

    [Fact]
    public void JsonArray_GetInt_CultureInvariant_WhenStringValue()
    {
        var prevCulture = System.Threading.Thread.CurrentThread.CurrentCulture;
        try
        {
            System.Threading.Thread.CurrentThread.CurrentCulture = new System.Globalization.CultureInfo("de-DE");

            var arr = new JsonArray();
            arr.Add("42");
            int result = arr.GetInt(0);
            Assert.Equal(42, result);
        }
        finally
        {
            System.Threading.Thread.CurrentThread.CurrentCulture = prevCulture;
        }
    }

    [Fact]
    public void JsonArray_FishCatchValue_RoundTrip_PreservesPrecision()
    {
        // Simulates the fish catch data flow: parse JSON -> read double -> format to string -> parse back -> serialize
        string json = """{"LargestCatchList":[0.0,1234567.890123456,42.5]}""";
        var obj = JsonObject.Parse(json);
        var arr = obj.GetArray("LargestCatchList")!;

        // Step 1: read large double from array (simulates LoadKnownFish)
        double largestCatch = arr.GetDouble(1);
        Assert.Equal(1234567.890123456, largestCatch, 6);

        // Step 2: convert to string for display (simulates grid cell value)
        string displayed = largestCatch.ToString(System.Globalization.CultureInfo.InvariantCulture);

        // Step 3: parse back from display string (simulates SaveKnownFish)
        Assert.True(double.TryParse(displayed, System.Globalization.NumberStyles.Float,
            System.Globalization.CultureInfo.InvariantCulture, out double parsed));
        Assert.Equal(largestCatch, parsed);

        // Step 4: write back to array
        arr.Set(1, parsed);

        // Step 5: serialize and verify full precision is preserved
        string serialized = JsonParser.Serialize(obj, false);
        Assert.Contains("1234567.890123456", serialized);
    }

    [Fact]
    public void JsonArray_ValidateType_AcceptsRawDouble()
    {
        // RawDouble must be accepted by the public Set/Add API for callers
        // that need to preserve original JSON text (e.g. CataloguePanel save).
        var arr = new JsonArray();
        var rd = new RawDouble(3.14, "3.14");
        arr.Add(rd);
        Assert.Equal(3.14, arr.GetDouble(0));

        // Serialization should use the preserved text
        string json = JsonParser.Serialize(arr, false);
        Assert.Contains("3.14", json);
    }

    [Fact]
    public void JsonArray_GetDoubleText_PreservesRawDoubleText()
    {
        // GetDoubleText should return the original JSON text for RawDouble values,
        // not a re-formatted version that might differ at trailing digits.
        string json = """[0.30000001192092898,14.361230850219727,0.0,42]""";
        var arr = JsonArray.Parse(json);

        // RawDouble values should return their exact original text
        Assert.Equal("0.30000001192092898", arr.GetDoubleText(0));
        Assert.Equal("14.361230850219727", arr.GetDoubleText(1));
        Assert.Equal("0.0", arr.GetDoubleText(2));
    }

    [Fact]
    public void JsonObject_GetDoubleText_PreservesRawDoubleText()
    {
        string json = """{"Scale":0.30000001192092898,"Trust":14.361230850219727}""";
        var obj = JsonObject.Parse(json);

        Assert.Equal("0.30000001192092898", obj.GetDoubleText("Scale"));
        Assert.Equal("14.361230850219727", obj.GetDoubleText("Trust"));
    }

    [Fact]
    public void FishCatchList_UnmodifiedValues_PreserveExactText()
    {
        // Simulates the FIXED CataloguePanel flow where unmodified values
        // are NOT written back, preserving the original RawDouble text.
        string json = """{"LargestCatchList":[0.30000001192092898,14.361230850219727,0.6000000238418579]}""";
        var obj = JsonObject.Parse(json);
        var arr = obj.GetArray("LargestCatchList")!;

        // LoadKnownFish: use GetDoubleText for display
        string displayed0 = arr.GetDoubleText(0);
        string displayed1 = arr.GetDoubleText(1);
        string displayed2 = arr.GetDoubleText(2);

        // SaveKnownFish: compare cell text to original - all unchanged
        Assert.Equal(displayed0, arr.GetDoubleText(0)); // no write needed
        Assert.Equal(displayed1, arr.GetDoubleText(1)); // no write needed
        Assert.Equal(displayed2, arr.GetDoubleText(2)); // no write needed

        // Serialize and verify exact original text is preserved
        string serialized = JsonParser.Serialize(obj, false);
        Assert.Contains("0.30000001192092898", serialized);
        Assert.Contains("14.361230850219727", serialized);
        Assert.Contains("0.6000000238418579", serialized);
    }

    [Fact]
    public void FishCatchList_ModifiedValue_UsesRawDoubleForNewValue()
    {
        // When a user edits a value, the new value should be stored as RawDouble
        // with round-trip format text to ensure consistent serialization.
        string json = """{"LargestCatchList":[0.30000001192092898,14.361230850219727]}""";
        var obj = JsonObject.Parse(json);
        var arr = obj.GetArray("LargestCatchList")!;

        // User edits value at index 1 to "25.5"
        string cellText = "25.5";
        string existingText = arr.GetDoubleText(1);
        Assert.NotEqual(cellText, existingText); // value was modified

        // Parse and store as RawDouble (the fixed save path)
        Assert.True(double.TryParse(cellText,
            System.Globalization.NumberStyles.Float,
            System.Globalization.CultureInfo.InvariantCulture, out double lc));
        string newText = lc.ToString("R", System.Globalization.CultureInfo.InvariantCulture);
        arr.Set(1, new RawDouble(lc, newText));

        // Verify: unmodified value at index 0 still has original text
        string serialized = JsonParser.Serialize(obj, false);
        Assert.Contains("0.30000001192092898", serialized);
        Assert.Contains("25.5", serialized);
    }

    [Fact]
    public void RawDouble_PreservationPattern_StatHelper()
    {
        // Tests the RawDouble preservation pattern used across the codebase:
        // only overwrite when the numeric value has actually changed.
        string json = """{"Value":0.30000001192092898}""";
        var obj = JsonObject.Parse(json);

        var existing = obj.Get("Value");
        Assert.IsType<RawDouble>(existing);
        var rd = (RawDouble)existing;
        Assert.Equal("0.30000001192092898", rd.Text);

        // Simulate: user edit produces same numeric value
        double userValue = rd.Value;
        Assert.True(existing is RawDouble rdCheck && rdCheck.Value == userValue);
        // Don't write back - preserve RawDouble

        // Verify original text is preserved in serialization
        string serialized = JsonParser.Serialize(obj, false);
        Assert.Contains("0.30000001192092898", serialized);
    }
}

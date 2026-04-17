using System.Globalization;
using System.Text.RegularExpressions;

namespace NMSE.Models;

/// <summary>
/// Represents a mutable JSON object with ordered key-value pairs.
/// Supports path-based access, transforms, deep cloning, and serialization.
/// </summary>
public partial class JsonObject
{
    private const int InitialCapacity = 4;
    private const int IndexThreshold = 8; // Build index when Length exceeds this
    private string[] _names;
    private object?[] _values;
    private Dictionary<string, int>? _index; // Lazy O(1) key->position lookup

    /// <summary>The number of key-value pairs in this object.</summary>
    public int Length { get; private set; }

    /// <summary>The parent container (a <see cref="JsonObject"/> or <see cref="JsonArray"/>) that holds this object, or <c>null</c> for root objects.</summary>
    internal object? Parent { get; set; }

    /// <summary>An optional listener that is notified when properties change on this object.</summary>
    public IPropertyChangeListener? Listener { get; set; }
    private Dictionary<string, Func<object?, object?>>? _transforms; // Lazy - only root uses transforms

    /// <summary>
    /// The name mapper used to translate between obfuscated and human-readable keys.
    /// Set on the root object during parsing if the save uses obfuscated names.
    /// Used during serialization to reverse-map names back to obfuscated form.
    /// </summary>
    public NMSE.Data.JsonNameMapper? NameMapper { get; set; }

    [GeneratedRegex(@"([^\.\[\]]+)|(?:\.([^\.\[\]]+))|(?:\[(\d+)\])")]
    private static partial Regex PathPattern();

    /// <summary>
    /// Initializes a new empty JSON object.
    /// </summary>
    public JsonObject()
    {
        _names = new string[InitialCapacity];
        _values = new object?[InitialCapacity];
        Length = 0;
    }

    /// <summary>
    /// Parses a JSON string into a <see cref="JsonObject"/>.
    /// </summary>
    /// <param name="json">The JSON string to parse.</param>
    /// <returns>The parsed JSON object.</returns>
    public static JsonObject Parse(string json) => JsonParser.ParseObject(json);

    /// <summary>
    /// Registers a path transform function that dynamically resolves property paths at access time.
    /// </summary>
    /// <param name="key">The path prefix to intercept.</param>
    /// <param name="transform">A function that receives the root object and returns the resolved path segment.</param>
    public void RegisterTransform(string key, Func<object?, object?> transform)
    {
        _transforms ??= new();
        _transforms[key] = transform;
    }

    /// <summary>
    /// Adds a new key-value pair to this object. Throws if the key already exists.
    /// </summary>
    /// <param name="name">The property name to add.</param>
    /// <param name="value">The value to associate with the name.</param>
    public void Add(string name, object? value)
    {
        if (_index != null)
        {
            if (_index.ContainsKey(name))
                throw new InvalidOperationException($"Duplicate key: {name}");
        }
        else
        {
            for (int i = 0; i < Length; i++)
                if (_names[i] == name) throw new InvalidOperationException($"Duplicate key: {name}");
        }

        EnsureCapacity(Length + 1);
        _names[Length] = name;
        _values[Length] = value;
        SetParent(value, this);
        if (_index != null)
            _index[name] = Length;
        Length++;

        // Build index once we exceed threshold
        if (_index == null && Length > IndexThreshold)
            BuildIndex();
    }

    /// <summary>
    /// Fast add without duplicate checking - for use by the parser only.
    /// The parser guarantees unique keys within each object during parsing,
    /// so the O(n) duplicate scan is unnecessary overhead.
    /// </summary>
    internal void AddUnchecked(string name, object? value)
    {
        EnsureCapacity(Length + 1);
        _names[Length] = name;
        _values[Length] = value;
        if (value is JsonObject obj) obj.Parent = this;
        else if (value is JsonArray arr) arr.Parent = this;
        Length++;
    }

    /// <summary>
    /// Re-maps existing keys using the given mapper.
    /// Called during parsing when obfuscated keys are detected after some
    /// non-obfuscated keys have already been added (e.g., PS4 saves that
    /// start with "Version" followed by obfuscated keys).
    /// </summary>
    internal void RemapKeys(NMSE.Data.JsonNameMapper mapper)
    {
        for (int i = 0; i < Length; i++)
        {
            if (mapper.IsObfuscatedKey(_names[i]))
                _names[i] = mapper.ToName(_names[i]);
        }
        _index = null; // Invalidate the index so it's rebuilt on next access
    }

    private void BuildIndex()
    {
        _index = new Dictionary<string, int>(Length, StringComparer.Ordinal);
        for (int i = 0; i < Length; i++)
            _index[_names[i]] = i;
    }

    private void EnsureCapacity(int needed)
    {
        if (_values.Length >= needed) return;
        int newSize = Math.Max(needed, _values.Length + (_values.Length >> 1));
        var newNames = new string[newSize];
        var newValues = new object?[newSize];
        Array.Copy(_names, newNames, Length);
        Array.Copy(_values, newValues, Length);
        _names = newNames;
        _values = newValues;
    }

    /// <summary>
    /// Returns the number of key-value pairs in this object.
    /// </summary>
    /// <returns>The count of entries.</returns>
    public int Size() => Length;

    /// <summary>
    /// Returns a snapshot of all property names in insertion order.
    /// </summary>
    /// <returns>A read-only list of the property names.</returns>
    public IReadOnlyList<string> Names()
    {
        var result = new string[Length];
        Array.Copy(_names, result, Length);
        return result;
    }

    /// <summary>
    /// Determines whether this object contains a property with the specified name.
    /// </summary>
    /// <param name="name">The property name to look up.</param>
    /// <returns><c>true</c> if the name exists; otherwise <c>false</c>.</returns>
    public bool Contains(string name)
    {
        if (_index != null)
            return _index.ContainsKey(name);
        for (int i = 0; i < Length; i++)
            if (_names[i] == name) return true;
        return false;
    }

    /// <summary>
    /// Gets the raw value associated with the specified name, without path resolution or transforms.
    /// </summary>
    /// <param name="name">The property name to look up.</param>
    /// <returns>The value, or <c>null</c> if the name is not found.</returns>
    public object? Get(string name)
    {
        if (_index != null)
            return _index.TryGetValue(name, out int idx) ? _values[idx] : null;
        for (int i = 0; i < Length; i++)
            if (_names[i] == name) return _values[i];
        return null;
    }

    /// <summary>
    /// Sets the value for the specified name. If the name already exists, updates in place; otherwise adds a new entry.
    /// </summary>
    /// <param name="name">The property name to set.</param>
    /// <param name="value">The new value to assign.</param>
    public void Set(string name, object? value)
    {
        if (_index != null)
        {
            if (_index.TryGetValue(name, out int idx))
            {
                var old = _values[idx];
                ClearParent(old);
                _values[idx] = value;
                SetParent(value, this);
                return;
            }
        }
        else
        {
            for (int i = 0; i < Length; i++)
            {
                if (_names[i] == name)
                {
                    var old = _values[i];
                    ClearParent(old);
                    _values[i] = value;
                    SetParent(value, this);
                    return;
                }
            }
        }
        Add(name, value);
    }

    /// <summary>
    /// Removes the property with the specified name. Does nothing if the name is not found.
    /// </summary>
    /// <param name="name">The property name to remove.</param>
    public void Remove(string name)
    {
        int removeIdx = -1;
        if (_index != null)
        {
            if (!_index.TryGetValue(name, out removeIdx))
                return;
        }
        else
        {
            for (int i = 0; i < Length; i++)
            {
                if (_names[i] == name) { removeIdx = i; break; }
            }
            if (removeIdx < 0) return;
        }

        ClearParent(_values[removeIdx]);
        Array.Copy(_names, removeIdx + 1, _names, removeIdx, Length - removeIdx - 1);
        Array.Copy(_values, removeIdx + 1, _values, removeIdx, Length - removeIdx - 1);
        _names[--Length] = null!;
        _values[Length] = null;

        // Rebuild index after removal (positions shifted)
        if (_index != null)
            BuildIndex();
    }

    /// <summary>
    /// Moves the property at <paramref name="fromIndex"/> to <paramref name="toIndex"/>,
    /// shifting other properties accordingly. Used for drag-drop reordering in the JSON editor.
    /// </summary>
    public void Reorder(int fromIndex, int toIndex)
    {
        if (fromIndex < 0 || fromIndex >= Length) throw new ArgumentOutOfRangeException(nameof(fromIndex));
        if (toIndex < 0 || toIndex >= Length) throw new ArgumentOutOfRangeException(nameof(toIndex));
        if (fromIndex == toIndex) return;

        string name = _names[fromIndex];
        object? value = _values[fromIndex];

        if (fromIndex < toIndex)
        {
            Array.Copy(_names, fromIndex + 1, _names, fromIndex, toIndex - fromIndex);
            Array.Copy(_values, fromIndex + 1, _values, fromIndex, toIndex - fromIndex);
        }
        else
        {
            Array.Copy(_names, toIndex, _names, toIndex + 1, fromIndex - toIndex);
            Array.Copy(_values, toIndex, _values, toIndex + 1, fromIndex - toIndex);
        }

        _names[toIndex] = name;
        _values[toIndex] = value;
        _index = null;
    }

    // Type-safe getters - use GetValue for path/transform support

    /// <summary>Returns the value at <paramref name="name"/> as a <see cref="JsonObject"/>, or <c>null</c>.</summary>
    /// <param name="name">The property name or dotted path.</param>
    /// <returns>The child object, or <c>null</c> if the value is missing or not an object.</returns>
    public JsonObject? GetObject(string name) => GetValue(name) as JsonObject;

    /// <summary>Returns the value at <paramref name="name"/> as a <see cref="JsonArray"/>, or <c>null</c>.</summary>
    /// <param name="name">The property name or dotted path.</param>
    /// <returns>The child array, or <c>null</c> if the value is missing or not an array.</returns>
    public JsonArray? GetArray(string name) => GetValue(name) as JsonArray;

    /// <summary>Returns the value at <paramref name="name"/> as a string, or <c>null</c>.</summary>
    /// <param name="name">The property name or dotted path.</param>
    /// <returns>The string value, or <c>null</c> if the value is missing or not a string.</returns>
    public string? GetString(string name) => GetValue(name) as string;

    /// <summary>Returns the value at <paramref name="name"/> converted to an <see cref="int"/>.</summary>
    /// <param name="name">The property name or dotted path.</param>
    /// <returns>The integer value.</returns>
    public int GetInt(string name)
    {
        var val = GetValue(name);
        return val is RawDouble rd ? (int)rd.Value : Convert.ToInt32(val, CultureInfo.InvariantCulture);
    }

    /// <summary>Returns the value at <paramref name="name"/> converted to a <see cref="long"/>.</summary>
    /// <param name="name">The property name or dotted path.</param>
    /// <returns>The long integer value.</returns>
    public long GetLong(string name)
    {
        var val = GetValue(name);
        return val is RawDouble rd ? (long)rd.Value : Convert.ToInt64(val, CultureInfo.InvariantCulture);
    }

    /// <summary>Returns the value at <paramref name="name"/> converted to a <see cref="double"/>.</summary>
    /// <param name="name">The property name or dotted path.</param>
    /// <returns>The double-precision floating-point value.</returns>
    public double GetDouble(string name)
    {
        var val = GetValue(name);
        return val is RawDouble rd ? rd.Value : Convert.ToDouble(val, CultureInfo.InvariantCulture);
    }

    /// <summary>
    /// Returns the display-safe text representation of the double at <paramref name="name"/>.
    /// If the value is a <see cref="RawDouble"/>, returns its original JSON text to preserve
    /// exact representations from the game save. Otherwise, formats the double using round-trip
    /// ("R") format to avoid silent precision loss.
    /// </summary>
    /// <param name="name">The property name or dotted path.</param>
    /// <returns>The text representation suitable for display and lossless round-tripping.</returns>
    public string GetDoubleText(string name)
    {
        var val = GetValue(name);
        if (val is RawDouble rd) return rd.Text;
        if (val is double d) return d.ToString("R", CultureInfo.InvariantCulture);
        return Convert.ToDouble(val, CultureInfo.InvariantCulture).ToString("R", CultureInfo.InvariantCulture);
    }

    /// <summary>
    /// Returns the value at <paramref name="name"/> converted to a <see cref="double"/>.
    /// This method exists for callers that conceptually deal with float-precision values
    /// (e.g., NMS "FloatValue" fields) but returns a full <see cref="double"/> to avoid
    /// silent precision loss that a <c>(float)</c> cast would cause for large numbers.
	/// This is explicitly because of the 'everything is an int64 in the end' math logic
	/// employed in the engine for almost all numbers that aren't split words etc.
    /// </summary>
    /// <param name="name">The property name or dotted path.</param>
    /// <returns>The double-precision floating-point value.</returns>
    public double GetFloat(string name)
    {
        var val = GetValue(name);
        return val is RawDouble rd ? rd.Value : Convert.ToDouble(val, CultureInfo.InvariantCulture);
    }

    /// <summary>Returns the value at <paramref name="name"/> converted to a <see cref="bool"/>.</summary>
    /// <param name="name">The property name or dotted path.</param>
    /// <returns>The boolean value.</returns>
    public bool GetBool(string name) => Convert.ToBoolean(GetValue(name), CultureInfo.InvariantCulture);

    /// <summary>Returns the value at <paramref name="name"/> converted to a <see cref="decimal"/>.</summary>
    /// <param name="name">The property name or dotted path.</param>
    /// <returns>The decimal value.</returns>
    public decimal GetDecimal(string name) => Convert.ToDecimal(GetValue(name), CultureInfo.InvariantCulture);

    // Path-based access
    // Supports dotted paths like "PlayerStateData.Health" and transforms

    /// <summary>
    /// Resolves a dotted path (e.g., "PlayerStateData.Health" or "Items[0].Id") to its value,
    /// applying any registered transforms along the way.
    /// </summary>
    /// <param name="path">A simple property name, dotted path, or bracket-indexed path.</param>
    /// <returns>The resolved value, or <c>null</c> if any segment is missing.</returns>
    public object? GetValue(string path)
    {
        // Check transforms first
        string resolvedPath = _transforms is { Count: > 0 } ? ResolveTransforms(path) : path;

        // Fast path: simple key (no dots, brackets) - skip regex
        if (resolvedPath.IndexOfAny(_pathChars) < 0)
            return Get(resolvedPath);

        object? current = this;
        var matches = PathPattern().Matches(resolvedPath);
        foreach (Match match in matches)
        {
            if (current is null) return null;

            string? key = match.Groups[1].Success ? match.Groups[1].Value :
                          match.Groups[2].Success ? match.Groups[2].Value : null;
            string? indexStr = match.Groups[3].Success ? match.Groups[3].Value : null;

            if (key is not null && current is JsonObject obj)
                current = obj.Get(key);
            else if (indexStr is not null && current is JsonArray arr)
                current = arr.Get(int.Parse(indexStr, CultureInfo.InvariantCulture));
            else
                return null;
        }
        return current;
    }

    private static readonly char[] _pathChars = { '.', '[', ']' };

    private string ResolveTransforms(string path)
    {
        if (_transforms == null) return path;
        foreach (var kvp in _transforms)
        {
            if (path == kvp.Key)
            {
                var result = kvp.Value(this);
                if (result is string s) return s;
            }
            else if (path.StartsWith(kvp.Key + ".", StringComparison.Ordinal) || path.StartsWith(kvp.Key + "[", StringComparison.Ordinal))
            {
                var result = kvp.Value(this);
                if (result is string s)
                    return s + path.Substring(kvp.Key.Length);
            }
        }
        return path;
    }

    /// <summary>
    /// Creates a deep copy of this object, recursively cloning all nested objects and arrays.
    /// </summary>
    /// <returns>A new <see cref="JsonObject"/> with cloned contents.</returns>
    public JsonObject DeepClone()
    {
        var clone = new JsonObject();
        int capacity = Math.Max(Length, InitialCapacity);
        clone._names = new string[capacity];
        clone._values = new object?[capacity];
        Array.Copy(_names, clone._names, Length);
        for (int i = 0; i < Length; i++)
        {
            clone._values[i] = _values[i] switch
            {
                JsonObject obj => obj.DeepClone(),
                JsonArray arr => arr.DeepClone(),
                _ => _values[i]
            };
            SetParent(clone._values[i], clone);
        }
        clone.Length = Length;
        return clone;
    }

    // File I/O

    /// <summary>
    /// Serializes this object to formatted JSON and writes it to the specified file path.
    /// Output uses human-readable (deobfuscated) key names so that exported files are easy
    /// to read and edit outside of the application.
    /// </summary>
    /// <param name="filePath">The file system path to write to.</param>
    public void ExportToFile(string filePath)
    {
        var json = JsonParser.Serialize(this, true, skipReverseMapping: true);
        File.WriteAllText(filePath, json);
    }

    /// <summary>
    /// Reads a JSON file from disk and parses it into a <see cref="JsonObject"/>.
    /// Handles both human-readable (deobfuscated) and obfuscated key formats:
    /// <list type="bullet">
    /// <item>Files exported by this application use human-readable keys and are parsed as-is.</item>
    /// <item>Files from NMSSaveEditor.jar or NomNom use obfuscated keys; the parser auto-detects
    ///        and deobfuscates them using the default name mapper.</item>
    /// </list>
    /// </summary>
    /// <param name="filePath">The file system path to read from.</param>
    /// <returns>The parsed JSON object with human-readable key names.</returns>
    public static JsonObject ImportFromFile(string filePath)
    {
        var json = File.ReadAllText(filePath);
        return Parse(json);
    }

    // For serialization

    /// <summary>Returns the internal names array for direct access during serialization.</summary>
    /// <returns>The backing array of property names (may contain trailing nulls beyond <see cref="Length"/>).</returns>
    internal string[] GetRawNames() => _names;

    /// <summary>Returns the internal values array for direct access during serialization.</summary>
    /// <returns>The backing array of property values (may contain trailing nulls beyond <see cref="Length"/>).</returns>
    internal object?[] GetRawValues() => _values;

    /// <inheritdoc />
    public override string ToString() => JsonParser.Serialize(this, false);

    /// <summary>
    /// Serializes this object to a formatted (indented) JSON string.
    /// </summary>
    /// <returns>The formatted JSON string representation.</returns>
    public string ToFormattedString() => JsonParser.Serialize(this, true);
    /// <summary>
    /// Serialize with human-readable names (no reverse mapping), for display purposes.
    /// </summary>
    public string ToDisplayString() => JsonParser.Serialize(this, true, skipReverseMapping: true);

    private static void SetParent(object? value, object parent)
    {
        if (value is JsonObject obj) obj.Parent = parent;
        else if (value is JsonArray arr) arr.Parent = parent;
    }

    private static void ClearParent(object? value)
    {
        if (value is JsonObject obj) obj.Parent = null;
        else if (value is JsonArray arr) arr.Parent = null;
    }
}

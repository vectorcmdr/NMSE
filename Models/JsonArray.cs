namespace NMSE.Models;

/// <summary>
/// Represents a mutable JSON array with ordered values.
/// Supports type-safe accessors, deep cloning, and serialization.
/// </summary>
public class JsonArray
{
    private const int InitialCapacity = 4;
    private object?[] _values;

    /// <summary>The number of elements in this array.</summary>
    public int Length { get; private set; }

    /// <summary>
    /// Parses a JSON string into a <see cref="JsonArray"/>.
    /// </summary>
    /// <param name="json">The JSON string to parse.</param>
    /// <returns>The parsed JSON array.</returns>
    public static JsonArray Parse(string json) => JsonParser.ParseArray(json);

    /// <summary>The parent container (a <see cref="JsonObject"/> or <see cref="JsonArray"/>) that holds this array, or <c>null</c> for root arrays.</summary>
    internal object? Parent { get; set; }

    /// <summary>An optional listener that is notified when elements change in this array.</summary>
    public IPropertyChangeListener? Listener { get; set; }

    /// <summary>
    /// Initializes a new empty JSON array.
    /// </summary>
    public JsonArray()
    {
        _values = new object?[InitialCapacity];
        Length = 0;
    }

    private void EnsureCapacity(int needed)
    {
        if (_values.Length >= needed) return;
        int newSize = Math.Max(needed, _values.Length + (_values.Length >> 1));
        var newValues = new object?[newSize];
        Array.Copy(_values, newValues, Length);
        _values = newValues;
    }

    /// <summary>
    /// Appends a value to the end of the array after validating its type.
    /// </summary>
    /// <param name="value">The value to add. Must be a supported JSON type.</param>
    public void Add(object? value)
    {
        ValidateType(value);
        EnsureCapacity(Length + 1);
        _values[Length] = value;
        SetParent(value, this);
        Length++;
    }

    /// <summary>
    /// Fast add without type validation - for use by the parser only.
    /// The parser only produces valid JSON types, so the type check is unnecessary.
    /// </summary>
    internal void AddUnchecked(object? value)
    {
        EnsureCapacity(Length + 1);
        _values[Length] = value;
        if (value is JsonObject obj) obj.Parent = this;
        else if (value is JsonArray arr) arr.Parent = this;
        Length++;
    }

    /// <summary>
    /// Inserts a value at the specified index, shifting subsequent elements.
    /// </summary>
    /// <param name="index">The zero-based index at which to insert.</param>
    /// <param name="value">The value to insert. Must be a supported JSON type.</param>
    public void Insert(int index, object? value)
    {
        ValidateType(value);
        EnsureCapacity(Length + 1);
        Array.Copy(_values, index, _values, index + 1, Length - index);
        _values[index] = value;
        SetParent(value, this);
        Length++;
    }

    /// <summary>
    /// Returns the value at the specified index.
    /// </summary>
    /// <param name="index">The zero-based index of the element to retrieve.</param>
    /// <returns>The value at the given index.</returns>
    public object? Get(int index)
    {
        if (index < 0 || index >= Length) throw new IndexOutOfRangeException();
        return _values[index];
    }

    /// <summary>
    /// Replaces the value at the specified index.
    /// </summary>
    /// <param name="index">The zero-based index of the element to replace.</param>
    /// <param name="value">The new value. Must be a supported JSON type.</param>
    public void Set(int index, object? value)
    {
        if (index < 0 || index >= Length) throw new IndexOutOfRangeException();
        ValidateType(value);
        var old = _values[index];
        ClearParent(old);
        _values[index] = value;
        SetParent(value, this);
    }

    /// <summary>
    /// Removes the element at the specified index, shifting subsequent elements.
    /// </summary>
    /// <param name="index">The zero-based index of the element to remove.</param>
    public void RemoveAt(int index)
    {
        if (index < 0 || index >= Length) throw new IndexOutOfRangeException();
        ClearParent(_values[index]);
        Array.Copy(_values, index + 1, _values, index, Length - index - 1);
        _values[--Length] = null;
    }

    /// <summary>
    /// Removes all elements from the array.
    /// </summary>
    public void Clear()
    {
        for (int i = 0; i < Length; i++)
        {
            ClearParent(_values[i]);
            _values[i] = null;
        }
        Length = 0;
    }

    /// <summary>
    /// Returns the index of the first occurrence of the specified value, or -1 if not found.
    /// </summary>
    /// <param name="value">The value to search for.</param>
    /// <returns>The zero-based index, or -1 if the value is not present.</returns>
    public int IndexOf(object? value)
    {
        for (int i = 0; i < Length; i++)
            if (Equals(_values[i], value)) return i;
        return -1;
    }

    // Type-safe accessors

    /// <summary>Returns the element at <paramref name="index"/> cast to a <see cref="JsonObject"/>.</summary>
    /// <param name="index">The zero-based element index.</param>
    /// <returns>The child object.</returns>
    public JsonObject GetObject(int index) => (JsonObject)Get(index)!;

    /// <summary>Returns the element at <paramref name="index"/> cast to a <see cref="JsonArray"/>.</summary>
    /// <param name="index">The zero-based element index.</param>
    /// <returns>The child array.</returns>
    public JsonArray GetArray(int index) => (JsonArray)Get(index)!;

    /// <summary>Returns the element at <paramref name="index"/> cast to a string.</summary>
    /// <param name="index">The zero-based element index.</param>
    /// <returns>The string value.</returns>
    public string GetString(int index) => (string)Get(index)!;

    /// <summary>Returns the element at <paramref name="index"/> converted to an <see cref="int"/>.</summary>
    /// <param name="index">The zero-based element index.</param>
    /// <returns>The integer value.</returns>
    public int GetInt(int index)
    {
        var val = Get(index);
        return val is RawDouble rd ? (int)rd.Value : Convert.ToInt32(val);
    }

    /// <summary>Returns the element at <paramref name="index"/> converted to a <see cref="long"/>.</summary>
    /// <param name="index">The zero-based element index.</param>
    /// <returns>The long integer value.</returns>
    public long GetLong(int index)
    {
        var val = Get(index);
        return val is RawDouble rd ? (long)rd.Value : Convert.ToInt64(val);
    }

    /// <summary>Returns the element at <paramref name="index"/> converted to a <see cref="double"/>.</summary>
    /// <param name="index">The zero-based element index.</param>
    /// <returns>The double-precision floating-point value.</returns>
    public double GetDouble(int index)
    {
        var val = Get(index);
        return val is RawDouble rd ? rd.Value : Convert.ToDouble(val);
    }

    /// <summary>Returns the element at <paramref name="index"/> converted to a <see cref="bool"/>.</summary>
    /// <param name="index">The zero-based element index.</param>
    /// <returns>The boolean value.</returns>
    public bool GetBool(int index) => Convert.ToBoolean(Get(index));

    /// <summary>Returns the element at <paramref name="index"/> converted to a <see cref="decimal"/>.</summary>
    /// <param name="index">The zero-based element index.</param>
    /// <returns>The decimal value.</returns>
    public decimal GetDecimal(int index) => Convert.ToDecimal(Get(index));

    /// <summary>
    /// Creates a deep copy of this array, recursively cloning all nested objects and arrays.
    /// </summary>
    /// <returns>A new <see cref="JsonArray"/> with cloned contents.</returns>
    public JsonArray DeepClone()
    {
        var clone = new JsonArray();
        clone._values = new object?[Math.Max(Length, InitialCapacity)];
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

    /// <summary>Returns the internal values array for direct access during serialization.</summary>
    /// <returns>The backing array (may contain trailing nulls beyond <see cref="Length"/>).</returns>
    internal object?[] GetRawValues() => _values;

    /// <inheritdoc />
    public override string ToString() => JsonParser.Serialize(this, false);

    /// <summary>
    /// Serializes this array to a formatted (indented) JSON string.
    /// </summary>
    /// <returns>The formatted JSON string representation.</returns>
    public string ToFormattedString() => JsonParser.Serialize(this, true);

    private static void ValidateType(object? value)
    {
        if (value is null or bool or int or long or float or double or decimal or string or JsonObject or JsonArray or BinaryData)
            return;
        throw new ArgumentException("Unsupported JSON value type");
    }

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

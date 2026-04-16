using System.Globalization;
using System.Text;

namespace NMSE.Models;

/// <summary>
/// Represents raw binary data parsed from a JSON string value containing non-ASCII bytes.
/// Used for NMS save file fields that contain Latin-1 encoded binary payloads.
/// </summary>
public class BinaryData : IEquatable<BinaryData>
{
    private static readonly Encoding Latin1 = Encoding.Latin1;
    private readonly byte[] _data;

    /// <summary>
    /// Initializes a new instance wrapping the given byte array.
    /// </summary>
    /// <param name="data">The raw byte data. Must not be <c>null</c>.</param>
    public BinaryData(byte[] data) => _data = data ?? throw new ArgumentNullException(nameof(data));

    /// <summary>
    /// Returns the underlying byte array.
    /// </summary>
    /// <returns>The raw byte data.</returns>
    public byte[] ToByteArray() => _data;

    /// <summary>
    /// Finds the first occurrence of the specified byte value.
    /// </summary>
    /// <param name="value">The byte value to search for.</param>
    /// <returns>The zero-based index of the byte, or -1 if not found.</returns>
    public int IndexOf(byte value)
    {
        for (int i = 0; i < _data.Length; i++)
            if (_data[i] == value) return i;
        return -1;
    }

    /// <summary>
    /// Extracts a substring from the binary data using Latin-1 encoding.
    /// </summary>
    /// <param name="start">The zero-based byte offset to start from.</param>
    /// <param name="length">The number of bytes to decode.</param>
    /// <returns>The decoded Latin-1 string.</returns>
    public string Substring(int start, int length) =>
        Latin1.GetString(_data, start, length);

    /// <summary>
    /// Returns the data as an uppercase hexadecimal string (two characters per byte).
    /// </summary>
    /// <returns>The hex-encoded string representation.</returns>
    public string ToHexString()
    {
        var sb = new StringBuilder(_data.Length * 2);
        foreach (byte b in _data)
            sb.Append(b.ToString("X2", CultureInfo.InvariantCulture));
        return sb.ToString();
    }

    /// <summary>
    /// Returns the data decoded as a Latin-1 string.
    /// </summary>
    /// <returns>The Latin-1 decoded string.</returns>
    public override string ToString() => Latin1.GetString(_data);

    /// <inheritdoc />
    public bool Equals(BinaryData? other) => other is not null && _data.AsSpan().SequenceEqual(other._data);

    /// <inheritdoc />
    public override bool Equals(object? obj) => obj is BinaryData other && Equals(other);

    /// <inheritdoc />
    public override int GetHashCode()
    {
        var hash = new HashCode();
        foreach (byte b in _data) hash.Add(b);
        return hash.ToHashCode();
    }
}

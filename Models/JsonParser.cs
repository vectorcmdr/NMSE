using System.Buffers;
using System.Text;
using System.Text.Unicode;

namespace NMSE.Models;

/// <summary>
/// Provides JSON parsing and serialization with support for NMS save file conventions,
/// including obfuscated key name mapping, binary data literals, and round-trip numeric precision.
/// </summary>
public static class JsonParser
{
    private const string HexChars = "0123456789ABCDEFabcdef";
    private const int MaxJsonNumberLength = 64;

    /// <summary>
    /// Pre-computed indentation strings for JSON formatting.
    /// Avoids allocating newline + tabs on every recursive serialization call.
    /// </summary>
    private const int MaxCachedDepth = 64;
    private static readonly string[] _indentCache;

    static JsonParser()
    {
        _indentCache = new string[MaxCachedDepth];
        _indentCache[0] = Environment.NewLine;
        for (int i = 1; i < MaxCachedDepth; i++)
            _indentCache[i] = _indentCache[i - 1] + "\t";
    }

    private static string GetIndent(int depth)
    {
        if (depth < MaxCachedDepth) return _indentCache[depth];
        return _indentCache[MaxCachedDepth - 1] + new string('\t', depth - MaxCachedDepth + 1);
    }

    /// <summary>
    /// String intern pool for JSON object keys to reduce memory from duplicate key allocations.
    /// NMS saves have thousands of objects with identical keys (e.g., "Value", "Id", "Type").
    /// </summary>
    private static readonly Dictionary<string, string> _keyPool = new(StringComparer.Ordinal);
    private static readonly object _keyPoolLock = new();

    private static string InternKey(string key)
    {
        lock (_keyPoolLock)
        {
            if (_keyPool.TryGetValue(key, out var existing))
                return existing;
            _keyPool[key] = key;
            return key;
        }
    }

    /// <summary>
    /// Default name mapper loaded from jsonmap.txt, used to auto-detect and translate
    /// obfuscated NMS save file key names during parsing.
    /// </summary>
    private static NMSE.Data.JsonNameMapper? _defaultSaveMapper;

    /// <summary>
    /// Set the default name mapper for save file parsing.
    /// Called at application startup with the mapper loaded from jsonmap.txt.
    /// </summary>
    public static void SetDefaultMapper(NMSE.Data.JsonNameMapper mapper) =>
        _defaultSaveMapper = mapper;

    /// <summary>
    /// Returns the default name mapper (if set), for callers that need to ensure
    /// parsed objects carry a mapper for correct save-to-disk serialization.
    /// </summary>
    public static NMSE.Data.JsonNameMapper? GetDefaultMapper() => _defaultSaveMapper;

    // SERIALIZATION

    /// <summary>
    /// Serializes a JSON value (object, array, or primitive) to a string.
    /// </summary>
    /// <param name="value">The value to serialize.</param>
    /// <param name="formatted">Whether to produce indented output with newlines.</param>
    /// <param name="skipReverseMapping">When <c>true</c>, skips reverse name mapping for display purposes.</param>
    /// <returns>The JSON string representation.</returns>
    public static string Serialize(object? value, bool formatted, bool skipReverseMapping = false)
    {
        // Extract the mapper from the root object if it's a JsonObject
        var mapper = skipReverseMapping ? null : (value as JsonObject)?.NameMapper;
        var sb = new StringBuilder();
        SerializeValue(sb, value, formatted ? 0 : -1, formatted, mapper, skipReverseMapping);
        return sb.ToString();
    }

    /// <summary>
    /// Serialize a value into the shared StringBuilder.
    /// depth &lt; 0 means unformatted (no newlines/indentation).
    /// </summary>
    private static void SerializeValue(StringBuilder sb, object? value, int depth, bool spaces,
        NMSE.Data.JsonNameMapper? mapper = null, bool skipReverseMapping = false)
    {
        switch (value)
        {
            case null: sb.Append("null"); break;
            case bool b: sb.Append(b ? "true" : "false"); break;
            case decimal d: sb.Append(d.ToString("G", System.Globalization.CultureInfo.InvariantCulture)); break;
            case int i: sb.Append(i); break;
            case long l: sb.Append(l); break;
            case float f: AppendFloat(sb, f); break;
            case RawDouble rd: sb.Append(rd.Text); break;
            case double d: AppendDouble(sb, d); break;
            case string s: AppendQuotedString(sb, s); break;
            case JsonObject obj: SerializeObject(sb, obj, depth, spaces, mapper, skipReverseMapping); break;
            case JsonArray arr: SerializeArray(sb, arr, depth, spaces, mapper, skipReverseMapping); break;
            case BinaryData bin: AppendQuotedBinaryData(sb, bin); break;
            default: throw new InvalidOperationException("Unsupported JSON value type");
        }
    }

    /// <summary>
    /// Append a double value using round-trip format ("R") to preserve full precision.
    /// NMS save files distinguish between integer (1) and float (1.0) types, so whole-number
    /// doubles must always include a decimal point (e.g., "1.0" not "1").
    /// </summary>
    private static void AppendDouble(StringBuilder sb, double d)
    {
        string s = d.ToString("R", System.Globalization.CultureInfo.InvariantCulture);
        sb.Append(s);
        // Ensure whole-number doubles keep their decimal point (1 -> 1.0)
        if (s.IndexOfAny(_floatIndicators) < 0)
            sb.Append(".0");
    }

    /// <summary>
    /// Append a float value using round-trip format ("R") with decimal point preservation.
    /// </summary>
    private static void AppendFloat(StringBuilder sb, float f)
    {
        string s = f.ToString("R", System.Globalization.CultureInfo.InvariantCulture);
        sb.Append(s);
        if (s.IndexOfAny(_floatIndicators) < 0)
            sb.Append(".0");
    }

    private static readonly char[] _floatIndicators = { '.', 'E', 'e' };

    private static void SerializeObject(StringBuilder sb, JsonObject obj, int depth, bool spaces,
        NMSE.Data.JsonNameMapper? mapper = null, bool skipReverseMapping = false)
    {
        sb.Append('{');
        var names = obj.GetRawNames();
        var values = obj.GetRawValues();
        // Use mapper from the object itself, or fall back to the one passed from the parent.
        // When skipReverseMapping is true, never reverse-map (display mode).
        var activeMapper = skipReverseMapping ? null : (obj.NameMapper ?? mapper);
        bool formatted = depth >= 0;
        int childDepth = formatted ? depth + 1 : -1;
        for (int i = 0; i < obj.Length; i++)
        {
            if (i > 0) sb.Append(',');
            if (formatted) sb.Append(GetIndent(childDepth));
            // Reverse-map human-readable name back to obfuscated key for saving
            string name = activeMapper != null ? activeMapper.ToKey(names[i]) : names[i];
            AppendQuotedString(sb, name);
            sb.Append(':');
            if (spaces) sb.Append(' ');
            SerializeValue(sb, values[i], childDepth, spaces, activeMapper, skipReverseMapping);
        }
        if (obj.Length > 0 && formatted) sb.Append(GetIndent(depth));
        sb.Append('}');
    }

    private static void SerializeArray(StringBuilder sb, JsonArray arr, int depth, bool spaces,
        NMSE.Data.JsonNameMapper? mapper = null, bool skipReverseMapping = false)
    {
        sb.Append('[');
        var values = arr.GetRawValues();
        bool formatted = depth >= 0;
        int childDepth = formatted ? depth + 1 : -1;
        for (int i = 0; i < arr.Length; i++)
        {
            if (i > 0) sb.Append(',');
            if (formatted) sb.Append(GetIndent(childDepth));
            SerializeValue(sb, values[i], childDepth, spaces, mapper, skipReverseMapping);
        }
        if (arr.Length > 0 && formatted) sb.Append(GetIndent(depth));
        sb.Append(']');
    }

    /// <summary>
    /// Append an escaped and quoted string directly to the StringBuilder,
    /// avoiding intermediate string allocations from EscapeString + QuoteString.
    /// </summary>
    private static void AppendQuotedString(StringBuilder sb, string s)
    {
        sb.Append('"');
        foreach (char c in s)
        {
            switch (c)
            {
                case '\r': sb.Append("\\r"); break;
                case '\n': sb.Append("\\n"); break;
                case '\t': sb.Append("\\t"); break;
                case '\f': sb.Append("\\f"); break;
                case '\b': sb.Append("\\b"); break;
                case '"': sb.Append("\\\""); break;
                case '\\': sb.Append("\\\\"); break;
                default:
                    if (c >= ' ' && c <= '~')
                        sb.Append(c);
                    else if (c >= ' ')
                    {
                        // Non-ASCII printable: write as \uXXXX to preserve
                        // characters such as \u03BB (λ) or \u0166 (Ŧ) in their
                        // escaped form for save-file round-trip safety.
                        sb.Append("\\u");
                        sb.Append(HexChars[(c >> 12) & 0xF]);
                        sb.Append(HexChars[(c >> 8) & 0xF]);
                        sb.Append(HexChars[(c >> 4) & 0xF]);
                        sb.Append(HexChars[c & 0xF]);
                    }
                    else
                    {
                        sb.Append("\\u");
                        sb.Append(HexChars[(c >> 12) & 0xF]);
                        sb.Append(HexChars[(c >> 8) & 0xF]);
                        sb.Append(HexChars[(c >> 4) & 0xF]);
                        sb.Append(HexChars[c & 0xF]);
                    }
                    break;
            }
        }
        sb.Append('"');
    }

    /// <summary>
    /// Append escaped and quoted binary data directly to the StringBuilder.
    /// </summary>
    private static void AppendQuotedBinaryData(StringBuilder sb, BinaryData data)
    {
        sb.Append('"');
        foreach (byte b in data.ToByteArray())
        {
            int v = b & 0xFF;
            switch (v)
            {
                case 13: sb.Append("\\r"); break;
                case 10: sb.Append("\\n"); break;
                case 9: sb.Append("\\t"); break;
                case 12: sb.Append("\\f"); break;
                case 8: sb.Append("\\b"); break;
                case 34: sb.Append("\\\""); break;
                case 92: sb.Append("\\\\"); break;
                default:
                    if (v >= 32)
                        // Printable ASCII (32-127) and Latin1 high bytes (128-255)
                        // are emitted as raw characters. Since save files use Latin1
                        // encoding, high bytes round-trip correctly and match the
                        // original NMS format (1 byte instead of 4 for \xHH).
                        sb.Append((char)v);
                    else
                    {
                        // Control characters (0x00-0x1F) not covered above use
                        // standard JSON \u00XX escapes for cross-editor compatibility.
                        sb.Append("\\u00");
                        sb.Append(HexChars[(v >> 4) & 0xF]);
                        sb.Append(HexChars[v & 0xF]);
                    }
                    break;
            }
        }
        sb.Append('"');
    }
    // PARSING

    /// <summary>
    /// Parses a JSON string into a value (object, array, or primitive).
    /// </summary>
    /// <param name="json">The JSON string to parse.</param>
    /// <returns>The parsed value, or <c>null</c> for JSON null.</returns>
    public static object? ParseValue(string json)
    {
        using var reader = new JsonReader(json);
        var result = ParseValue(reader, reader.ReadSkipWhitespace(), null);
        if (reader.ReadSkipWhitespace() >= 0)
            throw new JsonException("Invalid trailing data", reader.Line, reader.Column);
        return result;
    }

    /// <summary>
    /// Parses a JSON string that represents an object, optionally applying name mapping for obfuscated keys.
    /// </summary>
    /// <param name="json">The JSON string to parse.</param>
    /// <param name="mapper">An optional name mapper for translating obfuscated keys to human-readable names.</param>
    /// <returns>The parsed <see cref="JsonObject"/>.</returns>
    public static JsonObject ParseObject(string json, NMSE.Data.JsonNameMapper? mapper = null)
    {
        using var reader = new JsonReader(json);
        if (reader.ReadSkipWhitespace() != '{')
            throw new JsonException("Invalid object string", reader.Line, reader.Column);
        var result = ParseObjectBody(reader, mapper);
        if (reader.ReadSkipWhitespace() >= 0)
            throw new JsonException("Invalid trailing data", reader.Line, reader.Column);
        return result;
    }

    /// <summary>
    /// Parses a JSON string that represents an array.
    /// </summary>
    /// <param name="json">The JSON string to parse.</param>
    /// <returns>The parsed <see cref="JsonArray"/>.</returns>
    public static JsonArray ParseArray(string json)
    {
        using var reader = new JsonReader(json);
        if (reader.ReadSkipWhitespace() != '[')
            throw new JsonException("Invalid array string", reader.Line, reader.Column);
        var result = ParseArrayBody(reader, null);
        if (reader.ReadSkipWhitespace() >= 0)
            throw new JsonException("Invalid trailing data", reader.Line, reader.Column);
        return result;
    }

    private static object? ParseValue(JsonReader reader, int c, NMSE.Data.JsonNameMapper? mapper)
    {
        if (c < 0) throw new JsonException("Short read", reader.Line, reader.Column);

        return c switch
        {
            '{' => ParseObjectBody(reader, mapper),
            '[' => ParseArrayBody(reader, mapper),
            '"' => ParseString(reader),
            'f' => ParseFalse(reader),
            't' => ParseTrue(reader),
            'n' => ParseNull(reader),
            'd' => ParseDataLiteral(reader),
            '-' or (>= '0' and <= '9') => ParseNumber(reader, c),
            _ => throw new JsonException("Invalid token", reader.Line, reader.Column)
        };
    }

    private static JsonObject ParseObjectBody(JsonReader reader, NMSE.Data.JsonNameMapper? mapper)
    {
        var obj = new JsonObject();
        NMSE.Data.JsonNameMapper? activeMapper = mapper;
        int c = reader.ReadSkipWhitespace();
        if (c == '"')
        {
            // Auto-detect: check the first several keys for obfuscation.
            // PS4/PS5 saves may start with non-obfuscated keys (e.g. "Version")
            // followed by obfuscated ones (e.g. "XTp", "<h0"), so checking only
            // the first key is insufficient.
            int autoDetectRemaining = (mapper == null) ? 10 : 0;
            while (true)
            {
                string key = ParseStringValue(reader);

                // Auto-detect obfuscated keys on the first N keys of the root object
                if (autoDetectRemaining > 0 && activeMapper == null)
                {
                    autoDetectRemaining--;
                    if (_defaultSaveMapper != null && _defaultSaveMapper.IsObfuscatedKey(key))
                    {
                        activeMapper = _defaultSaveMapper;
                        autoDetectRemaining = 0;
                        // Re-map any keys already added to the object
                        obj.RemapKeys(activeMapper);
                    }
                }

                // Translate obfuscated key to human-readable name
                if (activeMapper != null)
                    key = activeMapper.ToName(key);

                // Intern key to deduplicate repeated key strings across objects
                key = InternKey(key);

                if (reader.ReadSkipWhitespace() != ':')
                    throw new JsonException("Invalid token", reader.Line, reader.Column);
                object? value = ParseValue(reader, reader.ReadSkipWhitespace(), activeMapper);
                obj.AddUnchecked(key, value);
                c = reader.ReadSkipWhitespace();
                if (c == '}') break;
                if (c != ',') throw new JsonException("Invalid token", reader.Line, reader.Column);
                c = reader.ReadSkipWhitespace();
                if (c != '"') throw new JsonException("Invalid token", reader.Line, reader.Column);
            }
        }
        else if (c != '}')
        {
            throw new JsonException("Invalid token", reader.Line, reader.Column);
        }

        // Store the mapper on the root object for use during serialization
        if (activeMapper != null)
            obj.NameMapper = activeMapper;

        return obj;
    }

    private static JsonArray ParseArrayBody(JsonReader reader, NMSE.Data.JsonNameMapper? mapper)
    {
        var arr = new JsonArray();
        int c = reader.ReadSkipWhitespace();
        if (c != ']')
        {
            while (true)
            {
                arr.AddUnchecked(ParseValue(reader, c, mapper));
                c = reader.ReadSkipWhitespace();
                if (c == ']') break;
                if (c != ',') throw new JsonException("Invalid token", reader.Line, reader.Column);
                c = reader.ReadSkipWhitespace();
            }
        }
        return arr;
    }

    private static object ParseFalse(JsonReader reader)
    {
        ExpectChar(reader, 'a'); ExpectChar(reader, 'l');
        ExpectChar(reader, 's'); ExpectChar(reader, 'e');
        return false;
    }

    private static object ParseTrue(JsonReader reader)
    {
        ExpectChar(reader, 'r'); ExpectChar(reader, 'u'); ExpectChar(reader, 'e');
        return true;
    }

    private static object? ParseNull(JsonReader reader)
    {
        ExpectChar(reader, 'u'); ExpectChar(reader, 'l'); ExpectChar(reader, 'l');
        return null;
    }

    private static object ParseDataLiteral(JsonReader reader)
    {
        ExpectChar(reader, 'a'); ExpectChar(reader, 't'); ExpectChar(reader, 'a');
        ExpectChar(reader, '(');
        if (reader.ReadSkipWhitespace() != '"')
            throw new JsonException("Invalid token", reader.Line, reader.Column);
        var data = ParseHexData(reader);
        if (reader.ReadSkipWhitespace() != ')')
            throw new JsonException("Invalid token", reader.Line, reader.Column);
        return data;
    }

    private static void ExpectChar(JsonReader reader, char expected)
    {
        if (reader.Read() != expected)
            throw new JsonException("Invalid token", reader.Line, reader.Column);
    }

    private static string ParseStringValue(JsonReader reader)
    {
        var result = ParseString(reader);
        if (result is string s) return s;
        throw new JsonException("Invalid string", reader.Line, reader.Column);
    }

    private static object ParseString(JsonReader reader)
    {
        // Fast path: scan directly through the source string for simple strings
        // (no escape sequences, no high bytes). This avoids StringBuilder allocation.
        string source = reader.Source;
        int startPos = reader.Position;
        int pos = startPos;

        while (pos < source.Length)
        {
            char c = source[pos];
            if (c == '"')
            {
                // Simple string - no escapes, no high bytes
                string result = source.Substring(startPos, pos - startPos);
                reader.Advance(pos + 1); // skip past closing quote
                return result;
            }
            if (c == '\\' || c >= 0x80)
                break; // Fall through to full parser
            pos++;
        }

        // Slow path: string has escapes or high bytes
        // Reset to start position and parse character by character
        reader.Advance(startPos);
        return ParseStringFull(reader);
    }

    private static object ParseStringFull(JsonReader reader)
    {
        var sb = new StringBuilder();
        // Use pooled byte buffer instead of MemoryStream to avoid per-call allocations
        byte[] byteBuffer = ArrayPool<byte>.Shared.Rent(256);
        int byteCount = 0;
        bool trackBytes = true;
        bool hasStringContent = true;
        bool hasHighBytes = false; // Track bytes >= 0x80 (non-ASCII); may be UTF-8 text or binary data

        try
        {
            int c;
            while ((c = reader.Read()) != '"')
            {
                if (c < 0) throw new JsonException("Short read");

                if (c == '\\')
                {
                    c = reader.Read();
                    if (c < 0) throw new JsonException("Short read");
                    switch (c)
                    {
                        case '0': c = 0; break;
                        case 'b': c = 8; break;
                        case 'f': c = 12; break;
                        case 'n': c = 10; break;
                        case 'r': c = 13; break;
                        case 't': c = 9; break;
                        case 'v': c = 11; break;
                        case 'u':
                            c = (ParseHexDigit(reader.Read()) << 12) | (ParseHexDigit(reader.Read()) << 8) |
                                (ParseHexDigit(reader.Read()) << 4) | ParseHexDigit(reader.Read());
                            if (c <= 255)
                            {
                                if (hasStringContent) sb.Append((char)c);
                                if (trackBytes) AppendByte(ref byteBuffer, ref byteCount, (byte)c);
                                // Note: \u00XX escapes are intentional Unicode, NOT raw binary.
                                // Do NOT set hasHighBytes here - only raw source bytes >= 0x80
                                // (from Latin-1 decoded binary payloads) should trigger BinaryData.
                            }
                            else
                            {
                                if (!hasStringContent)
                                    throw new JsonException("Mixed encodings detected in string");
                                trackBytes = false;
                                sb.Append((char)c);
                            }
                            continue;
                        case 'x':
                            c = (ParseHexDigit(reader.Read()) << 4) | ParseHexDigit(reader.Read());
                            if (!trackBytes)
                                throw new JsonException("Mixed encodings detected in string");
                            AppendByte(ref byteBuffer, ref byteCount, (byte)c);
                            hasStringContent = false;
                            continue;
                        // default: c stays as-is (for \\, \", etc.)
                    }
                }

                if (hasStringContent) sb.Append((char)c);
                if (trackBytes) AppendByte(ref byteBuffer, ref byteCount, (byte)c);
                // Detect raw bytes >= 0x80 which may indicate binary data or
                // UTF-8 multi-byte sequences read through Latin-1 encoding.
                // The distinction is made after the string ends via UTF-8 validation.
                if (c >= 0x80 && c <= 0xFF) hasHighBytes = true;
            }

            // If any raw high bytes (0x80-0xFF) were found, the string may be either:
            //   (a) UTF-8 encoded text (e.g. Greek λ, CJK characters, Cyrillic, etc.)
            //       that was read through Latin-1 encoding, OR
            //   (b) genuine binary data (e.g. TechPack payloads).
            // Distinguish by checking whether the bytes form a valid UTF-8 sequence.
            if (hasHighBytes && trackBytes)
            {
                var span = new ReadOnlySpan<byte>(byteBuffer, 0, byteCount);
                if (Utf8.IsValid(span))
                {
                    // Valid UTF-8 text - decode to a proper Unicode string.
                    return Encoding.UTF8.GetString(span);
                }

                // Not valid UTF-8 - genuine binary data.
                var result = new byte[byteCount];
                Array.Copy(byteBuffer, result, byteCount);
                return new BinaryData(result);
            }

            if (!hasStringContent && trackBytes)
            {
                var result = new byte[byteCount];
                Array.Copy(byteBuffer, result, byteCount);
                return new BinaryData(result);
            }

            return sb.ToString();
        }
        finally
        {
            ArrayPool<byte>.Shared.Return(byteBuffer);
        }
    }

    private static void AppendByte(ref byte[] buffer, ref int count, byte value)
    {
        if (count >= buffer.Length)
        {
            var newBuffer = ArrayPool<byte>.Shared.Rent(buffer.Length * 2);
            Array.Copy(buffer, newBuffer, count);
            ArrayPool<byte>.Shared.Return(buffer);
            buffer = newBuffer;
        }
        buffer[count++] = value;
    }

    private static BinaryData ParseHexData(JsonReader reader)
    {
        if (reader.Read() != '0') throw new JsonException("Invalid hex data", reader.Line, reader.Column);
        if (reader.Read() != 'x') throw new JsonException("Invalid hex data", reader.Line, reader.Column);

        byte[] buffer = ArrayPool<byte>.Shared.Rent(128);
        int count = 0;
        try
        {
            int c;
            while ((c = reader.Read()) != '"')
            {
                if (c < 0) throw new JsonException("Short read", reader.Line, reader.Column);
                int d = reader.Read();
                if (d < 0) throw new JsonException("Short read", reader.Line, reader.Column);

                int hi = HexChars.IndexOf((char)c);
                if (hi < 0) throw new JsonException("Invalid hex data", reader.Line, reader.Column);
                if (hi >= 16) hi -= 6;

                int lo = HexChars.IndexOf((char)d);
                if (lo < 0) throw new JsonException("Invalid hex data", reader.Line, reader.Column);
                if (lo >= 16) lo -= 6;

                AppendByte(ref buffer, ref count, (byte)((hi << 4) | lo));
            }

            var result = new byte[count];
            Array.Copy(buffer, result, count);
            return new BinaryData(result);
        }
        finally
        {
            ArrayPool<byte>.Shared.Return(buffer);
        }
    }

    private static int ParseHexDigit(int c)
    {
        if (c < 0) throw new IOException("short read");
        int index = HexChars.IndexOf((char)c);
        if (index < 0) throw new IOException("invalid hex char");
        if (index >= 16) index -= 6;
        return index;
    }

    private static object ParseNumber(JsonReader reader, int first)
    {
        bool negative = false;
        if (first == '-')
        {
            int next = reader.ReadDigit();
            if (next < 0) throw new JsonException("Invalid token", reader.Line, reader.Column);
            first = next;
            negative = true;
        }

        // Use long for integer accumulation (avoids decimal overhead)
        long intValue = first - '0';
        bool intOverflow = false;
        if (first != '0')
        {
            int d;
            while ((d = reader.ReadDigit()) >= 0)
            {
                // Check for overflow before multiply: if intValue > long.MaxValue/10, multiplication will overflow
                if (intValue > long.MaxValue / 10)
                    intOverflow = true;
                long next = intValue * 10 + (d - '0');
                if (next / 10 != intValue) intOverflow = true; // overflow in multiply+add
                intValue = next;
            }
        }

        bool isInteger = true;

        // For floating-point numbers, collect digits into a buffer and use double.Parse
        // for accurate IEEE 754 rounding (avoids cumulative errors from digit-by-digit accumulation).
        // The buffer is filled lazily only when a decimal point or exponent is encountered.
        char[]? numBuf = null;
        int numLen = 0;

        if (reader.ReadIf('.') >= 0)
        {
            isInteger = false;
            int d = reader.ReadDigit();
            if (d < 0) throw new JsonException("Invalid token", reader.Line, reader.Column);

            // Build the full number string for double.Parse
            numBuf = new char[MaxJsonNumberLength];
            if (negative) numBuf[numLen++] = '-';
            // Write the integer part we already accumulated
            string intStr = intValue.ToString();
            intStr.CopyTo(0, numBuf, numLen, intStr.Length);
            numLen += intStr.Length;
            numBuf[numLen++] = '.';
            numBuf[numLen++] = (char)d;
            while ((d = reader.ReadDigit()) >= 0)
                numBuf[numLen++] = (char)d;
        }

        // Exponent
        if (reader.ReadIfEither('e', 'E') >= 0)
        {
            if (numBuf == null)
            {
                // No decimal point but has exponent - build the buffer now
                numBuf = new char[MaxJsonNumberLength];
                if (negative) numBuf[numLen++] = '-';
                string intStr = intValue.ToString();
                intStr.CopyTo(0, numBuf, numLen, intStr.Length);
                numLen += intStr.Length;
            }
            isInteger = false;
            numBuf[numLen++] = 'E';
            int d = reader.ReadDigitOrSign();
            if (d == '+' || d == '-')
            {
                numBuf[numLen++] = (char)d;
                d = reader.ReadDigit();
            }
            if (d < 0) throw new JsonException("Invalid token", reader.Line, reader.Column);
            numBuf[numLen++] = (char)d;
            while ((d = reader.ReadDigit()) >= 0)
                numBuf[numLen++] = (char)d;
        }

        if (isInteger && !intOverflow)
        {
            if (negative) intValue = -intValue;
            if (intValue >= int.MinValue && intValue <= int.MaxValue)
                return (int)intValue;
            return intValue;
        }

        // Floating point: use double.Parse for accurate IEEE 754 rounding.
        // Wrap in RawDouble to preserve the original JSON text for byte-exact round-trip.
        if (numBuf != null)
        {
            var span = new ReadOnlySpan<char>(numBuf, 0, numLen);
            double dval = double.Parse(span,
                System.Globalization.NumberStyles.Float,
                System.Globalization.CultureInfo.InvariantCulture);
            return new RawDouble(dval, new string(span));
        }

        // Fallback for integer overflow without decimal/exponent
        double result = intValue;
        if (negative) result = -result;
        return result;
    }
}

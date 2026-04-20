using System.Globalization;
using NMSE.Models;

namespace NMSE.Core.Utilities;

/// <summary>
/// Shared helpers for preserving RawDouble-backed integer values when panels write
/// integer UI state back into JSON.
/// </summary>
internal static class RawNumberGuard
{
    /// <summary>
    /// Checks whether a UI integer value matches an existing raw JSON value after applying the
    /// provided clamp range.
    /// </summary>
    /// <param name="existing">The existing JSON value to compare.</param>
    /// <param name="uiValue">The UI integer value from the editor.</param>
    /// <param name="min">The minimum allowed integer value.</param>
    /// <param name="max">The maximum allowed integer value.</param>
    /// <returns><see langword="true"/> when the clamped integer value matches the existing raw value; otherwise <see langword="false"/>.</returns>
    internal static bool IsClampedIntValueUnchanged(object? existing, int uiValue, int min, int max)
        => TryGetInt(existing, out int raw) && uiValue == Math.Clamp(raw, min, max);

    /// <summary>
    /// Writes an integer value into a JSON object only when the existing raw value differs.
    /// </summary>
    /// <param name="target">The JSON object to update.</param>
    /// <param name="key">The property name within the JSON object.</param>
    /// <param name="value">The integer value to write.</param>
    internal static void SetInt(JsonObject? target, string key, int value)
    {
        if (target == null) return;
        var existing = target.Get(key);
        if (MatchesInt(existing, value))
            return;
        target.Set(key, value);
    }

    /// <summary>
    /// Writes an integer value into a JSON array only when the existing raw value differs.
    /// </summary>
    /// <param name="target">The JSON array to update.</param>
    /// <param name="index">The index within the JSON array.</param>
    /// <param name="value">The integer value to write.</param>
    internal static void SetInt(JsonArray? target, int index, int value)
    {
        if (target == null) return;
        var existing = target.Get(index);
        if (MatchesInt(existing, value))
            return;
        target.Set(index, value);
    }

    /// <summary>
    /// Writes a long integer value into a JSON object only when the existing raw value differs.
    /// </summary>
    /// <param name="target">The JSON object to update.</param>
    /// <param name="key">The property name within the JSON object.</param>
    /// <param name="value">The long value to write.</param>
    internal static void SetLong(JsonObject? target, string key, long value)
    {
        if (target == null) return;
        var existing = target.Get(key);
        if (MatchesLong(existing, value))
            return;
        target.Set(key, value);
    }

    /// <summary>
    /// Determines whether an existing raw JSON value represents the same integer as the provided value.
    /// </summary>
    /// <param name="existing">The existing JSON value to compare.</param>
    /// <param name="value">The integer value to compare against.</param>
    /// <returns><see langword="true"/> when the existing value is a <see cref="RawDouble"/> and equals <paramref name="value"/>; otherwise <see langword="false"/>.</returns>
    private static bool MatchesInt(object? existing, int value)
        => existing is RawDouble && TryGetInt(existing, out int raw) && raw == value;

    /// <summary>
    /// Determines whether an existing raw JSON value represents the same long integer as the provided value.
    /// </summary>
    /// <param name="existing">The existing JSON value to compare.</param>
    /// <param name="value">The long value to compare against.</param>
    /// <returns><see langword="true"/> when the existing value is a <see cref="RawDouble"/> and equals <paramref name="value"/>; otherwise <see langword="false"/>.</returns>
    private static bool MatchesLong(object? existing, long value)
        => existing is RawDouble && TryGetLong(existing, out long raw) && raw == value;

    /// <summary>
    /// Attempts to convert an arbitrary value to an integer.
    /// </summary>
    /// <param name="value">The value to convert.</param>
    /// <param name="result">The converted integer result when successful.</param>
    /// <returns><see langword="true"/> when conversion succeeded; otherwise <see langword="false"/>.</returns>
    private static bool TryGetInt(object? value, out int result)
    {
        result = default;
        try
        {
            result = value switch
            {
                RawDouble rd => Convert.ToInt32(rd.Value),
                null => default,
                _ => Convert.ToInt32(value, CultureInfo.InvariantCulture)
            };
            return value != null;
        }
        catch
        {
            return false;
        }
    }

    /// <summary>
    /// Attempts to convert an arbitrary value to a long integer.
    /// </summary>
    /// <param name="value">The value to convert.</param>
    /// <param name="result">The converted long integer result when successful.</param>
    /// <returns><see langword="true"/> when conversion succeeded; otherwise <see langword="false"/>.</returns>
    private static bool TryGetLong(object? value, out long result)
    {
        result = default;
        try
        {
            result = value switch
            {
                RawDouble rd => Convert.ToInt64(rd.Value),
                null => default,
                _ => Convert.ToInt64(value, CultureInfo.InvariantCulture)
            };
            return value != null;
        }
        catch
        {
            return false;
        }
    }
}

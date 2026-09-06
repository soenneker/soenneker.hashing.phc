using System;
using System.Collections.Generic;
using System.Globalization;
using Soenneker.Utils.PooledStringBuilders;

namespace Soenneker.Hashing.Phc;

/// <summary>
/// Parses and formats Password Hashing Competition (PHC) strings.
/// </summary>
public static class PhcFormatter
{
    /// <summary>
    /// Determines whether a value is a valid Password Hashing Competition (PHC) string.
    /// </summary>
    /// <param name="value">The value to validate.</param>
    /// <returns><see langword="true"/> when the value is a valid PHC string; otherwise, <see langword="false"/>.</returns>
    public static bool IsValid(string? value) => TryParse(value, out _);

    /// <summary>
    /// Parses a PHC string.
    /// </summary>
    /// <param name="value">The PHC string to parse.</param>
    /// <returns>The parsed PHC value.</returns>
    /// <exception cref="FormatException"><paramref name="value"/> is not a valid PHC string.</exception>
    public static PhcString Parse(string value)
    {
        ArgumentNullException.ThrowIfNull(value);

        if (TryParse(value, out PhcString? result))
            return result!;

        throw new FormatException("The value is not a valid PHC string.");
    }

    /// <summary>
    /// Tries to parse a PHC string.
    /// </summary>
    /// <param name="value">The PHC string to parse.</param>
    /// <param name="result">The parsed PHC value when successful.</param>
    /// <returns><see langword="true"/> when parsing succeeds; otherwise, <see langword="false"/>.</returns>
    public static bool TryParse(string? value, out PhcString? result)
    {
        result = null;

        if (string.IsNullOrEmpty(value) || value[0] != '$')
            return false;

        string[] fields = value.Split('$');
        if (fields.Length is < 2 or > 6 || fields[0].Length != 0 || !IsName(fields[1]))
            return false;

        var index = 2;
        int? version = null;
        var parameters = new List<PhcParameter>();

        if (index < fields.Length && fields[index].StartsWith("v=", StringComparison.Ordinal))
        {
            ReadOnlySpan<char> versionText = fields[index].AsSpan(2);
            if (versionText.IsEmpty || !int.TryParse(versionText, NumberStyles.None, CultureInfo.InvariantCulture, out int parsedVersion))
                return false;

            version = parsedVersion;
            index++;
        }

        if (index < fields.Length && fields[index].Contains('=', StringComparison.Ordinal))
        {
            string[] pairs = fields[index].Split(',');
            var names = new HashSet<string>(StringComparer.Ordinal);

            foreach (string pair in pairs)
            {
                int separator = pair.IndexOf('=');
                if (separator <= 0 || separator == pair.Length - 1)
                    return false;

                string name = pair[..separator];
                string parameterValue = pair[(separator + 1)..];
                if (!IsName(name) || !IsValue(parameterValue) || !names.Add(name))
                    return false;

                parameters.Add(new PhcParameter(name, parameterValue));
            }

            index++;
        }

        string? salt = index < fields.Length ? fields[index++] : null;
        string? hash = index < fields.Length ? fields[index++] : null;

        if (index != fields.Length || (salt is not null && !IsValue(salt)) || (hash is not null && !IsValue(hash)))
            return false;

        result = new PhcString(fields[1], version, parameters, salt, hash);
        return true;
    }

    /// <summary>
    /// Formats a structured PHC value.
    /// </summary>
    /// <param name="value">The value to format.</param>
    /// <returns>The PHC string.</returns>
    public static string Format(PhcString value)
    {
        ArgumentNullException.ThrowIfNull(value);

        var builder = new PooledStringBuilder(128);
        try
        {
            Append(value, ref builder);
            return builder.ToString();
        }
        finally
        {
            builder.Dispose();
        }
    }

    /// <summary>
    /// Tries to format a PHC value into a destination span.
    /// </summary>
    /// <param name="value">The value to format.</param>
    /// <param name="destination">The span that receives the formatted text.</param>
    /// <param name="charsWritten">The number of characters written.</param>
    /// <returns><see langword="true"/> when the destination is large enough; otherwise, <see langword="false"/>.</returns>
    public static bool TryFormat(PhcString value, Span<char> destination, out int charsWritten)
    {
        ArgumentNullException.ThrowIfNull(value);

        var builder = new PooledStringBuilder(128);
        try
        {
            Append(value, ref builder);
            ReadOnlySpan<char> formatted = builder.AsSpan();

            if (!formatted.TryCopyTo(destination))
            {
                charsWritten = 0;
                return false;
            }

            charsWritten = formatted.Length;
            return true;
        }
        finally
        {
            builder.Dispose();
        }
    }

    private static void Append(PhcString value, ref PooledStringBuilder builder)
    {
        builder.Append('$');
        builder.Append(value.Identifier);

        if (value.Version is int version)
        {
            builder.Append("$v=");
            builder.Append(version);
        }

        if (value.Parameters.Count > 0)
        {
            builder.Append('$');
            for (var i = 0; i < value.Parameters.Count; i++)
            {
                if (i > 0)
                    builder.Append(',');

                PhcParameter parameter = value.Parameters[i];
                builder.Append(parameter.Name);
                builder.Append('=');
                builder.Append(parameter.Value);
            }
        }

        if (value.Salt is not null)
        {
            builder.Append('$');
            builder.Append(value.Salt);
        }

        if (value.Hash is not null)
        {
            builder.Append('$');
            builder.Append(value.Hash);
        }
    }

    internal static void ValidateIdentifier(string value, string parameterName)
    {
        ArgumentNullException.ThrowIfNull(value, parameterName);
        if (!IsName(value))
            throw new ArgumentException("A PHC identifier must contain 1-32 lowercase ASCII letters, digits, or hyphens.", parameterName);
    }

    internal static void ValidateParameter(PhcParameter parameter, string parameterName)
    {
        if (!IsName(parameter.Name) || !IsValue(parameter.Value))
            throw new ArgumentException("A PHC parameter has an invalid name or value.", parameterName);
    }

    internal static string? ValidateData(string? value, string parameterName)
    {
        if (value is not null && !IsValue(value))
            throw new ArgumentException("PHC salt and hash values may contain only ASCII letters, digits, '/', '+', '.', or '-'.", parameterName);

        return value;
    }

    private static bool IsName(ReadOnlySpan<char> value)
    {
        if (value is { Length: 0 or > 32 })
            return false;

        foreach (char character in value)
        {
            if ((character < 'a' || character > 'z') && (character < '0' || character > '9') && character != '-')
                return false;
        }

        return true;
    }

    private static bool IsValue(ReadOnlySpan<char> value)
    {
        if (value.IsEmpty)
            return false;

        foreach (char character in value)
        {
            bool alphanumeric = character is >= 'a' and <= 'z' or >= 'A' and <= 'Z' or >= '0' and <= '9';
            if (!alphanumeric && character is not ('/' or '+' or '.' or '-'))
                return false;
        }

        return true;
    }
}

using System;
using System.Collections.Generic;

namespace Soenneker.Hashing.Phc;

/// <summary>
/// Represents the structured contents of a Password Hashing Competition (PHC) string.
/// </summary>
public sealed class PhcString
{
    private readonly PhcParameter[] _parameters;

    /// <summary>
    /// Gets the algorithm identifier.
    /// </summary>
    public string Identifier { get; }

    /// <summary>
    /// Gets the optional algorithm version.
    /// </summary>
    public int? Version { get; }

    /// <summary>
    /// Gets the parameters in their original order.
    /// </summary>
    public IReadOnlyList<PhcParameter> Parameters => _parameters;

    /// <summary>
    /// Gets the optional encoded salt.
    /// </summary>
    public string? Salt { get; }

    /// <summary>
    /// Gets the optional encoded hash.
    /// </summary>
    public string? Hash { get; }

    /// <summary>
    /// Creates a PHC value.
    /// </summary>
    /// <param name="identifier">The algorithm identifier.</param>
    /// <param name="version">The optional algorithm version.</param>
    /// <param name="parameters">Optional algorithm parameters. Their order is preserved.</param>
    /// <param name="salt">The optional encoded salt.</param>
    /// <param name="hash">The optional encoded hash. A hash requires a salt.</param>
    /// <exception cref="ArgumentException">A component is not valid PHC text.</exception>
    /// <exception cref="ArgumentOutOfRangeException"><paramref name="version"/> is negative.</exception>
    public PhcString(string identifier, int? version = null, IEnumerable<PhcParameter>? parameters = null, string? salt = null, string? hash = null)
    {
        PhcFormatter.ValidateIdentifier(identifier, nameof(identifier));

        if (version < 0)
            throw new ArgumentOutOfRangeException(nameof(version), version, "The PHC version cannot be negative.");

        Identifier = identifier;
        Version = version;
        Salt = PhcFormatter.ValidateData(salt, nameof(salt));
        Hash = PhcFormatter.ValidateData(hash, nameof(hash));

        if (Hash is not null && Salt is null)
            throw new ArgumentException("A PHC hash cannot be specified without a salt.", nameof(hash));

        _parameters = parameters is null ? [] : [.. parameters];
        var names = new HashSet<string>(StringComparer.Ordinal);

        for (var i = 0; i < _parameters.Length; i++)
        {
            PhcFormatter.ValidateParameter(_parameters[i], nameof(parameters));

            if (!names.Add(_parameters[i].Name))
                throw new ArgumentException($"The PHC parameter '{_parameters[i].Name}' is duplicated.", nameof(parameters));
        }
    }

    /// <summary>
    /// Tries to retrieve a parameter by its case-sensitive name.
    /// </summary>
    /// <param name="name">The parameter name.</param>
    /// <param name="value">The parameter value when found.</param>
    /// <returns><see langword="true"/> when the parameter exists; otherwise, <see langword="false"/>.</returns>
    public bool TryGetParameter(string name, out string? value)
    {
        foreach (PhcParameter parameter in _parameters)
        {
            if (!parameter.Name.Equals(name, StringComparison.Ordinal))
                continue;

            value = parameter.Value;
            return true;
        }

        value = null;
        return false;
    }

    /// <summary>
    /// Formats this value as a PHC string.
    /// </summary>
    public override string ToString() => PhcFormatter.Format(this);
}

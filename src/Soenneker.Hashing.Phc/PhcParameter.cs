using System;

namespace Soenneker.Hashing.Phc;

/// <summary>
/// Represents a named parameter in a Password Hashing Competition (PHC) string.
/// </summary>
/// <param name="Name">The parameter name.</param>
/// <param name="Value">The parameter value.</param>
public readonly record struct PhcParameter(string Name, string Value)
{
    /// <summary>
    /// Returns the parameter in <c>name=value</c> form.
    /// </summary>
    public override string ToString() => $"{Name}={Value}";
}

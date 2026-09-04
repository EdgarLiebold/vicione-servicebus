using System;
using System.Globalization;

namespace ViciOne.ServiceBus;

/// <summary>
/// Stable identity of a message contract at a persistence or wire boundary.
/// </summary>
/// <remarks>
/// The identity deliberately excludes CLR assembly identity. Assembly name, assembly version, public-key token, and
/// file location are deployment details and must never be required to deserialize durable ServiceBus infrastructure
/// data. A major version change means the wire contract is intentionally incompatible.
/// </remarks>
public readonly record struct MessageContractIdentity
{
    private const string VersionSeparator = ";v=";

    /// <summary>
    /// Initializes a new instance of the containing type.
    /// </summary>
    /// <param name="name">The name value.</param>
    /// <param name="majorVersion">The major version value.</param>
    public MessageContractIdentity(string name, int majorVersion)
    {
        Name = ValidateName(name);
        MajorVersion = ValidateMajorVersion(majorVersion);
    }

    /// <summary>
    /// Gets the application-owned stable contract name.
    /// </summary>
    public string Name { get; }

    /// <summary>
    /// Gets the incompatible contract generation. Version zero is never valid.
    /// </summary>
    public int MajorVersion { get; }

    /// <summary>
    /// Returns the string representation of this instance.
    /// </summary>
    /// <returns>The result of the operation.</returns>
    public override string ToString()
        => string.Concat(Name, VersionSeparator, MajorVersion.ToString(CultureInfo.InvariantCulture));

    /// <summary>
    /// Parses the supplied representation.
    /// </summary>
    /// <param name="value">The value.</param>
    /// <returns>The result of the operation.</returns>
    public static MessageContractIdentity Parse(string value)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(value);

        int separator = value.LastIndexOf(VersionSeparator, StringComparison.Ordinal);
        if (separator <= 0 || separator + VersionSeparator.Length >= value.Length)
            throw new FormatException($"'{value}' is not a valid message contract identity. Expected '<stable-name>{VersionSeparator}<major>'.");

        ReadOnlySpan<char> version = value.AsSpan(separator + VersionSeparator.Length);
        if (version.Length > 1 && version[0] == '0')
            throw new FormatException($"'{value}' contains a non-canonical contract major version.");

        if (!int.TryParse(version, NumberStyles.None, CultureInfo.InvariantCulture, out int majorVersion))
            throw new FormatException($"'{value}' contains an invalid contract major version.");

        return new MessageContractIdentity(value[..separator], majorVersion);
    }

    /// <summary>
    /// Attempts to parse the supplied representation.
    /// </summary>
    /// <param name="value">The value.</param>
    /// <param name="identity">The identity value.</param>
    /// <returns><see langword="true" /> when the condition is satisfied; otherwise, <see langword="false" />.</returns>
    public static bool TryParse(string? value, out MessageContractIdentity identity)
    {
        identity = default;
        if (string.IsNullOrWhiteSpace(value))
            return false;

        try
        {
            identity = Parse(value);
            return true;
        }
        catch (ArgumentException)
        {
            return false;
        }
        catch (FormatException)
        {
            return false;
        }
    }

    private static string ValidateName(string name)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(name);

        if (name.Length > 256)
            throw new ArgumentOutOfRangeException(nameof(name), name.Length, "A message contract name cannot exceed 256 characters.");

        foreach (char character in name)
        {
            if (char.IsControl(character) || char.IsWhiteSpace(character) || character == ';')
            {
                throw new ArgumentException(
                    "A message contract name cannot contain whitespace, control characters, or ';'.",
                    nameof(name));
            }
        }

        return name;
    }

    private static int ValidateMajorVersion(int majorVersion)
    {
        if (majorVersion is < 1 or > ushort.MaxValue)
        {
            throw new ArgumentOutOfRangeException(
                nameof(majorVersion),
                majorVersion,
                $"Contract major version must be between 1 and {ushort.MaxValue}.");
        }

        return majorVersion;
    }
}

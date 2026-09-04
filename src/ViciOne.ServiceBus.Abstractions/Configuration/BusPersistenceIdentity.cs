using System;
using System.ComponentModel;

#nullable enable

namespace ViciOne.ServiceBus.ProviderAbstractions;
/// <summary>
/// The one stable persistence namespace owned by a typed bus and shared by all persistent features.
/// Applications configure it through the typed AddViciOneServiceBus overload; provider packages consume it here.
/// </summary>
[EditorBrowsable(EditorBrowsableState.Never)]
public sealed class BusPersistenceIdentity<TBus>
    where TBus : class, IBus
{
    public const int MaximumLength = 128;

    private readonly string? _value;

    private BusPersistenceIdentity(string? value)
    {
        _value = value;
    }

    internal static BusPersistenceIdentity<TBus> Default { get; } = new("default");

    internal static BusPersistenceIdentity<TBus> Unspecified { get; } = new(null);

    public static BusPersistenceIdentity<TBus> Create(string value)
        => new(Validate(value));

    public bool IsSpecified => _value is not null;

    public string Require(string feature)
    {
        if (_value is not null)
            return _value;

        throw new ConfigurationException(
            $"Bus '{typeof(TBus)}' uses persistent feature '{feature}' but has no stable persistence identity. "
            + $"Configure AddViciOneServiceBus<{typeof(TBus).Name}>(persistenceIdentity: \"...\", bus => ...).");
    }

    internal static string Validate(string value)
    {
        if (string.IsNullOrWhiteSpace(value))
            throw new ArgumentException("A bus persistence identity must not be empty.", nameof(value));
        if (value.Length > MaximumLength)
        {
            throw new ArgumentOutOfRangeException(
                nameof(value),
                value.Length,
                $"A bus persistence identity cannot exceed {MaximumLength} characters.");
        }

        foreach (char character in value)
        {
            if (char.IsControl(character))
                throw new ArgumentException("A bus persistence identity cannot contain control characters.", nameof(value));
        }

        return value;
    }
}

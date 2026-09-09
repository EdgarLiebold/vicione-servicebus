using System;


namespace ViciOne.ServiceBus.Providers.Persistence;
/// <summary>
/// The one stable persistence namespace owned by a typed bus and shared by all persistent features.
/// Applications configure it through the typed AddViciOneServiceBus overload; provider packages consume it here.
/// </summary>
/// <typeparam name="TBus">The bus type.</typeparam>
public sealed class BusPersistenceIdentity<TBus>
    where TBus : class, IBus
{
    /// <summary>Gets the maximum permitted persistence-identity length.</summary>
    public const int MaximumLength = 128;

    private readonly string? _value;

    private BusPersistenceIdentity(string? value)
    {
        _value = value;
    }

    internal static BusPersistenceIdentity<TBus> Default { get; } = new("default");

    internal static BusPersistenceIdentity<TBus> Unspecified { get; } = new(null);

    /// <summary>Creates a validated persistence identity for this bus type.</summary>
    /// <param name="value">The stable identity shared by the bus's persistent features.</param>
    /// <returns>The validated persistence identity.</returns>
    public static BusPersistenceIdentity<TBus> Create(string value)
        => new(Validate(value));

    /// <summary>Gets whether a persistence identity was configured.</summary>
    public bool IsSpecified => _value is not null;

    /// <summary>Gets the configured persistence identity for a named persistent feature.</summary>
    /// <param name="feature">The persistent feature that requires the identity.</param>
    /// <returns>The configured persistence identity.</returns>
    public string Require(string feature)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(feature);

        if (_value is not null)
            return _value;

        throw new ConfigurationException(
            global::ViciOne.ServiceBus.Providers.Configuration.ConfigurationMessages.Create("Bus Persistence Identity", "unknown", $"Bus '{typeof(TBus)}' uses persistent feature '{feature}' but has no stable persistence identity. "
            + $"Configure AddViciOneServiceBus<{typeof(TBus).Name}>(persistenceIdentity: \"...\", bus => ...).", "Correct the named configuration before starting the host"));
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

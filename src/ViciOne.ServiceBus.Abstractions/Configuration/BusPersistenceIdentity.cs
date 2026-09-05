using System;

#nullable enable

namespace ViciOne.ServiceBus.Providers.Persistence;
/// <summary>
/// The one stable persistence namespace owned by a typed bus and shared by all persistent features.
/// Applications configure it through the typed AddViciOneServiceBus overload; provider packages consume it here.
/// </summary>
public sealed class BusPersistenceIdentity<TBus>
    where TBus : class, IBus
{
    /// <summary>
    /// Defines the maximum length value.
    /// </summary>
    public const int MaximumLength = 128;

    private readonly string? _value;

    private BusPersistenceIdentity(string? value)
    {
        _value = value;
    }

    internal static BusPersistenceIdentity<TBus> Default { get; } = new("default");

    internal static BusPersistenceIdentity<TBus> Unspecified { get; } = new(null);

    /// <summary>
    /// Performs the create operation.
    /// </summary>
    /// <param name="value">The value.</param>
    /// <returns>The result of the operation.</returns>
    public static BusPersistenceIdentity<TBus> Create(string value)
        => new(Validate(value));

    /// <summary>
    /// Gets the is specified value.
    /// </summary>
    public bool IsSpecified => _value is not null;

    /// <summary>
    /// Performs the require operation.
    /// </summary>
    /// <param name="feature">The feature value.</param>
    /// <returns>The result of the operation.</returns>
    public string Require(string feature)
    {
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

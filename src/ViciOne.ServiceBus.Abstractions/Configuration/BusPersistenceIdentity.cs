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
    /// <summary>Exposes the maximum length used by the containing type.</summary>
    public const int MaximumLength = 128;

    private readonly string? _value;

    private BusPersistenceIdentity(string? value)
    {
        _value = value;
    }

    internal static BusPersistenceIdentity<TBus> Default { get; } = new("default");

    internal static BusPersistenceIdentity<TBus> Unspecified { get; } = new(null);

    /// <summary>Creates the requested value.</summary>
    /// <param name="value">The value to process.</param>
    /// <returns>The newly created instance.</returns>
    public static BusPersistenceIdentity<TBus> Create(string value)
        => new(Validate(value));

    /// <summary>Gets a value indicating whether specified.</summary>
    public bool IsSpecified => _value is not null;

    /// <summary>Requires the selected capability.</summary>
    /// <param name="feature">The feature.</param>
    /// <returns>The string produced by the operation.</returns>
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

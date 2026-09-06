using System.Text;
using System.Text.RegularExpressions;

namespace ViciOne.ServiceBus.RabbitMq.Topology;

/// <summary>
/// Provides a rabbit mq entity name validator implementation.
/// </summary>
public sealed partial class RabbitMqEntityNameValidator :
    IEntityNameValidator
{
    const int MaxEntityNameBytes = 255;

    /// <summary>
    /// Gets the validator value.
    /// </summary>
    public static IEntityNameValidator Validator => Cached.EntityNameValidator;

    /// <summary>
    /// Performs the throw if invalid entity name operation.
    /// </summary>
    /// <param name="name">The name value.</param>
    public void ThrowIfInvalidEntityName(string name)
    {
        if (string.IsNullOrWhiteSpace(name))
            throw new RabbitMqAddressException(global::ViciOne.ServiceBus.Providers.Configuration.ConfigurationMessages.Create("RabbitMQ", "unknown", "The entity name must not be null, empty, or whitespace.", "Correct the named configuration before starting the host"));

        if (Encoding.UTF8.GetByteCount(name) > MaxEntityNameBytes)
            throw new RabbitMqAddressException(global::ViciOne.ServiceBus.Providers.Configuration.ConfigurationMessages.Create("RabbitMQ", "unknown", $"The UTF-8 encoded entity name must not exceed {MaxEntityNameBytes} bytes.", "Correct the named configuration before starting the host"));

        if (!EntityNamePattern().IsMatch(name))
        {
            throw new RabbitMqAddressException(
                global::ViciOne.ServiceBus.Providers.Configuration.ConfigurationMessages.Create("RabbitMQ", "unknown", "The entity name may contain only Unicode letters and decimal digits, hyphen, underscore, period, or colon.", "Correct the named configuration before starting the host"));
        }
    }

    /// <summary>
    /// Determines whether valid entity name.
    /// </summary>
    /// <param name="name">The name value.</param>
    /// <returns><see langword="true" /> when the condition is satisfied; otherwise, <see langword="false" />.</returns>
    public bool IsValidEntityName(string name)
    {
        return !string.IsNullOrWhiteSpace(name)
            && Encoding.UTF8.GetByteCount(name) <= MaxEntityNameBytes
            && EntityNamePattern().IsMatch(name);
    }

    [GeneratedRegex(@"^[\p{L}\p{Nd}_\-.:]+$", RegexOptions.CultureInvariant)]
    private static partial Regex EntityNamePattern();


    static class Cached
    {
        internal static readonly IEntityNameValidator EntityNameValidator = new RabbitMqEntityNameValidator();
    }
}

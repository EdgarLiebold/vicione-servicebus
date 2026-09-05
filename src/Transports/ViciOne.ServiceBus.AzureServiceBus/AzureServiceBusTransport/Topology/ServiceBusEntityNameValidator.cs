using System;
using System.Text.RegularExpressions;

namespace ViciOne.ServiceBus.AzureServiceBus.Topology;

/// <summary>
/// Provides a service bus entity name validator implementation.
/// </summary>
public class ServiceBusEntityNameValidator :
    IEntityNameValidator
{
    const int MaxLength = 260;
    static readonly Regex _regex = new Regex(@"^[A-Za-z0-9\-_\.:\/\$]+$", RegexOptions.Compiled);

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
            throw new ConfigurationException(global::ViciOne.ServiceBus.Providers.Configuration.ConfigurationMessages.Create("Azure Service Bus", "unknown", "The Azure Service Bus entity name must not be null or empty.", "Correct the named configuration before starting the host"));

        var success = IsValidEntityName(name);
        if (!success)
        {
            throw new ConfigurationException(
                global::ViciOne.ServiceBus.Providers.Configuration.ConfigurationMessages.Create("Azure Service Bus", "unknown", $"The Azure Service Bus entity name '{name}' must be at most {MaxLength} characters and contain only letters, digits, hyphens, underscores, periods, colons, slashes, or dollar signs.", "Correct the named configuration before starting the host"));
        }
    }

    /// <summary>
    /// Determines whether valid entity name.
    /// </summary>
    /// <param name="name">The name value.</param>
    /// <returns><see langword="true" /> when the condition is satisfied; otherwise, <see langword="false" />.</returns>
    public bool IsValidEntityName(string name)
    {
        return !string.IsNullOrWhiteSpace(name) && name.Length <= MaxLength && _regex.IsMatch(name);
    }


    static class Cached
    {
        internal static readonly IEntityNameValidator EntityNameValidator = new ServiceBusEntityNameValidator();
    }
}

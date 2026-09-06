using System;
using System.Text.RegularExpressions;

namespace ViciOne.ServiceBus.AzureServiceBus.Topology;

/// <summary>Validates Azure Service Bus subscription names against transport length and character constraints.</summary>
public class ServiceBusSubscriptionNameValidator :
    IEntityNameValidator
{
    const int MaxLength = 50;
    static readonly Regex _regex = new Regex(@"^[A-Za-z0-9\-_\.]+$", RegexOptions.Compiled);

    /// <summary>Gets the shared subscription-name validator.</summary>
    public static IEntityNameValidator Validator => Cached.EntityNameValidator;

    /// <summary>Throws a configuration exception when a subscription name is invalid.</summary>
    /// <param name="name">The candidate subscription name.</param>
    public void ThrowIfInvalidEntityName(string name)
    {
        if (string.IsNullOrWhiteSpace(name))
            throw new ConfigurationException(global::ViciOne.ServiceBus.Providers.Configuration.ConfigurationMessages.Create("Azure Service Bus", "unknown", "The Azure Service Bus subscription name must not be null or empty.", "Correct the named configuration before starting the host"));

        var success = IsValidEntityName(name);
        if (!success)
            throw new ConfigurationException(
                global::ViciOne.ServiceBus.Providers.Configuration.ConfigurationMessages.Create("Azure Service Bus", "unknown", $"The Azure Service Bus subscription name '{name}' must be at most {MaxLength} characters and contain only letters, digits, hyphens, underscores, or periods.", "Correct the named configuration before starting the host"));
    }

    /// <summary>Determines whether a subscription name is non-empty, no longer than 50 characters, and contains only supported characters.</summary>
    /// <param name="name">The candidate subscription name.</param>
    /// <returns><see langword="true"/> when the name is valid; otherwise, <see langword="false"/>.</returns>
    public bool IsValidEntityName(string name)
    {
        return !string.IsNullOrWhiteSpace(name) && name.Length <= MaxLength && _regex.IsMatch(name);
    }


    static class Cached
    {
        internal static readonly IEntityNameValidator EntityNameValidator = new ServiceBusSubscriptionNameValidator();
    }
}

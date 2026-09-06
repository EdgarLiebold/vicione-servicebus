using System.Text.RegularExpressions;

namespace ViciOne.ServiceBus.ActiveMq.Topology;

/// <summary>Validates ActiveMQ entity names against the supported portable character set.</summary>
public class ActiveMqEntityNameValidator :
    IEntityNameValidator
{
    static readonly Regex _regex = new Regex(@"^[A-Za-z0-9\-_\.:]+$", RegexOptions.Compiled);

    /// <summary>Gets the shared ActiveMQ entity-name validator.</summary>
    public static IEntityNameValidator Validator => Cached.EntityNameValidator;

    /// <summary>Throws when a name is blank or contains an unsupported character.</summary>
    /// <param name="name">The entity name to validate.</param>
    public void ThrowIfInvalidEntityName(string name)
    {
        if (string.IsNullOrWhiteSpace(name))
            throw new ActiveMqTransportConfigurationException("The entity name must not be null or empty");

        var success = IsValidEntityName(name);
        if (!success)
        {
            throw new ActiveMqTransportConfigurationException(
                "The entity name must be a sequence of these characters: letters, digits, hyphen, underscore, period, or colon.");
        }
    }

    /// <summary>Determines whether a name contains only letters, digits, hyphens, underscores, periods, or colons.</summary>
    /// <param name="name">The entity name to validate.</param>
    /// <returns><see langword="true" /> when the name is valid; otherwise, <see langword="false" />.</returns>
    public bool IsValidEntityName(string name)
    {
        return _regex.Match(name).Success;
    }


    static class Cached
    {
        internal static readonly IEntityNameValidator EntityNameValidator = new ActiveMqEntityNameValidator();
    }
}

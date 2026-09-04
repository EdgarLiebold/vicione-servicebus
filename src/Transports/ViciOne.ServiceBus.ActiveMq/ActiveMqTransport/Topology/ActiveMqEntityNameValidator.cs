using System.Text.RegularExpressions;

namespace ViciOne.ServiceBus.ActiveMq.Topology;

/// <summary>
/// Provides an active mq entity name validator implementation.
/// </summary>
public class ActiveMqEntityNameValidator :
    IEntityNameValidator
{
    static readonly Regex _regex = new Regex(@"^[A-Za-z0-9\-_\.:]+$", RegexOptions.Compiled);

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
            throw new ActiveMqTransportConfigurationException("The entity name must not be null or empty");

        var success = IsValidEntityName(name);
        if (!success)
        {
            throw new ActiveMqTransportConfigurationException(
                "The entity name must be a sequence of these characters: letters, digits, hyphen, underscore, period, or colon.");
        }
    }

    /// <summary>
    /// Determines whether valid entity name.
    /// </summary>
    /// <param name="name">The name value.</param>
    /// <returns><see langword="true" /> when the condition is satisfied; otherwise, <see langword="false" />.</returns>
    public bool IsValidEntityName(string name)
    {
        return _regex.Match(name).Success;
    }


    static class Cached
    {
        internal static readonly IEntityNameValidator EntityNameValidator = new ActiveMqEntityNameValidator();
    }
}

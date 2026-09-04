using System;
using System.Text.RegularExpressions;

namespace ViciOne.ServiceBus.AmazonSqs.Topology;

/// <summary>
/// Provides an amazon sqs entity name validator implementation.
/// </summary>
public class AmazonSqsEntityNameValidator :
    IEntityNameValidator
{
    const string FifoSuffix = ".fifo";
    static readonly Regex _baseNameRegex = new(@"^[A-Za-z0-9\-_]+$", RegexOptions.Compiled | RegexOptions.CultureInvariant);

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
            throw new AmazonSqsTransportConfigurationException("The entity name must not be null or empty");

        var success = IsValidEntityName(name);
        if (!success)
        {
            throw new AmazonSqsTransportConfigurationException(
                "An SQS queue name must be at most 80 characters and contain only letters, digits, hyphens, and underscores, with an optional final '.fifo' suffix.");
        }
    }

    /// <summary>
    /// Determines whether valid entity name.
    /// </summary>
    /// <param name="name">The name value.</param>
    /// <returns><see langword="true" /> when the condition is satisfied; otherwise, <see langword="false" />.</returns>
    public bool IsValidEntityName(string name)
    {
        if (string.IsNullOrWhiteSpace(name) || name.Length > 80)
            return false;

        var baseName = name.EndsWith(FifoSuffix, StringComparison.Ordinal)
            ? name[..^FifoSuffix.Length]
            : name;

        return baseName.Length > 0 && _baseNameRegex.IsMatch(baseName);
    }


    static class Cached
    {
        internal static readonly IEntityNameValidator EntityNameValidator = new AmazonSqsEntityNameValidator();
    }
}

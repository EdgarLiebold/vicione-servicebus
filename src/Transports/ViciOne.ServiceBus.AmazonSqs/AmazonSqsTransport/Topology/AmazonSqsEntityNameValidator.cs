using System;
using System.Text.RegularExpressions;

namespace ViciOne.ServiceBus.AmazonSqs.Topology;

/// <summary>Validates standard and FIFO Amazon SQS queue names.</summary>
public class AmazonSqsEntityNameValidator :
    IEntityNameValidator
{
    const string FifoSuffix = ".fifo";
    static readonly Regex _baseNameRegex = new(@"^[A-Za-z0-9\-_]+$", RegexOptions.Compiled | RegexOptions.CultureInvariant);

    /// <summary>Gets the shared Amazon SQS queue-name validator.</summary>
    public static IEntityNameValidator Validator => Cached.EntityNameValidator;

    /// <summary>Throws when a value is not a valid Amazon SQS queue name.</summary>
    /// <param name="name">The queue name to validate.</param>
    /// <exception cref="AmazonSqsTransportConfigurationException">The name is empty, too long, or contains unsupported characters.</exception>
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

    /// <summary>Determines whether a value is a valid standard or FIFO Amazon SQS queue name.</summary>
    /// <param name="name">The queue name to validate.</param>
    /// <returns><see langword="true"/> when the name meets Amazon SQS length, character, and suffix rules; otherwise, <see langword="false"/>.</returns>
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

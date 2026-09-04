using System;
using System.Text.RegularExpressions;

namespace ViciOne.ServiceBus.AmazonSqsTransport.Topology;

public class AmazonSqsEntityNameValidator :
    IEntityNameValidator
{
    const string FifoSuffix = ".fifo";
    static readonly Regex _baseNameRegex = new(@"^[A-Za-z0-9\-_]+$", RegexOptions.Compiled | RegexOptions.CultureInvariant);

    public static IEntityNameValidator Validator => Cached.EntityNameValidator;

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

using System;
using System.Collections.Generic;
using System.Linq;

namespace ViciOne.ServiceBus;

/// <summary>Reports an invalid service-bus configuration and its validation failures.</summary>
public class ConfigurationException :
    ViciOneServiceBusException
{
    /// <summary>Creates a configuration exception without a custom message or validation results.</summary>
    public ConfigurationException()
    {
    }

    /// <summary>Creates a configuration exception containing a stable snapshot of the validation results.</summary>
    /// <param name="results">The validation results that explain the invalid configuration.</param>
    /// <param name="message">The description of the configuration failure.</param>
    public ConfigurationException(IEnumerable<ValidationResult> results, string message)
        : base(message)
    {
        Results = Snapshot(results);
    }

    /// <summary>Creates a configuration exception containing validation results and an underlying failure.</summary>
    /// <param name="results">The validation results that explain the invalid configuration.</param>
    /// <param name="message">The description of the configuration failure.</param>
    /// <param name="innerException">The exception that caused this configuration failure.</param>
    public ConfigurationException(IEnumerable<ValidationResult> results, string message, Exception innerException)
        : base(message, innerException)
    {
        Results = Snapshot(results);
    }

    /// <summary>Creates a configuration exception with the specified failure message.</summary>
    /// <param name="message">The description of the configuration failure.</param>
    public ConfigurationException(string message)
        : base(message)
    {
    }

    /// <summary>Creates a configuration exception with an underlying failure.</summary>
    /// <param name="message">The description of the configuration failure.</param>
    /// <param name="innerException">The exception that caused this configuration failure.</param>
    public ConfigurationException(string message, Exception innerException)
        : base(message, innerException)
    {
    }

    /// <summary>Gets the immutable snapshot of validation results associated with the failure.</summary>
    /// <remarks>Membership is fixed at construction; entry instances retain their original ownership.</remarks>
    public IReadOnlyList<ValidationResult> Results { get; } = [];

    static IReadOnlyList<ValidationResult> Snapshot(IEnumerable<ValidationResult> results)
    {
        ArgumentNullException.ThrowIfNull(results);

        return Array.AsReadOnly(results.ToArray());
    }
}

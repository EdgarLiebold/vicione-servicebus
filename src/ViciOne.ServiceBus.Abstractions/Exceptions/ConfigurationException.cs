using System;
using System.Collections.Generic;

namespace ViciOne.ServiceBus;

/// <summary>
/// Represents an error related to configuration.
/// </summary>
public class ConfigurationException :
    ViciOneServiceBusException
{
    /// <summary>
    /// Initializes a new instance of the containing type.
    /// </summary>
    public ConfigurationException()
    {
    }

    /// <summary>
    /// Initializes a new instance of the containing type.
    /// </summary>
    /// <param name="results">The results value.</param>
    /// <param name="message">The message value.</param>
    public ConfigurationException(IEnumerable<ValidationResult> results, string message)
        : base(message)
    {
        Results = results;
    }

    /// <summary>
    /// Initializes a new instance of the containing type.
    /// </summary>
    /// <param name="results">The results value.</param>
    /// <param name="message">The message value.</param>
    /// <param name="innerException">The inner exception value.</param>
    public ConfigurationException(IEnumerable<ValidationResult> results, string message, Exception innerException)
        : base(message, innerException)
    {
        Results = results;
    }

    /// <summary>
    /// Initializes a new instance of the containing type.
    /// </summary>
    /// <param name="message">The message value.</param>
    public ConfigurationException(string message)
        : base(message)
    {
    }

    /// <summary>
    /// Initializes a new instance of the containing type.
    /// </summary>
    /// <param name="message">The message value.</param>
    /// <param name="innerException">The inner exception value.</param>
    public ConfigurationException(string message, Exception innerException)
        : base(message, innerException)
    {
    }

    /// <summary>
    /// Gets or sets the results value.
    /// </summary>
    public IEnumerable<ValidationResult> Results { get; protected set; } = [];
}

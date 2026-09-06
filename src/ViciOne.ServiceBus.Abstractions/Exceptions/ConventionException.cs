using System;

namespace ViciOne.ServiceBus;

/// <summary>
/// Represents an error related to convention.
/// </summary>
public class ConventionException :
    ViciOneServiceBusException
{
    /// <summary>
    /// Initializes a new instance of the containing type.
    /// </summary>
    public ConventionException()
    {
    }

    /// <summary>
    /// Initializes a new instance of the containing type.
    /// </summary>
    /// <param name="message">The message value.</param>
    public ConventionException(string message)
        : base(message)
    {
    }

    /// <summary>
    /// Initializes a new instance of the containing type.
    /// </summary>
    /// <param name="message">The message value.</param>
    /// <param name="innerException">The inner exception value.</param>
    public ConventionException(string message, Exception innerException)
        : base(message, innerException)
    {
    }
}

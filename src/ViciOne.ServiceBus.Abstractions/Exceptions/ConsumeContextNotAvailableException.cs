using System;

namespace ViciOne.ServiceBus;

/// <summary>
/// Represents an error related to consume context not available.
/// </summary>
[Serializable]
public class ConsumeContextNotAvailableException :
    ViciOneServiceBusException
{
    /// <summary>
    /// Initializes a new instance of the containing type.
    /// </summary>
    public ConsumeContextNotAvailableException()
        : this("A valid ConsumeContext was not available")
    {
    }

    /// <summary>
    /// Initializes a new instance of the containing type.
    /// </summary>
    /// <param name="message">The message value.</param>
    public ConsumeContextNotAvailableException(string message)
        : base(message)
    {
    }

    /// <summary>
    /// Initializes a new instance of the containing type.
    /// </summary>
    /// <param name="message">The message value.</param>
    /// <param name="innerException">The inner exception value.</param>
    public ConsumeContextNotAvailableException(string message, Exception innerException)
        : base(message, innerException)
    {
    }
}

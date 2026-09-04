using System;

namespace ViciOne.ServiceBus;

/// <summary>
/// Represents an error related to produce.
/// </summary>
[Serializable]
public class ProduceException :
    ViciOneServiceBusException
{
    /// <summary>
    /// Initializes a new instance of the containing type.
    /// </summary>
    public ProduceException()
    {
    }

    /// <summary>
    /// Initializes a new instance of the containing type.
    /// </summary>
    /// <param name="message">The message value.</param>
    public ProduceException(string message)
        : base(message)
    {
    }

    /// <summary>
    /// Initializes a new instance of the containing type.
    /// </summary>
    /// <param name="message">The message value.</param>
    /// <param name="innerException">The inner exception value.</param>
    public ProduceException(string message, Exception innerException)
        : base(message, innerException)
    {
    }
}

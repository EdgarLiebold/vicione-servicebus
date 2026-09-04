using System;

namespace ViciOne.ServiceBus;

/// <summary>
/// Represents an error related to publish.
/// </summary>
[Serializable]
public class PublishException :
    ViciOneServiceBusException
{
    /// <summary>
    /// Initializes a new instance of the containing type.
    /// </summary>
    public PublishException()
    {
    }

    /// <summary>
    /// Initializes a new instance of the containing type.
    /// </summary>
    /// <param name="message">The message value.</param>
    public PublishException(string message)
        : base(message)
    {
    }

    /// <summary>
    /// Initializes a new instance of the containing type.
    /// </summary>
    /// <param name="message">The message value.</param>
    /// <param name="innerException">The inner exception value.</param>
    public PublishException(string message, Exception innerException)
        : base(message, innerException)
    {
    }
}

using System;

namespace ViciOne.ServiceBus;

/// <summary>
/// Represents an error related to message data.
/// </summary>
[Serializable]
public class MessageDataException :
    ViciOneServiceBusException
{
    /// <summary>
    /// Initializes a new instance of the containing type.
    /// </summary>
    public MessageDataException()
    {
    }

    /// <summary>
    /// Initializes a new instance of the containing type.
    /// </summary>
    /// <param name="message">The message value.</param>
    public MessageDataException(string message)
        : base(message)
    {
    }

    /// <summary>
    /// Initializes a new instance of the containing type.
    /// </summary>
    /// <param name="message">The message value.</param>
    /// <param name="innerException">The inner exception value.</param>
    public MessageDataException(string message, Exception innerException)
        : base(message, innerException)
    {
    }
}

using System;

namespace ViciOne.ServiceBus;

/// <summary>
/// Represents an error related to consumer message.
/// </summary>
[Serializable]
public class ConsumerMessageException :
    ConsumerException
{
    /// <summary>
    /// Initializes a new instance of the containing type.
    /// </summary>
    public ConsumerMessageException()
    {
    }

    /// <summary>
    /// Initializes a new instance of the containing type.
    /// </summary>
    /// <param name="message">The message value.</param>
    public ConsumerMessageException(string message)
        : base(message)
    {
    }

    /// <summary>
    /// Initializes a new instance of the containing type.
    /// </summary>
    /// <param name="message">The message value.</param>
    /// <param name="innerException">The inner exception value.</param>
    public ConsumerMessageException(string message, Exception innerException)
        : base(message, innerException)
    {
    }
}

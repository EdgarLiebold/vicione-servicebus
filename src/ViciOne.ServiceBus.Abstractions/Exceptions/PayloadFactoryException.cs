using System;

namespace ViciOne.ServiceBus;

/// <summary>
/// Represents an error related to payload factory.
/// </summary>
[Serializable]
public class PayloadFactoryException :
    PayloadException
{
    /// <summary>
    /// Initializes a new instance of the containing type.
    /// </summary>
    public PayloadFactoryException()
    {
    }

    /// <summary>
    /// Initializes a new instance of the containing type.
    /// </summary>
    /// <param name="message">The message value.</param>
    public PayloadFactoryException(string message)
        : base(message)
    {
    }

    /// <summary>
    /// Initializes a new instance of the containing type.
    /// </summary>
    /// <param name="message">The message value.</param>
    /// <param name="innerException">The inner exception value.</param>
    public PayloadFactoryException(string message, Exception innerException)
        : base(message, innerException)
    {
    }
}

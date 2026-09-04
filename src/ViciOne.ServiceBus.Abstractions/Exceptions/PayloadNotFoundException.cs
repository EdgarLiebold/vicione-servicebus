using System;

namespace ViciOne.ServiceBus;

/// <summary>
/// Represents an error related to payload not found.
/// </summary>
[Serializable]
public class PayloadNotFoundException :
    PayloadException
{
    /// <summary>
    /// Initializes a new instance of the containing type.
    /// </summary>
    public PayloadNotFoundException()
    {
    }

    /// <summary>
    /// Initializes a new instance of the containing type.
    /// </summary>
    /// <param name="message">The message value.</param>
    public PayloadNotFoundException(string message)
        : base(message)
    {
    }

    /// <summary>
    /// Initializes a new instance of the containing type.
    /// </summary>
    /// <param name="message">The message value.</param>
    /// <param name="innerException">The inner exception value.</param>
    public PayloadNotFoundException(string message, Exception innerException)
        : base(message, innerException)
    {
    }
}

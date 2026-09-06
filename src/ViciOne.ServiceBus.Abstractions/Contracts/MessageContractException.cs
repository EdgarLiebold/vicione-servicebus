using System;

namespace ViciOne.ServiceBus;

/// <summary>Raised when durable message contract identity cannot be resolved safely.</summary>
public sealed class MessageContractException : Exception
{
    /// <summary>Initializes a new instance.</summary>
    /// <param name="message">The message to process.</param>
    public MessageContractException(string message)
        : base(message)
    {
    }

    /// <summary>Initializes a new instance.</summary>
    /// <param name="message">The message to process.</param>
    /// <param name="innerException">The inner exception.</param>
    public MessageContractException(string message, Exception innerException)
        : base(message, innerException)
    {
    }
}

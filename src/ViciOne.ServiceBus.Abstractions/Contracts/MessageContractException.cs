using System;

namespace ViciOne.ServiceBus;

/// <summary>Raised when durable message contract identity cannot be resolved safely.</summary>
public sealed class MessageContractException : Exception
{
    /// <summary>Initializes the exception with a description of the contract-identity failure.</summary>
    /// <param name="message">The description of the failure.</param>
    public MessageContractException(string message)
        : base(message)
    {
    }

    /// <summary>Initializes the exception with a description and the failure that prevented contract resolution.</summary>
    /// <param name="message">The description of the failure.</param>
    /// <param name="innerException">The failure that prevented contract resolution.</param>
    public MessageContractException(string message, Exception innerException)
        : base(message, innerException)
    {
    }
}

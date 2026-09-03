using System;

namespace ViciOne.ServiceBus;

/// <summary>
/// Raised when durable message contract identity cannot be resolved safely.
/// </summary>
public sealed class MessageContractException : Exception
{
    public MessageContractException(string message)
        : base(message)
    {
    }

    public MessageContractException(string message, Exception innerException)
        : base(message, innerException)
    {
    }
}

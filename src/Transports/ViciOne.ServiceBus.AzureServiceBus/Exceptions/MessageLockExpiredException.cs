using System;

namespace ViciOne.ServiceBus.AzureServiceBus;

/// <summary>Indicates that Azure Service Bus no longer holds the lock for a received message.</summary>
public class MessageLockExpiredException :
    TransportException
{
    /// <summary>Creates an exception without endpoint details.</summary>
    public MessageLockExpiredException()
    {
    }

    /// <summary>Creates an exception for the endpoint at which the lock expired.</summary>
    /// <param name="uri">The receive endpoint address.</param>
    public MessageLockExpiredException(Uri uri)
        : base(uri)
    {
    }

    /// <summary>Creates an exception with endpoint and diagnostic details.</summary>
    /// <param name="uri">The receive endpoint address.</param>
    /// <param name="message">A description of the lock failure.</param>
    public MessageLockExpiredException(Uri uri, string message)
        : base(uri, message)
    {
    }

    /// <summary>Creates an exception with endpoint, diagnostic details, and the underlying provider failure.</summary>
    /// <param name="uri">The receive endpoint address.</param>
    /// <param name="message">A description of the lock failure.</param>
    /// <param name="innerException">The provider exception that caused the failure.</param>
    public MessageLockExpiredException(Uri uri, string message, Exception innerException)
        : base(uri, message, innerException)
    {
    }
}

using System;

namespace ViciOne.ServiceBus.AzureServiceBus;

/// <summary>Indicates that an Azure Service Bus message exceeded its time-to-live interval.</summary>
public class MessageTimeToLiveExpiredException :
    TransportException
{
    /// <summary>Creates an exception without endpoint details.</summary>
    public MessageTimeToLiveExpiredException()
    {
    }

    /// <summary>Creates an exception for the endpoint at which the message expired.</summary>
    /// <param name="uri">The receive endpoint address.</param>
    public MessageTimeToLiveExpiredException(Uri uri)
        : base(uri)
    {
    }

    /// <summary>Creates an exception with endpoint and diagnostic details.</summary>
    /// <param name="uri">The receive endpoint address.</param>
    /// <param name="message">A description of the expiration failure.</param>
    public MessageTimeToLiveExpiredException(Uri uri, string message)
        : base(uri, message)
    {
    }

    /// <summary>Creates an exception with endpoint, diagnostic details, and the underlying provider failure.</summary>
    /// <param name="uri">The receive endpoint address.</param>
    /// <param name="message">A description of the expiration failure.</param>
    /// <param name="innerException">The provider exception that caused the failure.</param>
    public MessageTimeToLiveExpiredException(Uri uri, string message, Exception innerException)
        : base(uri, message, innerException)
    {
    }
}

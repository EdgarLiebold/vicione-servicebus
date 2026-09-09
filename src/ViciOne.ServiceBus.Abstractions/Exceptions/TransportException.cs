using System;

namespace ViciOne.ServiceBus;

/// <summary>Provides the exception base for failures associated with a transport endpoint.</summary>
public class TransportException :
    AbstractUriException
{
    /// <summary>Creates a transport exception without endpoint context.</summary>
    public TransportException()
    {
    }

    /// <summary>Creates a transport exception for the specified endpoint.</summary>
    /// <param name="uri">The transport endpoint associated with the failure.</param>
    public TransportException(Uri uri)
        : base(uri)
    {
    }

    /// <summary>Creates a transport exception for the specified endpoint.</summary>
    /// <param name="uri">The transport endpoint associated with the failure.</param>
    /// <param name="message">The description of the transport failure.</param>
    public TransportException(Uri uri, string message)
        : base(uri, message)
    {
    }

    /// <summary>Creates a transport exception for the specified endpoint and underlying failure.</summary>
    /// <param name="uri">The transport endpoint associated with the failure.</param>
    /// <param name="message">The description of the transport failure.</param>
    /// <param name="innerException">The exception raised by the transport.</param>
    public TransportException(Uri uri, string message, Exception innerException)
        : base(uri, message, innerException)
    {
    }
}

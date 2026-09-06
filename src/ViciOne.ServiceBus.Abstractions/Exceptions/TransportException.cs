using System;

namespace ViciOne.ServiceBus;

/// <summary>Represents an error related to transport.</summary>
public class TransportException :
    AbstractUriException
{
    /// <summary>Initializes a new instance.</summary>
    public TransportException()
    {
    }

    /// <summary>Initializes a new instance.</summary>
    /// <param name="uri">The uri.</param>
    public TransportException(Uri uri)
        : base(uri)
    {
    }

    /// <summary>Initializes a new instance.</summary>
    /// <param name="uri">The uri.</param>
    /// <param name="message">The message to process.</param>
    public TransportException(Uri uri, string message)
        : base(uri, message)
    {
    }

    /// <summary>Initializes a new instance.</summary>
    /// <param name="uri">The uri.</param>
    /// <param name="message">The message to process.</param>
    /// <param name="innerException">The inner exception.</param>
    public TransportException(Uri uri, string message, Exception innerException)
        : base(uri, message, innerException)
    {
    }
}

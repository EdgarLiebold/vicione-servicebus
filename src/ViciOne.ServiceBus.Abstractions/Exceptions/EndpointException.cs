using System;

namespace ViciOne.ServiceBus;

/// <summary>Represents an error related to endpoint.</summary>
public class EndpointException :
    AbstractUriException
{
    /// <summary>Initializes a new instance.</summary>
    public EndpointException()
    {
    }

    /// <summary>Initializes a new instance.</summary>
    /// <param name="uri">The uri.</param>
    public EndpointException(Uri uri)
        : base(uri)
    {
    }

    /// <summary>Initializes a new instance.</summary>
    /// <param name="uri">The uri.</param>
    /// <param name="message">The message to process.</param>
    public EndpointException(Uri uri, string message)
        : base(uri, message)
    {
    }

    /// <summary>Initializes a new instance.</summary>
    /// <param name="uri">The uri.</param>
    /// <param name="message">The message to process.</param>
    /// <param name="innerException">The inner exception.</param>
    public EndpointException(Uri uri, string message, Exception innerException)
        : base(uri, message, innerException)
    {
    }
}

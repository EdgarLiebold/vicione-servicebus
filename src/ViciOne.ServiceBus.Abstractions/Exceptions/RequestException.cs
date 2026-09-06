using System;

namespace ViciOne.ServiceBus;

/// <summary>Represents an error related to request.</summary>
public class RequestException :
    ViciOneServiceBusException
{
    /// <summary>Initializes a new instance.</summary>
    /// <param name="message">The message to process.</param>
    /// <param name="innerException">The inner exception.</param>
    /// <param name="response">The response.</param>
    public RequestException(string message, Exception innerException, object response)
        : base(message, innerException)
    {
        Response = response;
    }

    /// <summary>Initializes a new instance.</summary>
    public RequestException()
    {
    }

    /// <summary>Initializes a new instance.</summary>
    /// <param name="message">The message to process.</param>
    public RequestException(string message)
        : base(message)
    {
    }

    /// <summary>Initializes a new instance.</summary>
    /// <param name="message">The message to process.</param>
    /// <param name="innerException">The inner exception.</param>
    public RequestException(string message, Exception innerException)
        : base(message, innerException)
    {
    }

    /// <summary>Initializes a new instance.</summary>
    /// <param name="message">The message to process.</param>
    /// <param name="response">The response.</param>
    protected RequestException(string message, object response)
        : base(message)
    {
        Response = response;
    }

    /// <summary>Gets the response.</summary>
    public object? Response { get; }
}

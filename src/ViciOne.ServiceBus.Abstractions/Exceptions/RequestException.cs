using System;

namespace ViciOne.ServiceBus;

/// <summary>
/// Represents an error related to request.
/// </summary>
public class RequestException :
    ViciOneServiceBusException
{
    /// <summary>
    /// Initializes a new instance of the containing type.
    /// </summary>
    /// <param name="message">The message value.</param>
    /// <param name="innerException">The inner exception value.</param>
    /// <param name="response">The response value.</param>
    public RequestException(string message, Exception innerException, object response)
        : base(message, innerException)
    {
        Response = response;
    }

    /// <summary>
    /// Initializes a new instance of the containing type.
    /// </summary>
    public RequestException()
    {
    }

    /// <summary>
    /// Initializes a new instance of the containing type.
    /// </summary>
    /// <param name="message">The message value.</param>
    public RequestException(string message)
        : base(message)
    {
    }

    /// <summary>
    /// Initializes a new instance of the containing type.
    /// </summary>
    /// <param name="message">The message value.</param>
    /// <param name="innerException">The inner exception value.</param>
    public RequestException(string message, Exception innerException)
        : base(message, innerException)
    {
    }

    /// <summary>
    /// Initializes a new instance of the containing type.
    /// </summary>
    /// <param name="message">The message value.</param>
    /// <param name="response">The response value.</param>
    protected RequestException(string message, object response)
        : base(message)
    {
        Response = response;
    }

    /// <summary>
    /// Gets the response value.
    /// </summary>
    public object? Response { get; }
}

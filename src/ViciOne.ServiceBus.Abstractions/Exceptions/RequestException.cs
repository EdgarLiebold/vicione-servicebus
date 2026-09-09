using System;

namespace ViciOne.ServiceBus;

/// <summary>Reports a failure in the request-response lifecycle.</summary>
public class RequestException :
    ViciOneServiceBusException
{
    /// <summary>Creates a request exception without a custom failure message.</summary>
    public RequestException()
    {
    }

    /// <summary>Creates a request exception with the specified failure message.</summary>
    /// <param name="message">The description of the request failure.</param>
    public RequestException(string message)
        : base(message)
    {
    }

    /// <summary>Creates a request exception with an underlying failure.</summary>
    /// <param name="message">The description of the request failure.</param>
    /// <param name="innerException">The exception that caused the request to fail.</param>
    public RequestException(string message, Exception innerException)
        : base(message, innerException)
    {
    }
}

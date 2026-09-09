using System;

namespace ViciOne.ServiceBus;

/// <summary>Reports a connection failure together with its explicit retry classification.</summary>
public class ConnectionException :
    ViciOneServiceBusException
{
    /// <summary>Creates a non-transient connection exception without a custom message.</summary>
    public ConnectionException()
    {
    }

    /// <summary>Creates a non-transient connection exception with the specified failure message.</summary>
    /// <param name="message">The description of the connection failure.</param>
    public ConnectionException(string message)
        : base(message)
    {
    }

    /// <summary>Creates a connection exception with an explicit retry classification.</summary>
    /// <param name="message">The description of the connection failure.</param>
    /// <param name="isTransient">Whether retrying after a delay may restore the connection.</param>
    public ConnectionException(string message, bool isTransient)
        : base(message)
    {
        IsTransient = isTransient;
    }

    /// <summary>Creates a connection exception with an underlying failure and explicit retry classification.</summary>
    /// <param name="message">The description of the connection failure.</param>
    /// <param name="innerException">The exception raised by the connection provider.</param>
    /// <param name="isTransient">Whether retrying after a delay may restore the connection.</param>
    public ConnectionException(string message, Exception? innerException, bool isTransient)
        : base(message, innerException)
    {
        IsTransient = isTransient;
    }

    /// <summary>Gets whether retrying after a delay may restore the connection.</summary>
    public bool IsTransient { get; }
}

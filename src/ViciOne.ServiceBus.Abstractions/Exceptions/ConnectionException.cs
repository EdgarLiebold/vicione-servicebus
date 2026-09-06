using System;

namespace ViciOne.ServiceBus;

/// <summary>Represents an error related to connection.</summary>
public class ConnectionException :
    ViciOneServiceBusException
{
    /// <summary>Initializes a new instance.</summary>
    public ConnectionException()
    {
    }

    /// <summary>Initializes a new instance.</summary>
    /// <param name="isTransient">The is transient.</param>
    public ConnectionException(bool isTransient)
    {
        IsTransient = isTransient;
    }

    /// <summary>Initializes a new instance.</summary>
    /// <param name="message">The message to process.</param>
    /// <param name="isTransient">The is transient.</param>
    public ConnectionException(string message, bool isTransient = false)
        : base(message)
    {
        IsTransient = isTransient;
    }

    /// <summary>Initializes a new instance.</summary>
    /// <param name="message">The message to process.</param>
    /// <param name="innerException">The inner exception.</param>
    /// <param name="isTransient">The is transient.</param>
    public ConnectionException(string message, Exception? innerException, bool isTransient = true)
        : base(message, innerException)
    {
        IsTransient = isTransient;
    }

    /// <summary>Gets a value indicating whether transient.</summary>
    public bool IsTransient { get; }
}

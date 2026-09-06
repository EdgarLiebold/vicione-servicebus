using System;

namespace ViciOne.ServiceBus;

/// <summary>
/// Represents an error related to connection.
/// </summary>
public class ConnectionException :
    ViciOneServiceBusException
{
    /// <summary>
    /// Initializes a new instance of the containing type.
    /// </summary>
    public ConnectionException()
    {
    }

    /// <summary>
    /// Initializes a new instance of the containing type.
    /// </summary>
    /// <param name="isTransient">The is transient value.</param>
    public ConnectionException(bool isTransient)
    {
        IsTransient = isTransient;
    }

    /// <summary>
    /// Initializes a new instance of the containing type.
    /// </summary>
    /// <param name="message">The message value.</param>
    /// <param name="isTransient">The is transient value.</param>
    public ConnectionException(string message, bool isTransient = false)
        : base(message)
    {
        IsTransient = isTransient;
    }

    /// <summary>
    /// Initializes a new instance of the containing type.
    /// </summary>
    /// <param name="message">The message value.</param>
    /// <param name="innerException">The inner exception value.</param>
    /// <param name="isTransient">The is transient value.</param>
    public ConnectionException(string message, Exception? innerException, bool isTransient = true)
        : base(message, innerException)
    {
        IsTransient = isTransient;
    }

    /// <summary>
    /// Gets the is transient value.
    /// </summary>
    public bool IsTransient { get; }
}

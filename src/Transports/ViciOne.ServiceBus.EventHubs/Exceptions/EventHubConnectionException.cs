using System;

namespace ViciOne.ServiceBus.EventHubs;

/// <summary>
/// Represents an error related to event hub connection.
/// </summary>
public class EventHubConnectionException :
    ConnectionException
{
    /// <summary>
    /// Initializes a new instance of the containing type.
    /// </summary>
    public EventHubConnectionException()
    {
    }

    /// <summary>
    /// Initializes a new instance of the containing type.
    /// </summary>
    /// <param name="message">The message value.</param>
    public EventHubConnectionException(string message)
        : base(message)
    {
    }

    /// <summary>
    /// Initializes a new instance of the containing type.
    /// </summary>
    /// <param name="message">The message value.</param>
    /// <param name="innerException">The inner exception value.</param>
    public EventHubConnectionException(string message, Exception innerException)
        : base(message, innerException, IsExceptionTransient(innerException))
    {
    }

    static bool IsExceptionTransient(Exception exception)
    {
        return exception switch
        {
            UnauthorizedAccessException _ => false,
            _ => true
        };
    }
}

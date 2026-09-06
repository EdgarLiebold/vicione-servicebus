using System;

namespace ViciOne.ServiceBus.EventHubs;

/// <summary>Reports a failure while establishing or using an Event Hubs connection.</summary>
public class EventHubConnectionException :
    ConnectionException
{
    /// <summary>Initializes an Event Hubs connection exception without an error message.</summary>
    public EventHubConnectionException()
    {
    }

    /// <summary>Initializes an exception with a descriptive error message.</summary>
    /// <param name="message">The error message.</param>
    public EventHubConnectionException(string message)
        : base(message)
    {
    }

    /// <summary>Initializes an exception and classifies its inner failure as transient or permanent.</summary>
    /// <param name="message">The error message.</param>
    /// <param name="innerException">The provider failure that caused the connection error.</param>
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

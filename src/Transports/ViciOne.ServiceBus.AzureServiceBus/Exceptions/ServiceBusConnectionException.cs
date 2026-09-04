using System;

namespace ViciOne.ServiceBus.AzureServiceBus;

/// <summary>
/// Represents an error related to service bus connection.
/// </summary>
public class ServiceBusConnectionException :
    ConnectionException
{
    /// <summary>
    /// Initializes a new instance of the containing type.
    /// </summary>
    public ServiceBusConnectionException()
    {
    }

    /// <summary>
    /// Initializes a new instance of the containing type.
    /// </summary>
    /// <param name="message">The message value.</param>
    public ServiceBusConnectionException(string message)
        : base(message)
    {
    }

    /// <summary>
    /// Initializes a new instance of the containing type.
    /// </summary>
    /// <param name="message">The message value.</param>
    /// <param name="innerException">The inner exception value.</param>
    public ServiceBusConnectionException(string message, Exception innerException)
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

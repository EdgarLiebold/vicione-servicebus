using System;

namespace ViciOne.ServiceBus.AzureServiceBus;

/// <summary>Indicates that an Azure Service Bus namespace connection could not be established or maintained.</summary>
public class ServiceBusConnectionException :
    ConnectionException
{
    /// <summary>Creates a connection exception without diagnostic details.</summary>
    public ServiceBusConnectionException()
    {
    }

    /// <summary>Creates a connection exception with diagnostic details.</summary>
    /// <param name="message">A description of the connection failure.</param>
    public ServiceBusConnectionException(string message)
        : base(message)
    {
    }

    /// <summary>Creates a connection exception and classifies authorization failures as non-transient.</summary>
    /// <param name="message">A description of the connection failure.</param>
    /// <param name="innerException">The provider exception that caused the failure.</param>
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

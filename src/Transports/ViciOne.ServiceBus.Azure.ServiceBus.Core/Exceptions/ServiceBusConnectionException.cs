using System;

namespace ViciOne.ServiceBus;

public class ServiceBusConnectionException :
    ConnectionException
{
    public ServiceBusConnectionException()
    {
    }

    public ServiceBusConnectionException(string message)
        : base(message)
    {
    }

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

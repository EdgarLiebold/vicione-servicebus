using System;

namespace ViciOne.ServiceBus;

[Serializable]
public class RequestException :
    ViciOneServiceBusException
{
    public RequestException(string message, Exception innerException, object response)
        : base(message, innerException)
    {
        Response = response;
    }

    public RequestException()
    {
    }

    public RequestException(string message)
        : base(message)
    {
    }

    public RequestException(string message, Exception innerException)
        : base(message, innerException)
    {
    }

    protected RequestException(string message, object response)
        : base(message)
    {
        Response = response;
    }

    public object? Response { get; }
}

using System;

namespace ViciOne.ServiceBus.Internals.GraphValidation;

internal class CyclicGraphException :
    ViciOneServiceBusException
{
    public CyclicGraphException()
    {
    }

    public CyclicGraphException(string message)
        : base(message)
    {
    }

    public CyclicGraphException(string message, Exception innerException)
        : base(message, innerException)
    {
    }
}

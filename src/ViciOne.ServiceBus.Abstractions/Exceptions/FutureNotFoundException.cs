using System;

namespace ViciOne.ServiceBus;

[Serializable]
public class FutureNotFoundException :
    ViciOneServiceBusException
{
    public FutureNotFoundException()
    {
    }

    public FutureNotFoundException(Type type, Guid id)
        : base($"Future {TypeCache.GetShortName(type)}({id}) not found")
    {
    }

    public FutureNotFoundException(string message)
        : base(message)
    {
    }

    public FutureNotFoundException(string message, Exception innerException)
        : base(message, innerException)
    {
    }
}

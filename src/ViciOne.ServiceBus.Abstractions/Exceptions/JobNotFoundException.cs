using System;

namespace ViciOne.ServiceBus;

[Serializable]
public class JobNotFoundException :
    ViciOneServiceBusException
{
    public JobNotFoundException()
    {
    }

    public JobNotFoundException(string message)
        : base(message)
    {
    }
}

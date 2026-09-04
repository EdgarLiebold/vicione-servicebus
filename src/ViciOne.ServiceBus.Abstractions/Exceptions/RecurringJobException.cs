using System;

namespace ViciOne.ServiceBus;

[Serializable]
public class RecurringJobException :
    ViciOneServiceBusException
{
    public RecurringJobException()
    {
    }

    public RecurringJobException(string message)
        : base(message)
    {
    }
}

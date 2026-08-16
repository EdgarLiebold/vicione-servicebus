namespace ViciOne.ServiceBus;

using System;


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

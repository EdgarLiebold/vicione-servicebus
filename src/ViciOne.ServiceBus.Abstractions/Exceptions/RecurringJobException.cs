// ViciOne modification: WP-F2-SERVICEBUS-IDENTITY, 2026-08-07.
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

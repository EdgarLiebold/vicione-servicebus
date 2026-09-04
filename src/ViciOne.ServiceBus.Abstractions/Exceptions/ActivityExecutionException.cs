using System;

namespace ViciOne.ServiceBus;

[Serializable]
public class ActivityExecutionException :
    CourierException
{
    public ActivityExecutionException()
    {
    }

    public ActivityExecutionException(string message)
        : base(message)
    {
    }

    public ActivityExecutionException(string message, Exception innerException)
        : base(message, innerException)
    {
    }
}

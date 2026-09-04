using System;

namespace ViciOne.ServiceBus;

[Serializable]
public class RoutingSlipException :
    CourierException
{
    public RoutingSlipException()
    {
    }

    public RoutingSlipException(string message)
        : base(message)
    {
    }

    public RoutingSlipException(string message, Exception innerException)
        : base(message, innerException)
    {
    }
}

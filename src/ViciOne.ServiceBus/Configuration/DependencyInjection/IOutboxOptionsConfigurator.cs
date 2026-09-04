using System;

namespace ViciOne.ServiceBus;

public interface IOutboxOptionsConfigurator
{
    /// <summary>
    /// The number of messages to deliver at a time from the outbox to the broker
    /// </summary>
    public int MessageDeliveryLimit { set; }

    /// <summary>
    /// Transport Send timeout when delivering messages to the transport
    /// </summary>
    TimeSpan MessageDeliveryTimeout { set; }
}

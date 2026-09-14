using System;

namespace ViciOne.ServiceBus.Configuration;

/// <summary>Configures delivery from a consume outbox to the transport.</summary>
public interface IOutboxOptionsConfigurator
{
    /// <summary>Sets the maximum number of stored messages delivered in one batch.</summary>
    int MessageDeliveryLimit { set; }

    /// <summary>Sets the timeout for each transport send performed by outbox delivery.</summary>
    TimeSpan MessageDeliveryTimeout { set; }
}

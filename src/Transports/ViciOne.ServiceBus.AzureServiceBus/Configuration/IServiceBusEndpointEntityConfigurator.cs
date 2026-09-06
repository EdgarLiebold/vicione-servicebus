using System;

namespace ViciOne.ServiceBus.AzureServiceBus;

/// <summary>Configures nullable Azure Service Bus properties shared by queue and subscription entities.</summary>
public interface IServiceBusEndpointEntityConfigurator :
    IServiceBusEntityConfigurator
{
    /// <summary>Sets the initial lock duration for received messages.</summary>
    TimeSpan? LockDuration { set; }

    /// <summary>Sets the maximum delivery count. A message is automatically dead lettered after this number of deliveries.</summary>
    int? MaxDeliveryCount { set; }

    /// <summary>Sets whether the entity requires sessions for inbound messages.</summary>
    bool? RequiresSession { set; }

    /// <summary>Sets the maximum number of concurrent sessions.</summary>
    int? MaxConcurrentSessions { set; }

    /// <summary>Sets the maximum number of concurrent message callbacks for each session.</summary>
    int? MaxConcurrentCallsPerSession { set; }

    /// <summary>Sets whether expired messages are moved to the entity's dead-letter subqueue.</summary>
    bool? EnableDeadLetteringOnMessageExpiration { set; }

    /// <summary>Sets the path to the recipient to which the dead lettered message is forwarded.</summary>
    string ForwardDeadLetteredMessagesTo { set; }
}

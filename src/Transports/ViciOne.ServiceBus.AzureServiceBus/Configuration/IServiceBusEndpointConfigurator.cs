using System;

namespace ViciOne.ServiceBus.AzureServiceBus;

/// <summary>Configures Azure Service Bus entity and processor properties shared by queue and subscription endpoints.</summary>
public interface IServiceBusEndpointConfigurator
{

    /// <summary>Sets the idle duration after which Azure Service Bus deletes the queue or subscription.</summary>
    TimeSpan AutoDeleteOnIdle { set; }

    /// <summary>Sets the default lifetime of messages in the entity.</summary>
    TimeSpan DefaultMessageTimeToLive { set; }

    /// <summary>Sets a value that indicates whether server-side batched operations are enabled.</summary>
    bool EnableBatchedOperations { set; }

    /// <summary>Sets whether expired messages are moved to the entity's dead-letter subqueue.</summary>
    bool EnableDeadLetteringOnMessageExpiration { set; }

    /// <summary>Sets the path to the recipient to which the dead lettered message is forwarded.</summary>
    string ForwardDeadLetteredMessagesTo { set; }

    /// <summary>Sets the initial lock duration for received messages.</summary>
    TimeSpan LockDuration { set; }

    /// <summary>Sets the maximum delivery count. A message is automatically dead-lettered after this number of deliveries.</summary>
    int MaxDeliveryCount { set; }

    /// <summary>Sets whether the entity requires sessions for inbound messages.</summary>
    bool RequiresSession { set; }

    /// <summary>Sets the maximum number of sessions processed concurrently.</summary>
    int MaxConcurrentSessions { set; }

    /// <summary>Sets the maximum number of concurrent message callbacks for each session.</summary>
    int MaxConcurrentCallsPerSession { set; }

    /// <summary>Sets application-defined metadata stored with the entity.</summary>
    string UserMetadata { set; }

    /// <summary>Sets the maximum idle time to wait for a message from an accepted session.</summary>
    TimeSpan? SessionIdleTimeout { set; }

    /// <summary>Sets the maximum duration for automatic message- or session-lock renewal.</summary>
    TimeSpan MaxAutoRenewDuration { set; }
}

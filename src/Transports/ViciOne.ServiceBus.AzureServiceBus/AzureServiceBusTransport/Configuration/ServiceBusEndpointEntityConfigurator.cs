using System;

namespace ViciOne.ServiceBus.AzureServiceBus.Configuration;

/// <summary>Captures Azure Service Bus queue or subscription properties used by a receive endpoint.</summary>
public class ServiceBusEndpointEntityConfigurator :
    ServiceBusEntityConfigurator,
    IServiceBusEndpointEntityConfigurator
{
    /// <summary>Gets or sets whether expired messages are moved to the entity's dead-letter subqueue.</summary>
    public bool? EnableDeadLetteringOnMessageExpiration { get; set; }

    /// <summary>Gets or sets the entity path to which dead-lettered messages are forwarded.</summary>
    public string? ForwardDeadLetteredMessagesTo { get; set; }

    /// <summary>Gets or sets the initial lock duration for received messages.</summary>
    public TimeSpan? LockDuration { get; set; }

    /// <summary>Gets or sets the delivery-attempt limit before a message is dead-lettered.</summary>
    public int? MaxDeliveryCount { get; set; }

    /// <summary>Gets or sets whether the entity requires sessions.</summary>
    public bool? RequiresSession { get; set; }

    /// <summary>Gets or sets the maximum number of sessions processed concurrently.</summary>
    public int? MaxConcurrentSessions { get; set; }
    /// <summary>Gets or sets the maximum number of concurrent message callbacks for each session.</summary>
    public int? MaxConcurrentCallsPerSession { get; set; }
}

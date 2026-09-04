using System;

namespace ViciOne.ServiceBus.AzureServiceBus.Configuration;

/// <summary>
/// Provides a service bus endpoint entity configurator implementation.
/// </summary>
public class ServiceBusEndpointEntityConfigurator :
    ServiceBusEntityConfigurator,
    IServiceBusEndpointEntityConfigurator
{
    /// <summary>
    /// Gets or sets the enable dead lettering on message expiration value.
    /// </summary>
    public bool? EnableDeadLetteringOnMessageExpiration { get; set; }

    /// <summary>
    /// Gets or sets the forward dead lettered messages to value.
    /// </summary>
    public string? ForwardDeadLetteredMessagesTo { get; set; }

    /// <summary>
    /// Gets or sets the lock duration value.
    /// </summary>
    public TimeSpan? LockDuration { get; set; }

    /// <summary>
    /// Gets or sets the max delivery count value.
    /// </summary>
    public int? MaxDeliveryCount { get; set; }

    /// <summary>
    /// Gets or sets the requires session value.
    /// </summary>
    public bool? RequiresSession { get; set; }

    /// <summary>
    /// Gets or sets the max concurrent sessions value.
    /// </summary>
    public int? MaxConcurrentSessions { get; set; }
    /// <summary>
    /// Gets or sets the max concurrent calls per session value.
    /// </summary>
    public int? MaxConcurrentCallsPerSession { get; set; }
}

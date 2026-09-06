using System;

namespace ViciOne.ServiceBus.AzureServiceBus.Configuration;

/// <summary>Captures common Azure Service Bus queue, topic, and subscription properties.</summary>
public abstract class ServiceBusEntityConfigurator :
    IServiceBusEntityConfigurator
{
    /// <summary>Initializes the entity with the transport's default message lifetime.</summary>
    protected ServiceBusEntityConfigurator()
    {
        DefaultMessageTimeToLive = Defaults.DefaultMessageTimeToLive;
    }

    /// <summary>Gets or sets the idle duration after which Azure Service Bus deletes the entity.</summary>
    public TimeSpan? AutoDeleteOnIdle { get; set; }

    /// <summary>Gets or sets the default lifetime of messages sent to the entity.</summary>
    public TimeSpan? DefaultMessageTimeToLive { get; set; }

    /// <summary>Gets or sets whether server-side batched operations are enabled for the entity.</summary>
    public bool? EnableBatchedOperations { get; set; }

    /// <summary>Gets or sets application-defined metadata stored with the entity.</summary>
    public string? UserMetadata { get; set; }
}

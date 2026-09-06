using System;

namespace ViciOne.ServiceBus.AzureServiceBus;

/// <summary>Configures properties common to Azure Service Bus queues, topics, and subscriptions.</summary>
public interface IServiceBusEntityConfigurator
{
    /// <summary>Sets the idle duration after which Azure Service Bus deletes the entity.</summary>
    TimeSpan? AutoDeleteOnIdle { set; }

    /// <summary>Sets the default lifetime of messages in the entity.</summary>
    TimeSpan? DefaultMessageTimeToLive { set; }

    /// <summary>Sets a value that indicates whether server-side batched operations are enabled.</summary>
    bool? EnableBatchedOperations { set; }

    /// <summary>Sets application-defined metadata stored with the entity.</summary>
    string UserMetadata { set; }
}

using System;

namespace ViciOne.ServiceBus.AzureServiceBus;

/// <summary>Configures queue-specific entity properties on an Azure Service Bus receive endpoint.</summary>
public interface IServiceBusQueueEndpointConfigurator :
    IServiceBusEndpointConfigurator
{
    /// <summary>Sets how long Azure Service Bus retains message identifiers for duplicate detection.</summary>
    TimeSpan DuplicateDetectionHistoryTimeWindow { set; }

    /// <summary>Sets whether the queue is partitioned across message brokers.</summary>
    bool EnablePartitioning { set; }

    /// <summary>Sets the maximum queue size in megabytes.</summary>
    long MaxSizeInMegabytes { set; }

    /// <summary>Set the maximum message size, in kilobytes.</summary>
    long MaxMessageSizeInKilobytes { set; }

    /// <summary>Sets whether Azure Service Bus rejects duplicate message identifiers.</summary>
    bool RequiresDuplicateDetection { set; }

    /// <summary>Enables duplicate detection and sets its identifier-retention window.</summary>
    /// <param name="historyTimeWindow">How long message identifiers remain available for duplicate detection.</param>
    void EnableDuplicateDetection(TimeSpan historyTimeWindow);
}

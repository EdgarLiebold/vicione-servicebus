using System;

namespace ViciOne.ServiceBus.AzureServiceBus;

/// <summary>Configures creation properties shared by Azure Service Bus queues and topics.</summary>
public interface IServiceBusMessageEntityConfigurator :
    IServiceBusEntityConfigurator
{
    /// <summary>Gets the entity path relative to the base path.</summary>
    string Path { get; }

    /// <summary>Gets or sets the namespace-relative prefix applied to the entity path.</summary>
    string? BasePath { get; set; }

    /// <summary>Gets the entity path including its optional base-path prefix.</summary>
    string FullPath { get; }

    /// <summary>Sets how long Azure Service Bus retains message identifiers for duplicate detection.</summary>
    TimeSpan? DuplicateDetectionHistoryTimeWindow { set; }

    /// <summary>Sets whether the entity is partitioned across message brokers.</summary>
    bool? EnablePartitioning { set; }

    /// <summary>Sets the maximum entity size in megabytes.</summary>
    long? MaxSizeInMegabytes { set; }

    /// <summary>Set the maximum message size, in kilobytes.</summary>
    long? MaxMessageSizeInKilobytes { set; }

    /// <summary>Sets whether Azure Service Bus rejects duplicate message identifiers.</summary>
    bool? RequiresDuplicateDetection { set; }

    /// <summary>Enables duplicate detection and sets its identifier-retention window.</summary>
    /// <param name="historyTimeWindow">How long message identifiers remain available for duplicate detection.</param>
    void EnableDuplicateDetection(TimeSpan historyTimeWindow);
}

using System;

namespace ViciOne.ServiceBus.AzureServiceBus.Configuration;

/// <summary>Captures the common creation properties of Azure Service Bus queues and topics.</summary>
public abstract class ServiceBusMessageEntityConfigurator :
    ServiceBusEntityConfigurator,
    IServiceBusMessageEntityConfigurator
{
    string? _basePath;

    /// <summary>Initializes an entity with the transport's durable entity defaults.</summary>
    /// <param name="path">The entity path relative to the configured base path.</param>
    protected ServiceBusMessageEntityConfigurator(string path)
    {
        Path = path;

        AutoDeleteOnIdle = Defaults.AutoDeleteOnIdle;
        DefaultMessageTimeToLive = Defaults.DefaultMessageTimeToLive;
        EnableBatchedOperations = true;
    }

    /// <summary>Gets or sets the entity path relative to <see cref="BasePath"/>.</summary>
    public string Path { get; set; }

    /// <summary>Gets or sets the optional namespace-relative prefix applied to the entity path.</summary>
    public string? BasePath
    {
        get => _basePath;
        set => _basePath = value?.Trim('/');
    }

    /// <summary>Gets the entity path including its optional base-path prefix.</summary>
    public string FullPath => string.IsNullOrEmpty(BasePath) ? Path : $"{BasePath}/{Path.Trim('/')}";

    /// <summary>Gets or sets how long Azure Service Bus retains message identifiers for duplicate detection.</summary>
    public TimeSpan? DuplicateDetectionHistoryTimeWindow { get; set; }

    /// <summary>Gets or sets whether the entity is partitioned.</summary>
    public bool? EnablePartitioning { get; set; }

    /// <summary>Gets or sets the maximum entity size in megabytes.</summary>
    public long? MaxSizeInMegabytes { get; set; }

    /// <summary>Gets or sets the maximum individual message size in kilobytes.</summary>
    public long? MaxMessageSizeInKilobytes { get; set; }

    /// <summary>Gets or sets whether Azure Service Bus rejects duplicate message identifiers.</summary>
    public bool? RequiresDuplicateDetection { get; set; }

    /// <summary>Enables duplicate detection and sets its identifier-retention window.</summary>
    /// <param name="historyTimeWindow">How long message identifiers remain available for duplicate detection.</param>
    public void EnableDuplicateDetection(TimeSpan historyTimeWindow)
    {
        RequiresDuplicateDetection = true;
        DuplicateDetectionHistoryTimeWindow = historyTimeWindow;
    }
}

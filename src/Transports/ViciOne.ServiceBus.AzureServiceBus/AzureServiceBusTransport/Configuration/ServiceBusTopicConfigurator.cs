using System;
using System.Collections.Generic;
using Azure.Messaging.ServiceBus.Administration;
using ViciOne.ServiceBus.AzureServiceBus.Topology;

namespace ViciOne.ServiceBus.AzureServiceBus.Configuration;

/// <summary>Builds and validates the Azure Service Bus creation options for a topic.</summary>
public class ServiceBusTopicConfigurator :
    ServiceBusMessageEntityConfigurator,
    IServiceBusTopicConfigurator
{
    /// <summary>Initializes a topic and applies the temporary-entity idle timeout when requested.</summary>
    /// <param name="topicPath">The topic path.</param>
    /// <param name="temporary">Whether the topic should use the transport's temporary-entity lifetime.</param>
    public ServiceBusTopicConfigurator(string topicPath, bool temporary)
        : base(topicPath)
    {
        if (temporary)
            AutoDeleteOnIdle = Defaults.TemporaryAutoDeleteOnIdle;
    }

    /// <summary>Validates the topic path and Azure Service Bus idle-deletion constraint.</summary>
    /// <returns>The topic configuration failures.</returns>
    public IEnumerable<ValidationResult> Validate()
    {
        if (!ServiceBusEntityNameValidator.Validator.IsValidEntityName(FullPath))
            yield return this.Failure("Path", $"must be a valid topic path: {FullPath}");

        if (AutoDeleteOnIdle.HasValue && AutoDeleteOnIdle != TimeSpan.Zero && AutoDeleteOnIdle < TimeSpan.FromMinutes(5))
            yield return this.Failure("AutoDeleteOnIdle", "must be zero, or >= 5:00");
    }

    /// <summary>Gets or sets whether the topic supports ordered message processing.</summary>
    public bool? SupportOrdering { get; set; }

    /// <summary>Projects the configured values into Azure SDK topic-creation options.</summary>
    /// <returns>The SDK options for creating or comparing the topic.</returns>
    public CreateTopicOptions GetCreateTopicOptions()
    {
        var options = new CreateTopicOptions(FullPath);

        if (AutoDeleteOnIdle.HasValue)
            options.AutoDeleteOnIdle = AutoDeleteOnIdle.Value;

        if (DefaultMessageTimeToLive.HasValue)
            options.DefaultMessageTimeToLive = DefaultMessageTimeToLive.Value;

        if (DuplicateDetectionHistoryTimeWindow.HasValue)
            options.DuplicateDetectionHistoryTimeWindow = DuplicateDetectionHistoryTimeWindow.Value;

        if (EnableBatchedOperations.HasValue)
            options.EnableBatchedOperations = EnableBatchedOperations.Value;

        if (EnablePartitioning.HasValue)
            options.EnablePartitioning = EnablePartitioning.Value;

        if (MaxSizeInMegabytes.HasValue)
            options.MaxSizeInMegabytes = MaxSizeInMegabytes.Value;

        if (MaxMessageSizeInKilobytes.HasValue)
            options.MaxMessageSizeInKilobytes = MaxMessageSizeInKilobytes;

        if (RequiresDuplicateDetection.HasValue)
            options.RequiresDuplicateDetection = RequiresDuplicateDetection.Value;

        if (SupportOrdering.HasValue)
            options.SupportOrdering = SupportOrdering.Value;

        if (!string.IsNullOrWhiteSpace(UserMetadata))
            options.UserMetadata = UserMetadata;

        return options;
    }
}

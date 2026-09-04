using System;
using System.Collections.Generic;
using Azure.Messaging.ServiceBus.Administration;
using ViciOne.ServiceBus.AzureServiceBus.Topology;

namespace ViciOne.ServiceBus.AzureServiceBus.Configuration;

/// <summary>
/// Provides a service bus topic configurator implementation.
/// </summary>
public class ServiceBusTopicConfigurator :
    ServiceBusMessageEntityConfigurator,
    IServiceBusTopicConfigurator
{
    /// <summary>
    /// Initializes a new instance of the containing type.
    /// </summary>
    /// <param name="topicPath">The topic path value.</param>
    /// <param name="temporary">The temporary value.</param>
    public ServiceBusTopicConfigurator(string topicPath, bool temporary)
        : base(topicPath)
    {
        if (temporary)
            AutoDeleteOnIdle = Defaults.TemporaryAutoDeleteOnIdle;
    }

    /// <summary>
    /// Validates the current configuration.
    /// </summary>
    /// <returns>The result of the operation.</returns>
    public IEnumerable<ValidationResult> Validate()
    {
        if (!ServiceBusEntityNameValidator.Validator.IsValidEntityName(Path))
            yield return this.Failure("Path", $"must be a valid topic path: {Path}");

        if (AutoDeleteOnIdle.HasValue && AutoDeleteOnIdle != TimeSpan.Zero && AutoDeleteOnIdle < TimeSpan.FromMinutes(5))
            yield return this.Failure("AutoDeleteOnIdle", "must be zero, or >= 5:00");
    }

    /// <summary>
    /// Gets or sets the support ordering value.
    /// </summary>
    public bool? SupportOrdering { get; set; }

    /// <summary>
    /// Gets create topic options.
    /// </summary>
    /// <returns>The result of the operation.</returns>
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

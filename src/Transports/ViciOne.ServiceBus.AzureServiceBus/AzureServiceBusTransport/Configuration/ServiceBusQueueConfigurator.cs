using System;
using System.Collections.Generic;
using Azure.Messaging.ServiceBus.Administration;
using ViciOne.ServiceBus.AzureServiceBus.Topology;

namespace ViciOne.ServiceBus.AzureServiceBus.Configuration;

/// <summary>
/// Provides a service bus queue configurator implementation.
/// </summary>
public class ServiceBusQueueConfigurator :
    ServiceBusMessageEntityConfigurator,
    IServiceBusQueueConfigurator
{
    /// <summary>
    /// Initializes a new instance of the containing type.
    /// </summary>
    /// <param name="path">The path value.</param>
    public ServiceBusQueueConfigurator(string path)
        : base(path)
    {
        EnableDeadLetteringOnMessageExpiration = true;
        LockDuration = Defaults.LockDuration;
        MaxDeliveryCount = 5;
    }

    /// <summary>
    /// Gets or sets the enable dead lettering on message expiration value.
    /// </summary>
    public bool? EnableDeadLetteringOnMessageExpiration { get; set; }

    /// <summary>
    /// Gets or sets the enable dead lettering on filter evaluation exceptions value.
    /// </summary>
    public bool? EnableDeadLetteringOnFilterEvaluationExceptions { get; set; }

    /// <summary>
    /// Gets or sets the forward dead lettered messages to value.
    /// </summary>
    public string? ForwardDeadLetteredMessagesTo { get; set; }

    /// <summary>
    /// Gets or sets the forward to value.
    /// </summary>
    public string? ForwardTo { get; set; }

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

    /// <summary>
    /// Validates the current configuration.
    /// </summary>
    /// <returns>The result of the operation.</returns>
    public IEnumerable<ValidationResult> Validate()
    {
        if (!ServiceBusEntityNameValidator.Validator.IsValidEntityName(Path))
            yield return this.Failure("Path", $"must be a valid queue path: {Path}");

        if (AutoDeleteOnIdle.HasValue && AutoDeleteOnIdle != TimeSpan.Zero && AutoDeleteOnIdle < TimeSpan.FromMinutes(5))
            yield return this.Failure("AutoDeleteOnIdle", "must be zero, or >= 5:00");
    }

    /// <summary>
    /// Gets create queue options.
    /// </summary>
    /// <returns>The result of the operation.</returns>
    public CreateQueueOptions GetCreateQueueOptions()
    {
        var options = new CreateQueueOptions(FullPath);

        if (AutoDeleteOnIdle.HasValue)
            options.AutoDeleteOnIdle = AutoDeleteOnIdle.Value;

        if (DefaultMessageTimeToLive.HasValue)
            options.DefaultMessageTimeToLive = DefaultMessageTimeToLive.Value;

        if (DuplicateDetectionHistoryTimeWindow.HasValue)
            options.DuplicateDetectionHistoryTimeWindow = DuplicateDetectionHistoryTimeWindow.Value;

        if (EnableBatchedOperations.HasValue)
            options.EnableBatchedOperations = EnableBatchedOperations.Value;

        if (EnableDeadLetteringOnMessageExpiration.HasValue)
            options.DeadLetteringOnMessageExpiration = EnableDeadLetteringOnMessageExpiration.Value;

        if (EnablePartitioning.HasValue)
            options.EnablePartitioning = EnablePartitioning.Value;

        if (!string.IsNullOrWhiteSpace(ForwardDeadLetteredMessagesTo))
            options.ForwardDeadLetteredMessagesTo = ForwardDeadLetteredMessagesTo;

        if (!string.IsNullOrWhiteSpace(ForwardTo))
            options.ForwardTo = ForwardTo;

        if (LockDuration.HasValue)
            options.LockDuration = LockDuration.Value;

        if (MaxDeliveryCount.HasValue)
            options.MaxDeliveryCount = MaxDeliveryCount.Value;

        if (MaxSizeInMegabytes.HasValue)
            options.MaxSizeInMegabytes = MaxSizeInMegabytes.Value;

        if (MaxMessageSizeInKilobytes.HasValue)
            options.MaxMessageSizeInKilobytes = MaxMessageSizeInKilobytes;

        if (RequiresDuplicateDetection.HasValue)
            options.RequiresDuplicateDetection = RequiresDuplicateDetection.Value;

        if (RequiresSession.HasValue)
            options.RequiresSession = RequiresSession.Value;

        if (!string.IsNullOrWhiteSpace(UserMetadata))
            options.UserMetadata = UserMetadata;

        return options;
    }

    /// <summary>
    /// Gets queue address.
    /// </summary>
    /// <param name="hostAddress">The host address value.</param>
    /// <returns>The result of the operation.</returns>
    public Uri GetQueueAddress(Uri hostAddress)
    {
        return new ServiceBusEndpointAddress(hostAddress, Path, AutoDeleteOnIdle);
    }
}

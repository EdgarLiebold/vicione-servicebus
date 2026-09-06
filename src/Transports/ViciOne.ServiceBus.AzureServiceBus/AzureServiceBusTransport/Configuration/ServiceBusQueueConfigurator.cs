using System;
using System.Collections.Generic;
using Azure.Messaging.ServiceBus.Administration;
using ViciOne.ServiceBus.AzureServiceBus.Topology;

namespace ViciOne.ServiceBus.AzureServiceBus.Configuration;

/// <summary>Builds and validates the Azure Service Bus creation options for a queue.</summary>
public class ServiceBusQueueConfigurator :
    ServiceBusMessageEntityConfigurator,
    IServiceBusQueueConfigurator
{
    /// <summary>Initializes a queue with the transport's dead-lettering, lock, and delivery-count defaults.</summary>
    /// <param name="path">The queue path relative to the configured base path.</param>
    public ServiceBusQueueConfigurator(string path)
        : base(path)
    {
        EnableDeadLetteringOnMessageExpiration = true;
        LockDuration = Defaults.LockDuration;
        MaxDeliveryCount = 5;
    }

    /// <summary>Gets or sets whether expired messages are moved to the queue's dead-letter subqueue.</summary>
    public bool? EnableDeadLetteringOnMessageExpiration { get; set; }

    /// <summary>Gets or sets whether subscription filter evaluation failures are dead-lettered.</summary>
    public bool? EnableDeadLetteringOnFilterEvaluationExceptions { get; set; }

    /// <summary>Gets or sets the entity path to which dead-lettered messages are forwarded.</summary>
    public string? ForwardDeadLetteredMessagesTo { get; set; }

    /// <summary>Gets or sets the entity path to which active messages are forwarded.</summary>
    public string? ForwardTo { get; set; }

    /// <summary>Gets or sets the initial lock duration for received messages.</summary>
    public TimeSpan? LockDuration { get; set; }

    /// <summary>Gets or sets the delivery-attempt limit before a message is dead-lettered.</summary>
    public int? MaxDeliveryCount { get; set; }

    /// <summary>Gets or sets whether the queue requires sessions.</summary>
    public bool? RequiresSession { get; set; }

    /// <summary>Gets or sets the maximum number of sessions processed concurrently by the endpoint.</summary>
    public int? MaxConcurrentSessions { get; set; }
    /// <summary>Gets or sets the maximum number of concurrent message callbacks for each session.</summary>
    public int? MaxConcurrentCallsPerSession { get; set; }

    /// <summary>Validates the queue path and Azure Service Bus idle-deletion constraint.</summary>
    /// <returns>The queue configuration failures.</returns>
    public IEnumerable<ValidationResult> Validate()
    {
        if (!ServiceBusEntityNameValidator.Validator.IsValidEntityName(Path))
            yield return this.Failure("Path", $"must be a valid queue path: {Path}");

        if (AutoDeleteOnIdle.HasValue && AutoDeleteOnIdle != TimeSpan.Zero && AutoDeleteOnIdle < TimeSpan.FromMinutes(5))
            yield return this.Failure("AutoDeleteOnIdle", "must be zero, or >= 5:00");
    }

    /// <summary>Projects the configured values into Azure SDK queue-creation options.</summary>
    /// <returns>The SDK options for creating or comparing the queue.</returns>
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

    /// <summary>Builds the transport address for this queue in a namespace.</summary>
    /// <param name="hostAddress">The namespace address.</param>
    /// <returns>The queue address including temporary-entity metadata when configured.</returns>
    public Uri GetQueueAddress(Uri hostAddress)
    {
        return new ServiceBusEndpointAddress(hostAddress, Path, AutoDeleteOnIdle);
    }
}

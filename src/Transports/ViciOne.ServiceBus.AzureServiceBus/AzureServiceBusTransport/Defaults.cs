using System;
using Azure.Messaging.ServiceBus.Administration;

namespace ViciOne.ServiceBus.AzureServiceBus;

/// <summary>Defines Azure Service Bus entity, processor, shutdown, and header-size defaults.</summary>
public static class Defaults
{
    /// <summary>Gets the initial peek-lock duration for received messages.</summary>
    public static TimeSpan LockDuration { get; } = TimeSpan.FromMinutes(5);
    /// <summary>Gets the default lifetime assigned to messages on durable entities.</summary>
    public static TimeSpan DefaultMessageTimeToLive { get; } = TimeSpan.FromDays(365 + 1);
    /// <summary>Gets the shorter message lifetime used by basic transport scenarios.</summary>
    public static TimeSpan BasicMessageTimeToLive { get; } = TimeSpan.FromDays(14);

    /// <summary>Gets the idle-deletion duration used for durable entities.</summary>
    public static TimeSpan AutoDeleteOnIdle { get; } = TimeSpan.FromDays(427);
    /// <summary>Gets the minimum Azure Service Bus idle-deletion duration used for temporary entities.</summary>
    public static TimeSpan TemporaryAutoDeleteOnIdle { get; } = TimeSpan.FromMinutes(5);
    /// <summary>Gets the maximum duration for automatic message- or session-lock renewal.</summary>
    public static TimeSpan MaxAutoRenewDuration { get; } = TimeSpan.FromMinutes(5);

    /// <summary>Gets the optional session idle timeout; <see langword="null"/> delegates the timeout choice to the Azure SDK.</summary>
    public static TimeSpan? SessionIdleTimeout { get; } = null;
    /// <summary>Gets the transport's short processor-shutdown timeout.</summary>
    public static TimeSpan ShutdownTimeout { get; } = TimeSpan.FromMilliseconds(100);

    /// <summary>Gets the default maximum number of sessions processed concurrently.</summary>
    public static int MaxConcurrentSessions { get; } = 8;
    /// <summary>Gets the default maximum number of concurrent message callbacks for each session.</summary>
    public static int MaxConcurrentCallsPerSessions { get; } = 1;

    /// <summary>Specifies the maximum serialized transport-header length in bytes.</summary>
    public const int MaxHeaderLengthBytes = 32767;
    /// <summary>Specifies the corresponding maximum UTF-16 header length, reserving one terminating character.</summary>
    public const int MaxHeaderLength = MaxHeaderLengthBytes / sizeof(char) - 1;

    /// <summary>Creates durable queue options using the transport defaults.</summary>
    /// <param name="queueName">The Azure Service Bus queue name.</param>
    /// <returns>The default SDK queue-creation options.</returns>
    public static CreateQueueOptions GetCreateQueueOptions(string queueName)
    {
        return new CreateQueueOptions(queueName)
        {
            AutoDeleteOnIdle = AutoDeleteOnIdle,
            DefaultMessageTimeToLive = DefaultMessageTimeToLive,
            EnableBatchedOperations = true,
            DeadLetteringOnMessageExpiration = true,
            LockDuration = LockDuration,
            MaxDeliveryCount = 5
        };
    }

    /// <summary>Creates durable topic options using the transport defaults.</summary>
    /// <param name="topicName">The Azure Service Bus topic name.</param>
    /// <returns>The default SDK topic-creation options.</returns>
    public static CreateTopicOptions GetCreateTopicOptions(string topicName)
    {
        return new CreateTopicOptions(topicName)
        {
            AutoDeleteOnIdle = AutoDeleteOnIdle,
            DefaultMessageTimeToLive = DefaultMessageTimeToLive,
            EnableBatchedOperations = true
        };
    }
}

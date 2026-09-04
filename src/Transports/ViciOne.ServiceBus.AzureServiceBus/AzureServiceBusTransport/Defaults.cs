using System;
using Azure.Messaging.ServiceBus.Administration;

namespace ViciOne.ServiceBus.AzureServiceBus;

/// <summary>
/// Provides a defaults implementation.
/// </summary>
public static class Defaults
{
    /// <summary>
    /// Gets the lock duration value.
    /// </summary>
    public static TimeSpan LockDuration { get; } = TimeSpan.FromMinutes(5);
    /// <summary>
    /// Gets the default message time to live value.
    /// </summary>
    public static TimeSpan DefaultMessageTimeToLive { get; } = TimeSpan.FromDays(365 + 1);
    /// <summary>
    /// Gets the basic message time to live value.
    /// </summary>
    public static TimeSpan BasicMessageTimeToLive { get; } = TimeSpan.FromDays(14);

    /// <summary>
    /// Gets the auto delete on idle value.
    /// </summary>
    public static TimeSpan AutoDeleteOnIdle { get; } = TimeSpan.FromDays(427);
    /// <summary>
    /// Gets the temporary auto delete on idle value.
    /// </summary>
    public static TimeSpan TemporaryAutoDeleteOnIdle { get; } = TimeSpan.FromMinutes(5);
    /// <summary>
    /// Gets the max auto renew duration value.
    /// </summary>
    public static TimeSpan MaxAutoRenewDuration { get; } = TimeSpan.FromMinutes(5);

    /// <summary>
    /// Gets the session idle timeout value.
    /// </summary>
    public static TimeSpan? SessionIdleTimeout { get; } = null; // SDKs default is undefined - explicitly defined here for clarity
    /// <summary>
    /// Gets the shutdown timeout value.
    /// </summary>
    public static TimeSpan ShutdownTimeout { get; } = TimeSpan.FromMilliseconds(100);

    /// <summary>
    /// Gets the max concurrent sessions value.
    /// </summary>
    public static int MaxConcurrentSessions { get; } = 8;
    /// <summary>
    /// Gets the max concurrent calls per sessions value.
    /// </summary>
    public static int MaxConcurrentCallsPerSessions { get; } = 1;

    /// <summary>
    /// Defines the max header length bytes value.
    /// </summary>
    public const int MaxHeaderLengthBytes = 32767;
    /// <summary>
    /// Defines the max header length value.
    /// </summary>
    public const int MaxHeaderLength = MaxHeaderLengthBytes / sizeof(char) - 1;

    /// <summary>
    /// Gets create queue options.
    /// </summary>
    /// <param name="queueName">The queue name value.</param>
    /// <returns>The result of the operation.</returns>
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

    /// <summary>
    /// Gets create topic options.
    /// </summary>
    /// <param name="topicName">The topic name value.</param>
    /// <returns>The result of the operation.</returns>
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

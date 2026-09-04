using System;
using System.ComponentModel;
using Azure.Messaging.ServiceBus.Administration;

namespace ViciOne.ServiceBus.AzureServiceBusTransport;

[EditorBrowsable(EditorBrowsableState.Never)]
public static class Defaults
{
    public static TimeSpan LockDuration { get; } = TimeSpan.FromMinutes(5);
    public static TimeSpan DefaultMessageTimeToLive { get; } = TimeSpan.FromDays(365 + 1);
    public static TimeSpan BasicMessageTimeToLive { get; } = TimeSpan.FromDays(14);

    public static TimeSpan AutoDeleteOnIdle { get; } = TimeSpan.FromDays(427);
    public static TimeSpan TemporaryAutoDeleteOnIdle { get; } = TimeSpan.FromMinutes(5);
    public static TimeSpan MaxAutoRenewDuration { get; } = TimeSpan.FromMinutes(5);

    public static TimeSpan? SessionIdleTimeout { get; } = null; // SDKs default is undefined - explicitly defined here for clarity
    public static TimeSpan ShutdownTimeout { get; } = TimeSpan.FromMilliseconds(100);

    public static int MaxConcurrentSessions { get; } = 8;
    public static int MaxConcurrentCallsPerSessions { get; } = 1;

    public const int MaxHeaderLengthBytes = 32767;
    public const int MaxHeaderLength = MaxHeaderLengthBytes / sizeof(char) - 1;

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

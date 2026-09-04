using Microsoft.Extensions.Logging;
using ViciOne.ServiceBus.Logging;
using ViciOne.ServiceBus.SqlTransport.Topology;

namespace ViciOne.ServiceBus.SqlTransport;

/// <summary>
/// Provides a sql log messages implementation.
/// </summary>
public static class SqlLogMessages
{
    /// <summary>
    /// Defines the create topic subscription value.
    /// </summary>
    public static readonly LogMessage<TopicToTopicSubscription> CreateTopicSubscription = LogContext.Define<TopicToTopicSubscription>(LogLevel.Debug,
        "Create topic subscription: {TopicSubscription}");

    /// <summary>
    /// Defines the create queue subscription value.
    /// </summary>
    public static readonly LogMessage<TopicToQueueSubscription> CreateQueueSubscription = LogContext.Define<TopicToQueueSubscription>(LogLevel.Debug,
        "Create queue subscription: {QueueSubscription}");

    /// <summary>
    /// Defines the create topic value.
    /// </summary>
    public static readonly LogMessage<Topic> CreateTopic = LogContext.Define<Topic>(LogLevel.Debug, "Create topic: {Topic}");

    /// <summary>
    /// Defines the create queue value.
    /// </summary>
    public static readonly LogMessage<Queue> CreateQueue = LogContext.Define<Queue>(LogLevel.Debug, "Create queue: {Queue}");
}

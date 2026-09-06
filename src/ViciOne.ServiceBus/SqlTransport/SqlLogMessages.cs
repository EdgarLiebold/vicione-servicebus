using Microsoft.Extensions.Logging;
using ViciOne.ServiceBus.Logging;
using ViciOne.ServiceBus.SqlTransport.Topology;

namespace ViciOne.ServiceBus.SqlTransport;

/// <summary>Defines strongly typed log messages emitted by SQL transports.</summary>
public static class SqlLogMessages
{
    /// <summary>Exposes the create topic subscription used by the containing type.</summary>
    public static readonly LogMessage<TopicToTopicSubscription> CreateTopicSubscription = LogContext.Define<TopicToTopicSubscription>(LogLevel.Debug,
        "Create topic subscription: {TopicSubscription}");

    /// <summary>Exposes the create queue subscription used by the containing type.</summary>
    public static readonly LogMessage<TopicToQueueSubscription> CreateQueueSubscription = LogContext.Define<TopicToQueueSubscription>(LogLevel.Debug,
        "Create queue subscription: {QueueSubscription}");

    /// <summary>Exposes the create topic used by the containing type.</summary>
    public static readonly LogMessage<Topic> CreateTopic = LogContext.Define<Topic>(LogLevel.Debug, "Create topic: {Topic}");

    /// <summary>Exposes the create queue used by the containing type.</summary>
    public static readonly LogMessage<Queue> CreateQueue = LogContext.Define<Queue>(LogLevel.Debug, "Create queue: {Queue}");
}

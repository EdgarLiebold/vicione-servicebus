using Microsoft.Extensions.Logging;
using ViciOne.ServiceBus.Logging;
using ViciOne.ServiceBus.SqlTransport.Topology;

namespace ViciOne.ServiceBus.SqlTransport;

public static class SqlLogMessages
{
    public static readonly LogMessage<TopicToTopicSubscription> CreateTopicSubscription = LogContext.Define<TopicToTopicSubscription>(LogLevel.Debug,
        "Create topic subscription: {TopicSubscription}");

    public static readonly LogMessage<TopicToQueueSubscription> CreateQueueSubscription = LogContext.Define<TopicToQueueSubscription>(LogLevel.Debug,
        "Create queue subscription: {QueueSubscription}");

    public static readonly LogMessage<Topic> CreateTopic = LogContext.Define<Topic>(LogLevel.Debug, "Create topic: {Topic}");

    public static readonly LogMessage<Queue> CreateQueue = LogContext.Define<Queue>(LogLevel.Debug, "Create queue: {Queue}");
}

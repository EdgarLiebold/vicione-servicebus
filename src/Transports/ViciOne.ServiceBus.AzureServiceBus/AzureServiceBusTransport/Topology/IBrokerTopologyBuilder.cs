using Azure.Messaging.ServiceBus.Administration;

namespace ViciOne.ServiceBus.AzureServiceBus.Topology;

/// <summary>Collects Azure Service Bus entity declarations and forwarding relationships.</summary>
public interface IBrokerTopologyBuilder
{
    /// <summary>Adds or reuses a topic declaration.</summary>
    /// <param name="createTopicOptions">The Azure topic declaration options.</param>
    /// <returns>A handle to the topology topic.</returns>
    TopicHandle CreateTopic(CreateTopicOptions createTopicOptions);

    /// <summary>Adds or reuses a consumer subscription on a topic.</summary>
    /// <param name="topic">The source topic.</param>
    /// <param name="createSubscriptionOptions">The Azure subscription declaration options.</param>
    /// <param name="rule">The optional initial subscription rule.</param>
    /// <param name="filter">The optional broker rule filter.</param>
    /// <returns>A handle to the topology subscription.</returns>
    SubscriptionHandle CreateSubscription(TopicHandle topic, CreateSubscriptionOptions createSubscriptionOptions, CreateRuleOptions? rule,
        RuleFilter? filter);

    /// <summary>Adds a subscription that forwards from one topic to another.</summary>
    /// <param name="source">The source topic.</param>
    /// <param name="destination">The destination topic.</param>
    /// <param name="createSubscriptionOptions">The Azure forwarding subscription declaration options.</param>
    /// <returns>A handle to the topic-to-topic relationship.</returns>
    TopicSubscriptionHandle CreateTopicSubscription(TopicHandle source, TopicHandle destination, CreateSubscriptionOptions createSubscriptionOptions);

    /// <summary>Adds or reuses a queue declaration.</summary>
    /// <param name="createQueueOptions">The Azure queue declaration options.</param>
    /// <returns>A handle to the topology queue.</returns>
    QueueHandle CreateQueue(CreateQueueOptions createQueueOptions);

    /// <summary>Adds a subscription that forwards from a topic to a queue.</summary>
    /// <param name="exchange">The source topic handle.</param>
    /// <param name="queue">The forwarding destination queue.</param>
    /// <param name="createSubscriptionOptions">The Azure forwarding subscription declaration options.</param>
    /// <param name="rule">The optional initial subscription rule.</param>
    /// <param name="filter">The optional broker rule filter.</param>
    /// <returns>A handle to the topic-to-queue relationship.</returns>
    QueueSubscriptionHandle CreateQueueSubscription(TopicHandle exchange, QueueHandle queue, CreateSubscriptionOptions createSubscriptionOptions,
        CreateRuleOptions? rule, RuleFilter? filter);
}

using System.Collections.Generic;
using Azure.Messaging.ServiceBus.Administration;
using ViciOne.ServiceBus.AzureServiceBus.Topology;

namespace ViciOne.ServiceBus.AzureServiceBus.Configuration;

/// <summary>
/// Used to bind an exchange to the consuming queue's exchange
/// </summary>
public class SubscriptionConsumeTopologySpecification :
    IServiceBusConsumeTopologySpecification
{
    readonly CreateSubscriptionOptions _createSubscriptionOptions;
    readonly CreateTopicOptions _createTopicOptions;
    readonly RuleFilter? _filter;
    readonly CreateRuleOptions? _rule;

    /// <summary>
    /// Initializes a new instance of the containing type.
    /// </summary>
    /// <param name="createTopicOptions">The create topic options value.</param>
    /// <param name="createSubscriptionOptions">The create subscription options value.</param>
    /// <param name="rule">The rule value.</param>
    /// <param name="filter">The filter value.</param>
    public SubscriptionConsumeTopologySpecification(CreateTopicOptions createTopicOptions, CreateSubscriptionOptions createSubscriptionOptions,
        CreateRuleOptions? rule, RuleFilter? filter)
    {
        _createTopicOptions = createTopicOptions;
        _createSubscriptionOptions = createSubscriptionOptions;
        _rule = rule;
        _filter = filter;
    }

    /// <summary>
    /// Validates the current configuration.
    /// </summary>
    /// <returns>The result of the operation.</returns>
    public IEnumerable<ValidationResult> Validate()
    {
        yield break;
    }

    /// <summary>
    /// Applies this specification to the target builder.
    /// </summary>
    /// <param name="builder">The builder value.</param>
    public void Apply(IReceiveEndpointBrokerTopologyBuilder builder)
    {
        var topic = builder.CreateTopic(_createTopicOptions);

        _createSubscriptionOptions.ForwardTo = builder.Queue.Queue.CreateQueueOptions.Name;
        _createSubscriptionOptions.AutoDeleteOnIdle = builder.Queue.Queue.CreateQueueOptions.AutoDeleteOnIdle;

        builder.CreateQueueSubscription(topic, builder.Queue, _createSubscriptionOptions, _rule, _filter);
    }
}

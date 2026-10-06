using System.Collections.Generic;
using Azure.Messaging.ServiceBus.Administration;
using ViciOne.ServiceBus.AzureServiceBus.Topology;

namespace ViciOne.ServiceBus.AzureServiceBus.Configuration;

/// <summary>Configures a topic subscription that forwards messages to the consuming queue.</summary>
public class SubscriptionConsumeTopologySpecification :
    IServiceBusConsumeTopologySpecification
{
    readonly CreateSubscriptionOptions _createSubscriptionOptions;
    readonly CreateTopicOptions _createTopicOptions;
    readonly RuleFilter? _filter;
    readonly CreateRuleOptions? _rule;

    /// <summary>Initializes a topic-subscription forwarding operation.</summary>
    /// <param name="createTopicOptions">The source-topic creation options.</param>
    /// <param name="createSubscriptionOptions">The subscription options updated with the target queue during application.</param>
    /// <param name="rule">An optional complete subscription rule.</param>
    /// <param name="filter">An optional filter for the default subscription rule.</param>
    public SubscriptionConsumeTopologySpecification(CreateTopicOptions createTopicOptions, CreateSubscriptionOptions createSubscriptionOptions,
        CreateRuleOptions? rule, RuleFilter? filter)
    {
        _createTopicOptions = createTopicOptions;
        _createSubscriptionOptions = createSubscriptionOptions;
        _rule = rule;
        _filter = filter;
    }

    /// <summary>Validates mutually exclusive rule configuration retained by the forwarding subscription.</summary>
    /// <returns>Failures when both a complete rule and a default-rule filter are specified.</returns>
    public IEnumerable<ValidationResult> Validate()
    {
        if (_rule != null && _filter != null)
            yield return this.Failure("Rule/Filter", "only a rule or a filter may be specified");
    }

    /// <summary>Creates the source topic and a subscription that forwards deliveries to the endpoint queue.</summary>
    /// <param name="builder">The receive-endpoint topology builder that supplies the target queue.</param>
    public void Apply(IReceiveEndpointBrokerTopologyBuilder builder)
    {
        var topic = builder.CreateTopic(_createTopicOptions);

        _createSubscriptionOptions.ForwardTo = builder.Queue.Queue.CreateQueueOptions.Name;
        _createSubscriptionOptions.AutoDeleteOnIdle = builder.Queue.Queue.CreateQueueOptions.AutoDeleteOnIdle;

        builder.CreateQueueSubscription(topic, builder.Queue, _createSubscriptionOptions, _rule, _filter);
    }
}

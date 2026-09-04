using System;
using System.Linq;
using Azure.Messaging.ServiceBus.Administration;

namespace ViciOne.ServiceBus.AzureServiceBus.Topology;

/// <summary>
/// Provides a publish endpoint broker topology builder implementation.
/// </summary>
public class PublishEndpointBrokerTopologyBuilder :
    BrokerTopologyBuilder,
    IPublishEndpointBrokerTopologyBuilder
{
    /// <summary>
    /// Specifies the available options values.
    /// </summary>
    [Flags]
    public enum Options
    {
        /// <summary>
        /// Indicates flatten hierarchy.
        /// </summary>
        FlattenHierarchy = 0,
        /// <summary>
        /// Indicates maintain hierarchy.
        /// </summary>
        MaintainHierarchy = 1
    }


    readonly Options _options;

    readonly IServiceBusPublishTopology _topology;

    /// <summary>
    /// Initializes a new instance of the containing type.
    /// </summary>
    /// <param name="topology">The topology value.</param>
    /// <param name="options">The options value.</param>
    public PublishEndpointBrokerTopologyBuilder(IServiceBusPublishTopology topology, Options options = Options.MaintainHierarchy)
    {
        _topology = topology;
        _options = options;
    }

    /// <summary>
    /// The topic where the published message is sent
    /// </summary>
    public TopicHandle? Topic { get; set; }

    /// <summary>
    /// Creates implemented builder.
    /// </summary>
    /// <returns>The result of the operation.</returns>
    public IPublishEndpointBrokerTopologyBuilder CreateImplementedBuilder()
    {
        if (_options.HasFlag(Options.MaintainHierarchy))
            return new ImplementedBuilder(this, _topology, _options);

        return this;
    }


    class ImplementedBuilder :
        IPublishEndpointBrokerTopologyBuilder
    {
        readonly IPublishEndpointBrokerTopologyBuilder _builder;
        readonly Options _options;
        readonly IServiceBusPublishTopology _topology;
        TopicHandle? _topic;

        public ImplementedBuilder(IPublishEndpointBrokerTopologyBuilder builder, IServiceBusPublishTopology topology, Options options)
        {
            _builder = builder;
            _topology = topology;
            _options = options;
        }

        public TopicHandle? Topic
        {
            get => _topic;
            set
            {
                _topic = value;
                if (value != null && _builder.Topic is { } parentTopic)
                {
                    var subscriptionName = string.Join("-", value.Topic.CreateTopicOptions.Name.Split('/').Reverse());
                    var createSubscriptionOptions = new CreateSubscriptionOptions(parentTopic.Topic.CreateTopicOptions.Name,
                        _topology.FormatSubscriptionName(subscriptionName))
                    { ForwardTo = value.Topic.CreateTopicOptions.Name };

                    _builder.CreateTopicSubscription(parentTopic, value, createSubscriptionOptions);
                }
            }
        }

        public IPublishEndpointBrokerTopologyBuilder CreateImplementedBuilder()
        {
            return _options.HasFlag(Options.MaintainHierarchy) ? new ImplementedBuilder(this, _topology, _options) : this;
        }

        public TopicHandle CreateTopic(CreateTopicOptions createTopicOptions)
        {
            return _builder.CreateTopic(createTopicOptions);
        }

        public SubscriptionHandle CreateSubscription(TopicHandle topic, CreateSubscriptionOptions createSubscriptionOptions, CreateRuleOptions? rule,
            RuleFilter? filter)
        {
            return _builder.CreateSubscription(topic, createSubscriptionOptions, rule, filter);
        }

        public TopicSubscriptionHandle CreateTopicSubscription(TopicHandle source, TopicHandle destination,
            CreateSubscriptionOptions createSubscriptionOptions)
        {
            return _builder.CreateTopicSubscription(source, destination, createSubscriptionOptions);
        }

        public QueueHandle CreateQueue(CreateQueueOptions createQueueOptions)
        {
            return _builder.CreateQueue(createQueueOptions);
        }

        public QueueSubscriptionHandle CreateQueueSubscription(TopicHandle exchange, QueueHandle queue, CreateSubscriptionOptions createSubscriptionOptions,
            CreateRuleOptions? rule,
            RuleFilter? filter)
        {
            return _builder.CreateQueueSubscription(exchange, queue, createSubscriptionOptions, rule, filter);
        }
    }
}

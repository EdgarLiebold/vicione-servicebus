namespace ViciOne.ServiceBus.ActiveMq.Topology;

/// <summary>Builds ActiveMQ publish topics with optional implemented-message hierarchy.</summary>
public class PublishEndpointBrokerTopologyBuilder :
    BrokerTopologyBuilder,
    IPublishEndpointBrokerTopologyBuilder
{
    readonly PublishBrokerTopologyOptions _options;

    /// <summary>Creates a publish-topology builder.</summary>
    /// <param name="options">Options controlling implemented-message hierarchy.</param>
    public PublishEndpointBrokerTopologyBuilder(PublishBrokerTopologyOptions options = PublishBrokerTopologyOptions.FlattenHierarchy)
    {
        _options = options;
    }

    /// <summary>Gets the topic to which the message is published.</summary>
    public TopicHandle? Topic { get; set; }

    /// <summary>Creates the builder scope used for an implemented message contract.</summary>
    /// <returns>A nested builder when hierarchy is maintained; otherwise, this builder.</returns>
    public IPublishEndpointBrokerTopologyBuilder CreateImplementedBuilder()
    {
        if (_options.HasFlag(PublishBrokerTopologyOptions.MaintainHierarchy))
            return new ImplementedBuilder(this, _options);

        return this;
    }

    /// <summary>Creates array snapshots of the accumulated publish topology with retained entity references.</summary>
    /// <returns>The configured ActiveMQ broker topology.</returns>
    public BrokerTopology BuildBrokerTopology()
    {
        return new ActiveMqBrokerTopology(Topics, Queues, Consumers);
    }


    class ImplementedBuilder :
        IPublishEndpointBrokerTopologyBuilder
    {
        readonly IPublishEndpointBrokerTopologyBuilder _builder;
        readonly PublishBrokerTopologyOptions _options;

        public ImplementedBuilder(IPublishEndpointBrokerTopologyBuilder builder, PublishBrokerTopologyOptions options)
        {
            _builder = builder;
            _options = options;
        }

        public TopicHandle? Topic { get; set; }

        public IPublishEndpointBrokerTopologyBuilder CreateImplementedBuilder()
        {
            if (_options.HasFlag(PublishBrokerTopologyOptions.MaintainHierarchy))
                return new ImplementedBuilder(this, _options);

            return this;
        }

        public TopicHandle CreateTopic(string name, bool durable, bool autoDelete)
        {
            return _builder.CreateTopic(name, durable, autoDelete);
        }

        public QueueHandle CreateQueue(string name, bool durable, bool autoDelete)
        {
            return _builder.CreateQueue(name, durable, autoDelete);
        }

        public ConsumerHandle BindConsumer(TopicHandle topic, QueueHandle? queue, string? selector, string? consumerName = null, bool shared = false)
        {
            return _builder.BindConsumer(topic, queue, selector, consumerName, shared);
        }
    }
}

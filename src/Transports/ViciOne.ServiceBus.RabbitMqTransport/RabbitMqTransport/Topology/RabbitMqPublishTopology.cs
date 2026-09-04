using System;
using ViciOne.ServiceBus.Metadata;
using ViciOne.ServiceBus.Topology;

namespace ViciOne.ServiceBus.RabbitMqTransport.Topology;

public class RabbitMqPublishTopology :
    PublishTopology,
    IRabbitMqPublishTopologyConfigurator
{
    readonly IMessageTopology _messageTopology;

    public RabbitMqPublishTopology(IMessageTopology messageTopology)
    {
        _messageTopology = messageTopology;
        ExchangeTypeSelector = new FanoutExchangeTypeSelector();
    }

    public IExchangeTypeSelector ExchangeTypeSelector { get; }

    public PublishBrokerTopologyOptions BrokerTopologyOptions { get; set; }

    IRabbitMqMessagePublishTopology<T> IRabbitMqPublishTopology.GetMessageTopology<T>()
    {
        return GetMessageTopology<T>() as IRabbitMqMessagePublishTopologyConfigurator<T>
            ?? throw new InvalidOperationException($"The message topology for '{typeof(T)}' is not a RabbitMQ publish topology.");
    }

    IRabbitMqMessagePublishTopologyConfigurator IRabbitMqPublishTopologyConfigurator.GetMessageTopology(Type messageType)
    {
        return GetMessageTopology(messageType) as IRabbitMqMessagePublishTopologyConfigurator
            ?? throw new InvalidOperationException($"The message topology for '{messageType}' is not a RabbitMQ publish topology.");
    }

    public BrokerTopology GetPublishBrokerTopology()
    {
        var builder = new PublishEndpointBrokerTopologyBuilder(BrokerTopologyOptions);

        ForEachMessageType<IRabbitMqMessagePublishTopology>(x =>
        {
            x.Apply(builder);

            builder.Exchange = null;
        });

        return builder.BuildBrokerTopology();
    }

    IRabbitMqMessagePublishTopologyConfigurator<T> IRabbitMqPublishTopologyConfigurator.GetMessageTopology<T>()
    {
        return GetMessageTopology<T>() as IRabbitMqMessagePublishTopologyConfigurator<T>
            ?? throw new InvalidOperationException($"The message topology for '{typeof(T)}' is not a RabbitMQ publish topology.");
    }

    protected override IMessagePublishTopologyConfigurator CreateMessageTopology<T>()
    {
        var exchangeTypeSelector = new MessageExchangeTypeSelector<T>(ExchangeTypeSelector);

        var messageTopology = new RabbitMqMessagePublishTopology<T>(this, _messageTopology.GetMessageTopology<T>(), exchangeTypeSelector);

        var connector = new ImplementedMessageTypeConnector<T>(this, messageTopology);

        ImplementedMessageTypeCache<T>.EnumerateImplementedTypes(connector);

        OnMessageTopologyCreated(messageTopology);

        return messageTopology;
    }


    class ImplementedMessageTypeConnector<TMessage> :
        IImplementedMessageType
        where TMessage : class
    {
        readonly RabbitMqMessagePublishTopology<TMessage> _messagePublishTopologyConfigurator;
        readonly IRabbitMqPublishTopologyConfigurator _publishTopology;

        public ImplementedMessageTypeConnector(IRabbitMqPublishTopologyConfigurator publishTopology,
            RabbitMqMessagePublishTopology<TMessage> messagePublishTopologyConfigurator)
        {
            _publishTopology = publishTopology;
            _messagePublishTopologyConfigurator = messagePublishTopologyConfigurator;
        }

        public void ImplementsMessageType<T>(bool direct)
            where T : class
        {
            IRabbitMqMessagePublishTopologyConfigurator<T> messageTopology = _publishTopology.GetMessageTopology<T>();

            _messagePublishTopologyConfigurator.AddImplementedMessageConfigurator(messageTopology, direct);
        }
    }
}

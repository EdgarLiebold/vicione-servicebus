using System;
using ViciOne.ServiceBus.Metadata;
using ViciOne.ServiceBus.Topology;

namespace ViciOne.ServiceBus.InMemoryTransport;

/// <summary>Defines the topology for in memory publish.</summary>
public class InMemoryPublishTopology :
    PublishTopology,
    IInMemoryPublishTopologyConfigurator
{
    readonly IMessageTopology _messageTopology;

    /// <summary>Initializes a new instance.</summary>
    /// <param name="messageTopology">The message topology.</param>
    public InMemoryPublishTopology(IMessageTopology messageTopology)
    {
        _messageTopology = messageTopology;
    }

    IInMemoryMessagePublishTopology<T> IInMemoryPublishTopology.GetMessageTopology<T>()
    {
        return GetMessageTopology<T>() as IInMemoryMessagePublishTopology<T>
            ?? throw new InvalidOperationException($"The publish topology for {TypeCache<T>.ShortName} is not an in-memory topology.");
    }

    IInMemoryMessagePublishTopologyConfigurator<T> IInMemoryPublishTopologyConfigurator.GetMessageTopology<T>()
    {
        return GetMessageTopology<T>() as IInMemoryMessagePublishTopologyConfigurator<T>
            ?? throw new InvalidOperationException($"The publish topology for {TypeCache<T>.ShortName} is not configurable.");
    }

    IInMemoryMessagePublishTopologyConfigurator IInMemoryPublishTopologyConfigurator.GetMessageTopology(Type messageType)
    {
        return GetMessageTopology(messageType) as IInMemoryMessagePublishTopologyConfigurator
            ?? throw new InvalidOperationException($"The publish topology for {TypeCache.GetShortName(messageType)} is not configurable.");
    }

    /// <summary>Creates message topology.</summary>
    /// <typeparam name="T">The value type.</typeparam>
    /// <returns>The created message topology.</returns>
    protected override IMessagePublishTopologyConfigurator CreateMessageTopology<T>()
    {
        var topology = new InMemoryMessagePublishTopology<T>(this, _messageTopology.GetMessageTopology<T>());

        var connector = new ImplementedMessageTypeConnector<T>(this, topology);

        ImplementedMessageTypeCache<T>.EnumerateImplementedTypes(connector);

        OnMessageTopologyCreated(topology);

        return topology;
    }


    class ImplementedMessageTypeConnector<TMessage> :
        IImplementedMessageType
        where TMessage : class
    {
        readonly InMemoryMessagePublishTopology<TMessage> _messagePublishTopologyConfigurator;
        readonly IInMemoryPublishTopologyConfigurator _publishTopology;

        public ImplementedMessageTypeConnector(IInMemoryPublishTopologyConfigurator publishTopology,
            InMemoryMessagePublishTopology<TMessage> messagePublishTopologyConfigurator)
        {
            _publishTopology = publishTopology;
            _messagePublishTopologyConfigurator = messagePublishTopologyConfigurator;
        }

        public void ImplementsMessageType<T>(bool direct)
            where T : class
        {
            IInMemoryMessagePublishTopologyConfigurator<T> messageTopology = _publishTopology.GetMessageTopology<T>();

            _messagePublishTopologyConfigurator.AddImplementedMessageConfigurator(messageTopology, direct);
        }
    }
}

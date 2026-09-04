using System;
using ViciOne.ServiceBus.Metadata;
using ViciOne.ServiceBus.Topology;

namespace ViciOne.ServiceBus.InMemoryTransport;

/// <summary>
/// Provides an in memory publish topology implementation.
/// </summary>
public class InMemoryPublishTopology :
    PublishTopology,
    IInMemoryPublishTopologyConfigurator
{
    readonly IMessageTopology _messageTopology;

    /// <summary>
    /// Initializes a new instance of the containing type.
    /// </summary>
    /// <param name="messageTopology">The message topology value.</param>
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

    /// <summary>
    /// Creates message topology.
    /// </summary>
    /// <typeparam name="T">The t type.</typeparam>
    /// <returns>The result of the operation.</returns>
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

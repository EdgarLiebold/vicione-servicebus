using System;
using ViciOne.ServiceBus.Metadata;
using ViciOne.ServiceBus.Topology;

namespace ViciOne.ServiceBus.InMemoryTransport.Topology;

/// <summary>Creates message-specific publish topology for the in-memory transport.</summary>
internal sealed class InMemoryPublishTopology :
    PublishTopology,
    IInMemoryPublishTopology,
    IInMemoryPublishTopologyConfigurator
{
    readonly IMessageTopology _messageTopology;

    /// <summary>Creates publish topology over shared message entity-name configuration.</summary>
    /// <param name="messageTopology">The message topology used to name exchanges.</param>
    public InMemoryPublishTopology(IMessageTopology messageTopology)
    {
        _messageTopology = messageTopology ?? throw new ArgumentNullException(nameof(messageTopology));
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

    /// <summary>Creates publish topology for a message contract and its implemented contracts.</summary>
    /// <typeparam name="T">The published message contract.</typeparam>
    /// <returns>The message-specific publish topology.</returns>
    protected override IMessagePublishTopologyConfigurator CreateMessageTopology<T>()
    {
        var topology = new InMemoryMessagePublishTopology<T>(this, _messageTopology.GetMessageTopology<T>());

        var connector = new ImplementedMessageTypeConnector<T>(this, topology);

        ImplementedMessageTypeCache<T>.EnumerateImplementedTypes(connector);

        OnMessageTopologyCreated(topology);

        return topology;
    }


    sealed class ImplementedMessageTypeConnector<TMessage> :
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

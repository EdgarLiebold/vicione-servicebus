using System;
using System.Collections.Generic;
using System.Diagnostics.CodeAnalysis;
using ViciOne.ServiceBus.Configuration;
using ViciOne.ServiceBus.Topology;
using ViciOne.ServiceBus.Transports.Fabric;

namespace ViciOne.ServiceBus.InMemoryTransport.Topology;

/// <summary>Declares and composes in-memory exchange topology for one published message contract.</summary>
/// <typeparam name="TMessage">The published message contract.</typeparam>
internal sealed class InMemoryMessagePublishTopology<TMessage> :
    MessagePublishTopology<TMessage>,
    IInMemoryMessagePublishTopology<TMessage>,
    IInMemoryMessagePublishTopologyConfigurator<TMessage>
    where TMessage : class
{
    readonly List<IInMemoryMessagePublishTopology> _implementedMessageTypes;
    readonly IMessageTopology<TMessage> _messageTopology;

    /// <summary>Creates publish topology over shared topology and entity-name configuration.</summary>
    /// <param name="publishTopology">The parent publish topology.</param>
    /// <param name="messageTopology">The message entity-name topology.</param>
    public InMemoryMessagePublishTopology(IPublishTopologyConfigurator publishTopology, IMessageTopology<TMessage> messageTopology)
        : base(publishTopology ?? throw new ArgumentNullException(nameof(publishTopology)))
    {
        _messageTopology = messageTopology ?? throw new ArgumentNullException(nameof(messageTopology));
        _implementedMessageTypes = new List<IInMemoryMessagePublishTopology>();
    }

    /// <summary>Gets or sets the exchange routing behavior.</summary>
    public ExchangeType ExchangeType { get; set; }

    /// <summary>Declares the message exchange and its implemented-contract bindings.</summary>
    /// <param name="builder">The publish topology builder to update.</param>
    public void Apply(IMessageFabricPublishTopologyBuilder builder)
    {
        ArgumentNullException.ThrowIfNull(builder);
        if (Exclude)
            return;

        var exchangeName = _messageTopology.EntityName;

        builder.ExchangeDeclare(exchangeName, ExchangeType);

        if (builder.ExchangeName != null)
            builder.ExchangeBind(builder.ExchangeName, exchangeName, builder.ExchangeType == ExchangeType.Topic ? "#" : default);
        else
        {
            builder.ExchangeName = exchangeName;
            builder.ExchangeType = ExchangeType;
        }

        foreach (var configurator in _implementedMessageTypes)
            configurator.Apply(builder);
    }

    /// <summary>Resolves the message exchange against an in-memory host address.</summary>
    /// <param name="baseAddress">The in-memory host address.</param>
    /// <param name="publishAddress">Receives the canonical message exchange address.</param>
    /// <returns><see langword="true" /> because in-memory message topology always resolves an address.</returns>
    public override bool TryGetPublishAddress(Uri baseAddress, [NotNullWhen(true)] out Uri? publishAddress)
    {
        ArgumentNullException.ThrowIfNull(baseAddress);
        publishAddress = new InMemoryEndpointAddress(new InMemoryHostAddress(baseAddress), _messageTopology.EntityName, exchangeType: ExchangeType);
        return true;
    }

    /// <summary>Adds publish topology for a directly implemented message contract.</summary>
    /// <typeparam name="TImplemented">The implemented message contract.</typeparam>
    /// <param name="configurator">The implemented contract's publish topology.</param>
    /// <param name="direct">Whether the published contract implements the contract directly.</param>
    public void AddImplementedMessageConfigurator<TImplemented>(
        IInMemoryMessagePublishTopologyConfigurator<TImplemented> configurator,
        bool direct)
        where TImplemented : class
    {
        ArgumentNullException.ThrowIfNull(configurator);
        var adapter = new TypeAdapter<TImplemented>(configurator, direct);

        _implementedMessageTypes.Add(adapter);
    }


    sealed class TypeAdapter<T> :
        IInMemoryMessagePublishTopology
        where T : class
    {
        readonly bool _direct;
        readonly IInMemoryMessagePublishTopology _topology;

        public TypeAdapter(IInMemoryMessagePublishTopologyConfigurator<T> configurator, bool direct)
        {
            _direct = direct;
            _topology = configurator as IInMemoryMessagePublishTopology
                ?? throw new ArgumentException("The configurator does not expose in-memory publish topology.", nameof(configurator));
        }

        public void Apply(IMessageFabricPublishTopologyBuilder builder)
        {
            if (_direct)
            {
                var implementedBuilder = builder.CreateImplementedBuilder();

                _topology.Apply(implementedBuilder);
            }
        }
    }
}

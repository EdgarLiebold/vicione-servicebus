using System;
using System.Collections.Generic;
using System.Diagnostics.CodeAnalysis;
using ViciOne.ServiceBus.Configuration;
using ViciOne.ServiceBus.Topology;
using ViciOne.ServiceBus.Transports.Fabric;

namespace ViciOne.ServiceBus.InMemoryTransport;

/// <summary>Defines the topology for in memory message publish.</summary>
/// <typeparam name="TMessage">The message contract processed by the member.</typeparam>
public class InMemoryMessagePublishTopology<TMessage> :
    MessagePublishTopology<TMessage>,
    IInMemoryMessagePublishTopologyConfigurator<TMessage>
    where TMessage : class
{
    readonly List<IInMemoryMessagePublishTopology> _implementedMessageTypes;
    readonly IMessageTopology<TMessage> _messageTopology;

    /// <summary>Initializes a new instance.</summary>
    /// <param name="publishTopology">The publish topology.</param>
    /// <param name="messageTopology">The message topology.</param>
    public InMemoryMessagePublishTopology(IPublishTopologyConfigurator publishTopology, IMessageTopology<TMessage> messageTopology)
        : base(publishTopology)
    {
        _messageTopology = messageTopology;
        _implementedMessageTypes = new List<IInMemoryMessagePublishTopology>();
    }

    /// <summary>Gets or sets the exchange type.</summary>
    public ExchangeType ExchangeType { get; set; }

    /// <summary>Applies this specification to the target builder.</summary>
    /// <param name="builder">The builder that receives the configuration.</param>
    public void Apply(IMessageFabricPublishTopologyBuilder builder)
    {
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

    /// <summary>Attempts to get publish address.</summary>
    /// <param name="baseAddress">The base address.</param>
    /// <param name="publishAddress">Receives the publish address produced by the operation.</param>
    /// <returns><see langword="true" /> when the condition is satisfied; otherwise, <see langword="false" />.</returns>
    public override bool TryGetPublishAddress(Uri baseAddress, [NotNullWhen(true)] out Uri? publishAddress)
    {
        publishAddress = new InMemoryEndpointAddress(new InMemoryHostAddress(baseAddress), _messageTopology.EntityName, exchangeType: ExchangeType);
        return true;
    }

    /// <summary>Adds implemented message configurator to the configuration.</summary>
    /// <typeparam name="T">The value type.</typeparam>
    /// <param name="configurator">The configurator to update.</param>
    /// <param name="direct">The direct.</param>
    public void AddImplementedMessageConfigurator<T>(IInMemoryMessagePublishTopologyConfigurator<T> configurator, bool direct)
        where T : class
    {
        var adapter = new TypeAdapter<T>(configurator, direct);

        _implementedMessageTypes.Add(adapter);
    }


    class TypeAdapter<T> :
        IInMemoryMessagePublishTopology
        where T : class
    {
        readonly IInMemoryMessagePublishTopologyConfigurator<T> _configurator;
        readonly bool _direct;

        public TypeAdapter(IInMemoryMessagePublishTopologyConfigurator<T> configurator, bool direct)
        {
            _configurator = configurator;
            _direct = direct;
        }

        public void Apply(IMessageFabricPublishTopologyBuilder builder)
        {
            if (_direct)
            {
                var implementedBuilder = builder.CreateImplementedBuilder();

                _configurator.Apply(implementedBuilder);
            }
        }
    }
}

using System;
using System.Collections.Generic;
using System.Diagnostics.CodeAnalysis;
using ViciOne.ServiceBus.Configuration;
using ViciOne.ServiceBus.Topology;
using ViciOne.ServiceBus.Transports.Fabric;

#nullable enable
namespace ViciOne.ServiceBus.InMemoryTransport;

/// <summary>
/// Provides an in memory message publish topology implementation.
/// </summary>
/// <typeparam name="TMessage">The t message type.</typeparam>
public class InMemoryMessagePublishTopology<TMessage> :
    MessagePublishTopology<TMessage>,
    IInMemoryMessagePublishTopologyConfigurator<TMessage>
    where TMessage : class
{
    readonly List<IInMemoryMessagePublishTopology> _implementedMessageTypes;
    readonly IMessageTopology<TMessage> _messageTopology;

    /// <summary>
    /// Initializes a new instance of the containing type.
    /// </summary>
    /// <param name="publishTopology">The publish topology value.</param>
    /// <param name="messageTopology">The message topology value.</param>
    public InMemoryMessagePublishTopology(IPublishTopologyConfigurator publishTopology, IMessageTopology<TMessage> messageTopology)
        : base(publishTopology)
    {
        _messageTopology = messageTopology;
        _implementedMessageTypes = new List<IInMemoryMessagePublishTopology>();
    }

    /// <summary>
    /// Gets or sets the exchange type value.
    /// </summary>
    public ExchangeType ExchangeType { get; set; }

    /// <summary>
    /// Applies this specification to the target builder.
    /// </summary>
    /// <param name="builder">The builder value.</param>
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

    /// <summary>
    /// Attempts to get publish address.
    /// </summary>
    /// <param name="baseAddress">The base address value.</param>
    /// <param name="publishAddress">The publish address value.</param>
    /// <returns><see langword="true" /> when the condition is satisfied; otherwise, <see langword="false" />.</returns>
    public override bool TryGetPublishAddress(Uri baseAddress, [NotNullWhen(true)] out Uri? publishAddress)
    {
        publishAddress = new InMemoryEndpointAddress(new InMemoryHostAddress(baseAddress), _messageTopology.EntityName, exchangeType: ExchangeType);
        return true;
    }

    /// <summary>
    /// Adds implemented message configurator to the configuration.
    /// </summary>
    /// <typeparam name="T">The t type.</typeparam>
    /// <param name="configurator">The configurator value.</param>
    /// <param name="direct">The direct value.</param>
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

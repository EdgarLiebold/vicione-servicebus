using System;
using System.Collections.Generic;
using System.Linq;
using ViciOne.ServiceBus.RabbitMq.Configuration;

namespace ViciOne.ServiceBus.RabbitMq.Topology;

/// <summary>Collects the RabbitMQ exchange bindings required to consume one message contract.</summary>
/// <typeparam name="TMessage">The consumed message contract type.</typeparam>
public class RabbitMqMessageConsumeTopology<TMessage> :
    MessageConsumeTopology<TMessage>,
    IRabbitMqMessageConsumeTopologyConfigurator<TMessage>,
    IRabbitMqMessageConsumeTopologyConfigurator
    where TMessage : class
{
    readonly IMessageTopology<TMessage> _messageTopology;
    readonly IRabbitMqMessagePublishTopology<TMessage> _publishTopology;
    readonly List<IRabbitMqConsumeTopologySpecification> _specifications;

    /// <summary>Creates a message-specific consume topology.</summary>
    /// <param name="messageTopology">The message metadata associated with <typeparamref name="TMessage"/>.</param>
    /// <param name="exchangeTypeSelector">The exchange-type selector associated with this message topology.</param>
    /// <param name="publishTopology">The publish exchange topology to bind when consuming <typeparamref name="TMessage"/>.</param>
    public RabbitMqMessageConsumeTopology(IMessageTopology<TMessage> messageTopology, IMessageExchangeTypeSelector<TMessage> exchangeTypeSelector,
        IRabbitMqMessagePublishTopology<TMessage> publishTopology)
    {
        _messageTopology = messageTopology;
        _publishTopology = publishTopology;
        ExchangeTypeSelector = exchangeTypeSelector;

        _specifications = new List<IRabbitMqConsumeTopologySpecification>();
    }

    IMessageExchangeTypeSelector<TMessage> ExchangeTypeSelector { get; }

    /// <summary>Applies every configured message binding to a receive endpoint topology.</summary>
    /// <param name="builder">The receive-endpoint topology builder.</param>
    public void Apply(IReceiveEndpointBrokerTopologyBuilder builder)
    {
        foreach (var specification in _specifications)
            specification.Apply(builder);
    }

    /// <summary>Binds the message contract's publish exchange to the receive endpoint exchange.</summary>
    /// <param name="configure">An optional callback that customizes the exchange binding.</param>
    public void Bind(Action<IRabbitMqExchangeBindingConfigurator>? configure = null)
    {
        if (!IsBindableMessageType)
        {
            _specifications.Add(new InvalidRabbitMqConsumeTopologySpecification(TypeCache<TMessage>.ShortName, "Is not a bindable message type"));
            return;
        }

        var specification = new ExchangeBindingConsumeTopologySpecification(_publishTopology.Exchange);

        configure?.Invoke(specification);

        _specifications.Add(specification);
    }

    /// <summary>Validates the message topology and every configured binding.</summary>
    /// <returns>Every validation failure found for this message contract.</returns>
    public override IEnumerable<ValidationResult> Validate()
    {
        return base.Validate().Concat(_specifications.SelectMany(x => x.Validate()));
    }
}

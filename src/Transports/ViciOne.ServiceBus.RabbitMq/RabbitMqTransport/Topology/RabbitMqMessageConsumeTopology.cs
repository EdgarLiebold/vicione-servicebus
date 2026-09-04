using System;
using System.Collections.Generic;
using System.Linq;
using ViciOne.ServiceBus.RabbitMq.Configuration;

namespace ViciOne.ServiceBus.RabbitMq.Topology;

/// <summary>
/// Provides a rabbit mq message consume topology implementation.
/// </summary>
/// <typeparam name="TMessage">The t message type.</typeparam>
public class RabbitMqMessageConsumeTopology<TMessage> :
    MessageConsumeTopology<TMessage>,
    IRabbitMqMessageConsumeTopologyConfigurator<TMessage>,
    IRabbitMqMessageConsumeTopologyConfigurator
    where TMessage : class
{
    readonly IMessageTopology<TMessage> _messageTopology;
    readonly IRabbitMqMessagePublishTopology<TMessage> _publishTopology;
    readonly List<IRabbitMqConsumeTopologySpecification> _specifications;

    /// <summary>
    /// Initializes a new instance of the containing type.
    /// </summary>
    /// <param name="messageTopology">The message topology value.</param>
    /// <param name="exchangeTypeSelector">The exchange type selector value.</param>
    /// <param name="publishTopology">The publish topology value.</param>
    public RabbitMqMessageConsumeTopology(IMessageTopology<TMessage> messageTopology, IMessageExchangeTypeSelector<TMessage> exchangeTypeSelector,
        IRabbitMqMessagePublishTopology<TMessage> publishTopology)
    {
        _messageTopology = messageTopology;
        _publishTopology = publishTopology;
        ExchangeTypeSelector = exchangeTypeSelector;

        _specifications = new List<IRabbitMqConsumeTopologySpecification>();
    }

    IMessageExchangeTypeSelector<TMessage> ExchangeTypeSelector { get; }

    /// <summary>
    /// Applies this specification to the target builder.
    /// </summary>
    /// <param name="builder">The builder value.</param>
    public void Apply(IReceiveEndpointBrokerTopologyBuilder builder)
    {
        foreach (var specification in _specifications)
            specification.Apply(builder);
    }

    /// <summary>
    /// Performs the bind operation.
    /// </summary>
    /// <param name="configure">The configuration callback.</param>
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

    /// <summary>
    /// Validates the current configuration.
    /// </summary>
    /// <returns>The result of the operation.</returns>
    public override IEnumerable<ValidationResult> Validate()
    {
        return base.Validate().Concat(_specifications.SelectMany(x => x.Validate()));
    }
}

using System.Collections.Generic;
using System.Linq;
using ViciOne.ServiceBus.Configuration;
using ViciOne.ServiceBus.InMemoryTransport.Configuration;
using ViciOne.ServiceBus.Transports.Fabric;

#nullable enable
namespace ViciOne.ServiceBus.InMemoryTransport;

/// <summary>
/// Provides an in memory message consume topology implementation.
/// </summary>
/// <typeparam name="TMessage">The t message type.</typeparam>
public class InMemoryMessageConsumeTopology<TMessage> :
    MessageConsumeTopology<TMessage>,
    IInMemoryMessageConsumeTopologyConfigurator<TMessage>,
    IInMemoryMessageConsumeTopologyConfigurator
    where TMessage : class
{
    readonly IMessageTopology<TMessage> _messageTopology;
    readonly IInMemoryPublishTopology _publishTopology;
    readonly List<IInMemoryConsumeTopologySpecification> _specifications;

    /// <summary>
    /// Initializes a new instance of the containing type.
    /// </summary>
    /// <param name="messageTopology">The message topology value.</param>
    /// <param name="publishTopology">The publish topology value.</param>
    public InMemoryMessageConsumeTopology(IMessageTopology<TMessage> messageTopology, IInMemoryPublishTopologyConfigurator publishTopology)
    {
        _messageTopology = messageTopology;
        _publishTopology = publishTopology;
        _specifications = new List<IInMemoryConsumeTopologySpecification>();
    }

    /// <summary>
    /// Applies this specification to the target builder.
    /// </summary>
    /// <param name="builder">The builder value.</param>
    public void Apply(IMessageFabricConsumeTopologyBuilder builder)
    {
        foreach (var specification in _specifications)
            specification.Apply(builder);
    }

    /// <summary>
    /// Performs the bind operation.
    /// </summary>
    /// <param name="exchangeType">The exchange type value.</param>
    /// <param name="routingKey">The routing key value.</param>
    public void Bind(ExchangeType? exchangeType, string? routingKey = default)
    {
        if (!IsBindableMessageType)
        {
            _specifications.Add(new InvalidInMemoryConsumeTopologySpecification(TypeCache<TMessage>.ShortName, "Is not a bindable message type"));
            return;
        }

        var bindExchangeType = exchangeType ?? _publishTopology.GetMessageTopology<TMessage>().ExchangeType;

        var specification = new ExchangeBindingConsumeTopologySpecification(_messageTopology.EntityName, bindExchangeType, routingKey);

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

using System.Collections.Generic;
using System.Linq;
using ViciOne.ServiceBus.Configuration;
using ViciOne.ServiceBus.InMemoryTransport.Configuration;
using ViciOne.ServiceBus.Providers.Transports;
using ViciOne.ServiceBus.Transports.Fabric;

namespace ViciOne.ServiceBus.InMemoryTransport.Topology;

/// <summary>Collects exchange bindings for one consumed message contract.</summary>
/// <typeparam name="TMessage">The consumed message contract.</typeparam>
internal sealed class InMemoryMessageConsumeTopology<TMessage> :
    MessageConsumeTopology<TMessage>,
    IInMemoryMessageConsumeTopologyConfigurator<TMessage>,
    IInMemoryMessageConsumeTopologyConfigurator
    where TMessage : class
{
    readonly IMessageTopology<TMessage> _messageTopology;
    readonly IInMemoryPublishTopology _publishTopology;
    readonly List<IInMemoryConsumeTopologySpecification> _specifications;

    /// <summary>Creates message consume topology over shared entity-name and publish topology.</summary>
    /// <param name="messageTopology">The message entity-name topology.</param>
    /// <param name="publishTopology">The publish topology used to infer exchange behavior.</param>
    public InMemoryMessageConsumeTopology(IMessageTopology<TMessage> messageTopology, IInMemoryPublishTopologyConfigurator publishTopology)
    {
        _messageTopology = messageTopology ?? throw new ArgumentNullException(nameof(messageTopology));
        ArgumentNullException.ThrowIfNull(publishTopology);
        _publishTopology = publishTopology as IInMemoryPublishTopology
            ?? throw new ArgumentException("The publish topology is not an in-memory topology.", nameof(publishTopology));
        _specifications = new List<IInMemoryConsumeTopologySpecification>();
    }

    /// <summary>Applies every binding for this message contract.</summary>
    /// <param name="builder">The consume topology builder to update.</param>
    public void Apply(IMessageFabricConsumeTopologyBuilder builder)
    {
        ArgumentNullException.ThrowIfNull(builder);
        foreach (var specification in _specifications)
            specification.Apply(builder);
    }

    /// <summary>Binds the message contract's publish exchange to the receive endpoint.</summary>
    /// <param name="exchangeType">An exchange routing override, or <see langword="null" /> to use publish topology.</param>
    /// <param name="routingKey">The optional direct or topic routing key.</param>
    public void Bind(InMemoryExchangeType? exchangeType, string? routingKey = default)
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

    /// <summary>Validates inherited message topology and every binding.</summary>
    /// <returns>All message-topology validation failures.</returns>
    public override IEnumerable<ValidationResult> Validate()
    {
        return base.Validate().Concat(_specifications.SelectMany(x => x.Validate()));
    }
}

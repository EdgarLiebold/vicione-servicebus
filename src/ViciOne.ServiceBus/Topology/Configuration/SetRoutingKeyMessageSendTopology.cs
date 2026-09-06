using System;
using ViciOne.ServiceBus.Middleware;
using ViciOne.ServiceBus.Transports;

namespace ViciOne.ServiceBus.Configuration;

/// <summary>Defines the topology for set routing key message send.</summary>
/// <typeparam name="TMessage">The message contract processed by the member.</typeparam>
public class SetRoutingKeyMessageSendTopology<TMessage> :
    IMessageSendTopology<TMessage>
    where TMessage : class
{
    readonly IFilter<SendContext<TMessage>> _filter;

    /// <summary>Initializes a new instance.</summary>
    /// <param name="routingKeyFormatter">The routing key formatter.</param>
    public SetRoutingKeyMessageSendTopology(IMessageRoutingKeyFormatter<TMessage> routingKeyFormatter)
    {
        if (routingKeyFormatter == null)
            throw new ArgumentNullException(nameof(routingKeyFormatter));

        _filter = new SetRoutingKeyFilter<TMessage>(routingKeyFormatter);
    }

    /// <summary>Applies this specification to the target builder.</summary>
    /// <param name="builder">The builder that receives the configuration.</param>
    public void Apply(ITopologyPipeBuilder<SendContext<TMessage>> builder)
    {
        builder.AddFilter(_filter);
    }
}

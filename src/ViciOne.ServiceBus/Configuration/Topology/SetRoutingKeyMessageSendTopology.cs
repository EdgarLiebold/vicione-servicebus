using System;
using ViciOne.ServiceBus.Middleware;
using ViciOne.ServiceBus.Transports;

namespace ViciOne.ServiceBus.Configuration;

/// <summary>Adds routing-key assignment to the send pipe for one message contract.</summary>
/// <typeparam name="TMessage">The sent message contract type.</typeparam>
sealed class SetRoutingKeyMessageSendTopology<TMessage> :
    IMessageSendTopology<TMessage>
    where TMessage : class
{
    readonly IFilter<SendContext<TMessage>> _filter;

    /// <summary>Initializes topology with the formatter used by its send filter.</summary>
    /// <param name="routingKeyFormatter">The message-specific routing-key formatter.</param>
    public SetRoutingKeyMessageSendTopology(IMessageRoutingKeyFormatter<TMessage> routingKeyFormatter)
    {
        ArgumentNullException.ThrowIfNull(routingKeyFormatter);

        _filter = new SetRoutingKeyFilter<TMessage>(routingKeyFormatter);
    }

    /// <summary>Adds the routing-key filter to a send-pipe topology builder.</summary>
    /// <param name="builder">The send-pipe topology builder.</param>
    public void Apply(ITopologyPipeBuilder<SendContext<TMessage>> builder)
    {
        ArgumentNullException.ThrowIfNull(builder);

        builder.AddFilter(_filter);
    }
}

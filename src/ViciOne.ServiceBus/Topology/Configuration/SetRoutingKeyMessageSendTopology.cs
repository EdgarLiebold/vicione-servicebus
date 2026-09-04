using System;
using ViciOne.ServiceBus.Middleware;
using ViciOne.ServiceBus.Transports;

namespace ViciOne.ServiceBus.Configuration;

/// <summary>
/// Provides a set routing key message send topology implementation.
/// </summary>
/// <typeparam name="TMessage">The t message type.</typeparam>
public class SetRoutingKeyMessageSendTopology<TMessage> :
    IMessageSendTopology<TMessage>
    where TMessage : class
{
    readonly IFilter<SendContext<TMessage>> _filter;

    /// <summary>
    /// Initializes a new instance of the containing type.
    /// </summary>
    /// <param name="routingKeyFormatter">The routing key formatter value.</param>
    public SetRoutingKeyMessageSendTopology(IMessageRoutingKeyFormatter<TMessage> routingKeyFormatter)
    {
        if (routingKeyFormatter == null)
            throw new ArgumentNullException(nameof(routingKeyFormatter));

        _filter = new SetRoutingKeyFilter<TMessage>(routingKeyFormatter);
    }

    /// <summary>
    /// Applies this specification to the target builder.
    /// </summary>
    /// <param name="builder">The builder value.</param>
    public void Apply(ITopologyPipeBuilder<SendContext<TMessage>> builder)
    {
        builder.AddFilter(_filter);
    }
}

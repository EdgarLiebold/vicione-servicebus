using System;
using ViciOne.ServiceBus.Configuration;
using ViciOne.ServiceBus.Middleware;

namespace ViciOne.ServiceBus.Topology;

/// <summary>Adds correlation-identifier assignment to the send pipe for one message contract.</summary>
/// <typeparam name="T">The sent message contract type.</typeparam>
sealed class SetCorrelationIdMessageSendTopology<T> :
    IMessageSendTopology<T>
    where T : class
{
    readonly IFilter<SendContext<T>> _filter;

    /// <summary>Initializes topology with the resolver used by its send filter.</summary>
    /// <param name="messageCorrelationId">The correlation resolver.</param>
    public SetCorrelationIdMessageSendTopology(IMessageCorrelationId<T> messageCorrelationId)
    {
        ArgumentNullException.ThrowIfNull(messageCorrelationId);

        _filter = new SetCorrelationIdFilter<T>(messageCorrelationId);
    }

    /// <summary>Adds the correlation-identifier filter to a send-pipe topology builder.</summary>
    /// <param name="builder">The send-pipe topology builder.</param>
    public void Apply(ITopologyPipeBuilder<SendContext<T>> builder)
    {
        ArgumentNullException.ThrowIfNull(builder);

        builder.AddFilter(_filter);
    }
}

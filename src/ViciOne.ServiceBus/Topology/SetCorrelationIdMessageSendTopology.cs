using System;
using ViciOne.ServiceBus.Configuration;
using ViciOne.ServiceBus.Middleware;

namespace ViciOne.ServiceBus.Topology;

/// <summary>
/// Provides a set correlation id message send topology implementation.
/// </summary>
/// <typeparam name="T">The t type.</typeparam>
public class SetCorrelationIdMessageSendTopology<T> :
    IMessageSendTopology<T>
    where T : class
{
    readonly IFilter<SendContext<T>> _filter;

    /// <summary>
    /// Initializes a new instance of the containing type.
    /// </summary>
    /// <param name="messageCorrelationId">The message correlation id value.</param>
    public SetCorrelationIdMessageSendTopology(IMessageCorrelationId<T> messageCorrelationId)
    {
        if (messageCorrelationId == null)
            throw new ArgumentNullException(nameof(messageCorrelationId));

        _filter = new SetCorrelationIdFilter<T>(messageCorrelationId);
    }

    /// <summary>
    /// Applies this specification to the target builder.
    /// </summary>
    /// <param name="builder">The builder value.</param>
    public void Apply(ITopologyPipeBuilder<SendContext<T>> builder)
    {
        builder.AddFilter(_filter);
    }
}

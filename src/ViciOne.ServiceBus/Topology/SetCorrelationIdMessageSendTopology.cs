using System;
using ViciOne.ServiceBus.Configuration;
using ViciOne.ServiceBus.Middleware;

namespace ViciOne.ServiceBus.Topology;

/// <summary>Defines the topology for set correlation id message send.</summary>
/// <typeparam name="T">The value type.</typeparam>
public class SetCorrelationIdMessageSendTopology<T> :
    IMessageSendTopology<T>
    where T : class
{
    readonly IFilter<SendContext<T>> _filter;

    /// <summary>Initializes a new instance.</summary>
    /// <param name="messageCorrelationId">The message correlation id.</param>
    public SetCorrelationIdMessageSendTopology(IMessageCorrelationId<T> messageCorrelationId)
    {
        if (messageCorrelationId == null)
            throw new ArgumentNullException(nameof(messageCorrelationId));

        _filter = new SetCorrelationIdFilter<T>(messageCorrelationId);
    }

    /// <summary>Applies this specification to the target builder.</summary>
    /// <param name="builder">The builder that receives the configuration.</param>
    public void Apply(ITopologyPipeBuilder<SendContext<T>> builder)
    {
        builder.AddFilter(_filter);
    }
}

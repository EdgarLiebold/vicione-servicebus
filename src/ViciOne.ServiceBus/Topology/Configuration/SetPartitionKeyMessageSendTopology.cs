using System;
using ViciOne.ServiceBus.Middleware;
using ViciOne.ServiceBus.Transports;

namespace ViciOne.ServiceBus.Configuration;

/// <summary>Defines the topology for set partition key message send.</summary>
/// <typeparam name="TMessage">The message contract processed by the member.</typeparam>
public class SetPartitionKeyMessageSendTopology<TMessage> :
    IMessageSendTopology<TMessage>
    where TMessage : class
{
    readonly IFilter<SendContext<TMessage>> _filter;

    /// <summary>Initializes a new instance.</summary>
    /// <param name="partitionKeyFormatter">The partition key formatter.</param>
    public SetPartitionKeyMessageSendTopology(IMessagePartitionKeyFormatter<TMessage> partitionKeyFormatter)
    {
        if (partitionKeyFormatter == null)
            throw new ArgumentNullException(nameof(partitionKeyFormatter));

        _filter = new SetPartitionKeyFilter<TMessage>(partitionKeyFormatter);
    }

    /// <summary>Applies this specification to the target builder.</summary>
    /// <param name="builder">The builder that receives the configuration.</param>
    public void Apply(ITopologyPipeBuilder<SendContext<TMessage>> builder)
    {
        builder.AddFilter(_filter);
    }
}

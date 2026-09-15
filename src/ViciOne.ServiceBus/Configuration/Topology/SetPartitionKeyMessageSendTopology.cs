using System;
using ViciOne.ServiceBus.Middleware;
using ViciOne.ServiceBus.Transports;

namespace ViciOne.ServiceBus.Configuration;

/// <summary>Adds partition-key assignment to the send pipe for one message contract.</summary>
/// <typeparam name="TMessage">The sent message contract type.</typeparam>
sealed class SetPartitionKeyMessageSendTopology<TMessage> :
    IMessageSendTopology<TMessage>
    where TMessage : class
{
    readonly IFilter<SendContext<TMessage>> _filter;

    /// <summary>Initializes topology with the formatter used by its send filter.</summary>
    /// <param name="partitionKeyFormatter">The message-specific partition-key formatter.</param>
    public SetPartitionKeyMessageSendTopology(IMessagePartitionKeyFormatter<TMessage> partitionKeyFormatter)
    {
        ArgumentNullException.ThrowIfNull(partitionKeyFormatter);

        _filter = new SetPartitionKeyFilter<TMessage>(partitionKeyFormatter);
    }

    /// <summary>Adds the partition-key filter to a send-pipe topology builder.</summary>
    /// <param name="builder">The send-pipe topology builder.</param>
    public void Apply(ITopologyPipeBuilder<SendContext<TMessage>> builder)
    {
        ArgumentNullException.ThrowIfNull(builder);

        builder.AddFilter(_filter);
    }
}

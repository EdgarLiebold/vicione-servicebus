using System;
using ViciOne.ServiceBus.Middleware;
using ViciOne.ServiceBus.Transports;

namespace ViciOne.ServiceBus.Configuration;

/// <summary>
/// Provides a set partition key message send topology implementation.
/// </summary>
/// <typeparam name="TMessage">The t message type.</typeparam>
public class SetPartitionKeyMessageSendTopology<TMessage> :
    IMessageSendTopology<TMessage>
    where TMessage : class
{
    readonly IFilter<SendContext<TMessage>> _filter;

    /// <summary>
    /// Initializes a new instance of the containing type.
    /// </summary>
    /// <param name="partitionKeyFormatter">The partition key formatter value.</param>
    public SetPartitionKeyMessageSendTopology(IMessagePartitionKeyFormatter<TMessage> partitionKeyFormatter)
    {
        if (partitionKeyFormatter == null)
            throw new ArgumentNullException(nameof(partitionKeyFormatter));

        _filter = new SetPartitionKeyFilter<TMessage>(partitionKeyFormatter);
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

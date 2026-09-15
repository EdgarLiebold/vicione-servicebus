using System;
using System.Net.Mime;
using ViciOne.ServiceBus.Configuration;
using ViciOne.ServiceBus.Middleware;

namespace ViciOne.ServiceBus.Topology;

/// <summary>Adds serializer selection to the send pipe for one message contract.</summary>
/// <typeparam name="T">The sent message contract type.</typeparam>
sealed class SetSerializerMessageSendTopology<T> :
    IMessageSendTopology<T>
    where T : class
{
    readonly IFilter<SendContext<T>> _filter;

    /// <summary>Initializes topology with the serializer content type used by its send filter.</summary>
    /// <param name="contentType">The registered serializer content type.</param>
    public SetSerializerMessageSendTopology(ContentType contentType)
    {
        ArgumentNullException.ThrowIfNull(contentType);

        _filter = new SetSerializerFilter<T>(contentType);
    }

    /// <summary>Adds the serializer-selection filter to a send-pipe topology builder.</summary>
    /// <param name="builder">The send-pipe topology builder.</param>
    public void Apply(ITopologyPipeBuilder<SendContext<T>> builder)
    {
        ArgumentNullException.ThrowIfNull(builder);

        builder.AddFilter(_filter);
    }
}

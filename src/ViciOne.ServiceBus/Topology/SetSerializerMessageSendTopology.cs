using System;
using System.Net.Mime;
using ViciOne.ServiceBus.Configuration;
using ViciOne.ServiceBus.Middleware;

namespace ViciOne.ServiceBus.Topology;

public class SetSerializerMessageSendTopology<T> :
    IMessageSendTopology<T>
    where T : class
{
    readonly IFilter<SendContext<T>> _filter;

    public SetSerializerMessageSendTopology(ContentType contentType)
    {
        if (contentType == null)
            throw new ArgumentNullException(nameof(contentType));

        _filter = new SetSerializerFilter<T>(contentType);
    }

    public void Apply(ITopologyPipeBuilder<SendContext<T>> builder)
    {
        builder.AddFilter(_filter);
    }
}

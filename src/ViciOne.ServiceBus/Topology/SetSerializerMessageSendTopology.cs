using System;
using System.Net.Mime;
using ViciOne.ServiceBus.Configuration;
using ViciOne.ServiceBus.Middleware;

namespace ViciOne.ServiceBus.Topology;

/// <summary>Defines the topology for set serializer message send.</summary>
/// <typeparam name="T">The value type.</typeparam>
public class SetSerializerMessageSendTopology<T> :
    IMessageSendTopology<T>
    where T : class
{
    readonly IFilter<SendContext<T>> _filter;

    /// <summary>Initializes a new instance.</summary>
    /// <param name="contentType">The runtime content type used by the operation.</param>
    public SetSerializerMessageSendTopology(ContentType contentType)
    {
        if (contentType == null)
            throw new ArgumentNullException(nameof(contentType));

        _filter = new SetSerializerFilter<T>(contentType);
    }

    /// <summary>Applies this specification to the target builder.</summary>
    /// <param name="builder">The builder that receives the configuration.</param>
    public void Apply(ITopologyPipeBuilder<SendContext<T>> builder)
    {
        builder.AddFilter(_filter);
    }
}

using System;
using System.Net.Mime;
using ViciOne.ServiceBus.Configuration;
using ViciOne.ServiceBus.Middleware;

namespace ViciOne.ServiceBus.Topology;

/// <summary>
/// Provides a set serializer message send topology implementation.
/// </summary>
/// <typeparam name="T">The t type.</typeparam>
public class SetSerializerMessageSendTopology<T> :
    IMessageSendTopology<T>
    where T : class
{
    readonly IFilter<SendContext<T>> _filter;

    /// <summary>
    /// Initializes a new instance of the containing type.
    /// </summary>
    /// <param name="contentType">The content type value.</param>
    public SetSerializerMessageSendTopology(ContentType contentType)
    {
        if (contentType == null)
            throw new ArgumentNullException(nameof(contentType));

        _filter = new SetSerializerFilter<T>(contentType);
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

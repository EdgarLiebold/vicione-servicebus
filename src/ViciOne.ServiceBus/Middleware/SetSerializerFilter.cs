using System.Net.Mime;
using System.Threading.Tasks;

namespace ViciOne.ServiceBus.Middleware;

/// <summary>Selects an available serializer matching the configured content type before forwarding the send context.</summary>
/// <typeparam name="T">The message type.</typeparam>
public class SetSerializerFilter<T> :
    IFilter<SendContext<T>>
    where T : class
{
    readonly ContentType _contentType;

    /// <summary>Initializes a new instance.</summary>
    /// <param name="contentType">The runtime content type used by the operation.</param>
    public SetSerializerFilter(ContentType contentType)
    {
        _contentType = contentType;
    }

    /// <summary>Selects an available serializer for the configured content type before invoking the continuation.</summary>
    /// <param name="context">The context associated with the operation.</param>
    /// <param name="next">The next pipeline stage to invoke.</param>
    /// <returns>A task that represents the asynchronous operation.</returns>
    public Task SendAsync(SendContext<T> context, IPipe<SendContext<T>> next)
    {
        if (context.Serialization.TryGetMessageSerializer(_contentType, out var serializer))
            context.Serializer = serializer;

        return next.SendAsync(context);
    }

    /// <summary>Writes diagnostic information to the probe context.</summary>
    /// <param name="context">The context associated with the operation.</param>
    public void Probe(ProbeContext context)
    {
        context.CreateFilterScope("SetMessageSerializer");
    }
}

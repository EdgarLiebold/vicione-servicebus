using System.Net.Mime;
using System.Threading.Tasks;

namespace ViciOne.ServiceBus.Middleware;

/// <summary>
/// Sets the CorrelationId header uses the supplied implementation.
/// </summary>
/// <typeparam name="T">The message type</typeparam>
public class SetSerializerFilter<T> :
    IFilter<SendContext<T>>
    where T : class
{
    readonly ContentType _contentType;

    /// <summary>
    /// Initializes a new instance of the containing type.
    /// </summary>
    /// <param name="contentType">The content type value.</param>
    public SetSerializerFilter(ContentType contentType)
    {
        _contentType = contentType;
    }

    /// <summary>
    /// Sends a message to the configured destination.
    /// </summary>
    /// <param name="context">The operation context.</param>
    /// <param name="next">The next value.</param>
    /// <returns>The result of the operation.</returns>
    public Task SendAsync(SendContext<T> context, IPipe<SendContext<T>> next)
    {
        if (context.Serialization.TryGetMessageSerializer(_contentType, out var serializer))
            context.Serializer = serializer;

        return next.SendAsync(context);
    }

    /// <summary>
    /// Performs the probe operation.
    /// </summary>
    /// <param name="context">The operation context.</param>
    public void Probe(ProbeContext context)
    {
        context.CreateFilterScope("SetMessageSerializer");
    }
}

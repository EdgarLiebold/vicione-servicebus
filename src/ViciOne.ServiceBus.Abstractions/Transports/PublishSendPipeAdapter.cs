using System.Threading.Tasks;

namespace ViciOne.ServiceBus.Transports;

/// <summary>
/// Provides a publish send pipe adapter implementation.
/// </summary>
/// <typeparam name="T">The t type.</typeparam>
public class PublishSendPipeAdapter<T> :
    IPipe<SendContext<T>>
    where T : class
{
    readonly IPipe<PublishContext<T>> _pipe;

    /// <summary>
    /// Initializes a new instance of the containing type.
    /// </summary>
    /// <param name="pipe">The pipe value.</param>
    public PublishSendPipeAdapter(IPipe<PublishContext<T>> pipe)
    {
        _pipe = pipe;
    }

    void IProbeSite.Probe(ProbeContext context)
    {
        _pipe.Probe(context);
    }

    /// <summary>
    /// Sends a message to the configured destination.
    /// </summary>
    /// <param name="context">The operation context.</param>
    /// <returns>The result of the operation.</returns>
    public Task SendAsync(SendContext<T> context)
    {
        var publishContext = context.GetPayload<PublishContext<T>>();

        return _pipe.SendAsync(publishContext);
    }
}

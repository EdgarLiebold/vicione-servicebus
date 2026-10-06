using System.Threading.Tasks;

namespace ViciOne.ServiceBus.Transports;

/// <summary>Adapts publish send pipe between component contracts.</summary>
/// <typeparam name="T">The value type.</typeparam>
public class PublishSendPipeAdapter<T> :
    IPipe<SendContext<T>>
    where T : class
{
    readonly IPipe<PublishContext<T>> _pipe;

    /// <summary>Initializes a new instance.</summary>
    /// <param name="pipe">The pipeline stages to apply.</param>
    public PublishSendPipeAdapter(IPipe<PublishContext<T>> pipe)
    {
        _pipe = pipe;
    }

    void IProbeSite.Probe(ProbeContext context)
    {
        _pipe.Probe(context);
    }

    /// <summary>Resolves the typed publish-context payload and forwards it to the retained publish pipeline.</summary>
    /// <param name="context">The context associated with the operation.</param>
    /// <returns>A task that represents the asynchronous operation.</returns>
    public Task SendAsync(SendContext<T> context)
    {
        var publishContext = context.GetPayload<PublishContext<T>>();

        return _pipe.SendAsync(publishContext);
    }
}

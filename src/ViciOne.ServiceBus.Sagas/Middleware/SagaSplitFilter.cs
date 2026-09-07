using System.Threading.Tasks;

namespace ViciOne.ServiceBus.Middleware;

/// <summary>
/// Splits a context item off the pipe and carries it out-of-band to be merged
/// once the next filter has completed.
/// </summary>
/// <typeparam name="TSaga">The saga state managed by the member.</typeparam>
/// <typeparam name="TMessage">The message contract processed by the member.</typeparam>
public class SagaSplitFilter<TSaga, TMessage> :
    IFilter<SagaConsumeContext<TSaga, TMessage>>
    where TMessage : class
    where TSaga : class, ISaga
{
    readonly IFilter<SagaConsumeContext<TSaga>> _next;

    /// <summary>Initializes a new instance.</summary>
    /// <param name="next">The next pipeline stage to invoke.</param>
    public SagaSplitFilter(IFilter<SagaConsumeContext<TSaga>> next)
    {
        _next = next;
    }

    void IProbeSite.Probe(ProbeContext context)
    {
        var scope = context.CreateFilterScope("split");
        scope.Set(new { SagaType = TypeCache<TSaga>.ShortName });

        _next.Probe(scope);
    }

    /// <summary>Sends a message to the configured destination.</summary>
    /// <param name="context">The context associated with the operation.</param>
    /// <param name="next">The next pipeline stage to invoke.</param>
    /// <returns>A task that represents the asynchronous operation.</returns>
    public Task SendAsync(SagaConsumeContext<TSaga, TMessage> context, IPipe<SagaConsumeContext<TSaga, TMessage>> next)
    {
        var mergePipe = new SagaMergePipe<TSaga, TMessage>(next);

        return _next.SendAsync(context, mergePipe);
    }
}

using System.Diagnostics;
using System.Threading.Tasks;

namespace ViciOne.ServiceBus.Middleware;

/// <summary>
/// Splits a context item off the pipe and carries it out-of-band to be merged
/// once the next filter has completed.
/// </summary>
/// <typeparam name="TConsumer">The consumer implementation used by the member.</typeparam>
/// <typeparam name="TMessage">The message contract processed by the member.</typeparam>
public class ConsumerSplitFilter<TConsumer, TMessage> :
    IFilter<ConsumerConsumeContext<TConsumer, TMessage>>
    where TMessage : class
    where TConsumer : class
{
    readonly IFilter<ConsumerConsumeContext<TConsumer>> _next;

    /// <summary>Initializes a new instance.</summary>
    /// <param name="next">The next pipeline stage to invoke.</param>
    public ConsumerSplitFilter(IFilter<ConsumerConsumeContext<TConsumer>> next)
    {
        _next = next;
    }

    void IProbeSite.Probe(ProbeContext context)
    {
        var scope = context.CreateFilterScope("split");
        scope.Set(new { ConsumerType = TypeCache<TConsumer>.ShortName });

        _next.Probe(scope);
    }

    /// <summary>Runs the consumer filter with a continuation adapted to the typed message context.</summary>
    /// <param name="context">The context associated with the operation.</param>
    /// <param name="next">The next pipeline stage to invoke.</param>
    /// <returns>A task that represents the asynchronous operation.</returns>
    [DebuggerNonUserCode]
    public Task SendAsync(ConsumerConsumeContext<TConsumer, TMessage> context, IPipe<ConsumerConsumeContext<TConsumer, TMessage>> next)
    {
        var mergePipe = new ConsumerMergePipe<TConsumer, TMessage>(next);

        return _next.SendAsync(context, mergePipe);
    }
}

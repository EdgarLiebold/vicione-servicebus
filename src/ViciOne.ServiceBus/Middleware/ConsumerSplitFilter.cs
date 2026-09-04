using System.Diagnostics;
using System.Threading.Tasks;

namespace ViciOne.ServiceBus.Middleware;

/// <summary>
/// Splits a context item off the pipe and carries it out-of-band to be merged
/// once the next filter has completed
/// </summary>
/// <typeparam name="TConsumer"></typeparam>
/// <typeparam name="TMessage"></typeparam>
public class ConsumerSplitFilter<TConsumer, TMessage> :
    IFilter<ConsumerConsumeContext<TConsumer, TMessage>>
    where TMessage : class
    where TConsumer : class
{
    readonly IFilter<ConsumerConsumeContext<TConsumer>> _next;

    /// <summary>
    /// Initializes a new instance of the containing type.
    /// </summary>
    /// <param name="next">The next value.</param>
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

    /// <summary>
    /// Sends a message to the configured destination.
    /// </summary>
    /// <param name="context">The operation context.</param>
    /// <param name="next">The next value.</param>
    /// <returns>The result of the operation.</returns>
    [DebuggerNonUserCode]
    public Task SendAsync(ConsumerConsumeContext<TConsumer, TMessage> context, IPipe<ConsumerConsumeContext<TConsumer, TMessage>> next)
    {
        var mergePipe = new ConsumerMergePipe<TConsumer, TMessage>(next);

        return _next.SendAsync(context, mergePipe);
    }
}

using System.Diagnostics;
using System.Threading.Tasks;

namespace ViciOne.ServiceBus.Middleware;

/// <summary>
/// Splits a context item off the pipe and carries it out-of-band to be merged
/// once the next filter has completed
/// </summary>
/// <typeparam name="TSaga"></typeparam>
/// <typeparam name="TMessage"></typeparam>
public class SagaMessageSplitFilter<TSaga, TMessage> :
    IFilter<SagaConsumeContext<TSaga, TMessage>>
    where TMessage : class
    where TSaga : class, ISaga
{
    readonly IFilter<ConsumeContext<TMessage>> _next;

    /// <summary>
    /// Initializes a new instance of the containing type.
    /// </summary>
    /// <param name="next">The next value.</param>
    public SagaMessageSplitFilter(IFilter<ConsumeContext<TMessage>> next)
    {
        _next = next;
    }

    void IProbeSite.Probe(ProbeContext context)
    {
        var scope = context.CreateFilterScope("split");
        scope.Set(new { MessageType = TypeCache<TMessage>.ShortName });

        _next.Probe(scope);
    }

    /// <summary>
    /// Sends a message to the configured destination.
    /// </summary>
    /// <param name="context">The operation context.</param>
    /// <param name="next">The next value.</param>
    /// <returns>The result of the operation.</returns>
    [DebuggerNonUserCode]
    public Task SendAsync(SagaConsumeContext<TSaga, TMessage> context, IPipe<SagaConsumeContext<TSaga, TMessage>> next)
    {
        var mergePipe = new SagaMessageMergePipe<TSaga, TMessage>(next, context);

        return _next.SendAsync(context, mergePipe);
    }
}

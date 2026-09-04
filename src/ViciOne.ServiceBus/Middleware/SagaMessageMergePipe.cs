using System.Threading.Tasks;
using ViciOne.ServiceBus.Context;

namespace ViciOne.ServiceBus.Middleware;

/// <summary>
/// Merges the out-of-band Saga back into the context
/// </summary>
/// <typeparam name="TSaga"></typeparam>
/// <typeparam name="TMessage"></typeparam>
public class SagaMessageMergePipe<TSaga, TMessage> :
    IPipe<ConsumeContext<TMessage>>
    where TMessage : class
    where TSaga : class, ISaga
{
    readonly SagaConsumeContext<TSaga, TMessage> _context;
    readonly IPipe<SagaConsumeContext<TSaga, TMessage>> _output;

    /// <summary>
    /// Initializes a new instance of the containing type.
    /// </summary>
    /// <param name="output">The output value.</param>
    /// <param name="context">The operation context.</param>
    public SagaMessageMergePipe(IPipe<SagaConsumeContext<TSaga, TMessage>> output, SagaConsumeContext<TSaga, TMessage> context)
    {
        _output = output;
        _context = context;
    }

    void IProbeSite.Probe(ProbeContext context)
    {
        var scope = context.CreateFilterScope("merge");
        scope.Set(new
        {
            SagaType = TypeCache<TSaga>.ShortName,
            MessageType = TypeCache<TMessage>.ShortName
        });

        _output.Probe(scope);
    }

    /// <summary>
    /// Sends a message to the configured destination.
    /// </summary>
    /// <param name="context">The operation context.</param>
    /// <returns>The result of the operation.</returns>
    public Task SendAsync(ConsumeContext<TMessage> context)
    {
        if (ReferenceEquals(context, _context))
            return _output.SendAsync(_context);

        return context is SagaConsumeContext<TSaga, TMessage> consumerContext
            ? _output.SendAsync(consumerContext)
            : _output.SendAsync(new SagaConsumeContextProxy<TSaga, TMessage>(context, _context));
    }
}

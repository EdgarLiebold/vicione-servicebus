using System;
using System.Threading.Tasks;
using ViciOne.ServiceBus.Context;

namespace ViciOne.ServiceBus.Middleware;

/// <summary>
/// Merges an adapted message context with the original saga owner. The composite exposes the original
/// saga view as the typed saga payload, then resolves all other payloads from the adapted message context.
/// </summary>
/// <typeparam name="TSaga">The saga state managed by the member.</typeparam>
/// <typeparam name="TMessage">The message contract processed by the member.</typeparam>
public class SagaMessageMergePipe<TSaga, TMessage> :
    IPipe<ConsumeContext<TMessage>>
    where TMessage : class
    where TSaga : class, ISaga
{
    readonly SagaConsumeContext<TSaga, TMessage> _context;
    readonly IPipe<SagaConsumeContext<TSaga, TMessage>> _output;

    /// <summary>Initializes a new instance.</summary>
    /// <param name="output">The output.</param>
    /// <param name="context">The context associated with the operation.</param>
    public SagaMessageMergePipe(IPipe<SagaConsumeContext<TSaga, TMessage>> output, SagaConsumeContext<TSaga, TMessage> context)
    {
        _output = output ?? throw new ArgumentNullException(nameof(output));
        _context = context ?? throw new ArgumentNullException(nameof(context));
    }

    void IProbeSite.Probe(ProbeContext context)
    {
        ArgumentNullException.ThrowIfNull(context);

        var scope = context.CreateFilterScope("merge");
        scope.Set(new
        {
            SagaType = TypeCache<TSaga>.ShortName,
            MessageType = TypeCache<TMessage>.ShortName
        });

        _output.Probe(scope);
    }

    /// <summary>Sends a message to the configured destination.</summary>
    /// <param name="context">The context associated with the operation.</param>
    /// <returns>A task that represents the asynchronous operation.</returns>
    public Task SendAsync(ConsumeContext<TMessage> context)
    {
        ArgumentNullException.ThrowIfNull(context);

        if (ReferenceEquals(context, _context))
            return _output.SendAsync(_context);

        return _output.SendAsync(new SagaConsumeContextProxy<TSaga, TMessage>(context, _context));
    }
}

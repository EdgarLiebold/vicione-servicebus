using System;
using System.Threading.Tasks;
using ViciOne.ServiceBus.Context;

namespace ViciOne.ServiceBus.Middleware;

/// <summary>
/// Merges an adapted saga view with its original typed message context. The composite exposes its
/// current saga view as the typed saga payload, then resolves all other payloads from the original
/// message context.
/// </summary>
/// <typeparam name="TSaga">The saga state managed by the member.</typeparam>
/// <typeparam name="TMessage">The message contract processed by the member.</typeparam>
public class SagaMergePipe<TSaga, TMessage> :
    IPipe<SagaConsumeContext<TSaga>>
    where TMessage : class
    where TSaga : class, ISaga
{
    readonly SagaConsumeContext<TSaga, TMessage>? _context;
    readonly IPipe<SagaConsumeContext<TSaga, TMessage>> _output;

    /// <summary>Initializes a new instance.</summary>
    /// <param name="output">The output.</param>
    public SagaMergePipe(IPipe<SagaConsumeContext<TSaga, TMessage>> output)
    {
        _output = output ?? throw new ArgumentNullException(nameof(output));
    }

    /// <summary>Initializes a merge that retains the original typed message context.</summary>
    /// <param name="output">The output.</param>
    /// <param name="context">The original typed context that owns the message side of the merge.</param>
    internal SagaMergePipe(IPipe<SagaConsumeContext<TSaga, TMessage>> output, SagaConsumeContext<TSaga, TMessage> context)
        : this(output)
    {
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
    public Task SendAsync(SagaConsumeContext<TSaga> context)
    {
        ArgumentNullException.ThrowIfNull(context);

        SagaConsumeContext<TSaga, TMessage>? originalContext = _context;
        if (originalContext is null)
        {
            if (context is SagaConsumeContext<TSaga, TMessage> consumerContext)
                return _output.SendAsync(consumerContext);

            throw new ArgumentException($"The message could not be retrieved: {TypeCache<TMessage>.ShortName}", nameof(context));
        }

        if (ReferenceEquals(context, originalContext))
            return _output.SendAsync(originalContext);

        if (context is SagaConsumeContext<TSaga, TMessage> adaptedContext)
            return _output.SendAsync(new SagaConsumeContextProxy<TSaga, TMessage>(originalContext, adaptedContext));

        return _output.SendAsync(new SagaOnlyConsumeContextProxy(originalContext, context));
    }

    /// <summary>
    /// Uses the original typed context as the message and payload owner while forwarding live saga state
    /// and completion to an adapter that intentionally exposes only the saga half.
    /// </summary>
    sealed class SagaOnlyConsumeContextProxy :
        ConsumeContextProxy<TMessage>,
        SagaConsumeContext<TSaga, TMessage>
    {
        readonly SagaConsumeContext<TSaga> _sagaContext;

        public SagaOnlyConsumeContextProxy(
            SagaConsumeContext<TSaga, TMessage> messageContext,
            SagaConsumeContext<TSaga> sagaContext)
            : base(messageContext)
        {
            _sagaContext = sagaContext ?? throw new ArgumentNullException(nameof(sagaContext));
        }

        public override Guid? CorrelationId => Saga.CorrelationId;

        public TSaga Saga => _sagaContext.Saga;

        public bool IsCompleted => _sagaContext.IsCompleted;

        public Task SetCompletedAsync(CancellationToken cancellationToken = default)
        {
            return _sagaContext.SetCompletedAsync(cancellationToken);
        }
    }
}

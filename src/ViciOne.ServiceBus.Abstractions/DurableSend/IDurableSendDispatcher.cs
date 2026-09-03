using System.Threading;
using System.Threading.Tasks;

namespace ViciOne.ServiceBus;

/// <summary>Transport adapter used by the generic durable sender to dispatch an already serialized retained message.</summary>
public interface IDurableSendDispatcher<TBus>
    where TBus : class, IBus
{
    /// <summary>
    /// Dispatches one persisted message and reports the selected transport's real retirement boundary. For a durable
    /// broker/provider hand-off, return <see cref="DurableSendCompletionMode.TransportAcceptance"/> only after that
    /// provider's durable acceptance boundary has completed. For a volatile in-process enqueue, propagate
    /// <see cref="DurableSendDispatchContext.ConsumerCompletion"/> as process-local pipeline context and return
    /// <see cref="DurableSendCompletionMode.ConsumerCompletion"/>. The completion capability MUST NOT be serialized.
    /// </summary>
    Task<DurableSendDispatchResult> DispatchAsync(
        DurableSendDispatchContext context,
        CancellationToken cancellationToken = default);
}

using System.Threading;
using System.Threading.Tasks;

namespace ViciOne.ServiceBus.Providers.Persistence;

/// <summary>Transport adapter used by the generic durable sender to dispatch an already serialized retained message.</summary>
/// <typeparam name="TBus">The bus type.</typeparam>
public interface IDurableSendDispatcher<TBus>
    where TBus : class, IBus
{
    /// <summary>
    /// Dispatches one persisted message and reports the selected transport's real completion boundary. For a durable
    /// broker/provider hand-off, return <see cref="DurableSendCompletionMode.TransportAcceptance"/> only after that
    /// provider's durable acceptance boundary has completed. For a volatile in-process enqueue, propagate
    /// <see cref="DurableSendDispatchContext.ConsumerCompletion"/> as process-local pipeline context and return
    /// <see cref="DurableSendCompletionMode.ConsumerCompletion"/>. The completion capability MUST NOT be serialized.
    /// </summary>
    /// <param name="context">The context associated with the operation.</param>
    /// <param name="cancellationToken">The token used to cancel the operation.</param>
    /// <returns>A task that produces the dispatch outcome.</returns>
    Task<DurableSendDispatchResult> DispatchAsync(
        DurableSendDispatchContext context,
        CancellationToken cancellationToken = default);
}

using System;
using System.ComponentModel;
using System.Threading;
using System.Threading.Tasks;

namespace ViciOne.ServiceBus;

/// <summary>
/// Executes a consumer pipeline delegate under a consumer-local concurrency contract. The gate owns acquisition and
/// release so callers cannot leak or double-release a slot.
/// </summary>
[EditorBrowsable(EditorBrowsableState.Never)]
public interface IConsumerConcurrencyGate<in TMessage>
{
    ValueTask ExecuteAsync<TState>(
        TMessage message,
        TState state,
        Func<TState, CancellationToken, ValueTask> next,
        CancellationToken cancellationToken = default);
}

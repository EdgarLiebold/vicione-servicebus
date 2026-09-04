using System;
using System.Threading;
using System.Threading.Tasks;

namespace ViciOne.ServiceBus.Advanced.Registration;

/// <summary>
/// Executes a consumer pipeline delegate under a consumer-local concurrency contract. The gate owns acquisition and
/// release so callers cannot leak or double-release a slot.
/// </summary>
public interface IConsumerConcurrencyGate<in TMessage>
{
    /// <summary>
    /// Performs the execute operation.
    /// </summary>
    /// <typeparam name="TState">The t state type.</typeparam>
    /// <param name="message">The message value.</param>
    /// <param name="state">The state value.</param>
    /// <param name="next">The next value.</param>
    /// <param name="cancellationToken">The token used to cancel the operation.</param>
    /// <returns>The result of the operation.</returns>
    ValueTask ExecuteAsync<TState>(
        TMessage message,
        TState state,
        Func<TState, CancellationToken, ValueTask> next,
        CancellationToken cancellationToken = default);
}

using System;
using System.Threading;
using System.Threading.Tasks;

namespace ViciOne.ServiceBus.Advanced.Registration;

/// <summary>
/// Executes a consumer pipeline delegate under a consumer-local concurrency contract. The gate owns acquisition and
/// release so callers cannot leak or double-release a slot.
/// </summary>
/// <typeparam name="TMessage">The message contract processed by the member.</typeparam>
public interface IConsumerConcurrencyGate<in TMessage>
{
    /// <summary>Runs the configured action.</summary>
    /// <typeparam name="TState">The state carried by the operation.</typeparam>
    /// <param name="message">The message to process.</param>
    /// <param name="state">The state.</param>
    /// <param name="next">The next pipeline stage to invoke.</param>
    /// <param name="cancellationToken">The token used to cancel the operation.</param>
    /// <returns>A task that represents the asynchronous operation.</returns>
    ValueTask ExecuteAsync<TState>(
        TMessage message,
        TState state,
        Func<TState, CancellationToken, ValueTask> next,
        CancellationToken cancellationToken = default);
}

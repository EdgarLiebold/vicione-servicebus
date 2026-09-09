using System;
using System.Threading;
using System.Threading.Tasks;

namespace ViciOne.ServiceBus.Advanced.Registration;

/// <summary>
/// Admits a consumer pipeline delegate under a consumer-local concurrency policy and owns release of the admission.
/// </summary>
/// <typeparam name="TMessage">The message contract processed by the member.</typeparam>
public interface IConsumerConcurrencyGate<in TMessage>
{
    /// <summary>Waits for admission, invokes the consumer delegate, and releases the admission when it completes.</summary>
    /// <typeparam name="TState">The state passed to the consumer delegate.</typeparam>
    /// <param name="message">The message used to select an admission partition, when configured.</param>
    /// <param name="state">The state supplied to <paramref name="next"/>.</param>
    /// <param name="next">The consumer delegate invoked after admission.</param>
    /// <param name="cancellationToken">The token that cancels the admission wait and consumer delegate.</param>
    /// <returns>A task that completes after the consumer delegate and admission release complete.</returns>
    ValueTask ExecuteAsync<TState>(
        TMessage message,
        TState state,
        Func<TState, CancellationToken, ValueTask> next,
        CancellationToken cancellationToken = default);
}

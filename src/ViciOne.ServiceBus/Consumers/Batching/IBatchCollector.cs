using System;
using System.Threading.Tasks;

namespace ViciOne.ServiceBus.Batching;

/// <summary>
/// Defines the contract for batch collector.
/// </summary>
/// <typeparam name="TMessage">The t message type.</typeparam>
public interface IBatchCollector<TMessage> :
    IAsyncDisposable,
    IProbeSite
    where TMessage : class
{
    /// <summary>
    /// Performs the collect operation.
    /// </summary>
    /// <param name="context">The operation context.</param>
    /// <param name="cancellationToken">The token used to cancel the operation.</param>
    /// <returns>The result of the operation.</returns>
    Task<BatchConsumer<TMessage>> CollectAsync(ConsumeContext<TMessage> context, CancellationToken cancellationToken = default);

    /// <summary>
    /// Complete the consumer, since it's already completed, to clear the dictionary if it matches
    /// </summary>
    /// <param name="context"></param>
    /// <param name="consumer"></param>
    /// <param name="cancellationToken">The token used to cancel the operation.</param>
    Task CompleteAsync(ConsumeContext<TMessage> context, BatchConsumer<TMessage> consumer, CancellationToken cancellationToken = default);
}

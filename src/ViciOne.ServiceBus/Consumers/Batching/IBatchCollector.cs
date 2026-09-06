using System;
using System.Threading.Tasks;

namespace ViciOne.ServiceBus.Batching;

/// <summary>Defines the operations required by batch collector.</summary>
/// <typeparam name="TMessage">The message contract processed by the member.</typeparam>
public interface IBatchCollector<TMessage> :
    IAsyncDisposable,
    IProbeSite
    where TMessage : class
{
    /// <summary>Collects the matching values.</summary>
    /// <param name="context">The context associated with the operation.</param>
    /// <param name="cancellationToken">The token used to cancel the operation.</param>
    /// <returns>A task that produces the collect outcome.</returns>
    Task<BatchConsumer<TMessage>> CollectAsync(ConsumeContext<TMessage> context, CancellationToken cancellationToken = default);

    /// <summary>Complete the consumer, since it's already completed, to clear the dictionary if it matches.</summary>
    /// <param name="context">The context associated with the operation.</param>
    /// <param name="consumer">The consumer.</param>
    /// <param name="cancellationToken">The token used to cancel the operation.</param>
    /// <returns>A task that represents the asynchronous operation.</returns>
    Task CompleteAsync(ConsumeContext<TMessage> context, BatchConsumer<TMessage> consumer, CancellationToken cancellationToken = default);
}

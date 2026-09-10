using System;
using System.Threading.Tasks;

namespace ViciOne.ServiceBus.Batching;

/// <summary>Coordinates message admission, completed-batch removal, and terminal draining.</summary>
/// <typeparam name="TMessage">The message contract collected into each batch.</typeparam>
internal interface IBatchCollector<TMessage> :
    IAsyncDisposable,
    IProbeSite
    where TMessage : class
{
    /// <summary>Adds one message to its current batch.</summary>
    /// <param name="context">The message context to collect.</param>
    /// <param name="cancellationToken">Cancels admission to the serialized collector.</param>
    /// <returns>The batch consumer whose completion governs the message pipeline.</returns>
    Task<BatchConsumer<TMessage>> CollectAsync(ConsumeContext<TMessage> context, CancellationToken cancellationToken = default);

    /// <summary>Removes a completed consumer from the active-batch lookup when it still matches.</summary>
    /// <param name="context">The message context used to resolve the active batch.</param>
    /// <param name="consumer">The completed batch consumer.</param>
    /// <param name="cancellationToken">Cancels admission to the serialized collector.</param>
    /// <returns>A task that completes after the active-batch lookup has been updated.</returns>
    Task CompleteAsync(ConsumeContext<TMessage> context, BatchConsumer<TMessage> consumer, CancellationToken cancellationToken = default);
}

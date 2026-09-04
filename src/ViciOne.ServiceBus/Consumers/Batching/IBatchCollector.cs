using System;
using System.Threading.Tasks;

namespace ViciOne.ServiceBus.Batching;

public interface IBatchCollector<TMessage> :
    IAsyncDisposable,
    IProbeSite
    where TMessage : class
{
    Task<BatchConsumer<TMessage>> CollectAsync(ConsumeContext<TMessage> context, CancellationToken cancellationToken = default);

    /// <summary>
    /// Complete the consumer, since it's already completed, to clear the dictionary if it matches
    /// </summary>
    /// <param name="context"></param>
    /// <param name="consumer"></param>
    /// <param name="cancellationToken">The token used to cancel the operation.</param>
    Task CompleteAsync(ConsumeContext<TMessage> context, BatchConsumer<TMessage> consumer, CancellationToken cancellationToken = default);
}

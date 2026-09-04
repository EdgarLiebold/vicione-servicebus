using System;
using System.Threading.Tasks;

namespace ViciOne.ServiceBus.Saga;

/// <summary>
/// Provides an in memory saga consume context factory implementation.
/// </summary>
/// <typeparam name="TSaga">The t saga type.</typeparam>
public class InMemorySagaConsumeContextFactory<TSaga> :
    ISagaConsumeContextFactory<IndexedSagaDictionary<TSaga>, TSaga>
    where TSaga : class, ISaga
{
    /// <summary>
    /// Creates saga consume context.
    /// </summary>
    /// <typeparam name="T">The t type.</typeparam>
    /// <param name="context">The operation context.</param>
    /// <param name="consumeContext">The consume context value.</param>
    /// <param name="instance">The instance value.</param>
    /// <param name="mode">The mode value.</param>
    /// <returns>The result of the operation.</returns>
    public async Task<SagaConsumeContext<TSaga, T>> CreateSagaConsumeContextAsync<T>(IndexedSagaDictionary<TSaga> context, ConsumeContext<T> consumeContext,
        TSaga instance, SagaConsumeContextMode mode)
        where T : class
    {
        SagaInstance<TSaga> sagaInstance;
        switch (mode)
        {
            case SagaConsumeContextMode.Add:
            case SagaConsumeContextMode.Insert:
                sagaInstance = new SagaInstance<TSaga>(instance);

                await sagaInstance.MarkInUseAsync(consumeContext.CancellationToken).ConfigureAwait(false);

                context.Add(sagaInstance);
                break;

            case SagaConsumeContextMode.Load:
                sagaInstance = context[instance.CorrelationId]
                    ?? throw new InvalidOperationException($"Saga {instance.CorrelationId} was not found in the in-memory repository.");

                await sagaInstance.MarkInUseAsync(consumeContext.CancellationToken).ConfigureAwait(false);

                break;

            default:
                throw new ArgumentOutOfRangeException(nameof(mode));
        }

        return new InMemorySagaConsumeContext<TSaga, T>(consumeContext, sagaInstance);
    }
}

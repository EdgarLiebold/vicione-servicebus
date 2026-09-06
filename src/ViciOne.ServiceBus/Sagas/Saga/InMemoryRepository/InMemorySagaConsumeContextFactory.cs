using System;
using System.Threading.Tasks;

namespace ViciOne.ServiceBus.Saga;

/// <summary>Creates in memory saga consume context instances.</summary>
/// <typeparam name="TSaga">The saga state managed by the member.</typeparam>
public class InMemorySagaConsumeContextFactory<TSaga> :
    ISagaConsumeContextFactory<IndexedSagaDictionary<TSaga>, TSaga>
    where TSaga : class, ISaga
{
    /// <summary>Creates saga consume context.</summary>
    /// <typeparam name="T">The value type.</typeparam>
    /// <param name="context">The context associated with the operation.</param>
    /// <param name="consumeContext">The consume context.</param>
    /// <param name="instance">The instance.</param>
    /// <param name="mode">The mode.</param>
    /// <returns>A task that produces the created value.</returns>
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

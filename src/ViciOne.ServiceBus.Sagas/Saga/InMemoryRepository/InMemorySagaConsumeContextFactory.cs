using System;
using System.Threading.Tasks;

namespace ViciOne.ServiceBus.Saga;

/// <summary>Acquires an in-memory saga lease and transfers it to a message consume context.</summary>
/// <typeparam name="TSaga">The referenced saga state type.</typeparam>
public class InMemorySagaConsumeContextFactory<TSaga> :
    ISagaConsumeContextFactory<IndexedSagaDictionary<TSaga>, TSaga>
    where TSaga : class, ISaga
{
    /// <summary>Acquires the selected saga, registering new state for Add and Insert modes.</summary>
    /// <typeparam name="T">The consumed message type.</typeparam>
    /// <param name="context">The required saga dictionary; Add and Insert require its operation lease to be held by the caller.</param>
    /// <param name="consumeContext">The required message context whose token cancels saga acquisition.</param>
    /// <param name="instance">The required state to register, or whose identifier selects an existing saga.</param>
    /// <param name="mode">Whether to register new state or acquire an existing saga.</param>
    /// <returns>A context owning the acquired saga lease and requiring disposal.</returns>
    public async Task<SagaConsumeContext<TSaga, T>> CreateSagaConsumeContextAsync<T>(IndexedSagaDictionary<TSaga> context, ConsumeContext<T> consumeContext,
        TSaga instance, SagaConsumeContextMode mode)
        where T : class
    {
        ArgumentNullException.ThrowIfNull(context);
        ArgumentNullException.ThrowIfNull(consumeContext);
        ArgumentNullException.ThrowIfNull(instance);

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

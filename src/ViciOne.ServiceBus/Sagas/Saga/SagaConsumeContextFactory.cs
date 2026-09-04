using System.Threading.Tasks;
using ViciOne.ServiceBus.Context;

namespace ViciOne.ServiceBus.Saga;

/// <summary>
/// Provides a saga consume context factory implementation.
/// </summary>
/// <typeparam name="TContext">The t context type.</typeparam>
/// <typeparam name="TSaga">The t saga type.</typeparam>
public class SagaConsumeContextFactory<TContext, TSaga> :
    ISagaConsumeContextFactory<TContext, TSaga>
    where TSaga : class, ISaga
    where TContext : class
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
    public Task<SagaConsumeContext<TSaga, T>> CreateSagaConsumeContextAsync<T>(TContext context, ConsumeContext<T> consumeContext, TSaga instance,
        SagaConsumeContextMode mode)
        where T : class
    {
        return Task.FromResult<SagaConsumeContext<TSaga, T>>(new DefaultSagaConsumeContext<TSaga, T>(consumeContext, instance));
    }
}

using System.Threading.Tasks;
using ViciOne.ServiceBus.Context;

namespace ViciOne.ServiceBus.Saga;

/// <summary>Creates saga consume context instances.</summary>
/// <typeparam name="TContext">The pipeline context carried by the member.</typeparam>
/// <typeparam name="TSaga">The saga state managed by the member.</typeparam>
public class SagaConsumeContextFactory<TContext, TSaga> :
    ISagaConsumeContextFactory<TContext, TSaga>
    where TSaga : class, ISaga
    where TContext : class
{
    /// <summary>Creates saga consume context.</summary>
    /// <typeparam name="T">The value type.</typeparam>
    /// <param name="context">The context associated with the operation.</param>
    /// <param name="consumeContext">The consume context.</param>
    /// <param name="instance">The instance.</param>
    /// <param name="mode">The mode.</param>
    /// <returns>A task that produces the created value.</returns>
    public Task<SagaConsumeContext<TSaga, T>> CreateSagaConsumeContextAsync<T>(TContext context, ConsumeContext<T> consumeContext, TSaga instance,
        SagaConsumeContextMode mode)
        where T : class
    {
        return Task.FromResult<SagaConsumeContext<TSaga, T>>(new DefaultSagaConsumeContext<TSaga, T>(consumeContext, instance));
    }
}

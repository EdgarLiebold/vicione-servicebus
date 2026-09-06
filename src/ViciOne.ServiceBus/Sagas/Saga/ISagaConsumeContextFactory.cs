using System.Threading.Tasks;

namespace ViciOne.ServiceBus.Saga;

/// <summary>Creates the <see cref="SagaConsumeContext{TSaga,T}" /> as needed by the <see cref="QuerySagaRepositoryContext{TSaga}" />.</summary>
/// <typeparam name="TContext">The pipeline context carried by the member.</typeparam>
/// <typeparam name="TSaga">The saga state managed by the member.</typeparam>
public interface ISagaConsumeContextFactory<in TContext, TSaga>
    where TContext : class
    where TSaga : class, ISaga
{
    /// <summary>Create a new <see cref="SagaConsumeContext{TSaga,T}" />.</summary>
    /// <typeparam name="T">The message type.</typeparam>
    /// <param name="context">The <see cref="QuerySagaRepositoryContext{TSaga}" />.</param>
    /// <param name="consumeContext">The message consume context being delivered to the saga.</param>
    /// <param name="instance">The saga instance.</param>
    /// <param name="mode">The creation mode of the saga instance.</param>
    /// <returns>A task that produces the created value.</returns>
    Task<SagaConsumeContext<TSaga, T>> CreateSagaConsumeContextAsync<T>(TContext context, ConsumeContext<T> consumeContext, TSaga instance,
        SagaConsumeContextMode mode)
        where T : class;
}


/// <summary>Creates the <see cref="SagaConsumeContext{TSaga,T}" /> as needed by the <see cref="QuerySagaRepositoryContext{TSaga}" />.</summary>
/// <typeparam name="TSaga">The saga state managed by the member.</typeparam>
public interface ISagaConsumeContextFactory<TSaga>
    where TSaga : class, ISaga
{
    /// <summary>Create a new <see cref="SagaConsumeContext{TSaga,T}" />.</summary>
    /// <typeparam name="T">The message type.</typeparam>
    /// <param name="consumeContext">The message consume context being delivered to the saga.</param>
    /// <param name="instance">The saga instance.</param>
    /// <param name="mode">The creation mode of the saga instance.</param>
    /// <returns>A task that produces the created value.</returns>
    Task<SagaConsumeContext<TSaga, T>> CreateSagaConsumeContextAsync<T>(ConsumeContext<T> consumeContext, TSaga instance, SagaConsumeContextMode mode)
        where T : class;
}

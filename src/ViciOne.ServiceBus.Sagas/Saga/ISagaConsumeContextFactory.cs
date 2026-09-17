using System.Threading.Tasks;

namespace ViciOne.ServiceBus.Saga;

/// <summary>Creates message-specific saga contexts using a repository's storage context.</summary>
/// <typeparam name="TContext">The storage context supplied by the repository.</typeparam>
/// <typeparam name="TSaga">The saga state type.</typeparam>
public interface ISagaConsumeContextFactory<in TContext, TSaga>
    where TContext : class
    where TSaga : class, ISaga
{
    /// <summary>Creates a context combining the consumed message and selected saga state.</summary>
    /// <typeparam name="T">The message type.</typeparam>
    /// <param name="context">The repository-specific storage context, such as a saga dictionary or database context.</param>
    /// <param name="consumeContext">The message consume context being delivered to the saga.</param>
    /// <param name="instance">The state to register or the state identifying a saga to acquire.</param>
    /// <param name="mode">Whether the repository is adding, inserting or loading state.</param>
    /// <returns>A non-null task returning the consume context; ownership and persistence behavior depend on the provider.</returns>
    Task<SagaConsumeContext<TSaga, T>> CreateSagaConsumeContextAsync<T>(TContext context, ConsumeContext<T> consumeContext, TSaga instance,
        SagaConsumeContextMode mode)
        where T : class;
}


/// <summary>Creates message-specific saga contexts using its associated repository storage.</summary>
/// <typeparam name="TSaga">The saga state type.</typeparam>
public interface ISagaConsumeContextFactory<TSaga>
    where TSaga : class, ISaga
{
    /// <summary>Creates a context combining the consumed message and selected saga state.</summary>
    /// <typeparam name="T">The message type.</typeparam>
    /// <param name="consumeContext">The message consume context being delivered to the saga.</param>
    /// <param name="instance">The state to register or the state identifying a saga to acquire.</param>
    /// <param name="mode">Whether the repository is adding, inserting or loading state.</param>
    /// <returns>A non-null task returning the consume context; ownership and persistence behavior depend on the provider.</returns>
    Task<SagaConsumeContext<TSaga, T>> CreateSagaConsumeContextAsync<T>(ConsumeContext<T> consumeContext, TSaga instance, SagaConsumeContextMode mode)
        where T : class;
}

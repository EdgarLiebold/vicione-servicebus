using System;
using System.Threading.Tasks;
using ViciOne.ServiceBus.Context;

namespace ViciOne.ServiceBus.Saga;

/// <summary>Wraps saga state without acquiring leases or performing repository writes.</summary>
/// <typeparam name="TContext">The repository operation's storage context type.</typeparam>
/// <typeparam name="TSaga">The referenced saga state type.</typeparam>
public class SagaConsumeContextFactory<TContext, TSaga> :
    ISagaConsumeContextFactory<TContext, TSaga>
    where TSaga : class, ISaga
    where TContext : class
{
    /// <summary>Validates the operation inputs and wraps the exact supplied message and saga state.</summary>
    /// <typeparam name="T">The consumed message type.</typeparam>
    /// <param name="context">The required storage context; persistence remains owned by its repository.</param>
    /// <param name="consumeContext">The required consumed-message context.</param>
    /// <param name="instance">The required saga state.</param>
    /// <param name="mode">A defined creation mode; this wrapper performs no mode-specific persistence.</param>
    /// <returns>A task returning a new context that retains the supplied saga state.</returns>
    public Task<SagaConsumeContext<TSaga, T>> CreateSagaConsumeContextAsync<T>(TContext context, ConsumeContext<T> consumeContext, TSaga instance,
        SagaConsumeContextMode mode)
        where T : class
    {
        ArgumentNullException.ThrowIfNull(context);
        ArgumentNullException.ThrowIfNull(consumeContext);
        ArgumentNullException.ThrowIfNull(instance);
        if (mode is not (SagaConsumeContextMode.Load or SagaConsumeContextMode.Add or SagaConsumeContextMode.Insert))
            throw new ArgumentOutOfRangeException(nameof(mode));

        return Task.FromResult<SagaConsumeContext<TSaga, T>>(new DefaultSagaConsumeContext<TSaga, T>(consumeContext, instance));
    }
}

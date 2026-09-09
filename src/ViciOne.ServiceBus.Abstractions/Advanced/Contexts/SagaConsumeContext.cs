namespace ViciOne.ServiceBus.Advanced;

/// <summary>Provides a saga instance together with the typed message it is consuming.</summary>
/// <typeparam name="TSaga">The saga type.</typeparam>
/// <typeparam name="TMessage">The message type.</typeparam>
public interface SagaConsumeContext<out TSaga, out TMessage> :
    SagaConsumeContext<TSaga>,
    ConsumeContext<TMessage>
    where TSaga : class
    where TMessage : class
{
}


/// <summary>Provides a saga instance and message-independent consume operations.</summary>
/// <typeparam name="TSaga">The saga type.</typeparam>
public interface SagaConsumeContext<out TSaga> :
    ConsumeContext
    where TSaga : class
{
    /// <summary>Gets the saga instance participating in the current consume operation.</summary>
    TSaga Saga { get; }

    /// <summary>Gets whether the saga completed and may be removed from its repository.</summary>
    bool IsCompleted { get; }

    /// <summary>
    /// Marks the saga as completed so that its repository can remove or archive it.
    /// </summary>
    /// <param name="cancellationToken">The cancellation token.</param>
    /// <returns>A task that represents saga completion.</returns>
    Task SetCompletedAsync(CancellationToken cancellationToken = default);
}

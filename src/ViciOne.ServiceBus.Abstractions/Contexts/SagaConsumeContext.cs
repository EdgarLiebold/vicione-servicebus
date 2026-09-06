using System.Threading.Tasks;

namespace ViciOne.ServiceBus.Advanced;

/// <summary>Consume context including the saga instance consuming the message.</summary>
/// <typeparam name="TSaga">The saga type.</typeparam>
/// <typeparam name="TMessage">The message type.</typeparam>
public interface SagaConsumeContext<out TSaga, out TMessage> :
    SagaConsumeContext<TSaga>,
    ConsumeContext<TMessage>
    where TSaga : class
    where TMessage : class
{
}


/// <summary>
/// Consume context including the saga instance consuming the message. Note
/// this does not expose the message type, for filters that do not care about message type.
/// </summary>
/// <typeparam name="TSaga">The saga type.</typeparam>
public interface SagaConsumeContext<out TSaga> :
    ConsumeContext
    where TSaga : class
{
    /// <summary>The saga instance for the current consume operation.</summary>
    TSaga Saga { get; }

    /// <summary>True if the saga has been completed, signaling that the repository may remove it.</summary>
    bool IsCompleted { get; }

    /// <summary>
    /// Mark the saga instance as completed, which may remove it from the repository or archive it, etc.
    /// Once completed, a saga instance should never again be visible, even if the same CorrelationId is
    /// specified.
    /// </summary>
    /// <param name="cancellationToken">The token used to cancel the operation.</param>
    /// <returns>A task that represents the asynchronous operation.</returns>
    Task SetCompletedAsync(CancellationToken cancellationToken = default);
}

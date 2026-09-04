using System;
using System.Threading.Tasks;

namespace ViciOne.ServiceBus.Configuration;

/// <summary>
/// Provides an event missing instance configurator implementation.
/// </summary>
/// <typeparam name="TSaga">The t saga type.</typeparam>
/// <typeparam name="TMessage">The t message type.</typeparam>
public class EventMissingInstanceConfigurator<TSaga, TMessage> :
    IMissingInstanceConfigurator<TSaga, TMessage>
    where TSaga : SagaStateMachineInstance
    where TMessage : class
{
    /// <summary>
    /// Performs the discard operation.
    /// </summary>
    /// <returns>The result of the operation.</returns>
    public IPipe<ConsumeContext<TMessage>> Discard()
    {
        return Pipe.Empty<ConsumeContext<TMessage>>();
    }

    /// <summary>
    /// Performs the fault operation.
    /// </summary>
    /// <returns>The result of the operation.</returns>
    public IPipe<ConsumeContext<TMessage>> Fault()
    {
        return Execute(context =>
            throw new SagaException("An existing saga instance was not found", typeof(TSaga), typeof(TMessage), context.CorrelationId ?? Guid.Empty));
    }

    /// <summary>
    /// Performs the execute operation.
    /// </summary>
    /// <param name="callback">The callback value.</param>
    /// <returns>The result of the operation.</returns>
    public IPipe<ConsumeContext<TMessage>> ExecuteAsync(Func<ConsumeContext<TMessage>, Task> callback)
    {
        return callback.ToPipe();
    }

    /// <summary>
    /// Performs the execute operation.
    /// </summary>
    /// <param name="callback">The callback value.</param>
    /// <returns>The result of the operation.</returns>
    public IPipe<ConsumeContext<TMessage>> Execute(Action<ConsumeContext<TMessage>> callback)
    {
        return Pipe.Execute(callback);
    }
}

using System;
using System.Threading.Tasks;

namespace ViciOne.ServiceBus.Configuration;

/// <summary>Configures event missing instance.</summary>
/// <typeparam name="TSaga">The saga state managed by the member.</typeparam>
/// <typeparam name="TMessage">The message contract processed by the member.</typeparam>
public class EventMissingInstanceConfigurator<TSaga, TMessage> :
    IMissingInstanceConfigurator<TSaga, TMessage>
    where TSaga : SagaStateMachineInstance
    where TMessage : class
{
    /// <summary>Discards the current value.</summary>
    /// <returns>The pipe produced by the operation.</returns>
    public IPipe<ConsumeContext<TMessage>> Discard()
    {
        return Pipe.Empty<ConsumeContext<TMessage>>();
    }

    /// <summary>Creates or reports a fault.</summary>
    /// <returns>The pipe produced by the operation.</returns>
    public IPipe<ConsumeContext<TMessage>> Fault()
    {
        return Execute(context =>
            throw new SagaException("An existing saga instance was not found", typeof(TSaga), typeof(TMessage), context.CorrelationId ?? Guid.Empty));
    }

    /// <summary>Runs the configured action.</summary>
    /// <param name="callback">The callback invoked by the operation.</param>
    /// <returns>The pipe produced by the operation.</returns>
    public IPipe<ConsumeContext<TMessage>> ExecuteAwaited(Func<ConsumeContext<TMessage>, Task> callback)
    {
        return callback.ToPipe();
    }

    /// <summary>Runs the configured action.</summary>
    /// <param name="callback">The callback invoked by the operation.</param>
    /// <returns>The pipe produced by the operation.</returns>
    public IPipe<ConsumeContext<TMessage>> Execute(Action<ConsumeContext<TMessage>> callback)
    {
        return Pipe.Execute(callback);
    }
}

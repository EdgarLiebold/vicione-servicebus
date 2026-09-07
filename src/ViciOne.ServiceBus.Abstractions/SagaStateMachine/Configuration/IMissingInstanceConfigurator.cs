using System;
using System.Threading.Tasks;

namespace ViciOne.ServiceBus.Sagas;

/// <summary>Configures missing instance.</summary>
/// <typeparam name="TSaga">The saga state managed by the member.</typeparam>
/// <typeparam name="TMessage">The message contract processed by the member.</typeparam>
public interface IMissingInstanceConfigurator<TSaga, TMessage>
    where TSaga : SagaStateMachineInstance
    where TMessage : class
{
    /// <summary>Discard the event, silently ignoring the missing instance for the event.</summary>
    /// <returns>The pipe produced by the operation.</returns>
    IPipe<ConsumeContext<TMessage>> Discard();

    /// <summary>Fault the saga consumer, which moves the message to the error queue.</summary>
    /// <returns>The pipe produced by the operation.</returns>
    IPipe<ConsumeContext<TMessage>> Fault();

    /// <summary>Execute an asynchronous method when the instance is missed, allowing a custom behavior to be specified.</summary>
    /// <param name="callback">The callback invoked by the operation.</param>
    /// <returns>The pipe produced by the operation.</returns>
    IPipe<ConsumeContext<TMessage>> ExecuteAwaited(Func<ConsumeContext<TMessage>, Task> callback);

    /// <summary>Execute a method when the instance is missed, allowing a custom behavior to be specified.</summary>
    /// <param name="callback">The callback invoked by the operation.</param>
    /// <returns>The pipe produced by the operation.</returns>
    IPipe<ConsumeContext<TMessage>> Execute(Action<ConsumeContext<TMessage>> callback);
}

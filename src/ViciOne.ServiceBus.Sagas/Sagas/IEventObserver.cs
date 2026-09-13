using System;
using System.Threading.Tasks;

namespace ViciOne.ServiceBus.Sagas;

/// <summary>Receives notifications about event events.</summary>
/// <typeparam name="TSaga">The saga state managed by the member.</typeparam>
public interface IEventObserver<TSaga>
    where TSaga : class, ISagaStateMachineInstance
{
    /// <summary>Called before the event context is delivered to the activities.</summary>
    /// <param name="context">The event context.</param>
    /// <returns>A task that represents the asynchronous operation.</returns>
    Task PreExecuteAsync(IBehaviorContext<TSaga> context);

    /// <summary>Called before the event context is delivered to the activities.</summary>
    /// <typeparam name="T">The event data type.</typeparam>
    /// <param name="context">The event context.</param>
    /// <returns>A task that represents the asynchronous operation.</returns>
    Task PreExecuteAsync<T>(IBehaviorContext<TSaga, T> context)
        where T : class;

    /// <summary>Called when the event has been processed by the activities.</summary>
    /// <param name="context">The event context.</param>
    /// <returns>A task that represents the asynchronous operation.</returns>
    Task PostExecuteAsync(IBehaviorContext<TSaga> context);

    /// <summary>Called when the event has been processed by the activities.</summary>
    /// <typeparam name="T">The event data type.</typeparam>
    /// <param name="context">The event context.</param>
    /// <returns>A task that represents the asynchronous operation.</returns>
    Task PostExecuteAsync<T>(IBehaviorContext<TSaga, T> context)
        where T : class;

    /// <summary>Called when the activity execution faults and is not handled by the activities.</summary>
    /// <param name="context">The event context.</param>
    /// <param name="exception">The exception that was thrown.</param>
    /// <returns>A task that represents the asynchronous operation.</returns>
    Task ExecuteFaultAsync(IBehaviorContext<TSaga> context, Exception exception);

    /// <summary>Called when the activity execution faults and is not handled by the activities.</summary>
    /// <typeparam name="T">The message type.</typeparam>
    /// <param name="context">The event context.</param>
    /// <param name="exception">The exception that was thrown.</param>
    /// <returns>A task that represents the asynchronous operation.</returns>
    Task ExecuteFaultAsync<T>(IBehaviorContext<TSaga, T> context, Exception exception)
        where T : class;
}

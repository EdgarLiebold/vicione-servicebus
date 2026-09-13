using System;

namespace ViciOne.ServiceBus.Sagas;

/// <summary>Provides scheduling metadata stored by a saga state machine.</summary>
/// <typeparam name="TSaga">The saga state type.</typeparam>
public interface ISchedule<TSaga>
    where TSaga : class, ISagaStateMachineInstance
{
    /// <summary>Gets the schedule declaration name.</summary>
    string Name { get; }

    /// <summary>Calculates the delivery delay for the current saga context.</summary>
    /// <param name="context">The saga and event context used to calculate the delay.</param>
    /// <returns>The interval before message delivery.</returns>
    TimeSpan GetDelay(IBehaviorContext<TSaga> context);

    /// <summary>Reads the scheduler token associated with a saga instance.</summary>
    /// <param name="instance">The saga instance to inspect.</param>
    /// <returns>The scheduler token, or <see langword="null" /> when no message is scheduled.</returns>
    Guid? GetTokenId(TSaga instance);

    /// <summary>Writes or clears the scheduler token on a saga instance.</summary>
    /// <param name="instance">The saga instance to update.</param>
    /// <param name="tokenId">The scheduler token, or <see langword="null" /> to clear it.</param>
    void SetTokenId(TSaga instance, Guid? tokenId);
}


/// <summary>Describes the events raised when a scheduled message reaches a saga state machine.</summary>
/// <typeparam name="TSaga">The saga state type.</typeparam>
/// <typeparam name="TMessage">The scheduled message type.</typeparam>
public interface ISchedule<TSaga, TMessage> :
    ISchedule<TSaga>
    where TSaga : class, ISagaStateMachineInstance
    where TMessage : class
{
    /// <summary>
    /// Gets or sets the event raised only for the currently active scheduler token.
    /// </summary>
    IEvent<TMessage> Received { get; set; }

    /// <summary>
    /// Gets or sets the event raised for every matching scheduled message, including superseded deliveries.
    /// </summary>
    IEvent<TMessage> AnyReceived { get; set; }
}

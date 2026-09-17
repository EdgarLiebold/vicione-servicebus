using System;

namespace ViciOne.ServiceBus.Sagas;

/// <summary>Selects an absolute scheduling time from a behavior context.</summary>
/// <typeparam name="TSaga">The saga state managed by the member.</typeparam>
/// <param name="context">The context associated with the operation.</param>
/// <returns>The absolute time at which the message is scheduled.</returns>
public delegate DateTimeOffset ScheduleTimeProvider<TSaga>(IBehaviorContext<TSaga> context)
    where TSaga : class, ISagaStateMachineInstance;


/// <summary>Selects an absolute scheduling time from a message-specific behavior context.</summary>
/// <typeparam name="TSaga">The saga state managed by the member.</typeparam>
/// <typeparam name="TMessage">The message contract processed by the member.</typeparam>
/// <param name="context">The context associated with the operation.</param>
/// <returns>The absolute time at which the message is scheduled.</returns>
public delegate DateTimeOffset ScheduleTimeProvider<TSaga, in TMessage>(IBehaviorContext<TSaga, TMessage> context)
    where TMessage : class
    where TSaga : class, ISagaStateMachineInstance;

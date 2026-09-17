using System;

namespace ViciOne.ServiceBus.Sagas;

/// <summary>Selects a scheduling delay from a behavior context.</summary>
/// <typeparam name="TSaga">The saga state managed by the member.</typeparam>
/// <param name="context">The context associated with the operation.</param>
/// <returns>The delay before the message is scheduled.</returns>
public delegate TimeSpan ScheduleDelayProvider<TSaga>(IBehaviorContext<TSaga> context)
    where TSaga : class, ISagaStateMachineInstance;


/// <summary>Selects a scheduling delay from a message-specific behavior context.</summary>
/// <typeparam name="TSaga">The saga state managed by the member.</typeparam>
/// <typeparam name="TMessage">The message contract processed by the member.</typeparam>
/// <param name="context">The context associated with the operation.</param>
/// <returns>The delay before the message is scheduled.</returns>
public delegate TimeSpan ScheduleDelayProvider<TSaga, in TMessage>(IBehaviorContext<TSaga, TMessage> context)
    where TMessage : class
    where TSaga : class, ISagaStateMachineInstance;

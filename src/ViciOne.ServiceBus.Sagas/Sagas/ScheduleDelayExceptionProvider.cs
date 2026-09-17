using System;

namespace ViciOne.ServiceBus.Sagas;

/// <summary>Selects a scheduling delay from an exception behavior context.</summary>
/// <typeparam name="TSaga">The saga state managed by the member.</typeparam>
/// <typeparam name="TException">The exception handled by the member.</typeparam>
/// <param name="context">The context associated with the operation.</param>
/// <returns>The delay before the message is scheduled.</returns>
public delegate TimeSpan ScheduleDelayExceptionProvider<TSaga, in TException>(IBehaviorExceptionContext<TSaga, TException> context)
    where TSaga : class, ISagaStateMachineInstance
    where TException : Exception;


/// <summary>Selects a scheduling delay from a message-specific exception behavior context.</summary>
/// <typeparam name="TSaga">The saga state managed by the member.</typeparam>
/// <typeparam name="TMessage">The message contract processed by the member.</typeparam>
/// <typeparam name="TException">The exception handled by the member.</typeparam>
/// <param name="context">The context associated with the operation.</param>
/// <returns>The delay before the message is scheduled.</returns>
public delegate TimeSpan ScheduleDelayExceptionProvider<TSaga, in TMessage, in TException>(IBehaviorExceptionContext<TSaga, TMessage, TException> context)
    where TMessage : class
    where TSaga : class, ISagaStateMachineInstance
    where TException : Exception;

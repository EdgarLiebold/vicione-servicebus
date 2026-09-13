using System;

namespace ViciOne.ServiceBus.Sagas;

/// <summary>Represents the method that handles schedule time exception provider.</summary>
/// <typeparam name="TSaga">The saga state managed by the member.</typeparam>
/// <typeparam name="TException">The exception handled by the member.</typeparam>
/// <param name="context">The context associated with the operation.</param>
/// <returns>The value produced by the operation.</returns>
public delegate DateTimeOffset ScheduleTimeExceptionProvider<TSaga, in TException>(IBehaviorExceptionContext<TSaga, TException> context)
    where TSaga : class, ISagaStateMachineInstance
    where TException : Exception;


/// <summary>Represents the method that handles schedule time exception provider.</summary>
/// <typeparam name="TSaga">The saga state managed by the member.</typeparam>
/// <typeparam name="TMessage">The message contract processed by the member.</typeparam>
/// <typeparam name="TException">The exception handled by the member.</typeparam>
/// <param name="context">The context associated with the operation.</param>
/// <returns>The value produced by the operation.</returns>
public delegate DateTimeOffset ScheduleTimeExceptionProvider<TSaga, in TMessage, in TException>(IBehaviorExceptionContext<TSaga, TMessage, TException> context)
    where TSaga : class, ISagaStateMachineInstance
    where TMessage : class
    where TException : Exception;

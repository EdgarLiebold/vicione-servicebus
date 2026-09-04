using System;

namespace ViciOne.ServiceBus.Sagas;

/// <summary>
/// Represents the method that handles schedule delay exception provider.
/// </summary>
/// <typeparam name="TSaga">The t saga type.</typeparam>
/// <typeparam name="TException">The t exception type.</typeparam>
/// <param name="context">The operation context.</param>
/// <returns>The result of the operation.</returns>
public delegate TimeSpan ScheduleDelayExceptionProvider<TSaga, in TException>(BehaviorExceptionContext<TSaga, TException> context)
    where TSaga : class, SagaStateMachineInstance
    where TException : Exception;


/// <summary>
/// Represents the method that handles schedule delay exception provider.
/// </summary>
/// <typeparam name="TSaga">The t saga type.</typeparam>
/// <typeparam name="TMessage">The t message type.</typeparam>
/// <typeparam name="TException">The t exception type.</typeparam>
/// <param name="context">The operation context.</param>
/// <returns>The result of the operation.</returns>
public delegate TimeSpan ScheduleDelayExceptionProvider<TSaga, in TMessage, in TException>(BehaviorExceptionContext<TSaga, TMessage, TException> context)
    where TMessage : class
    where TSaga : class, SagaStateMachineInstance
    where TException : Exception;

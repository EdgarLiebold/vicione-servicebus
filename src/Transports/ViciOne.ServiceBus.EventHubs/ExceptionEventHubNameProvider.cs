using System;

namespace ViciOne.ServiceBus.EventHubs;

/// <summary>Selects the destination Event Hub while handling a data-bearing behavior exception.</summary>
/// <typeparam name="TInstance">The saga instance type.</typeparam>
/// <typeparam name="TData">The behavior data type.</typeparam>
/// <typeparam name="TException">The handled exception type.</typeparam>
/// <param name="context">The current exception behavior context.</param>
/// <returns>The destination Event Hub entity name.</returns>
public delegate string ExceptionEventHubNameProvider<TInstance, in TData, in TException>(IBehaviorExceptionContext<TInstance, TData, TException> context)
    where TException : Exception
    where TData : class
    where TInstance : class, ISagaStateMachineInstance;


/// <summary>Selects the destination Event Hub while handling a behavior exception.</summary>
/// <typeparam name="TInstance">The saga instance type.</typeparam>
/// <typeparam name="TException">The handled exception type.</typeparam>
/// <param name="context">The current exception behavior context.</param>
/// <returns>The destination Event Hub entity name.</returns>
public delegate string ExceptionEventHubNameProvider<TInstance, in TException>(IBehaviorExceptionContext<TInstance, TException> context)
    where TException : Exception
    where TInstance : class, ISagaStateMachineInstance;

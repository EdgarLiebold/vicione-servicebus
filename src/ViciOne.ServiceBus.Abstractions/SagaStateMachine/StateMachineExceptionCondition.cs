using System;

namespace ViciOne.ServiceBus.Sagas;

/// <summary>Filters activities based on the conditional statement.</summary>
/// <typeparam name="TSaga">The saga state managed by the member.</typeparam>
/// <typeparam name="TException">The exception handled by the member.</typeparam>
/// <param name="context">The context associated with the operation.</param>
/// <returns><see langword="true" /> when the condition is satisfied; otherwise, <see langword="false" />.</returns>
public delegate bool StateMachineExceptionCondition<TSaga, in TException>(BehaviorExceptionContext<TSaga, TException> context)
    where TException : Exception
    where TSaga : class, SagaStateMachineInstance;


/// <summary>Filters activities based on the conditional statement.</summary>
/// <typeparam name="TSaga">The saga state managed by the member.</typeparam>
/// <typeparam name="TMessage">The message contract processed by the member.</typeparam>
/// <typeparam name="TException">The exception handled by the member.</typeparam>
/// <param name="context">The context associated with the operation.</param>
/// <returns><see langword="true" /> when the condition is satisfied; otherwise, <see langword="false" />.</returns>
public delegate bool StateMachineExceptionCondition<TSaga, in TMessage, in TException>(BehaviorExceptionContext<TSaga, TMessage, TException> context)
    where TException : Exception
    where TSaga : class, SagaStateMachineInstance
    where TMessage : class;

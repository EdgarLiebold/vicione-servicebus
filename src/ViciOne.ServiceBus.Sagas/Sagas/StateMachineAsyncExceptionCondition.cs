using System;
using System.Threading.Tasks;

namespace ViciOne.ServiceBus.Sagas;

/// <summary>Evaluates an exception behavior condition asynchronously.</summary>
/// <typeparam name="TSaga">The saga state-machine instance type.</typeparam>
/// <typeparam name="TException">The exception type available to the condition.</typeparam>
/// <param name="context">The exception behavior context evaluated by the condition.</param>
/// <returns>A task whose result indicates whether the exception behavior condition is satisfied.</returns>
public delegate Task<bool> StateMachineAsyncExceptionCondition<TSaga, in TException>(BehaviorExceptionContext<TSaga, TException> context)
    where TException : Exception
    where TSaga : class, SagaStateMachineInstance;


/// <summary>Evaluates a message-specific exception behavior condition asynchronously.</summary>
/// <typeparam name="TSaga">The saga state-machine instance type.</typeparam>
/// <typeparam name="TMessage">The message contract available to the condition.</typeparam>
/// <typeparam name="TException">The exception type available to the condition.</typeparam>
/// <param name="context">The exception behavior context evaluated by the condition.</param>
/// <returns>A task whose result indicates whether the exception behavior condition is satisfied.</returns>
public delegate Task<bool> StateMachineAsyncExceptionCondition<TSaga, in TMessage, in TException>(
    BehaviorExceptionContext<TSaga, TMessage, TException> context)
    where TException : Exception
    where TSaga : class, SagaStateMachineInstance
    where TMessage : class;

using System;

namespace ViciOne.ServiceBus.Sagas;

/// <summary>
/// Represents the method that handles schedule time provider.
/// </summary>
/// <typeparam name="TSaga">The t saga type.</typeparam>
/// <param name="context">The operation context.</param>
/// <returns>The result of the operation.</returns>
public delegate DateTimeOffset ScheduleTimeProvider<TSaga>(BehaviorContext<TSaga> context)
    where TSaga : class, SagaStateMachineInstance;


/// <summary>
/// Represents the method that handles schedule time provider.
/// </summary>
/// <typeparam name="TSaga">The t saga type.</typeparam>
/// <typeparam name="TMessage">The t message type.</typeparam>
/// <param name="context">The operation context.</param>
/// <returns>The result of the operation.</returns>
public delegate DateTimeOffset ScheduleTimeProvider<TSaga, in TMessage>(BehaviorContext<TSaga, TMessage> context)
    where TMessage : class
    where TSaga : class, SagaStateMachineInstance;

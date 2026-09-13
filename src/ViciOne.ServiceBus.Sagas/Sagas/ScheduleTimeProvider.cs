using System;

namespace ViciOne.ServiceBus.Sagas;

/// <summary>Represents the method that handles schedule time provider.</summary>
/// <typeparam name="TSaga">The saga state managed by the member.</typeparam>
/// <param name="context">The context associated with the operation.</param>
/// <returns>The value produced by the operation.</returns>
public delegate DateTimeOffset ScheduleTimeProvider<TSaga>(IBehaviorContext<TSaga> context)
    where TSaga : class, ISagaStateMachineInstance;


/// <summary>Represents the method that handles schedule time provider.</summary>
/// <typeparam name="TSaga">The saga state managed by the member.</typeparam>
/// <typeparam name="TMessage">The message contract processed by the member.</typeparam>
/// <param name="context">The context associated with the operation.</param>
/// <returns>The value produced by the operation.</returns>
public delegate DateTimeOffset ScheduleTimeProvider<TSaga, in TMessage>(IBehaviorContext<TSaga, TMessage> context)
    where TMessage : class
    where TSaga : class, ISagaStateMachineInstance;

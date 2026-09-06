using System;

namespace ViciOne.ServiceBus.Sagas;

/// <summary>Represents the method that handles schedule delay provider.</summary>
/// <typeparam name="TSaga">The saga state managed by the member.</typeparam>
/// <param name="context">The context associated with the operation.</param>
/// <returns>The value produced by the operation.</returns>
public delegate TimeSpan ScheduleDelayProvider<TSaga>(BehaviorContext<TSaga> context)
    where TSaga : class, SagaStateMachineInstance;


/// <summary>Represents the method that handles schedule delay provider.</summary>
/// <typeparam name="TSaga">The saga state managed by the member.</typeparam>
/// <typeparam name="TMessage">The message contract processed by the member.</typeparam>
/// <param name="context">The context associated with the operation.</param>
/// <returns>The value produced by the operation.</returns>
public delegate TimeSpan ScheduleDelayProvider<TSaga, in TMessage>(BehaviorContext<TSaga, TMessage> context)
    where TMessage : class
    where TSaga : class, SagaStateMachineInstance;

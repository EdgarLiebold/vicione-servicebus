using System;

namespace ViciOne.ServiceBus.Sagas;

/// <summary>Returns the address for the message provided.</summary>
/// <typeparam name="TSaga">The saga instance.</typeparam>
/// <typeparam name="TMessage">The message data.</typeparam>
/// <param name="context">The context associated with the operation.</param>
/// <returns>The value produced by the operation.</returns>
public delegate Uri DestinationAddressProvider<TSaga, in TMessage>(BehaviorContext<TSaga, TMessage> context)
    where TSaga : class, SagaStateMachineInstance
    where TMessage : class;


/// <summary>Returns the address for the message provided.</summary>
/// <typeparam name="TSaga">The saga instance.</typeparam>
/// <param name="context">The context associated with the operation.</param>
/// <returns>The value produced by the operation.</returns>
public delegate Uri DestinationAddressProvider<TSaga>(BehaviorContext<TSaga> context)
    where TSaga : class, SagaStateMachineInstance;

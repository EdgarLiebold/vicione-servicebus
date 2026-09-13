using System;

namespace ViciOne.ServiceBus.Sagas;

/// <summary>Returns the address for the message provided.</summary>
/// <typeparam name="TSaga">The saga instance.</typeparam>
/// <typeparam name="TMessage">The message data.</typeparam>
/// <param name="context">The context associated with the operation.</param>
/// <returns>The value produced by the operation.</returns>
public delegate Uri DestinationAddressProvider<TSaga, in TMessage>(IBehaviorContext<TSaga, TMessage> context)
    where TSaga : class, ISagaStateMachineInstance
    where TMessage : class;


/// <summary>Returns the address for the message provided.</summary>
/// <typeparam name="TSaga">The saga instance.</typeparam>
/// <param name="context">The context associated with the operation.</param>
/// <returns>The value produced by the operation.</returns>
public delegate Uri DestinationAddressProvider<TSaga>(IBehaviorContext<TSaga> context)
    where TSaga : class, ISagaStateMachineInstance;

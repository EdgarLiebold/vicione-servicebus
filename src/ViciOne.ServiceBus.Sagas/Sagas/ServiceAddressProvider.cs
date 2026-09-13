using System;

namespace ViciOne.ServiceBus.Sagas;

/// <summary>Provides an address for the request service.</summary>
/// <typeparam name="TSaga">The saga state managed by the member.</typeparam>
/// <param name="context">The context associated with the operation.</param>
/// <returns>The value produced by the operation.</returns>
public delegate Uri ServiceAddressProvider<TSaga>(IBehaviorContext<TSaga> context)
    where TSaga : class, ISagaStateMachineInstance;


/// <summary>Provides an address for the request service.</summary>
/// <typeparam name="TSaga">The saga state managed by the member.</typeparam>
/// <typeparam name="TMessage">The message contract processed by the member.</typeparam>
/// <param name="context">The context associated with the operation.</param>
/// <returns>The value produced by the operation.</returns>
public delegate Uri ServiceAddressProvider<TSaga, in TMessage>(IBehaviorContext<TSaga, TMessage> context)
    where TSaga : class, ISagaStateMachineInstance
    where TMessage : class;

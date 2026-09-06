using System;

namespace ViciOne.ServiceBus.Futures;

/// <summary>Represents the method that handles request address provider.</summary>
/// <typeparam name="TMessage">The message contract processed by the member.</typeparam>
/// <param name="context">The context associated with the operation.</param>
/// <returns>The value produced by the operation.</returns>
public delegate Uri? RequestAddressProvider<in TMessage>(BehaviorContext<FutureState, TMessage> context)
    where TMessage : class;

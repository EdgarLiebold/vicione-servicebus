using System;

namespace ViciOne.ServiceBus.Futures;

/// <summary>
/// Represents the method that handles request address provider.
/// </summary>
/// <typeparam name="TMessage">The t message type.</typeparam>
/// <param name="context">The operation context.</param>
/// <returns>The result of the operation.</returns>
public delegate Uri? RequestAddressProvider<in TMessage>(BehaviorContext<FutureState, TMessage> context)
    where TMessage : class;

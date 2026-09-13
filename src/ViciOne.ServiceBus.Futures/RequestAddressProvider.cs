using System;

namespace ViciOne.ServiceBus.Futures;

/// <summary>Selects the destination address for a request issued by a future.</summary>
/// <typeparam name="TMessage">The future event contract used to choose the destination.</typeparam>
/// <param name="context">The future event context from which the destination is selected.</param>
/// <returns>The request address, or <see langword="null" /> to publish the request.</returns>
public delegate Uri? RequestAddressProvider<in TMessage>(IBehaviorContext<FutureState, TMessage> context)
    where TMessage : class;

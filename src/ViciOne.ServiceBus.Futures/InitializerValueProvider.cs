namespace ViciOne.ServiceBus.Futures;

/// <summary>Provides property values used to initialize a message from a typed future event.</summary>
/// <typeparam name="TMessage">The future event contract available to the initializer.</typeparam>
/// <param name="context">The future event context from which initializer values are selected.</param>
/// <returns>An object whose readable properties contribute initializer values.</returns>
public delegate object InitializerValueProvider<in TMessage>(BehaviorContext<FutureState, TMessage> context)
    where TMessage : class;


/// <summary>Provides property values used to initialize a message from future state.</summary>
/// <param name="context">The future state context from which initializer values are selected.</param>
/// <returns>An object whose readable properties contribute initializer values.</returns>
public delegate object InitializerValueProvider(BehaviorContext<FutureState> context);

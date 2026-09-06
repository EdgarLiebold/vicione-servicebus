namespace ViciOne.ServiceBus.Futures;

/// <summary>Given the event context and request, returns an object used to complete the initialization of the object type.</summary>
/// <typeparam name="TMessage">The data type included in the context.</typeparam>
/// <param name="context">The context associated with the operation.</param>
/// <returns>The value produced by the operation.</returns>
public delegate object InitializerValueProvider<in TMessage>(BehaviorContext<FutureState, TMessage> context)
    where TMessage : class;


/// <summary>Given the event context and request, returns an object used to complete the initialization of the object type.</summary>
/// <param name="context">The context associated with the operation.</param>
/// <returns>The value produced by the operation.</returns>
public delegate object InitializerValueProvider(BehaviorContext<FutureState> context);

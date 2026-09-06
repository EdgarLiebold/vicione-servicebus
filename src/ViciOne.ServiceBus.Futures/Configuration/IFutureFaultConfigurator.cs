namespace ViciOne.ServiceBus.Configuration;

/// <summary>Configures how an input message produces a future's terminal fault.</summary>
/// <typeparam name="TFault">The future fault contract.</typeparam>
/// <typeparam name="TInput">The input message contract.</typeparam>
public interface IFutureFaultConfigurator<TFault, out TInput>
    where TInput : class
    where TFault : class
{
    /// <summary>Sets the synchronous factory that creates the future fault.</summary>
    /// <param name="factoryMethod">The factory that creates the fault from the input context.</param>
    void SetFaultFactory(EventMessageFactory<FutureState, TInput, TFault> factoryMethod);

    /// <summary>Sets the asynchronous factory that creates the future fault.</summary>
    /// <param name="factoryMethod">The asynchronous factory that creates the fault from the input context.</param>
    void SetFaultFactory(AsyncEventMessageFactory<FutureState, TInput, TFault> factoryMethod);

    /// <summary>Sets the initializer values used to create the future fault.</summary>
    /// <param name="valueProvider">The provider of additional fault property values.</param>
    void SetFaultInitializer(InitializerValueProvider<TInput> valueProvider);
}


/// <summary>Configures how future state produces the terminal fault of a future.</summary>
/// <typeparam name="TFault">The future fault contract.</typeparam>
public interface IFutureFaultConfigurator<TFault>
    where TFault : class
{
    /// <summary>Sets the synchronous factory that creates the future fault.</summary>
    /// <param name="factoryMethod">The factory that creates the fault from future state.</param>
    void SetFaultFactory(EventMessageFactory<FutureState, TFault> factoryMethod);

    /// <summary>Sets the asynchronous factory that creates the future fault.</summary>
    /// <param name="factoryMethod">The asynchronous factory that creates the fault from future state.</param>
    void SetFaultFactory(AsyncEventMessageFactory<FutureState, TFault> factoryMethod);

    /// <summary>Sets the initializer values used to create the future fault.</summary>
    /// <param name="valueProvider">The provider of additional fault property values.</param>
    void SetFaultInitializer(InitializerValueProvider valueProvider);
}

namespace ViciOne.ServiceBus.Configuration;

/// <summary>Configures how an input message produces the terminal fault of a future.</summary>
/// <typeparam name="TFault">The future fault contract.</typeparam>
/// <typeparam name="TInput">The input message contract.</typeparam>
public interface IFutureFaultConfigurator<TFault, out TInput>
    where TInput : class
    where TFault : class
{
    /// <summary>Creates the future fault with a synchronous message factory.</summary>
    /// <param name="factoryMethod">The factory that creates the fault from the input context.</param>
    void SetFaultedUsingFactory(EventMessageFactory<FutureState, TInput, TFault> factoryMethod);

    /// <summary>Creates the future fault with an asynchronous message factory.</summary>
    /// <param name="factoryMethod">The asynchronous factory that creates the fault from the input context.</param>
    void SetFaultedUsingFactory(AsyncEventMessageFactory<FutureState, TInput, TFault> factoryMethod);

    /// <summary>Initializes the future fault from the initiating command and additional property values.</summary>
    /// <param name="valueProvider">The provider of additional fault property values.</param>
    void SetFaultedUsingInitializer(InitializerValueProvider<TInput> valueProvider);
}


/// <summary>Configures how future state produces the terminal fault of a future.</summary>
/// <typeparam name="TFault">The future fault contract.</typeparam>
public interface IFutureFaultConfigurator<TFault>
    where TFault : class
{
    /// <summary>Creates the future fault with a synchronous message factory.</summary>
    /// <param name="factoryMethod">The factory that creates the fault from future state.</param>
    void SetFaultedUsingFactory(EventMessageFactory<FutureState, TFault> factoryMethod);

    /// <summary>Creates the future fault with an asynchronous message factory.</summary>
    /// <param name="factoryMethod">The asynchronous factory that creates the fault from future state.</param>
    void SetFaultedUsingFactory(AsyncEventMessageFactory<FutureState, TFault> factoryMethod);

    /// <summary>Initializes the future fault from the initiating command and additional property values.</summary>
    /// <param name="valueProvider">The provider of additional fault property values.</param>
    void SetFaultedUsingInitializer(InitializerValueProvider valueProvider);
}

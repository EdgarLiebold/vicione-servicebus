namespace ViciOne.ServiceBus.Configuration;

/// <summary>Configures how an input message produces a future's successful result.</summary>
/// <typeparam name="TResult">The successful future result contract.</typeparam>
/// <typeparam name="TInput">The input message contract.</typeparam>
public interface IFutureResultConfigurator<TResult, out TInput>
    where TInput : class
    where TResult : class
{
    /// <summary>Sets the synchronous factory that creates the future result.</summary>
    /// <param name="factoryMethod">The factory that creates the result from the input context.</param>
    void SetResultFactory(EventMessageFactory<FutureState, TInput, TResult> factoryMethod);

    /// <summary>Sets the asynchronous factory that creates the future result.</summary>
    /// <param name="factoryMethod">The asynchronous factory that creates the result from the input context.</param>
    void SetResultFactory(AsyncEventMessageFactory<FutureState, TInput, TResult> factoryMethod);

    /// <summary>Sets the initializer values used to create the future result.</summary>
    /// <param name="valueProvider">The provider of additional result property values.</param>
    void SetResultInitializer(InitializerValueProvider<TInput> valueProvider);
}


/// <summary>Configures how future state produces the successful result of a future.</summary>
/// <typeparam name="TResult">The successful future result contract.</typeparam>
public interface IFutureResultConfigurator<TResult>
    where TResult : class
{
    /// <summary>Sets the synchronous factory that creates the future result.</summary>
    /// <param name="factoryMethod">The factory that creates the result from future state.</param>
    void SetResultFactory(EventMessageFactory<FutureState, TResult> factoryMethod);

    /// <summary>Sets the asynchronous factory that creates the future result.</summary>
    /// <param name="factoryMethod">The asynchronous factory that creates the result from future state.</param>
    void SetResultFactory(AsyncEventMessageFactory<FutureState, TResult> factoryMethod);

    /// <summary>Sets the initializer values used to create the future result.</summary>
    /// <param name="valueProvider">The provider of additional result property values.</param>
    void SetResultInitializer(InitializerValueProvider valueProvider);
}

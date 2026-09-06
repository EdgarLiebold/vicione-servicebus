namespace ViciOne.ServiceBus.Configuration;

/// <summary>Configures how an input message produces the successful result of a future.</summary>
/// <typeparam name="TResult">The successful future result contract.</typeparam>
/// <typeparam name="TInput">The input message contract.</typeparam>
public interface IFutureResultConfigurator<TResult, out TInput>
    where TInput : class
    where TResult : class
{
    /// <summary>Creates the future result with a synchronous message factory.</summary>
    /// <param name="factoryMethod">The factory that creates the result from the input context.</param>
    void SetCompletedUsingFactory(EventMessageFactory<FutureState, TInput, TResult> factoryMethod);

    /// <summary>Creates the future result with an asynchronous message factory.</summary>
    /// <param name="factoryMethod">The asynchronous factory that creates the result from the input context.</param>
    void SetCompletedUsingFactory(AsyncEventMessageFactory<FutureState, TInput, TResult> factoryMethod);

    /// <summary>Initializes the future result from the initiating command and additional property values.</summary>
    /// <param name="valueProvider">The provider of additional result property values.</param>
    void SetCompletedUsingInitializer(InitializerValueProvider<TInput> valueProvider);
}


/// <summary>Configures how future state produces the successful result of a future.</summary>
/// <typeparam name="TResult">The successful future result contract.</typeparam>
public interface IFutureResultConfigurator<TResult>
    where TResult : class
{
    /// <summary>Creates the future result with a synchronous message factory.</summary>
    /// <param name="factoryMethod">The factory that creates the result from future state.</param>
    void SetCompletedUsingFactory(EventMessageFactory<FutureState, TResult> factoryMethod);

    /// <summary>Creates the future result with an asynchronous message factory.</summary>
    /// <param name="factoryMethod">The asynchronous factory that creates the result from future state.</param>
    void SetCompletedUsingFactory(AsyncEventMessageFactory<FutureState, TResult> factoryMethod);

    /// <summary>Initializes the future result from the initiating command and additional property values.</summary>
    /// <param name="valueProvider">The provider of additional result property values.</param>
    void SetCompletedUsingInitializer(InitializerValueProvider valueProvider);
}

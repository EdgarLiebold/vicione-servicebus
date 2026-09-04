namespace ViciOne.ServiceBus.Advanced;

/// <summary>
/// Represents the method that handles request pipe configurator callback.
/// </summary>
/// <typeparam name="TRequest">The t request type.</typeparam>
/// <param name="configurator">The configurator value.</param>
/// <returns>The result of the operation.</returns>
public delegate void RequestPipeConfiguratorCallback<TRequest>(IRequestPipeConfigurator<TRequest> configurator)
    where TRequest : class;

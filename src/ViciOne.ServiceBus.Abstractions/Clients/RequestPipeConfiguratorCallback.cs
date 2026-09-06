namespace ViciOne.ServiceBus.Advanced;

/// <summary>Represents the method that handles request pipe configurator callback.</summary>
/// <typeparam name="TRequest">The request type.</typeparam>
/// <param name="configurator">The configurator to update.</param>
public delegate void RequestPipeConfiguratorCallback<TRequest>(IRequestPipeConfigurator<TRequest> configurator)
    where TRequest : class;

namespace ViciOne.ServiceBus.Advanced;

/// <summary>Configures the send pipeline and delivery settings for one request.</summary>
/// <typeparam name="TRequest">The request message contract.</typeparam>
/// <param name="configurator">The pending request to configure.</param>
public delegate void RequestPipeConfiguratorCallback<TRequest>(IRequestPipeConfigurator<TRequest> configurator)
    where TRequest : class;

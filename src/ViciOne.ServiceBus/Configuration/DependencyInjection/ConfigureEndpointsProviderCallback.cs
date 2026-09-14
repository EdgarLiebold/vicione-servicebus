namespace ViciOne.ServiceBus.Configuration;

/// <summary>Configures each conventionally created receive endpoint with access to its registration context.</summary>
/// <param name="context">The registration context that resolves bus-owned services and components.</param>
/// <param name="queueName">The endpoint name, or <see langword="null" /> when no name is available.</param>
/// <param name="configurator">The receive endpoint to configure.</param>
public delegate void ConfigureEndpointsProviderCallback(IRegistrationContext context, string? queueName, IReceiveEndpointConfigurator configurator);

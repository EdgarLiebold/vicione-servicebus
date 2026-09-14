namespace ViciOne.ServiceBus.Configuration;

/// <summary>Configures each receive endpoint created by conventional endpoint registration.</summary>
/// <param name="queueName">The endpoint name, or <see langword="null" /> when no name is available.</param>
/// <param name="configurator">The receive endpoint to configure.</param>
public delegate void ConfigureEndpointsCallback(string? queueName, IReceiveEndpointConfigurator configurator);

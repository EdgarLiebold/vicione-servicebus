namespace ViciOne.ServiceBus.Configuration;

/// <summary>Represents the method that handles configure endpoints callback.</summary>
/// <param name="queueName">The queue name.</param>
/// <param name="configurator">The configurator to update.</param>
public delegate void ConfigureEndpointsCallback(string? queueName, IReceiveEndpointConfigurator configurator);

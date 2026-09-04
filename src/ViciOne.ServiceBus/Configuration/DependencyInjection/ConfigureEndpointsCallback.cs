namespace ViciOne.ServiceBus.Configuration;

/// <summary>
/// Represents the method that handles configure endpoints callback.
/// </summary>
/// <param name="queueName">The queue name value.</param>
/// <param name="configurator">The configurator value.</param>
/// <returns>The result of the operation.</returns>
public delegate void ConfigureEndpointsCallback(string? queueName, IReceiveEndpointConfigurator configurator);

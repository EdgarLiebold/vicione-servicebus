namespace ViciOne.ServiceBus.Configuration;

/// <summary>
/// Represents the method that handles configure endpoints provider callback.
/// </summary>
/// <param name="context">The operation context.</param>
/// <param name="queueName">The queue name value.</param>
/// <param name="configurator">The configurator value.</param>
/// <returns>The result of the operation.</returns>
public delegate void ConfigureEndpointsProviderCallback(IRegistrationContext context, string? queueName, IReceiveEndpointConfigurator configurator);

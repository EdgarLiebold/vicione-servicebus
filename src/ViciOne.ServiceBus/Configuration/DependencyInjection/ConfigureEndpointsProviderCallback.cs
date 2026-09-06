namespace ViciOne.ServiceBus.Configuration;

/// <summary>Represents the method that handles configure endpoints provider callback.</summary>
/// <param name="context">The context associated with the operation.</param>
/// <param name="queueName">The queue name.</param>
/// <param name="configurator">The configurator to update.</param>
public delegate void ConfigureEndpointsProviderCallback(IRegistrationContext context, string? queueName, IReceiveEndpointConfigurator configurator);

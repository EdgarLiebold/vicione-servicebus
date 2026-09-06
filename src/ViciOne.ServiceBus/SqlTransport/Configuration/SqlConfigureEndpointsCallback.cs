namespace ViciOne.ServiceBus.Providers.Transports;

/// <summary>Represents the method that handles sql configure endpoints callback.</summary>
/// <param name="context">The context associated with the operation.</param>
/// <param name="queueName">The queue name.</param>
/// <param name="configurator">The configurator to update.</param>
public delegate void SqlConfigureEndpointsCallback(IRegistrationContext context, string? queueName, ISqlReceiveEndpointConfigurator configurator);

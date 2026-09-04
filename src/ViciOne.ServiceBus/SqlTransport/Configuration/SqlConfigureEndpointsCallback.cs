namespace ViciOne.ServiceBus.Providers.Transports;

/// <summary>
/// Represents the method that handles sql configure endpoints callback.
/// </summary>
/// <param name="context">The operation context.</param>
/// <param name="queueName">The queue name value.</param>
/// <param name="configurator">The configurator value.</param>
/// <returns>The result of the operation.</returns>
public delegate void SqlConfigureEndpointsCallback(IRegistrationContext context, string? queueName, ISqlReceiveEndpointConfigurator configurator);

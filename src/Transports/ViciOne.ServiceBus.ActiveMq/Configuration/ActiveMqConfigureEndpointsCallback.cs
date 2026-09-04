namespace ViciOne.ServiceBus.ActiveMq;

/// <summary>
/// Represents the method that handles active mq configure endpoints callback.
/// </summary>
/// <param name="context">The operation context.</param>
/// <param name="queueName">The queue name value.</param>
/// <param name="configurator">The configurator value.</param>
/// <returns>The result of the operation.</returns>
public delegate void ActiveMqConfigureEndpointsCallback(IRegistrationContext context, string queueName, IActiveMqReceiveEndpointConfigurator configurator);

namespace ViciOne.ServiceBus.RabbitMq;

/// <summary>
/// Represents the method that handles rabbit mq configure endpoints callback.
/// </summary>
/// <param name="context">The operation context.</param>
/// <param name="queueName">The queue name value.</param>
/// <param name="configurator">The configurator value.</param>
/// <returns>The result of the operation.</returns>
public delegate void RabbitMqConfigureEndpointsCallback(IRegistrationContext context, string? queueName, IRabbitMqReceiveEndpointConfigurator configurator);

namespace ViciOne.ServiceBus.RabbitMq;

/// <summary>Configures a RabbitMQ receive endpoint after registrations have been applied.</summary>
/// <param name="context">The registration context for resolving endpoint dependencies.</param>
/// <param name="queueName">The endpoint queue name, when the endpoint has one.</param>
/// <param name="configurator">The RabbitMQ receive-endpoint configurator.</param>
public delegate void RabbitMqConfigureEndpointsCallback(IRegistrationContext context, string? queueName, IRabbitMqReceiveEndpointConfigurator configurator);

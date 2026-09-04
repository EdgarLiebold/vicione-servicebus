namespace ViciOne.ServiceBus;

public delegate void RabbitMqConfigureEndpointsCallback(IRegistrationContext context, string? queueName, IRabbitMqReceiveEndpointConfigurator configurator);

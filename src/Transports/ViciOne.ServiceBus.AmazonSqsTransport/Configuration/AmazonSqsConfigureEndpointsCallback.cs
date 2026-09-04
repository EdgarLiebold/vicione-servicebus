namespace ViciOne.ServiceBus;

public delegate void AmazonSqsConfigureEndpointsCallback(IRegistrationContext context, string? queueName, IAmazonSqsReceiveEndpointConfigurator configurator);

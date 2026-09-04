namespace ViciOne.ServiceBus;

public delegate void ConfigureEndpointsProviderCallback(IRegistrationContext context, string? queueName, IReceiveEndpointConfigurator configurator);

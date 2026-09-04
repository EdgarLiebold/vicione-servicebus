namespace ViciOne.ServiceBus;

public delegate void ConfigureEndpointsCallback(string queueName, IReceiveEndpointConfigurator configurator);

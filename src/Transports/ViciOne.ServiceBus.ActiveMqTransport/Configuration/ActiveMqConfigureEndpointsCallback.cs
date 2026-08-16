namespace ViciOne.ServiceBus;

public delegate void ActiveMqConfigureEndpointsCallback(IRegistrationContext context, string queueName, IActiveMqReceiveEndpointConfigurator configurator);

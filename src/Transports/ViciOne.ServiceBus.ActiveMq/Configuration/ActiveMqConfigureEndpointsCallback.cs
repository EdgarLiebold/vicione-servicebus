namespace ViciOne.ServiceBus.ActiveMq;

/// <summary>Configures a named ActiveMQ receive endpoint during registration.</summary>
/// <param name="context">The dependency-injection registration context.</param>
/// <param name="queueName">The configured queue name.</param>
/// <param name="configurator">The ActiveMQ receive-endpoint configurator.</param>
public delegate void ActiveMqConfigureEndpointsCallback(IRegistrationContext context, string queueName, IActiveMqReceiveEndpointConfigurator configurator);

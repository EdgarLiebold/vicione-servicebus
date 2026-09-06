namespace ViciOne.ServiceBus.AzureServiceBus;

/// <summary>Configures a named Azure Service Bus receive endpoint during endpoint registration.</summary>
/// <param name="context">The registration context that resolves endpoint dependencies.</param>
/// <param name="queueName">The namespace-relative queue name.</param>
/// <param name="configurator">The Azure Service Bus endpoint configurator.</param>
public delegate void ServiceBusConfigureEndpointsCallback(IRegistrationContext context, string queueName, IServiceBusReceiveEndpointConfigurator configurator);

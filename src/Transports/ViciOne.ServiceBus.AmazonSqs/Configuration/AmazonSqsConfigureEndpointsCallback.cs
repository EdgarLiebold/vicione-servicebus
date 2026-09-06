namespace ViciOne.ServiceBus.AmazonSqs;

/// <summary>Configures an Amazon SQS receive endpoint discovered during endpoint registration.</summary>
/// <param name="context">The service registration context.</param>
/// <param name="queueName">The resolved queue name, or <see langword="null"/> when it is not yet available.</param>
/// <param name="configurator">The Amazon SQS receive-endpoint configurator.</param>
public delegate void AmazonSqsConfigureEndpointsCallback(IRegistrationContext context, string? queueName, IAmazonSqsReceiveEndpointConfigurator configurator);

namespace ViciOne.ServiceBus.AmazonSqs;

/// <summary>
/// Represents the method that handles amazon sqs configure endpoints callback.
/// </summary>
/// <param name="context">The operation context.</param>
/// <param name="queueName">The queue name value.</param>
/// <param name="configurator">The configurator value.</param>
/// <returns>The result of the operation.</returns>
public delegate void AmazonSqsConfigureEndpointsCallback(IRegistrationContext context, string? queueName, IAmazonSqsReceiveEndpointConfigurator configurator);

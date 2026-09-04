using System;
using System.Reflection;
using System.Threading.Tasks;
using Azure.Core.Amqp;
using Azure.Messaging.ServiceBus;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.DependencyInjection.Extensions;
using ViciOne.ServiceBus.AzureServiceBus;
using ViciOne.ServiceBus.Serialization;

namespace ViciOne.ServiceBus.Testing;

/// <summary>
/// Provides extension methods for azure functions test.
/// </summary>
public static class AzureFunctionsTestExtensions
{
    /// <summary>
    /// Adds azure functions test components to the configuration.
    /// </summary>
    /// <param name="configurator">The configurator value.</param>
    /// <returns>The result of the operation.</returns>
    public static IBusRegistrationConfigurator AddAzureFunctionsTestComponents(this IBusRegistrationConfigurator configurator)
    {
        configurator.Services.TryAddSingleton<IAsyncBusHandle, AsyncBusHandle>();
        configurator.Services.TryAddSingleton<IMessageReceiver, MessageReceiver>();

        return configurator;
    }

    /// <summary>
    /// Handle the Azure Service Bus message using the specified consumer
    /// </summary>
    /// <param name="harness"></param>
    /// <param name="message"></param>
    /// <typeparam name="TConsumer"></typeparam>
    /// <param name="cancellationToken">The token used to cancel the operation.</param>
    public static Task HandleConsumerAsync<TConsumer>(this ITestHarness harness, object message, CancellationToken cancellationToken = default)
        where TConsumer : class, IConsumer
    {
        if (cancellationToken.IsCancellationRequested) return global::System.Threading.Tasks.Task.FromCanceled(cancellationToken); var body = ServiceBusMetadataJson.ObjectDeserializer.SerializeObject(message);

        var messageBody = new AmqpMessageBody([new BinaryData(body.GetBytes()).ToMemory()]);
        var annotatedMessage = new AmqpAnnotatedMessage(messageBody)
        {
            Header = { DeliveryCount = 1 },
            Properties =
            {
                MessageId = new AmqpMessageId(NewId.NextGuid().ToString()),
                ContentType = SystemTextJsonRawMessageSerializer.JsonContentType.MediaType
            }
        };

        ConstructorInfo constructor = typeof(ServiceBusReceivedMessage).GetConstructor(
            BindingFlags.NonPublic | BindingFlags.Instance, null, [typeof(AmqpAnnotatedMessage)], null)
            ?? throw new InvalidOperationException("The Azure Service Bus received-message constructor is unavailable.");
        var receivedMessage = (ServiceBusReceivedMessage)constructor.Invoke([annotatedMessage]);

        var receiver = harness.Scope.ServiceProvider.GetRequiredService<IMessageReceiver>();
        var formatter = harness.Scope.ServiceProvider.GetService<IEndpointNameFormatter>() ?? DefaultEndpointNameFormatter.Instance;

        return receiver.HandleConsumerAsync<TConsumer>(formatter.Consumer<TConsumer>(), receivedMessage, cancellationToken);
    }
}

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

/// <summary>Provides Azure Functions test-harness integration.</summary>
public static class AzureFunctionsTestExtensions
{
    /// <summary>Registers the bus handle and message receiver used to invoke consumers in Azure Functions tests.</summary>
    /// <param name="configurator">The bus registration configurator to update.</param>
    /// <returns>The same bus registration configurator.</returns>
    public static IBusRegistrationConfigurator AddAzureFunctionsTestComponents(this IBusRegistrationConfigurator configurator)
    {
        configurator.Services.TryAddSingleton<IAsyncBusHandle, AsyncBusHandle>();
        configurator.Services.TryAddSingleton<IMessageReceiver, MessageReceiver>();

        return configurator;
    }

    /// <summary>Serializes a message as an Azure Service Bus delivery and dispatches it to the specified consumer.</summary>
    /// <typeparam name="TConsumer">The consumer type to invoke.</typeparam>
    /// <param name="harness">The test harness whose service scope resolves the receiver.</param>
    /// <param name="message">The message instance to serialize and dispatch.</param>
    /// <param name="cancellationToken">The token that cancels dispatch.</param>
    /// <returns>A task that completes when the receiver finishes handling the message.</returns>
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

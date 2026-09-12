using System;
using System.Threading.Tasks;
using Azure.Messaging.ServiceBus;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.DependencyInjection.Extensions;
using ViciOne.ServiceBus.AzureServiceBus;
using ViciOne.ServiceBus.Serialization;
using ViciOne.ServiceBus.Testing;

namespace ViciOne.ServiceBus.AzureServiceBus.Testing;

/// <summary>Provides Azure Functions test-harness integration.</summary>
public static class AzureFunctionsTestExtensions
{
    /// <summary>Registers the bus handle and message receiver used to invoke consumers in Azure Functions tests.</summary>
    /// <param name="configurator">The bus registration configurator to update.</param>
    /// <returns>The same bus registration configurator.</returns>
    public static IBusRegistrationConfigurator AddAzureFunctionsTestComponents(this IBusRegistrationConfigurator configurator)
    {
        ArgumentNullException.ThrowIfNull(configurator);

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
        ArgumentNullException.ThrowIfNull(harness);
        ArgumentNullException.ThrowIfNull(message);
        if (cancellationToken.IsCancellationRequested)
            return Task.FromCanceled(cancellationToken);

        var body = ServiceBusMetadataJson.ObjectDeserializer.SerializeObject(message);

        ServiceBusReceivedMessage receivedMessage = ServiceBusModelFactory.ServiceBusReceivedMessage(
            body: BinaryData.FromBytes(body.ToArray()),
            messageId: NewId.NextGuid().ToString(),
            contentType: SystemTextJsonRawMessageSerializer.JsonContentType.MediaType,
            deliveryCount: 1);

        var receiver = harness.Scope.ServiceProvider.GetRequiredService<IMessageReceiver>();
        var formatter = harness.Scope.ServiceProvider.GetService<IEndpointNameFormatter>() ?? DefaultEndpointNameFormatter.Instance;

        return receiver.HandleConsumerAsync<TConsumer>(formatter.Consumer<TConsumer>(), receivedMessage, cancellationToken);
    }
}

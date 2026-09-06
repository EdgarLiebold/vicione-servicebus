using System;
using System.Threading;
using System.Threading.Tasks;
using Azure.Messaging.ServiceBus;

namespace ViciOne.ServiceBus.AzureServiceBus;

/// <summary>Dispatches Azure Functions trigger messages through configured consumers, sagas, or execute activities.</summary>
public interface IMessageReceiver :
    IDisposable
{
    /// <summary>Dispatches a queue message through every matching configured consumer and saga.</summary>
    /// <param name="queueName">The queue name used to construct the receive input address.</param>
    /// <param name="message">The received Azure Service Bus message.</param>
    /// <param name="cancellationToken">Cancels dispatch and message processing.</param>
    /// <returns>The cached queue receiver's dispatch task.</returns>
    Task HandleAsync(string queueName, ServiceBusReceivedMessage message, CancellationToken cancellationToken);

    /// <summary>Dispatches a subscription message through every matching configured consumer and saga.</summary>
    /// <param name="topicPath">The topic path used to construct the receive input address.</param>
    /// <param name="subscriptionName">The subscription name associated with the trigger.</param>
    /// <param name="message">The received Azure Service Bus message.</param>
    /// <param name="cancellationToken">Cancels dispatch and message processing.</param>
    /// <returns>The cached subscription receiver's dispatch task.</returns>
    Task HandleAsync(string topicPath, string subscriptionName, ServiceBusReceivedMessage message, CancellationToken cancellationToken);

    /// <summary>Dispatches a queue message through one consumer type.</summary>
    /// <typeparam name="TConsumer">The consumer type to invoke.</typeparam>
    /// <param name="queueName">The queue name used to construct the receive input address.</param>
    /// <param name="message">The received Azure Service Bus message.</param>
    /// <param name="cancellationToken">Cancels dispatch and message processing.</param>
    /// <returns>The cached queue receiver's dispatch task for <typeparamref name="TConsumer"/>.</returns>
    Task HandleConsumerAsync<TConsumer>(string queueName, ServiceBusReceivedMessage message, CancellationToken cancellationToken)
        where TConsumer : class, IConsumer;

    /// <summary>Dispatches a subscription message through one consumer type.</summary>
    /// <typeparam name="TConsumer">The consumer type to invoke.</typeparam>
    /// <param name="topicPath">The topic path used to construct the receive input address.</param>
    /// <param name="subscriptionName">The subscription name associated with the trigger.</param>
    /// <param name="message">The received Azure Service Bus message.</param>
    /// <param name="cancellationToken">Cancels dispatch and message processing.</param>
    /// <returns>The cached subscription receiver's dispatch task for <typeparamref name="TConsumer"/>.</returns>
    Task HandleConsumerAsync<TConsumer>(string topicPath, string subscriptionName, ServiceBusReceivedMessage message, CancellationToken cancellationToken)
        where TConsumer : class, IConsumer;

    /// <summary>Dispatches a queue message through one saga type.</summary>
    /// <typeparam name="TSaga">The saga state type to invoke.</typeparam>
    /// <param name="queueName">The queue name used to construct the receive input address.</param>
    /// <param name="message">The received Azure Service Bus message.</param>
    /// <param name="cancellationToken">Cancels dispatch and saga processing.</param>
    /// <returns>The cached queue receiver's dispatch task for <typeparamref name="TSaga"/>.</returns>
    Task HandleSagaAsync<TSaga>(string queueName, ServiceBusReceivedMessage message, CancellationToken cancellationToken)
        where TSaga : class, ISaga;

    /// <summary>Dispatches a subscription message through one saga type.</summary>
    /// <typeparam name="TSaga">The saga state type to invoke.</typeparam>
    /// <param name="topicPath">The topic path used to construct the receive input address.</param>
    /// <param name="subscriptionName">The subscription name associated with the trigger.</param>
    /// <param name="message">The received Azure Service Bus message.</param>
    /// <param name="cancellationToken">Cancels dispatch and saga processing.</param>
    /// <returns>The cached subscription receiver's dispatch task for <typeparamref name="TSaga"/>.</returns>
    Task HandleSagaAsync<TSaga>(string topicPath, string subscriptionName, ServiceBusReceivedMessage message, CancellationToken cancellationToken)
        where TSaga : class, ISaga;

    /// <summary>Dispatches a queue message through one execute-activity type.</summary>
    /// <typeparam name="TActivity">The execute-activity type to invoke.</typeparam>
    /// <param name="queueName">The queue name used to construct the receive input address.</param>
    /// <param name="message">The received Azure Service Bus message.</param>
    /// <param name="cancellationToken">Cancels dispatch and activity processing.</param>
    /// <returns>The cached queue receiver's dispatch task for <typeparamref name="TActivity"/>.</returns>
    Task HandleExecuteActivityAsync<TActivity>(string queueName, ServiceBusReceivedMessage message, CancellationToken cancellationToken)
        where TActivity : class;
}

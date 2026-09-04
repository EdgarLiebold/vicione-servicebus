using System;
using System.Collections.Concurrent;
using System.Threading;
using System.Threading.Tasks;
using Azure.Messaging.ServiceBus;
using ViciOne.ServiceBus.AzureServiceBus.Configuration;
using ViciOne.ServiceBus.Transports;

namespace ViciOne.ServiceBus.AzureServiceBus;

/// <summary>
/// Provides a message receiver implementation.
/// </summary>
public class MessageReceiver :
    IMessageReceiver
{
    const string PathDelimiter = @"/";
    const string SubscriptionsSubPath = "Subscriptions";

    readonly IAsyncBusHandle _busHandle;
    readonly IServiceBusHostConfiguration _hostConfiguration;
    readonly ConcurrentDictionary<string, Lazy<IServiceBusMessageReceiver>> _receivers;
    readonly IBusRegistrationContext _registration;

    /// <summary>
    /// Initializes a new instance of the containing type.
    /// </summary>
    /// <param name="registration">The registration value.</param>
    /// <param name="busHandle">The bus handle value.</param>
    /// <param name="busInstance">The bus instance value.</param>
    public MessageReceiver(IBusRegistrationContext registration, IAsyncBusHandle busHandle, IBusInstance busInstance)
    {
        _hostConfiguration = busInstance.HostConfiguration as IServiceBusHostConfiguration
            ?? throw new ConfigurationException("The hostConfiguration was not properly configured for Azure Service Bus");

        _registration = registration;
        _busHandle = busHandle;

        _receivers = new ConcurrentDictionary<string, Lazy<IServiceBusMessageReceiver>>();
    }

    /// <summary>
    /// Performs the handle operation.
    /// </summary>
    /// <param name="queueName">The queue name value.</param>
    /// <param name="message">The message value.</param>
    /// <param name="cancellationToken">The token used to cancel the operation.</param>
    /// <returns>The result of the operation.</returns>
    public Task HandleAsync(string queueName, ServiceBusReceivedMessage message, CancellationToken cancellationToken)
    {
        var receiver = CreateMessageReceiver(queueName, cfg =>
        {
            cfg.ConfigureConsumers(_registration);
            cfg.ConfigureSagas(_registration);
        });

        return receiver.HandleAsync(message, cancellationToken);
    }

    /// <summary>
    /// Performs the handle operation.
    /// </summary>
    /// <param name="topicPath">The topic path value.</param>
    /// <param name="subscriptionName">The subscription name value.</param>
    /// <param name="message">The message value.</param>
    /// <param name="cancellationToken">The token used to cancel the operation.</param>
    /// <returns>The result of the operation.</returns>
    public Task HandleAsync(string topicPath, string subscriptionName, ServiceBusReceivedMessage message, CancellationToken cancellationToken)
    {
        var receiver = CreateMessageReceiver(topicPath, subscriptionName, cfg =>
        {
            cfg.ConfigureConsumers(_registration);
            cfg.ConfigureSagas(_registration);
        });

        return receiver.HandleAsync(message, cancellationToken);
    }

    /// <summary>
    /// Performs the handle consumer operation.
    /// </summary>
    /// <typeparam name="TConsumer">The t consumer type.</typeparam>
    /// <param name="queueName">The queue name value.</param>
    /// <param name="message">The message value.</param>
    /// <param name="cancellationToken">The token used to cancel the operation.</param>
    /// <returns>The result of the operation.</returns>
    public Task HandleConsumerAsync<TConsumer>(string queueName, ServiceBusReceivedMessage message, CancellationToken cancellationToken)
        where TConsumer : class, IConsumer
    {
        var receiver = CreateMessageReceiver(queueName, cfg =>
        {
            cfg.ConfigureConsumer<TConsumer>(_registration);
        });

        return receiver.HandleAsync(message, cancellationToken);
    }

    /// <summary>
    /// Performs the handle consumer operation.
    /// </summary>
    /// <typeparam name="TConsumer">The t consumer type.</typeparam>
    /// <param name="topicPath">The topic path value.</param>
    /// <param name="subscriptionName">The subscription name value.</param>
    /// <param name="message">The message value.</param>
    /// <param name="cancellationToken">The token used to cancel the operation.</param>
    /// <returns>The result of the operation.</returns>
    public Task HandleConsumerAsync<TConsumer>(string topicPath, string subscriptionName, ServiceBusReceivedMessage message, CancellationToken cancellationToken)
        where TConsumer : class, IConsumer
    {
        var receiver = CreateMessageReceiver(topicPath, subscriptionName, cfg =>
        {
            cfg.ConfigureConsumer<TConsumer>(_registration);
        });

        return receiver.HandleAsync(message, cancellationToken);
    }

    /// <summary>
    /// Performs the handle saga operation.
    /// </summary>
    /// <typeparam name="TSaga">The t saga type.</typeparam>
    /// <param name="queueName">The queue name value.</param>
    /// <param name="message">The message value.</param>
    /// <param name="cancellationToken">The token used to cancel the operation.</param>
    /// <returns>The result of the operation.</returns>
    public Task HandleSagaAsync<TSaga>(string queueName, ServiceBusReceivedMessage message, CancellationToken cancellationToken)
        where TSaga : class, ISaga
    {
        var receiver = CreateMessageReceiver(queueName, cfg =>
        {
            cfg.ConfigureSaga<TSaga>(_registration);
        });

        return receiver.HandleAsync(message, cancellationToken);
    }

    /// <summary>
    /// Performs the handle saga operation.
    /// </summary>
    /// <typeparam name="TSaga">The t saga type.</typeparam>
    /// <param name="topicPath">The topic path value.</param>
    /// <param name="subscriptionName">The subscription name value.</param>
    /// <param name="message">The message value.</param>
    /// <param name="cancellationToken">The token used to cancel the operation.</param>
    /// <returns>The result of the operation.</returns>
    public Task HandleSagaAsync<TSaga>(string topicPath, string subscriptionName, ServiceBusReceivedMessage message, CancellationToken cancellationToken)
        where TSaga : class, ISaga
    {
        var receiver = CreateMessageReceiver(topicPath, subscriptionName, cfg =>
        {
            cfg.ConfigureSaga<TSaga>(_registration);
        });

        return receiver.HandleAsync(message, cancellationToken);
    }

    /// <summary>
    /// Performs the handle execute activity operation.
    /// </summary>
    /// <typeparam name="TActivity">The t activity type.</typeparam>
    /// <param name="queueName">The queue name value.</param>
    /// <param name="message">The message value.</param>
    /// <param name="cancellationToken">The token used to cancel the operation.</param>
    /// <returns>The result of the operation.</returns>
    public Task HandleExecuteActivityAsync<TActivity>(string queueName, ServiceBusReceivedMessage message, CancellationToken cancellationToken)
        where TActivity : class
    {
        var receiver = CreateMessageReceiver(queueName, cfg =>
        {
            cfg.ConfigureExecuteActivity(_registration, typeof(TActivity));
        });

        return receiver.HandleAsync(message, cancellationToken);
    }

    /// <summary>
    /// Releases the resources owned by this instance.
    /// </summary>
    public void Dispose()
    {
    }

    IServiceBusMessageReceiver CreateMessageReceiver(string queueName, Action<IReceiveEndpointConfigurator> configure)
    {
        if (string.IsNullOrWhiteSpace(queueName))
            throw new ArgumentNullException(nameof(queueName));
        if (configure == null)
            throw new ArgumentNullException(nameof(configure));

        return _receivers.GetOrAdd(queueName, name => new Lazy<IServiceBusMessageReceiver>(() =>
        {
            var endpointConfiguration = _hostConfiguration.CreateReceiveEndpointConfiguration(queueName);

            var configurator = new QueueBrokeredMessageReceiverConfiguration(_hostConfiguration, endpointConfiguration);

            configure(configurator);

            return configurator.Build();
        })).Value;
    }

    IServiceBusMessageReceiver CreateMessageReceiver(string topicPath, string subscriptionName, Action<IReceiveEndpointConfigurator> configure)
    {
        if (string.IsNullOrWhiteSpace(topicPath))
            throw new ArgumentNullException(nameof(topicPath));
        if (configure == null)
            throw new ArgumentNullException(nameof(configure));

        var subscriptionPath = string.Concat(topicPath, PathDelimiter, SubscriptionsSubPath, PathDelimiter, subscriptionName);

        return _receivers.GetOrAdd(subscriptionPath, name => new Lazy<IServiceBusMessageReceiver>(() =>
        {
            var topicConfigurator = new ServiceBusTopicConfigurator(topicPath, false);

            static void NoConfigure(IServiceBusSubscriptionEndpointConfigurator _)
            {
            }

            var endpointConfiguration = _hostConfiguration.CreateSubscriptionEndpointConfiguration(subscriptionName, topicConfigurator.Path, NoConfigure);

            var configurator = new SubscriptionBrokeredMessageReceiverConfiguration(_hostConfiguration, endpointConfiguration);

            configure(configurator);

            return configurator.Build();
        })).Value;
    }
}

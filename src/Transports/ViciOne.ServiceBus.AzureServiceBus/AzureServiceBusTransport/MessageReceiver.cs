using System;
using System.Collections.Concurrent;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Azure.Messaging.ServiceBus;
using Microsoft.Extensions.DependencyInjection;
using ViciOne.ServiceBus.AzureServiceBus.Configuration;
using ViciOne.ServiceBus.Configuration;
using ViciOne.ServiceBus.Sagas.Configuration;
using ViciOne.ServiceBus.Transports;

namespace ViciOne.ServiceBus.AzureServiceBus;

/// <summary>Caches receive pipelines used to dispatch Azure Functions trigger messages.</summary>
public class MessageReceiver :
    IMessageReceiver
{
    readonly IAsyncBusHandle _busHandle;
    readonly IServiceBusHostConfiguration _hostConfiguration;
    readonly ConcurrentDictionary<ReceiverKey, Lazy<IServiceBusMessageReceiver>> _receivers;
    readonly IBusRegistrationContext _registration;

    /// <summary>Creates a receiver bound to an Azure Service Bus bus instance.</summary>
    /// <param name="registration">The registration context used to configure consumers and sagas.</param>
    /// <param name="busHandle">The asynchronous bus handle retained for the receiver lifetime.</param>
    /// <param name="busInstance">The bus instance that supplies Azure host configuration.</param>
    public MessageReceiver(IBusRegistrationContext registration, IAsyncBusHandle busHandle, IBusInstance busInstance)
    {
        _hostConfiguration = busInstance.HostConfiguration as IServiceBusHostConfiguration
            ?? throw new ConfigurationException(global::ViciOne.ServiceBus.Providers.Configuration.ConfigurationMessages.Create("Azure Service Bus", "unknown", "The hostConfiguration was not properly configured for Azure Service Bus", "Correct the named configuration before starting the host"));

        _registration = registration;
        _busHandle = busHandle;

        _receivers = new ConcurrentDictionary<ReceiverKey, Lazy<IServiceBusMessageReceiver>>();
    }

    /// <summary>Dispatches a queue delivery through every matching configured consumer and saga.</summary>
    /// <param name="queueName">The source queue name.</param>
    /// <param name="message">The received Azure Service Bus message.</param>
    /// <param name="cancellationToken">Cancels dispatch.</param>
    /// <returns>The cached queue receiver's dispatch task.</returns>
    public Task HandleAsync(string queueName, ServiceBusReceivedMessage message, CancellationToken cancellationToken)
    {
        var receiver = CreateMessageReceiver(queueName, ReceiverKind.All, null, ConfigureAll);

        return receiver.HandleAsync(message, cancellationToken);
    }

    /// <summary>Dispatches a subscription delivery through every matching configured consumer and saga.</summary>
    /// <param name="topicPath">The source topic path.</param>
    /// <param name="subscriptionName">The source subscription name.</param>
    /// <param name="message">The received Azure Service Bus message.</param>
    /// <param name="cancellationToken">Cancels dispatch.</param>
    /// <returns>The cached subscription receiver's dispatch task.</returns>
    public Task HandleAsync(string topicPath, string subscriptionName, ServiceBusReceivedMessage message, CancellationToken cancellationToken)
    {
        var receiver = CreateMessageReceiver(topicPath, subscriptionName, ReceiverKind.All, null, ConfigureAll);

        return receiver.HandleAsync(message, cancellationToken);
    }

    /// <summary>Dispatches a queue delivery through one consumer type.</summary>
    /// <typeparam name="TConsumer">The consumer type to invoke.</typeparam>
    /// <param name="queueName">The source queue name.</param>
    /// <param name="message">The received Azure Service Bus message.</param>
    /// <param name="cancellationToken">Cancels dispatch.</param>
    /// <returns>The cached queue receiver's dispatch task for <typeparamref name="TConsumer"/>.</returns>
    public Task HandleConsumerAsync<TConsumer>(string queueName, ServiceBusReceivedMessage message, CancellationToken cancellationToken)
        where TConsumer : class, IConsumer
    {
        var receiver = CreateMessageReceiver(queueName, ReceiverKind.Consumer, typeof(TConsumer), (registration, cfg) =>
        {
            cfg.ConfigureConsumer<TConsumer>(registration);
        });

        return receiver.HandleAsync(message, cancellationToken);
    }

    /// <summary>Dispatches a subscription delivery through one consumer type.</summary>
    /// <typeparam name="TConsumer">The consumer type to invoke.</typeparam>
    /// <param name="topicPath">The source topic path.</param>
    /// <param name="subscriptionName">The source subscription name.</param>
    /// <param name="message">The received Azure Service Bus message.</param>
    /// <param name="cancellationToken">Cancels dispatch.</param>
    /// <returns>The cached subscription receiver's dispatch task for <typeparamref name="TConsumer"/>.</returns>
    public Task HandleConsumerAsync<TConsumer>(string topicPath, string subscriptionName, ServiceBusReceivedMessage message, CancellationToken cancellationToken)
        where TConsumer : class, IConsumer
    {
        var receiver = CreateMessageReceiver(topicPath, subscriptionName, ReceiverKind.Consumer, typeof(TConsumer), (registration, cfg) =>
        {
            cfg.ConfigureConsumer<TConsumer>(registration);
        });

        return receiver.HandleAsync(message, cancellationToken);
    }

    /// <summary>Dispatches a queue delivery through one saga type.</summary>
    /// <typeparam name="TSaga">The saga state type to invoke.</typeparam>
    /// <param name="queueName">The source queue name.</param>
    /// <param name="message">The received Azure Service Bus message.</param>
    /// <param name="cancellationToken">Cancels dispatch.</param>
    /// <returns>The cached queue receiver's dispatch task for <typeparamref name="TSaga"/>.</returns>
    public Task HandleSagaAsync<TSaga>(string queueName, ServiceBusReceivedMessage message, CancellationToken cancellationToken)
        where TSaga : class, ISaga
    {
        var receiver = CreateMessageReceiver(queueName, ReceiverKind.Saga, typeof(TSaga), (registration, cfg) =>
        {
            cfg.ConfigureSaga<TSaga>(registration);
        });

        return receiver.HandleAsync(message, cancellationToken);
    }

    /// <summary>Dispatches a subscription delivery through one saga type.</summary>
    /// <typeparam name="TSaga">The saga state type to invoke.</typeparam>
    /// <param name="topicPath">The source topic path.</param>
    /// <param name="subscriptionName">The source subscription name.</param>
    /// <param name="message">The received Azure Service Bus message.</param>
    /// <param name="cancellationToken">Cancels dispatch.</param>
    /// <returns>The cached subscription receiver's dispatch task for <typeparamref name="TSaga"/>.</returns>
    public Task HandleSagaAsync<TSaga>(string topicPath, string subscriptionName, ServiceBusReceivedMessage message, CancellationToken cancellationToken)
        where TSaga : class, ISaga
    {
        var receiver = CreateMessageReceiver(topicPath, subscriptionName, ReceiverKind.Saga, typeof(TSaga), (registration, cfg) =>
        {
            cfg.ConfigureSaga<TSaga>(registration);
        });

        return receiver.HandleAsync(message, cancellationToken);
    }

    /// <summary>Dispatches a queue delivery through one execute-activity type.</summary>
    /// <typeparam name="TActivity">The execute-activity type to invoke.</typeparam>
    /// <param name="queueName">The source queue name.</param>
    /// <param name="message">The received Azure Service Bus message.</param>
    /// <param name="cancellationToken">Cancels dispatch.</param>
    /// <returns>The cached queue receiver's dispatch task for <typeparamref name="TActivity"/>.</returns>
    public Task HandleExecuteActivityAsync<TActivity>(string queueName, ServiceBusReceivedMessage message, CancellationToken cancellationToken)
        where TActivity : class
    {
        var receiver = CreateMessageReceiver(queueName, ReceiverKind.ExecuteActivity, typeof(TActivity), (registration, cfg) =>
        {
            cfg.ConfigureExecuteActivity(registration, typeof(TActivity));
        });

        return receiver.HandleAsync(message, cancellationToken);
    }

    /// <summary>Completes disposal; cached receiver pipelines are owned by the bus configuration.</summary>
    public void Dispose()
    {
    }

    IServiceBusMessageReceiver CreateMessageReceiver(string queueName, ReceiverKind kind, Type? handlerType,
        Action<IRegistrationContext, IReceiveEndpointConfigurator> configure)
    {
        if (string.IsNullOrWhiteSpace(queueName))
            throw new ArgumentNullException(nameof(queueName));
        if (configure == null)
            throw new ArgumentNullException(nameof(configure));

        return _receivers.GetOrAdd(new ReceiverKey(queueName, null, false, kind, handlerType), _ => new Lazy<IServiceBusMessageReceiver>(() =>
        {
            var endpointConfiguration = _hostConfiguration.CreateReceiveEndpointConfiguration(queueName);

            var configurator = new QueueBrokeredMessageReceiverConfiguration(_hostConfiguration, endpointConfiguration);

            configure(CreateRegistrationContext(), configurator);

            return configurator.Build();
        })).Value;
    }

    IServiceBusMessageReceiver CreateMessageReceiver(string topicPath, string subscriptionName, ReceiverKind kind, Type? handlerType,
        Action<IRegistrationContext, IReceiveEndpointConfigurator> configure)
    {
        if (string.IsNullOrWhiteSpace(topicPath))
            throw new ArgumentNullException(nameof(topicPath));
        if (configure == null)
            throw new ArgumentNullException(nameof(configure));

        return _receivers.GetOrAdd(new ReceiverKey(topicPath, subscriptionName, true, kind, handlerType), _ => new Lazy<IServiceBusMessageReceiver>(() =>
        {
            var topicConfigurator = new ServiceBusTopicConfigurator(topicPath, false);

            static void NoConfigure(IServiceBusSubscriptionEndpointConfigurator _)
            {
            }

            var endpointConfiguration = _hostConfiguration.CreateSubscriptionEndpointConfiguration(subscriptionName, topicConfigurator.Path, NoConfigure);

            var configurator = new SubscriptionBrokeredMessageReceiverConfiguration(_hostConfiguration, endpointConfiguration);

            configure(CreateRegistrationContext(), configurator);

            return configurator.Build();
        })).Value;
    }

    IRegistrationContext CreateRegistrationContext()
    {
        return new BusRegistrationContext(_registration, _registration.GetRequiredService<IContainerSelector>(),
            _registration as ISetScopedConsumeContext ?? _registration.GetRequiredService<ISetScopedConsumeContext>(), _registration.BusType);
    }

    static void ConfigureAll(IRegistrationContext registration, IReceiveEndpointConfigurator configurator)
    {
        configurator.ConfigureConsumers(registration);
        if (registration.GetServices<IConsumerKind>().Any(kind => string.Equals(kind.Name, "Saga", StringComparison.Ordinal)))
            configurator.ConfigureSagas(registration);
    }

    enum ReceiverKind
    {
        All,
        Consumer,
        Saga,
        ExecuteActivity
    }

    readonly record struct ReceiverKey(string Path, string? Subscription, bool IsSubscription, ReceiverKind Kind, Type? HandlerType);
}

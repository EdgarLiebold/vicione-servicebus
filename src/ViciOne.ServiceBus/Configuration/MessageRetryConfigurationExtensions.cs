using System;
using System.Threading;
using ViciOne.ServiceBus.Configuration;
using ViciOne.ServiceBus.Middleware;

namespace ViciOne.ServiceBus.Configuration;

/// <summary>
/// Provides extension methods for message retry configuration.
/// </summary>
public static class MessageRetryConfigurationExtensions
{
    /// <summary>
    /// Configures retry once for every message type handled by a consumer, handler, or saga. The retry
    /// filter runs before the consumer factory or saga repository.
    /// </summary>
    /// <param name="configurator"></param>
    /// <param name="configure"></param>
    public static void UseMessageRetry(this IConsumePipeConfigurator configurator, Action<IRetryConfigurator> configure)
    {
        ArgumentNullException.ThrowIfNull(configurator);
        ArgumentNullException.ThrowIfNull(configure);

        CancellationToken stoppingToken = CancellationToken.None;
        if (configurator is IBusFactoryConfigurator connector)
        {
            var retryObserver = new RetryBusObserver();
            connector.ConnectBusObserver(retryObserver);
            stoppingToken = retryObserver.Stopping;
        }

        var _ = new MessageRetryConfigurationObserver(configurator, stoppingToken, configure);
    }

    /// <summary>
    /// Configures retry once for every message type handled by a consumer, handler, or saga. The retry
    /// filter runs before the consumer factory or saga repository and is cancelled when the bus stops.
    /// </summary>
    /// <param name="configurator"></param>
    /// <param name="connector">
    /// The bus factory configurator, to connect the observer, to cancel retries if the bus is stopped
    /// </param>
    /// <param name="configure"></param>
    public static void UseMessageRetry(this IConsumePipeConfigurator configurator, IBusFactoryConfigurator connector,
        Action<IRetryConfigurator> configure)
    {
        ArgumentNullException.ThrowIfNull(configurator);
        ArgumentNullException.ThrowIfNull(connector);
        ArgumentNullException.ThrowIfNull(configure);

        var retryObserver = new RetryBusObserver();
        connector.ConnectBusObserver(retryObserver);

        var _ = new MessageRetryConfigurationObserver(configurator, retryObserver.Stopping, configure);
    }

    /// <summary>
    /// Configures retry for every message type handled by the consumer.
    /// </summary>
    /// <param name="configurator"></param>
    /// <param name="configure"></param>
    public static void UseMessageRetry<TConsumer>(this IConsumerConfigurator<TConsumer> configurator, Action<IRetryConfigurator> configure)
        where TConsumer : class
    {
        ArgumentNullException.ThrowIfNull(configurator);
        ArgumentNullException.ThrowIfNull(configure);

        var observer = new MessageRetryConsumerConfigurationObserver<TConsumer>(configurator, CancellationToken.None, configure);
        configurator.ConnectConsumerConfigurationObserver(observer);
    }

    /// <summary>
    /// Configures retry for every message type handled by the consumer and cancels it when the bus stops.
    /// </summary>
    /// <param name="configurator"></param>
    /// <param name="busFactoryConfigurator">
    /// The bus factory configurator, to connect the observer, to cancel retries if the bus is stopped
    /// </param>
    /// <param name="configure"></param>
    public static void UseMessageRetry<TConsumer>(this IConsumerConfigurator<TConsumer> configurator, IBusFactoryConfigurator busFactoryConfigurator,
        Action<IRetryConfigurator> configure)
        where TConsumer : class
    {
        ArgumentNullException.ThrowIfNull(configurator);
        ArgumentNullException.ThrowIfNull(busFactoryConfigurator);
        ArgumentNullException.ThrowIfNull(configure);

        var retryObserver = new RetryBusObserver();
        busFactoryConfigurator.ConnectBusObserver(retryObserver);

        var observer = new MessageRetryConsumerConfigurationObserver<TConsumer>(configurator, retryObserver.Stopping, configure);
        configurator.ConnectConsumerConfigurationObserver(observer);
    }

    /// <summary>
    /// Configures retry for every message type handled by the saga before the saga repository.
    /// </summary>
    /// <param name="configurator"></param>
    /// <param name="configure"></param>
    public static void UseMessageRetry<TSaga>(this ISagaConfigurator<TSaga> configurator, Action<IRetryConfigurator> configure)
        where TSaga : class, ISaga
    {
        ArgumentNullException.ThrowIfNull(configurator);
        ArgumentNullException.ThrowIfNull(configure);

        var observer = new MessageRetrySagaConfigurationObserver<TSaga>(configurator, CancellationToken.None, configure);
        configurator.ConnectSagaConfigurationObserver(observer);
    }

    /// <summary>
    /// Configures retry for every message type handled by the saga before the saga repository and
    /// cancels it when the bus stops.
    /// </summary>
    /// <param name="configurator"></param>
    /// <param name="busFactoryConfigurator">
    /// The bus factory configurator, to connect the observer, to cancel retries if the bus is stopped
    /// </param>
    /// <param name="configure"></param>
    public static void UseMessageRetry<TSaga>(this ISagaConfigurator<TSaga> configurator, IBusFactoryConfigurator busFactoryConfigurator,
        Action<IRetryConfigurator> configure)
        where TSaga : class, ISaga
    {
        ArgumentNullException.ThrowIfNull(configurator);
        ArgumentNullException.ThrowIfNull(busFactoryConfigurator);
        ArgumentNullException.ThrowIfNull(configure);

        var retryObserver = new RetryBusObserver();
        busFactoryConfigurator.ConnectBusObserver(retryObserver);

        var observer = new MessageRetrySagaConfigurationObserver<TSaga>(configurator, retryObserver.Stopping, configure);
        configurator.ConnectSagaConfigurationObserver(observer);
    }

    /// <summary>
    /// Configures retry for the handler's message type.
    /// </summary>
    /// <param name="configurator"></param>
    /// <param name="configure"></param>
    public static void UseMessageRetry<TMessage>(this IHandlerConfigurator<TMessage> configurator, Action<IRetryConfigurator> configure)
        where TMessage : class
    {
        ArgumentNullException.ThrowIfNull(configurator);
        ArgumentNullException.ThrowIfNull(configure);

        var observer = new MessageRetryHandlerConfigurationObserver(CancellationToken.None, configure);
        configurator.ConnectHandlerConfigurationObserver(observer);
    }

    /// <summary>
    /// Configures retry for the handler's message type and cancels it when the bus stops.
    /// </summary>
    /// <param name="configurator"></param>
    /// <param name="busFactoryConfigurator">
    /// The bus factory configurator, to connect the observer, to cancel retries if the bus is stopped
    /// </param>
    /// <param name="configure"></param>
    public static void UseMessageRetry<TMessage>(this IHandlerConfigurator<TMessage> configurator, IBusFactoryConfigurator busFactoryConfigurator,
        Action<IRetryConfigurator> configure)
        where TMessage : class
    {
        ArgumentNullException.ThrowIfNull(configurator);
        ArgumentNullException.ThrowIfNull(busFactoryConfigurator);
        ArgumentNullException.ThrowIfNull(configure);

        var retryObserver = new RetryBusObserver();
        busFactoryConfigurator.ConnectBusObserver(retryObserver);

        var observer = new MessageRetryHandlerConfigurationObserver(retryObserver.Stopping, configure);
        configurator.ConnectHandlerConfigurationObserver(observer);
    }
}

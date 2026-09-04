using System;
using ViciOne.ServiceBus.Configuration;

#nullable enable
namespace ViciOne.ServiceBus.Configuration;

/// <summary>
/// Provides extension methods for timeout configurator.
/// </summary>
public static class TimeoutConfiguratorExtensions
{
    /// <summary>
    /// Cancels context's CancellationToken once timeout is reached.
    /// </summary>
    /// <param name="configurator">The pipe configurator</param>
    /// <param name="configure">Configure timeout</param>
    public static void UseTimeout<T>(this IPipeConfigurator<ConsumeContext<T>> configurator, Action<ITimeoutConfigurator> configure)
        where T : class
    {
        ArgumentNullException.ThrowIfNull(configurator);
        ArgumentNullException.ThrowIfNull(configure);

        var specification = new TimeoutSpecification<T>();

        configure(specification);

        configurator.AddPipeSpecification(specification);
    }

    /// <summary>
    /// Cancels context's CancellationToken once timeout is reached.
    /// </summary>
    /// <param name="configurator">The pipe configurator</param>
    /// <param name="configure">Configure timeout</param>
    public static void UseTimeout(this IConsumePipeConfigurator configurator, Action<ITimeoutConfigurator> configure)
    {
        ArgumentNullException.ThrowIfNull(configurator);
        ArgumentNullException.ThrowIfNull(configure);

        _ = new TimeoutConfigurationObserver(configurator, configure);
    }

    /// <summary>
    /// Cancels context's CancellationToken once timeout is reached.
    /// </summary>
    /// <param name="configurator">The pipe configurator</param>
    /// <param name="configure">Configure timeout</param>
    public static void UseTimeout<TConsumer>(this IConsumerConfigurator<TConsumer> configurator, Action<ITimeoutConfigurator> configure)
        where TConsumer : class
    {
        ArgumentNullException.ThrowIfNull(configurator);
        ArgumentNullException.ThrowIfNull(configure);

        var observer = new TimeoutConsumerConfigurationObserver<TConsumer>(configurator, configure);
        configurator.ConnectConsumerConfigurationObserver(observer);
    }

    /// <summary>
    /// Cancels context's CancellationToken once timeout is reached.
    /// </summary>
    /// <param name="configurator">The pipe configurator</param>
    /// <param name="configure">Configure timeout</param>
    public static void UseTimeout<TSaga>(this ISagaConfigurator<TSaga> configurator, Action<ITimeoutConfigurator> configure)
        where TSaga : class, ISaga
    {
        ArgumentNullException.ThrowIfNull(configurator);
        ArgumentNullException.ThrowIfNull(configure);

        var observer = new TimeoutSagaConfigurationObserver<TSaga>(configurator, configure);
        configurator.ConnectSagaConfigurationObserver(observer);
    }

    /// <summary>
    /// Cancels context's CancellationToken once timeout is reached.
    /// </summary>
    /// <param name="configurator">The pipe configurator</param>
    /// <param name="configure">Configure timeout</param>
    public static void UseTimeout<TMessage>(this IHandlerConfigurator<TMessage> configurator, Action<ITimeoutConfigurator> configure)
        where TMessage : class
    {
        ArgumentNullException.ThrowIfNull(configurator);
        ArgumentNullException.ThrowIfNull(configure);

        var observer = new TimeoutHandlerConfigurationObserver(configure);
        configurator.ConnectHandlerConfigurationObserver(observer);
    }
}

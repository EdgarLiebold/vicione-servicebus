using System;
using System.Threading;
using ViciOne.ServiceBus.Configuration;
using ViciOne.ServiceBus.RetryPolicies;

namespace ViciOne.ServiceBus.Configuration;

/// <summary>Provides extension methods for scheduled redelivery configuration.</summary>
public static class ScheduledRedeliveryConfigurationExtensions
{
    /// <summary>Uses the configured message scheduler to redeliver a message according to the retry policy.</summary>
    /// <typeparam name="T">The value type.</typeparam>
    /// <param name="configurator">The configurator to update.</param>
    /// <param name="configure">The callback used to configure the component.</param>
    public static void UseScheduledRedelivery<T>(this IPipeConfigurator<ConsumeContext<T>> configurator, Action<IRetryConfigurator> configure)
        where T : class
    {
        if (configurator == null)
            throw new ArgumentNullException(nameof(configurator));

        var redeliverySpecification = new ScheduledRedeliveryPipeSpecification<T>();

        configurator.AddPipeSpecification(redeliverySpecification);

        var retrySpecification = new RedeliveryRetryPipeSpecification<T>(redeliverySpecification);

        configure?.Invoke(retrySpecification);

        configurator.AddPipeSpecification(retrySpecification);
    }

    /// <summary>Use the message scheduler to schedule redelivery of a specific message type based upon the retry policy.</summary>
    /// <typeparam name="T">The value type.</typeparam>
    /// <param name="configurator">The configurator to update.</param>
    /// <param name="retryPolicy">The retry policy.</param>
    public static void UseScheduledRedelivery<T>(this IPipeConfigurator<ConsumeContext<T>> configurator, IRetryPolicy retryPolicy)
        where T : class
    {
        if (configurator == null)
            throw new ArgumentNullException(nameof(configurator));

        var redeliverySpecification = new ScheduledRedeliveryPipeSpecification<T>();

        configurator.AddPipeSpecification(redeliverySpecification);

        var retrySpecification = new RedeliveryRetryPipeSpecification<T>(redeliverySpecification);

        retrySpecification.SetRetryPolicy(exceptionFilter =>
            new ConsumeContextRetryPolicy<ConsumeContext<T>, RetryConsumeContext<T>>(retryPolicy, CancellationToken.None, Factory));

        configurator.AddPipeSpecification(retrySpecification);
    }

    static RetryConsumeContext<T> Factory<T>(ConsumeContext<T> context, IRetryPolicy retryPolicy, RetryContext? retryContext)
        where T : class
    {
        return new RetryConsumeContext<T>(context, retryPolicy, retryContext);
    }

    /// <summary>Configure scheduled redelivery for all message types.</summary>
    /// <param name="configurator">The configurator to update.</param>
    /// <param name="configureRetry">The configure retry.</param>
    public static void UseScheduledRedelivery(this IConsumePipeConfigurator configurator, Action<IRetryConfigurator> configureRetry)
    {
        if (configurator == null)
            throw new ArgumentNullException(nameof(configurator));

        if (configureRetry == null)
            throw new ArgumentNullException(nameof(configureRetry));

        var observer = new ScheduledRedeliveryConfigurationObserver(configurator, configureRetry);
    }

    /// <summary>Configure scheduled redelivery for the consumer, regardless of message type.</summary>
    /// <typeparam name="TConsumer">The consumer implementation used by the member.</typeparam>
    /// <param name="configurator">The configurator to update.</param>
    /// <param name="configure">The callback used to configure the component.</param>
    public static void UseScheduledRedelivery<TConsumer>(this IConsumerConfigurator<TConsumer> configurator, Action<IRetryConfigurator> configure)
        where TConsumer : class
    {
        if (configurator == null)
            throw new ArgumentNullException(nameof(configurator));

        var observer = new ScheduledRedeliveryConsumerConfigurationObserver<TConsumer>(configurator, configure);
        configurator.ConnectConsumerConfigurationObserver(observer);
    }

    /// <summary>Configures scheduled redelivery for the message type handled by this handler.</summary>
    /// <typeparam name="TMessage">The message contract processed by the member.</typeparam>
    /// <param name="configurator">The configurator to update.</param>
    /// <param name="configure">The callback used to configure the component.</param>
    public static void UseScheduledRedelivery<TMessage>(this IHandlerConfigurator<TMessage> configurator, Action<IRetryConfigurator> configure)
        where TMessage : class
    {
        if (configurator == null)
            throw new ArgumentNullException(nameof(configurator));

        var observer = new ScheduledRedeliveryHandlerConfigurationObserver(configure);
        configurator.ConnectHandlerConfigurationObserver(observer);
    }
}

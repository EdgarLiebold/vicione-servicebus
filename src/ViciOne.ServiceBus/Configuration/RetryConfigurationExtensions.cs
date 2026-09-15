using System;
using System.Linq;
using ViciOne.ServiceBus.Configuration;
using ViciOne.ServiceBus.Middleware;
using ViciOne.ServiceBus.RetryPolicies;

namespace ViciOne.ServiceBus.Configuration;

/// <summary>Adds retry policies to explicit pipe and consume-pipeline scopes.</summary>
public static class RetryConfigurationExtensions
{
    /// <summary>Adds retry handling to an untyped consume pipeline.</summary>
    /// <param name="configurator">The consume-pipeline configurator.</param>
    /// <param name="configure">Configures exception selection and retry timing.</param>
    public static void UseMessageRetry(this IPipeConfigurator<ConsumeContext> configurator, Action<IRetryConfigurator> configure)
    {
        ArgumentNullException.ThrowIfNull(configurator);
        ArgumentNullException.ThrowIfNull(configure);

        var specification = new ConsumeContextRetryPipeSpecification();

        configure(specification);

        configurator.AddPipeSpecification(specification);
    }

    /// <summary>Adds retry handling to a message-specific consume pipeline.</summary>
    /// <typeparam name="T">The consumed message type.</typeparam>
    /// <param name="configurator">The message consume-pipeline configurator.</param>
    /// <param name="configure">Configures exception selection and retry timing.</param>
    public static void UseMessageRetry<T>(this IPipeConfigurator<ConsumeContext<T>> configurator, Action<IRetryConfigurator> configure)
        where T : class
    {
        ArgumentNullException.ThrowIfNull(configurator);
        ArgumentNullException.ThrowIfNull(configure);

        var specification = new ConsumeContextRetryPipeSpecification<ConsumeContext<T>, RetryConsumeContext<T>>(Factory);

        configure(specification);

        configurator.AddPipeSpecification(specification);
    }

    /// <summary>Adds retry handling for one message type to a consume pipeline.</summary>
    /// <typeparam name="T">The consumed message type.</typeparam>
    /// <param name="configurator">The consume-pipeline configurator.</param>
    /// <param name="configure">Configures exception selection and retry timing.</param>
    public static void UseMessageRetry<T>(this IConsumePipeConfigurator configurator, Action<IRetryConfigurator> configure)
        where T : class
    {
        ArgumentNullException.ThrowIfNull(configurator);
        ArgumentNullException.ThrowIfNull(configure);

        var specification = new ConsumeContextRetryPipeSpecification<ConsumeContext<T>, RetryConsumeContext<T>>(Factory);

        configure(specification);

        configurator.AddPipeSpecification(specification);
    }

    static RetryConsumeContext<T> Factory<T>(ConsumeContext<T> context, IRetryPolicy retryPolicy, RetryContext? retryContext)
        where T : class
    {
        return new RetryConsumeContext<T>(context, retryPolicy, retryContext);
    }

    /// <summary>Adds retry handling to a consumer-specific pipeline.</summary>
    /// <typeparam name="TConsumer">The consumer type.</typeparam>
    /// <param name="configurator">The consumer-pipeline configurator.</param>
    /// <param name="configure">Configures exception selection and retry timing.</param>
    public static void UseMessageRetry<TConsumer>(this IPipeConfigurator<ConsumerConsumeContext<TConsumer>> configurator, Action<IRetryConfigurator> configure)
        where TConsumer : class
    {
        ArgumentNullException.ThrowIfNull(configurator);
        ArgumentNullException.ThrowIfNull(configure);

        var specification =
            new ConsumeContextRetryPipeSpecification<ConsumerConsumeContext<TConsumer>, RetryConsumerConsumeContext<TConsumer>>(Factory);

        configure(specification);

        configurator.AddPipeSpecification(specification);
    }

    static RetryConsumerConsumeContext<TConsumer> Factory<TConsumer>(ConsumerConsumeContext<TConsumer> context, IRetryPolicy retryPolicy,
        RetryContext? retryContext)
        where TConsumer : class
    {
        return new RetryConsumerConsumeContext<TConsumer>(context, retryPolicy, retryContext);
    }

    /// <summary>Adds retry handling to a pipeline.</summary>
    /// <typeparam name="T">The pipeline context type.</typeparam>
    /// <param name="configurator">The pipeline configurator.</param>
    /// <param name="configure">Configures exception selection and retry timing.</param>
    public static void UseRetry<T>(this IPipeConfigurator<T> configurator, Action<IRetryConfigurator> configure)
        where T : class, PipeContext
    {
        ArgumentNullException.ThrowIfNull(configurator);
        ArgumentNullException.ThrowIfNull(configure);

        var specification = new RetryPipeSpecification<T>();

        configure(specification);

        configurator.AddPipeSpecification(specification);
    }

    /// <summary>Adds retry handling that is canceled when the bus stops.</summary>
    /// <param name="configurator">The consume-pipeline configurator.</param>
    /// <param name="connector">The bus factory used to observe shutdown.</param>
    /// <param name="configure">Configures exception selection and retry timing.</param>
    public static void UseMessageRetry(this IPipeConfigurator<ConsumeContext> configurator, IBusFactoryConfigurator connector,
        Action<IRetryConfigurator> configure)
    {
        ArgumentNullException.ThrowIfNull(configurator);
        ArgumentNullException.ThrowIfNull(connector);
        ArgumentNullException.ThrowIfNull(configure);

        var observer = new RetryBusObserver();
        connector.ConnectBusObserver(observer);

        var specification = new ConsumeContextRetryPipeSpecification(observer.Stopping);

        configure(specification);

        configurator.AddPipeSpecification(specification);
    }

    /// <summary>Adds message-specific retry handling that is canceled when the bus stops.</summary>
    /// <typeparam name="T">The consumed message type.</typeparam>
    /// <param name="configurator">The message consume-pipeline configurator.</param>
    /// <param name="connector">The bus factory used to observe shutdown.</param>
    /// <param name="configure">Configures exception selection and retry timing.</param>
    public static void UseMessageRetry<T>(this IPipeConfigurator<ConsumeContext<T>> configurator, IBusFactoryConfigurator connector,
        Action<IRetryConfigurator> configure)
        where T : class
    {
        ArgumentNullException.ThrowIfNull(configurator);
        ArgumentNullException.ThrowIfNull(connector);
        ArgumentNullException.ThrowIfNull(configure);

        var observer = new RetryBusObserver();
        connector.ConnectBusObserver(observer);

        var specification = new ConsumeContextRetryPipeSpecification<ConsumeContext<T>, RetryConsumeContext<T>>(Factory, observer.Stopping);

        configure(specification);

        configurator.AddPipeSpecification(specification);
    }

    /// <summary>Adds consumer-specific retry handling that is canceled when the bus stops.</summary>
    /// <typeparam name="TConsumer">The consumer type.</typeparam>
    /// <param name="configurator">The consumer-pipeline configurator.</param>
    /// <param name="connector">The bus factory used to observe shutdown.</param>
    /// <param name="configure">Configures exception selection and retry timing.</param>
    public static void UseMessageRetry<TConsumer>(this IPipeConfigurator<ConsumerConsumeContext<TConsumer>> configurator, IBusFactoryConfigurator connector,
        Action<IRetryConfigurator> configure)
        where TConsumer : class
    {
        ArgumentNullException.ThrowIfNull(configurator);
        ArgumentNullException.ThrowIfNull(connector);
        ArgumentNullException.ThrowIfNull(configure);

        var observer = new RetryBusObserver();
        connector.ConnectBusObserver(observer);

        var specification =
            new ConsumeContextRetryPipeSpecification<ConsumerConsumeContext<TConsumer>, RetryConsumerConsumeContext<TConsumer>>(Factory, observer.Stopping);

        configure(specification);

        configurator.AddPipeSpecification(specification);
    }

    /// <summary>Configures the pipeline to make no retry attempts.</summary>
    /// <param name="configurator">The retry-policy configurator.</param>
    /// <returns>The same configurator for fluent configuration.</returns>
    public static IRetryPolicyConfigurator None(this IRetryPolicyConfigurator configurator)
    {
        ArgumentNullException.ThrowIfNull(configurator);

        configurator.SetRetryPolicy(filter => new NoRetryPolicy(filter));

        return configurator;
    }

    /// <summary>Configures the specified number of immediate retry attempts.</summary>
    /// <param name="configurator">The retry-policy configurator.</param>
    /// <param name="retryLimit">The number of retries to attempt.</param>
    /// <returns>The same configurator for fluent configuration.</returns>
    public static IRetryPolicyConfigurator Immediate(this IRetryPolicyConfigurator configurator, int retryLimit)
    {
        ArgumentNullException.ThrowIfNull(configurator);

        configurator.SetRetryPolicy(filter => new ImmediateRetryPolicy(filter, retryLimit));

        return configurator;
    }

    /// <summary>Configures a retry attempt for every delay in the explicit schedule.</summary>
    /// <param name="configurator">The retry-policy configurator.</param>
    /// <param name="intervals">The delay before each retry attempt.</param>
    /// <returns>The same configurator for fluent configuration.</returns>
    public static IRetryPolicyConfigurator Intervals(this IRetryPolicyConfigurator configurator, params TimeSpan[] intervals)
    {
        ArgumentNullException.ThrowIfNull(configurator);
        ArgumentNullException.ThrowIfNull(intervals);
        TimeSpan[] schedule = [.. intervals];

        configurator.SetRetryPolicy(filter => new IntervalRetryPolicy(filter, schedule));

        return configurator;
    }

    /// <summary>Configures a fixed delay for the specified number of retry attempts.</summary>
    /// <param name="configurator">The retry-policy configurator.</param>
    /// <param name="retryCount">The number of retry attempts.</param>
    /// <param name="interval">The interval between each retry attempt.</param>
    /// <returns>The same configurator for fluent configuration.</returns>
    public static IRetryPolicyConfigurator Interval(this IRetryPolicyConfigurator configurator, int retryCount, TimeSpan interval)
    {
        ArgumentNullException.ThrowIfNull(configurator);
        ArgumentOutOfRangeException.ThrowIfNegativeOrZero(retryCount);

        configurator.SetRetryPolicy(filter => new IntervalRetryPolicy(filter, Enumerable.Repeat(interval, retryCount).ToArray()));

        return configurator;
    }

    /// <summary>Configures bounded exponentially increasing jittered retry delays.</summary>
    /// <param name="configurator">The retry-policy configurator.</param>
    /// <param name="retryLimit">The maximum number of retry attempts.</param>
    /// <param name="minInterval">The minimum retry delay.</param>
    /// <param name="maxInterval">The maximum retry delay.</param>
    /// <param name="intervalDelta">The base exponential delay increment.</param>
    /// <returns>The same configurator for fluent configuration.</returns>
    public static IRetryPolicyConfigurator Exponential(this IRetryPolicyConfigurator configurator, int retryLimit, TimeSpan minInterval, TimeSpan maxInterval,
        TimeSpan intervalDelta)
    {
        ArgumentNullException.ThrowIfNull(configurator);

        configurator.SetRetryPolicy(filter => new ExponentialRetryPolicy(filter, retryLimit, minInterval, maxInterval, intervalDelta));

        return configurator;
    }

    /// <summary>Configures a linearly increasing delay between retry attempts.</summary>
    /// <param name="configurator">The retry-policy configurator.</param>
    /// <param name="retryLimit">The number of retry attempts.</param>
    /// <param name="initialInterval">The initial retry interval.</param>
    /// <param name="intervalIncrement">The interval to add to the retry interval with each subsequent retry.</param>
    /// <returns>The same configurator for fluent configuration.</returns>
    public static IRetryPolicyConfigurator Incremental(this IRetryPolicyConfigurator configurator, int retryLimit, TimeSpan initialInterval, TimeSpan intervalIncrement)
    {
        ArgumentNullException.ThrowIfNull(configurator);

        configurator.SetRetryPolicy(filter => new IncrementalRetryPolicy(filter, retryLimit, initialInterval, intervalIncrement));

        return configurator;
    }
}

using System;
using System.Text;
using System.Threading;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.DependencyInjection.Extensions;
using ViciOne.ServiceBus.Configuration;
using ViciOne.ServiceBus.Contracts;
using ViciOne.ServiceBus.Middleware;
using ViciOne.ServiceBus.RetryPolicies;

namespace ViciOne.ServiceBus.Sagas.Configuration;

/// <summary>
/// Configures middleware that is specific to saga pipelines.
/// </summary>
public static class SagaPipelineConfigurationExtensions
{
    /// <summary>
    /// Limits the number of concurrently consumed saga messages.
    /// </summary>
    /// <typeparam name="TSaga">The saga type.</typeparam>
    /// <param name="configurator">The saga configurator.</param>
    /// <param name="concurrentMessageLimit">The maximum number of concurrently consumed messages.</param>
    public static void UseConcurrentMessageLimit<TSaga>(this ISagaConfigurator<TSaga> configurator, int concurrentMessageLimit)
        where TSaga : class, ISaga
    {
        ArgumentNullException.ThrowIfNull(configurator);

        var observer = new ConcurrencyLimitSagaConfigurationObserver<TSaga>(configurator, concurrentMessageLimit);
        configurator.ConnectSagaConfigurationObserver(observer);
    }

    /// <summary>
    /// Limits the number of concurrently consumed saga messages and exposes runtime limit management.
    /// </summary>
    /// <typeparam name="TSaga">The saga type.</typeparam>
    /// <param name="configurator">The saga configurator.</param>
    /// <param name="concurrentMessageLimit">The initial concurrency limit.</param>
    /// <param name="managementEndpointConfigurator">The management endpoint configurator.</param>
    /// <param name="id">The optional identifier used to select the limit at runtime.</param>
    public static void UseConcurrentMessageLimit<TSaga>(this ISagaConfigurator<TSaga> configurator, int concurrentMessageLimit,
        IReceiveEndpointConfigurator managementEndpointConfigurator, string? id = null)
        where TSaga : class, ISaga
    {
        ArgumentNullException.ThrowIfNull(configurator);
        ArgumentNullException.ThrowIfNull(managementEndpointConfigurator);

        var observer = new ConcurrencyLimitSagaConfigurationObserver<TSaga>(configurator, concurrentMessageLimit, id);
        configurator.ConnectSagaConfigurationObserver(observer);

        managementEndpointConfigurator.Instance(observer.Limiter, x =>
        {
            x.UseConcurrentMessageLimit(1);
            x.Message<SetConcurrencyLimit>(m => m.UseMessageRetry(r => r.None()));
        });
    }

    /// <summary>
    /// Configures delayed redelivery for every message handled by a saga.
    /// </summary>
    /// <typeparam name="TSaga">The saga type.</typeparam>
    /// <param name="configurator">The saga configurator.</param>
    /// <param name="configure">The redelivery policy callback.</param>
    public static void UseDelayedRedelivery<TSaga>(this ISagaConfigurator<TSaga> configurator, Action<IRedeliveryConfigurator> configure)
        where TSaga : class, ISaga
    {
        ArgumentNullException.ThrowIfNull(configurator);
        ArgumentNullException.ThrowIfNull(configure);

        var observer = new DelayedRedeliverySagaConfigurationObserver<TSaga>(configurator, configure);
        configurator.ConnectSagaConfigurationObserver(observer);
    }

    /// <summary>
    /// Adds an in-memory outbox to a saga pipeline using the supplied registration context.
    /// </summary>
    /// <typeparam name="TSaga">The saga type.</typeparam>
    /// <param name="configurator">The saga configurator.</param>
    /// <param name="context">The registration context.</param>
    /// <param name="configure">The optional outbox callback.</param>
    public static void UseVolatileOutbox<TSaga>(this ISagaConfigurator<TSaga> configurator, IRegistrationContext context,
        Action<IOutboxConfigurator>? configure = null)
        where TSaga : class, ISaga
    {
        ArgumentNullException.ThrowIfNull(configurator);
        ArgumentNullException.ThrowIfNull(context);

        var observer = new InMemoryOutboxSagaConfigurationObserver<TSaga>(context, configurator, configure);
        configurator.ConnectSagaConfigurationObserver(observer);
    }

    /// <summary>
    /// Adds an in-memory outbox to a saga pipeline.
    /// </summary>
    /// <typeparam name="TSaga">The saga type.</typeparam>
    /// <param name="configurator">The saga configurator.</param>
    /// <param name="configure">The optional outbox callback.</param>
    public static void UseVolatileOutbox<TSaga>(this ISagaConfigurator<TSaga> configurator, Action<IOutboxConfigurator>? configure = null)
        where TSaga : class, ISaga
    {
        ArgumentNullException.ThrowIfNull(configurator);

        var observer = new InMemoryOutboxSagaConfigurationObserver<TSaga>((ISetScopedConsumeContext?)null, configurator, configure);
        configurator.ConnectSagaConfigurationObserver(observer);
    }

    /// <summary>
    /// Configures retry for every message handled by a saga.
    /// </summary>
    /// <typeparam name="TSaga">The saga type.</typeparam>
    /// <param name="configurator">The saga configurator.</param>
    /// <param name="configure">The retry policy callback.</param>
    public static void UseMessageRetry<TSaga>(this ISagaConfigurator<TSaga> configurator, Action<IRetryConfigurator> configure)
        where TSaga : class, ISaga
    {
        ArgumentNullException.ThrowIfNull(configurator);
        ArgumentNullException.ThrowIfNull(configure);

        var observer = new MessageRetrySagaConfigurationObserver<TSaga>(configurator, CancellationToken.None, configure);
        configurator.ConnectSagaConfigurationObserver(observer);
    }

    /// <summary>
    /// Configures retry for every message handled by a saga and cancels retry waits when the bus stops.
    /// </summary>
    /// <typeparam name="TSaga">The saga type.</typeparam>
    /// <param name="configurator">The saga configurator.</param>
    /// <param name="busFactoryConfigurator">The bus configurator that supplies the stopping signal.</param>
    /// <param name="configure">The retry policy callback.</param>
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
    /// Adds retry middleware inside a saga repository pipeline.
    /// </summary>
    /// <typeparam name="TSaga">The saga type.</typeparam>
    /// <param name="configurator">The saga pipe configurator.</param>
    /// <param name="configure">The retry policy callback.</param>
    public static void UseMessageRetry<TSaga>(this IPipeConfigurator<SagaConsumeContext<TSaga>> configurator,
        Action<IRetryConfigurator> configure)
        where TSaga : class, ISaga
    {
        ArgumentNullException.ThrowIfNull(configurator);
        ArgumentNullException.ThrowIfNull(configure);

        var specification =
            new ConsumeContextRetryPipeSpecification<SagaConsumeContext<TSaga>, RetrySagaConsumeContext<TSaga>>(CreateRetryContext);
        configure(specification);
        configurator.AddPipeSpecification(specification);
    }

    /// <summary>
    /// Adds retry middleware inside a saga repository pipeline and cancels retry waits when the bus stops.
    /// </summary>
    /// <typeparam name="TSaga">The saga type.</typeparam>
    /// <param name="configurator">The saga pipe configurator.</param>
    /// <param name="connector">The bus configurator that supplies the stopping signal.</param>
    /// <param name="configure">The retry policy callback.</param>
    public static void UseMessageRetry<TSaga>(this IPipeConfigurator<SagaConsumeContext<TSaga>> configurator,
        IBusFactoryConfigurator connector, Action<IRetryConfigurator> configure)
        where TSaga : class, ISaga
    {
        ArgumentNullException.ThrowIfNull(configurator);
        ArgumentNullException.ThrowIfNull(connector);
        ArgumentNullException.ThrowIfNull(configure);

        var observer = new RetryBusObserver();
        connector.ConnectBusObserver(observer);

        var specification =
            new ConsumeContextRetryPipeSpecification<SagaConsumeContext<TSaga>, RetrySagaConsumeContext<TSaga>>(
                CreateRetryContext, observer.Stopping);
        configure(specification);
        configurator.AddPipeSpecification(specification);
    }

    /// <summary>
    /// Configures scheduled redelivery for every message handled by a saga.
    /// </summary>
    /// <typeparam name="TSaga">The saga type.</typeparam>
    /// <param name="configurator">The saga configurator.</param>
    /// <param name="configure">The redelivery policy callback.</param>
    public static void UseScheduledRedelivery<TSaga>(this ISagaConfigurator<TSaga> configurator, Action<IRetryConfigurator> configure)
        where TSaga : class, ISaga
    {
        ArgumentNullException.ThrowIfNull(configurator);
        ArgumentNullException.ThrowIfNull(configure);

        var observer = new ScheduledRedeliverySagaConfigurationObserver<TSaga>(configurator, configure);
        configurator.ConnectSagaConfigurationObserver(observer);
    }

    /// <summary>
    /// Applies a timeout to every message handled by a saga.
    /// </summary>
    /// <typeparam name="TSaga">The saga type.</typeparam>
    /// <param name="configurator">The saga configurator.</param>
    /// <param name="configure">The timeout callback.</param>
    public static void UseTimeout<TSaga>(this ISagaConfigurator<TSaga> configurator, Action<ITimeoutConfigurator> configure)
        where TSaga : class, ISaga
    {
        ArgumentNullException.ThrowIfNull(configurator);
        ArgumentNullException.ThrowIfNull(configure);

        var observer = new TimeoutSagaConfigurationObserver<TSaga>(configurator, configure);
        configurator.ConnectSagaConfigurationObserver(observer);
    }

    /// <summary>
    /// Routes exceptions from a saga pipeline through the supplied rescue pipe.
    /// </summary>
    /// <typeparam name="TSaga">The saga type.</typeparam>
    /// <param name="configurator">The saga pipe configurator.</param>
    /// <param name="rescuePipe">The exception pipe.</param>
    /// <param name="configure">The optional rescue callback.</param>
    public static void UseRescue<TSaga>(this IPipeConfigurator<SagaConsumeContext<TSaga>> configurator,
        IPipe<ExceptionSagaConsumeContext<TSaga>> rescuePipe, Action<IExceptionConfigurator>? configure = null)
        where TSaga : class, ISaga
    {
        ArgumentNullException.ThrowIfNull(configurator);
        ArgumentNullException.ThrowIfNull(rescuePipe);

        var specification = new SagaConsumeContextRescuePipeSpecification<TSaga>(rescuePipe);
        configure?.Invoke(specification);
        configurator.AddPipeSpecification(specification);
    }

    /// <summary>
    /// Partitions saga messages by a <see cref="Guid" /> key.
    /// </summary>
    /// <typeparam name="TSaga">The saga type.</typeparam>
    /// <param name="configurator">The saga pipe configurator.</param>
    /// <param name="partitionCount">The number of partitions.</param>
    /// <param name="keyProvider">The partition-key provider.</param>
    public static void UsePartitioner<TSaga>(this IPipeConfigurator<SagaConsumeContext<TSaga>> configurator,
        int partitionCount, Func<SagaConsumeContext<TSaga>, Guid> keyProvider)
        where TSaga : class, ISaga
    {
        ArgumentNullException.ThrowIfNull(configurator);
        ArgumentNullException.ThrowIfNull(keyProvider);

        ConfigurePartitioner(configurator, partitionCount, context => keyProvider(context).ToByteArray());
    }

    /// <summary>
    /// Partitions saga messages by a text key.
    /// </summary>
    /// <typeparam name="TSaga">The saga type.</typeparam>
    /// <param name="configurator">The saga pipe configurator.</param>
    /// <param name="partitionCount">The number of partitions.</param>
    /// <param name="keyProvider">The partition-key provider.</param>
    /// <param name="encoding">The optional key encoding; UTF-8 is used by default.</param>
    public static void UsePartitioner<TSaga>(this IPipeConfigurator<SagaConsumeContext<TSaga>> configurator,
        int partitionCount, Func<SagaConsumeContext<TSaga>, string> keyProvider, Encoding? encoding = null)
        where TSaga : class, ISaga
    {
        ArgumentNullException.ThrowIfNull(configurator);
        ArgumentNullException.ThrowIfNull(keyProvider);

        Encoding textEncoding = encoding ?? Encoding.UTF8;
        ConfigurePartitioner(configurator, partitionCount, context =>
        {
            string key = keyProvider(context)
                ?? throw new InvalidOperationException("The partition key provider returned null.");
            return textEncoding.GetBytes(key);
        });
    }

    /// <summary>
    /// Configures a registered saga by runtime type on a receive endpoint.
    /// </summary>
    /// <param name="configurator">The receive endpoint configurator.</param>
    /// <param name="registration">The registration context.</param>
    /// <param name="sagaType">The saga type.</param>
    public static void ConfigureSaga(this IReceiveEndpointConfigurator configurator, IRegistrationContext registration, Type sagaType)
    {
        ArgumentNullException.ThrowIfNull(configurator);
        ArgumentNullException.ThrowIfNull(registration);
        ArgumentNullException.ThrowIfNull(sagaType);

        registration.ConfigureSaga(sagaType, configurator);
    }

    /// <summary>
    /// Configures a registered saga on a receive endpoint.
    /// </summary>
    /// <typeparam name="TSaga">The saga type.</typeparam>
    /// <param name="configurator">The receive endpoint configurator.</param>
    /// <param name="registration">The registration context.</param>
    /// <param name="configure">The optional saga callback.</param>
    public static void ConfigureSaga<TSaga>(this IReceiveEndpointConfigurator configurator, IRegistrationContext registration,
        Action<ISagaConfigurator<TSaga>>? configure = null)
        where TSaga : class, ISaga
    {
        ArgumentNullException.ThrowIfNull(configurator);
        ArgumentNullException.ThrowIfNull(registration);

        registration.ConfigureSaga(configurator, configure);
    }

    /// <summary>
    /// Configures every registered saga on a receive endpoint.
    /// </summary>
    /// <param name="configurator">The receive endpoint configurator.</param>
    /// <param name="registration">The registration context.</param>
    public static void ConfigureSagas(this IReceiveEndpointConfigurator configurator, IRegistrationContext registration)
    {
        ArgumentNullException.ThrowIfNull(configurator);
        ArgumentNullException.ThrowIfNull(registration);

        registration.ConfigureSagas(configurator);
    }

    /// <summary>
    /// Registers a state-machine event observer.
    /// </summary>
    /// <typeparam name="TInstance">The state-machine instance type.</typeparam>
    /// <typeparam name="TObserver">The observer type.</typeparam>
    /// <param name="services">The service collection.</param>
    /// <returns>The service collection.</returns>
    public static IServiceCollection AddEventObserver<TInstance, TObserver>(this IServiceCollection services)
        where TInstance : class, SagaStateMachineInstance
        where TObserver : class, IEventObserver<TInstance>
    {
        ArgumentNullException.ThrowIfNull(services);

        services.TryAddEnumerable(ServiceDescriptor.Singleton<IEventObserver<TInstance>, TObserver>());
        return services;
    }

    /// <summary>
    /// Registers a state-machine event observer created by a factory.
    /// </summary>
    /// <typeparam name="TInstance">The state-machine instance type.</typeparam>
    /// <typeparam name="TObserver">The observer type.</typeparam>
    /// <param name="services">The service collection.</param>
    /// <param name="factory">The observer factory.</param>
    /// <returns>The service collection.</returns>
    public static IServiceCollection AddEventObserver<TInstance, TObserver>(this IServiceCollection services,
        Func<IServiceProvider, TObserver> factory)
        where TInstance : class, SagaStateMachineInstance
        where TObserver : class, IEventObserver<TInstance>
    {
        ArgumentNullException.ThrowIfNull(services);
        ArgumentNullException.ThrowIfNull(factory);

        services.TryAddEnumerable(ServiceDescriptor.Singleton<IEventObserver<TInstance>, TObserver>(factory));
        return services;
    }

    /// <summary>
    /// Registers a state-machine state observer.
    /// </summary>
    /// <typeparam name="TInstance">The state-machine instance type.</typeparam>
    /// <typeparam name="TObserver">The observer type.</typeparam>
    /// <param name="services">The service collection.</param>
    /// <returns>The service collection.</returns>
    public static IServiceCollection AddStateObserver<TInstance, TObserver>(this IServiceCollection services)
        where TInstance : class, SagaStateMachineInstance
        where TObserver : class, IStateObserver<TInstance>
    {
        ArgumentNullException.ThrowIfNull(services);

        services.TryAddEnumerable(ServiceDescriptor.Singleton<IStateObserver<TInstance>, TObserver>());
        return services;
    }

    /// <summary>
    /// Registers a state-machine state observer created by a factory.
    /// </summary>
    /// <typeparam name="TInstance">The state-machine instance type.</typeparam>
    /// <typeparam name="TObserver">The observer type.</typeparam>
    /// <param name="services">The service collection.</param>
    /// <param name="factory">The observer factory.</param>
    /// <returns>The service collection.</returns>
    public static IServiceCollection AddStateObserver<TInstance, TObserver>(this IServiceCollection services,
        Func<IServiceProvider, TObserver> factory)
        where TInstance : class, SagaStateMachineInstance
        where TObserver : class, IStateObserver<TInstance>
    {
        ArgumentNullException.ThrowIfNull(services);
        ArgumentNullException.ThrowIfNull(factory);

        services.TryAddEnumerable(ServiceDescriptor.Singleton<IStateObserver<TInstance>, TObserver>(factory));
        return services;
    }

    static RetrySagaConsumeContext<TSaga> CreateRetryContext<TSaga>(SagaConsumeContext<TSaga> context,
        IRetryPolicy retryPolicy, RetryContext? retryContext)
        where TSaga : class, ISaga
    {
        return new RetrySagaConsumeContext<TSaga>(context, retryPolicy, retryContext);
    }

    static void ConfigurePartitioner<TSaga>(IPipeConfigurator<SagaConsumeContext<TSaga>> configurator,
        int partitionCount, PartitionKeyProvider<SagaConsumeContext<TSaga>> keyProvider)
        where TSaga : class, ISaga
    {
        var partitioner = new Partitioner(partitionCount, new Murmur3UnsafeHashGenerator());
        configurator.AddPipeSpecification(new PartitionSagaSpecification<TSaga>(partitioner, keyProvider));
    }
}

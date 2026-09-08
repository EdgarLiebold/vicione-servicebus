using System;
using System.Collections.Specialized;
using System.Globalization;
using System.Linq;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.DependencyInjection.Extensions;
using Quartz;
using ViciOne.ServiceBus.Advanced;
using ViciOne.ServiceBus.Advanced.Initializers;
using ViciOne.ServiceBus.Advanced.Registration;
using ViciOne.ServiceBus.Configuration;
using ViciOne.ServiceBus.Providers.Configuration;
using ViciOne.ServiceBus.Providers.Persistence;
using ViciOne.ServiceBus.Quartz.Configuration;
using ViciOne.ServiceBus.Quartz.Consumers;
using ViciOne.ServiceBus.Quartz.Runtime;
using ViciOne.ServiceBus.Scheduling;
using ViciOne.ServiceBus.Util;

namespace ViciOne.ServiceBus.Quartz;

/// <summary>Configures Quartz-backed message scheduling for a bus.</summary>
public static class QuartzSchedulingExtensions
{
    /// <summary>Creates and owns an isolated in-memory Quartz scheduler connected to a directly configured bus.</summary>
    /// <param name="configurator">The bus factory configuration to update.</param>
    /// <param name="configure">Optional scheduler lifecycle and endpoint configuration.</param>
    /// <returns>A lease that exposes the endpoint and must be disposed after the bus is finally stopped.</returns>
    public static QuartzSchedulerLease ConfigureInMemoryQuartzScheduler(
        this IBusFactoryConfigurator configurator,
        Action<QuartzSchedulerOptions>? configure = null)
    {
        ArgumentNullException.ThrowIfNull(configurator);
        ISchedulerFactory schedulerFactory = CreateInMemorySchedulerFactory();
        try
        {
            return ConfigureDirectScheduler(configurator, schedulerFactory, ownsSchedulerFactory: true, configure);
        }
        catch (Exception configurationFailure)
        {
            try
            {
                DisposeSchedulerFactory(schedulerFactory);
            }
            catch (Exception cleanupFailure)
            {
                throw new AggregateException(
                    "Quartz configuration and scheduler-factory cleanup both failed.",
                    configurationFailure,
                    cleanupFailure);
            }

            throw;
        }
    }

    /// <summary>Connects a caller-owned Quartz scheduler factory to a directly configured bus.</summary>
    /// <param name="configurator">The bus factory configuration to update.</param>
    /// <param name="schedulerFactory">The scheduler factory that remains owned by the caller.</param>
    /// <param name="configure">Optional scheduler lifecycle and endpoint configuration.</param>
    /// <returns>A lease that exposes the endpoint and must be disposed after the bus is finally stopped.</returns>
    public static QuartzSchedulerLease ConfigureQuartzScheduler(
        this IBusFactoryConfigurator configurator,
        ISchedulerFactory schedulerFactory,
        Action<QuartzSchedulerOptions>? configure = null)
    {
        ArgumentNullException.ThrowIfNull(configurator);
        ArgumentNullException.ThrowIfNull(schedulerFactory);
        return ConfigureDirectScheduler(configurator, schedulerFactory, ownsSchedulerFactory: false, configure);
    }

    /// <summary>Registers Quartz scheduling for the default bus with an explicit factory resolver.</summary>
    /// <param name="configurator">The default bus registration to update.</param>
    /// <param name="schedulerFactory">The resolver that selects the scheduler factory owned outside this adapter.</param>
    /// <param name="configure">Optional endpoint, lifecycle, retry, and time-zone configuration.</param>
    public static void AddQuartzScheduling(
        this IBusRegistrationConfigurator configurator,
        Func<IServiceProvider, ISchedulerFactory> schedulerFactory,
        Action<QuartzEndpointOptions>? configure = null)
    {
        RegisterQuartz<IBus>(configurator, schedulerFactory, ownsSchedulerFactory: false, configure);
    }

    /// <summary>Registers Quartz scheduling for one typed bus with an explicit factory resolver.</summary>
    /// <typeparam name="TBus">The bus whose scheduler state and lifecycle are isolated.</typeparam>
    /// <param name="configurator">The typed bus registration to update.</param>
    /// <param name="schedulerFactory">The resolver that selects the scheduler factory owned outside this adapter.</param>
    /// <param name="configure">Optional endpoint, lifecycle, retry, and time-zone configuration.</param>
    public static void AddQuartzScheduling<TBus>(
        this IBusRegistrationConfigurator<TBus> configurator,
        Func<IServiceProvider, ISchedulerFactory> schedulerFactory,
        Action<QuartzEndpointOptions>? configure = null)
        where TBus : class, IBus
    {
        RegisterQuartz<TBus>(configurator, schedulerFactory, ownsSchedulerFactory: false, configure);
    }

    /// <summary>Adds the Quartz scheduling consumers registered for the context's bus to a receive endpoint.</summary>
    /// <param name="configurator">The receive endpoint to configure.</param>
    /// <param name="context">The registration context that identifies and resolves the owning bus.</param>
    public static void ConfigureQuartzScheduling(
        this IReceiveEndpointConfigurator configurator,
        IBusRegistrationContext context)
    {
        ArgumentNullException.ThrowIfNull(configurator);
        ArgumentNullException.ThrowIfNull(context);

        ConfigureConsumer(configurator, context, typeof(ScheduleMessageConsumer<>));
        ConfigureConsumer(configurator, context, typeof(CancelScheduledMessageConsumer<>));
        ConfigureConsumer(configurator, context, typeof(PauseScheduledMessageConsumer<>));
        ConfigureConsumer(configurator, context, typeof(ResumeScheduledMessageConsumer<>));
    }

    /// <summary>Uses an explicitly resolved, externally owned Quartz scheduler for one reliable-messaging bus.</summary>
    /// <typeparam name="TBus">The bus whose scheduling state is isolated.</typeparam>
    /// <param name="configurator">The reliable-messaging configuration that owns the bus.</param>
    /// <param name="schedulerFactory">The resolver that returns the factory assigned to this bus.</param>
    /// <param name="configure">Optional endpoint, lifecycle, retry, and time-zone configuration.</param>
    /// <returns>The same reliable-messaging configurator.</returns>
    public static IReliableMessagingConfigurator<TBus> UseQuartzScheduler<TBus>(
        this IReliableMessagingConfigurator<TBus> configurator,
        Func<IServiceProvider, ISchedulerFactory> schedulerFactory,
        Action<QuartzEndpointOptions>? configure = null)
        where TBus : class, IBus
    {
        ArgumentNullException.ThrowIfNull(configurator);
        ArgumentNullException.ThrowIfNull(schedulerFactory);
        ConfigureReliableScheduler(configurator, schedulerFactory, ownsSchedulerFactory: false, configure);
        return configurator;
    }

    /// <summary>Uses an adapter-owned volatile in-memory Quartz scheduler for one reliable-messaging bus.</summary>
    /// <typeparam name="TBus">The bus whose scheduling state is isolated.</typeparam>
    /// <param name="configurator">The reliable-messaging configuration that owns the bus.</param>
    /// <param name="configure">Optional endpoint, lifecycle, retry, and time-zone configuration.</param>
    /// <returns>The same reliable-messaging configurator.</returns>
    public static IReliableMessagingConfigurator<TBus> UseInMemoryQuartzScheduler<TBus>(
        this IReliableMessagingConfigurator<TBus> configurator,
        Action<QuartzEndpointOptions>? configure = null)
        where TBus : class, IBus
    {
        ArgumentNullException.ThrowIfNull(configurator);
        ConfigureReliableScheduler(
            configurator,
            static _ => CreateInMemorySchedulerFactory(),
            ownsSchedulerFactory: true,
            configure);
        return configurator;
    }

    internal static ISchedulerFactory CreateInMemorySchedulerFactory()
    {
        var configuration = new NameValueCollection
        {
            ["quartz.scheduler.instanceName"] = $"ViciOne.ServiceBus-{NewId.Next().ToString(FormatUtil.Formatter)}",
            ["quartz.threadPool.maxConcurrency"] = Environment.ProcessorCount.ToString(CultureInfo.InvariantCulture),
        };

        return QuartzSchedulerBuilder.Create()
            .UseProperties(configuration)
            .Build();
    }

    private static QuartzSchedulerLease ConfigureDirectScheduler(
        IBusFactoryConfigurator configurator,
        ISchedulerFactory schedulerFactory,
        bool ownsSchedulerFactory,
        Action<QuartzSchedulerOptions>? configure)
    {
        var options = new QuartzSchedulerOptions();
        configure?.Invoke(options);
        QuartzSchedulerSettings settings = options.CreateSettings(schedulerFactory);
        var observer = new DirectQuartzSchedulerLifecycleObserver(settings);
        IPartitioner? partitioner = null;
        Uri? inputAddress = null;

        try
        {
            configurator.ReceiveEndpoint(settings.QueueName, endpoint =>
            {
                int partitionCount = settings.ConcurrentMessageLimit ?? settings.PrefetchCount ?? Environment.ProcessorCount;
                IPartitioner commandPartitioner = configurator.CreatePartitioner(partitionCount);
                partitioner = commandPartitioner;
                if (settings.PrefetchCount.HasValue)
                    endpoint.PrefetchCount = settings.PrefetchCount.Value;
                endpoint.ConcurrentMessageLimit = settings.ConcurrentMessageLimit;
                endpoint.UseTechnicalMessageRetry();
                endpoint.Consumer(
                    () => new ScheduleMessageConsumer<IBus>(
                        settings.SchedulerFactory,
                        settings.TimeZoneResolver,
                        settings.SchedulerNamespace,
                        settings.DeliveryRetryPolicy),
                    consumer =>
                    {
                        consumer.Message<ScheduleMessage>(message =>
                            message.UsePartitioner(commandPartitioner, context => context.Message.TokenId));
                        consumer.Message<ScheduleRecurringMessage>(message =>
                            message.UsePartitioner(commandPartitioner, context => QuartzTriggerKey.GetPartitionKey(
                                context.Message.Schedule.ScheduleId,
                                context.Message.Schedule.ScheduleGroup)));
                    });
                endpoint.Consumer(() => new CancelScheduledMessageConsumer<IBus>(
                        settings.SchedulerFactory,
                        settings.SchedulerNamespace),
                    consumer =>
                    {
                        consumer.Message<CancelScheduledMessage>(message =>
                            message.UsePartitioner(commandPartitioner, context => context.Message.TokenId));
                        consumer.Message<CancelScheduledRecurringMessage>(message =>
                            message.UsePartitioner(commandPartitioner, context => QuartzTriggerKey.GetPartitionKey(
                                context.Message.ScheduleId,
                                context.Message.ScheduleGroup)));
                    });
                endpoint.Consumer(() => new PauseScheduledMessageConsumer<IBus>(
                        settings.SchedulerFactory,
                        settings.SchedulerNamespace),
                    consumer => consumer.Message<PauseScheduledRecurringMessage>(message =>
                        message.UsePartitioner(commandPartitioner, context => QuartzTriggerKey.GetPartitionKey(
                            context.Message.ScheduleId,
                            context.Message.ScheduleGroup))));
                endpoint.Consumer(() => new ResumeScheduledMessageConsumer<IBus>(
                        settings.SchedulerFactory,
                        settings.SchedulerNamespace),
                    consumer => consumer.Message<ResumeScheduledRecurringMessage>(message =>
                        message.UsePartitioner(commandPartitioner, context => QuartzTriggerKey.GetPartitionKey(
                            context.Message.ScheduleId,
                            context.Message.ScheduleGroup))));

                configurator.ConfigureMessageScheduler(endpoint.InputAddress);
                configurator.ConnectBusObserver(observer);
                inputAddress = endpoint.InputAddress;
            });

            return new QuartzSchedulerLease(
                inputAddress ?? throw new ConfigurationException(ConfigurationMessages.Create(
                    "Quartz scheduling",
                    typeof(IBus).FullName ?? nameof(IBus),
                    "The configured scheduling endpoint did not expose an input address",
                    "Use a receive endpoint transport that provides a valid input address")),
                schedulerFactory,
                ownsSchedulerFactory,
                settings.WaitForJobsToComplete,
                partitioner ?? throw new ConfigurationException(ConfigurationMessages.Create(
                    "Quartz scheduling",
                    typeof(IBus).FullName ?? nameof(IBus),
                    "The configured scheduling endpoint did not create its command partitioner",
                    "Configure the endpoint through a bus factory that supports partitioned consumers")));
        }
        catch (Exception configurationFailure)
        {
            if (partitioner is not null)
            {
                try
                {
                    partitioner.DisposeAsync().AsTask().GetAwaiter().GetResult();
                }
                catch (Exception cleanupFailure)
                {
                    throw new AggregateException(
                        "Quartz endpoint configuration and partitioner cleanup both failed.",
                        configurationFailure,
                        cleanupFailure);
                }
            }

            throw;
        }
    }

    private static void ConfigureReliableScheduler<TBus>(
        IReliableMessagingConfigurator<TBus> configurator,
        Func<IServiceProvider, ISchedulerFactory> schedulerFactory,
        bool ownsSchedulerFactory,
        Action<QuartzEndpointOptions>? configure)
        where TBus : class, IBus
    {
        IReliableMessagingProviderConfigurator provider = RequireProvider(configurator, typeof(TBus));
        var options = new QuartzEndpointOptions();
        configure?.Invoke(options);
        RegisterQuartz<TBus>(
            provider.RegistrationConfigurator,
            schedulerFactory,
            ownsSchedulerFactory,
            target => Copy(options, target));
        RegisterSchedulerContracts(configurator);
        provider.UseEndpointSchedulerAdapter(new Uri($"queue:{options.QueueName}", UriKind.Absolute));
    }

    private static void RegisterQuartz<TBus>(
        IBusRegistrationConfigurator configurator,
        Func<IServiceProvider, ISchedulerFactory> schedulerFactory,
        bool ownsSchedulerFactory,
        Action<QuartzEndpointOptions>? configure)
        where TBus : class, IBus
    {
        ArgumentNullException.ThrowIfNull(configurator);
        ArgumentNullException.ThrowIfNull(schedulerFactory);
        IServiceCollection services = configurator.Services;
        if (services.Any(static descriptor => descriptor.ServiceType == typeof(QuartzSchedulingRegistration<TBus>)))
        {
            throw new ConfigurationException(ConfigurationMessages.Create(
                "Quartz scheduling",
                typeof(TBus).FullName ?? typeof(TBus).Name,
                "Quartz scheduling was already configured",
                "Configure exactly one scheduler adapter per bus"));
        }

        var options = new QuartzEndpointOptions();
        configure?.Invoke(options);
        QuartzEndpointSettings settings = options.CreateSettings(typeof(TBus));

        services.AddSingleton<QuartzSchedulingRegistration<TBus>>();
        services.TryAddSingleton(TimeProvider.System);
        services.TryAddSingleton<QuartzSchedulerClaimRegistry>();
        services.AddSingleton(provider =>
        {
            EnsureSingleLifecycleOwner<TBus>(services);
            return new QuartzSchedulerBinding<TBus>(
                schedulerFactory(provider),
                ownsSchedulerFactory,
                settings,
                provider.GetRequiredService<TimeProvider>(),
                provider.GetRequiredService<QuartzSchedulerClaimRegistry>());
        });
        services.TryAddTransient<QuartzScheduledMessageJob<TBus>>();
        services.AddBusObserver<TBus, QuartzSchedulerLifecycleObserver<TBus>>();
        services.AddSingleton(_ => new QuartzEndpointDefinition<TBus>(settings));

        configurator.AddConsumer(
            typeof(ScheduleMessageConsumer<>).MakeGenericType(typeof(TBus)),
            typeof(ScheduleMessageConsumerDefinition<>).MakeGenericType(typeof(TBus)));
        configurator.AddConsumer(
            typeof(CancelScheduledMessageConsumer<>).MakeGenericType(typeof(TBus)),
            typeof(CancelScheduledMessageConsumerDefinition<>).MakeGenericType(typeof(TBus)));
        configurator.AddConsumer(
            typeof(PauseScheduledMessageConsumer<>).MakeGenericType(typeof(TBus)),
            typeof(PauseScheduledMessageConsumerDefinition<>).MakeGenericType(typeof(TBus)));
        configurator.AddConsumer(
            typeof(ResumeScheduledMessageConsumer<>).MakeGenericType(typeof(TBus)),
            typeof(ResumeScheduledMessageConsumerDefinition<>).MakeGenericType(typeof(TBus)));
    }

    private static void ConfigureConsumer(
        IReceiveEndpointConfigurator configurator,
        IBusRegistrationContext context,
        Type openConsumerType)
    {
        context.ConfigureConsumer(openConsumerType.MakeGenericType(context.BusType), configurator);
    }

    private static IReliableMessagingProviderConfigurator RequireProvider(
        IReliableMessagingConfigurator configurator,
        Type busType)
    {
        if (configurator is IReliableMessagingProviderConfigurator provider && provider.BusType == busType)
            return provider;

        throw new ConfigurationException(
            global::ViciOne.ServiceBus.Providers.Configuration.ConfigurationMessages.Create(
                "Scheduling",
                "unknown",
                $"The reliable-messaging configurator does not own bus '{busType}'.",
                "Choose the adapter inside the owning UseReliableMessaging block"));
    }

    private static void EnsureSingleLifecycleOwner<TBus>(IServiceCollection services)
        where TBus : class, IBus
    {
        bool hasQuartzHostedService = services.Any(static descriptor =>
            descriptor.ImplementationType is not null
            && typeof(QuartzHostedService).IsAssignableFrom(descriptor.ImplementationType));
        if (hasQuartzHostedService)
        {
            throw new ConfigurationException(ConfigurationMessages.Create(
                "Quartz scheduling",
                typeof(TBus).FullName ?? typeof(TBus).Name,
                "QuartzHostedService is already configured and would introduce a second scheduler lifecycle owner",
                "Remove QuartzHostedService and let the bus-bound adapter own the scheduler lifecycle"));
        }
    }

    private static void DisposeSchedulerFactory(ISchedulerFactory schedulerFactory)
    {
        if (schedulerFactory is IAsyncDisposable asyncDisposable)
            asyncDisposable.DisposeAsync().AsTask().GetAwaiter().GetResult();
        else if (schedulerFactory is IDisposable disposable)
            disposable.Dispose();
    }

    private static void RegisterSchedulerContracts(IReliableMessagingConfigurator configurator)
    {
        configurator.AddMessageContract<ScheduleMessage>("vicione.scheduler.schedule", 1);
        configurator.AddMessageContract<CancelScheduledMessage>("vicione.scheduler.cancel", 1);
        configurator.AddMessageContract<ScheduleRecurringMessage>("vicione.scheduler.recurring.schedule", 1);
        configurator.AddMessageContract<CancelScheduledRecurringMessage>("vicione.scheduler.recurring.cancel", 1);
        configurator.AddMessageContract<PauseScheduledRecurringMessage>("vicione.scheduler.recurring.pause", 1);
        configurator.AddMessageContract<ResumeScheduledRecurringMessage>("vicione.scheduler.recurring.resume", 1);
    }

    private static void Copy(QuartzEndpointOptions source, QuartzEndpointOptions target)
    {
        target.PrefetchCount = source.PrefetchCount;
        target.ConcurrentMessageLimit = source.ConcurrentMessageLimit;
        target.QueueName = source.QueueName;
        target.TimeZoneResolver = source.TimeZoneResolver;
        target.StartDelay = source.StartDelay;
        target.WaitForJobsToComplete = source.WaitForJobsToComplete;
        target.DeliveryRetryPolicy = source.DeliveryRetryPolicy;
    }
}

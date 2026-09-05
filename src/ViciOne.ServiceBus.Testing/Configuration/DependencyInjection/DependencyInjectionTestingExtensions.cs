using System;
using System.Diagnostics;
using System.IO;
using System.Linq;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.DependencyInjection.Extensions;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using ViciOne.ServiceBus;
using ViciOne.ServiceBus.Configuration;
using ViciOne.ServiceBus.DependencyInjection;
using ViciOne.ServiceBus.DependencyInjection.Registration;
using ViciOne.ServiceBus.DependencyInjection.Testing;
using ViciOne.ServiceBus.Internals;
using ViciOne.ServiceBus.Logging;
using ViciOne.ServiceBus.Testing;
using ViciOne.ServiceBus.Testing.Implementations;
using ViciOne.ServiceBus.Transports;

#nullable enable
namespace Microsoft.Extensions.DependencyInjection;

/// <summary>
/// Provides extension methods for dependency injection testing.
/// </summary>
public static class DependencyInjectionTestingExtensions
{
    /// <summary>
    /// AddViciOneServiceBus, including the test harness, to the container.
    /// To specify a transport, add the appropriate UsingXxx method. If no transport is specified, the
    /// default in-memory transport will be used, and ConfigureEndpoints will be called.
    /// The registration fails when a bus has already been configured, because silently replacing production registrations
    /// would make the test container exercise a different topology.
    /// </summary>
    public static IServiceCollection AddViciOneServiceBusTestHarness(this IServiceCollection services, Action<IBusRegistrationConfigurator>? configure = null)
    {
        return AddViciOneServiceBusTestHarness(services, Console.Out, configure);
    }

    /// <summary>
    /// AddViciOneServiceBus, including the test harness, to the container.
    /// To specify a transport, add the appropriate UsingXxx method. If no transport is specified, the
    /// default in-memory transport will be used, and ConfigureEndpoints will be called.
    /// The registration fails when a bus has already been configured, because silently replacing production registrations
    /// would make the test container exercise a different topology.
    /// </summary>
    public static IServiceCollection AddViciOneServiceBusTestHarness(this IServiceCollection services, TextWriter textWriter,
        Action<IBusRegistrationConfigurator>? configure = null)
    {
        ArgumentNullException.ThrowIfNull(services);
        ArgumentNullException.ThrowIfNull(textWriter);

        if (services.Any(static descriptor => descriptor.ServiceType == typeof(IBus)))
        {
            throw new ConfigurationException(
                "Test harness for bus 'default': IBus is already registered. Configure AddViciOneServiceBusTestHarness as the only default-bus registration.");
        }

        AddViciOneServiceBusTextWriterLogger(services, textWriter);

        services.AddOptions<TestHarnessOptions>()
            .Validate(
                static options => options.TestTimeout > TimeSpan.Zero,
                "Test harness for bus 'default': TestTimeout must be greater than zero. Set TestTimeout to a positive duration.")
            .Validate(
                static options => options.TestInactivityTimeout > TimeSpan.Zero,
                "Test harness for bus 'default': TestInactivityTimeout must be greater than zero. Set TestInactivityTimeout to a positive duration.")
            .Validate(
                static options => Enum.IsDefined(options.ContextSaveMode),
                "Test harness for bus 'default': ContextSaveMode is not defined. Select a valid TestContextSaveMode value.")
            .Validate(
                static options => options.MaximumSavedContexts > 0,
                "Test harness for bus 'default': MaximumSavedContexts must be greater than zero. Set a positive bounded retention count.")
            .ValidateOnStart();
        services.TryAddSingleton<TimeProvider>(_ => TimeProvider.System);
        services.AddBusObserver<ContainerTestHarnessBusObserver>();
        services.TryAddSingleton<ITestHarness>(provider => provider.GetRequiredService<ContainerTestHarness>());
        services.TryAddSingleton<ContainerTestHarness>();

        services.AddOptions<ViciOneServiceBusHostOptions>().Configure(options =>
        {
            options.WaitUntilStarted = true;
        });

        return services.AddViciOneServiceBus(x =>
        {
            var harnessConfigurator = new TestHarnessRegistrationConfigurator(x);

            harnessConfigurator.SetInMemorySagaRepositoryProvider();

            configure?.Invoke(harnessConfigurator);

            harnessConfigurator.ConfigureConsumerKindTestHarnesses();

            if (!MessageLimitsConfigurationExtensions.HasLimits<IBus>(services))
                x.Limits(MessageLimits.Conservative);

            var addScheduler = false;
            if (services.All(d => d.ServiceType != typeof(IMessageScheduler)))
            {
                x.AddDelayedMessageScheduler();
                addScheduler = true;
            }

            if (harnessConfigurator.UseDefaultBusFactory)
            {
                harnessConfigurator.UsingInMemory((context, cfg) =>
                {
                    if (addScheduler)
                        cfg.ConfigureDelayedMessageScheduler();

                    cfg.ConfigureEndpoints(context);
                });
            }
        });
    }


    /// <summary>
    /// Internally used by AddViciOneServiceBusTestHarness to add a console-based <see cref="ILogger"/> for unit testing
    /// </summary>
    /// <param name="services"></param>
    /// <param name="textWriter"></param>
    public static IServiceCollection AddViciOneServiceBusTextWriterLogger(this IServiceCollection services, TextWriter? textWriter = null)
    {
        services.AddOptions<TextWriterLoggerOptions>()
            .Validate(
                static options => Enum.IsDefined(options.LogLevel),
                "Test logger for bus 'default': LogLevel is not defined. Select a valid LogLevel value.")
            .ValidateOnStart();
        services.TryAddSingleton<ILoggerFactory>(provider =>
            new TextWriterLoggerFactory(textWriter ?? Console.Out, provider.GetRequiredService<IOptions<TextWriterLoggerOptions>>(),
                provider.GetService<TimeProvider>()));
        services.TryAddSingleton(typeof(ILogger<>), typeof(Logger<>));

        return services;
    }

    /// <summary>
    /// Adds a telemetry listener to the test harness, which outputs a timeline view of the unit test
    /// </summary>
    /// <param name="services"></param>
    /// <param name="includeDetails">If true, additional details from each span are shown</param>
    public static IServiceCollection AddTelemetryListener(this IServiceCollection services, bool includeDetails = false)
    {
        return services.AddTelemetryListener(Console.Out, includeDetails);
    }

    /// <summary>
    /// Adds a telemetry listener to the test harness, which outputs a timeline view of the unit test
    /// </summary>
    /// <param name="services"></param>
    /// <param name="textWriter">Override the default Console.Out TextWriter</param>
    /// <param name="includeDetails">If true, additional details from each span are shown</param>
    public static IServiceCollection AddTelemetryListener(this IServiceCollection services, TextWriter textWriter, bool includeDetails = false)
    {
        var (methodName, className) = GetTestMethodInfo();

        services.TryAddSingleton(_ => new TestActivityListener(textWriter, methodName, className, includeDetails));

        return services;
    }

    /// <summary>
    /// Specify the test and/or the test inactivity timeouts that should be used by the test harness.
    /// </summary>
    /// <param name="configurator"></param>
    /// <param name="testTimeout">If specified, changes the test timeout</param>
    /// <param name="testInactivityTimeout">If specified, changes the test inactivity timeout</param>
    /// <returns></returns>
    public static IBusRegistrationConfigurator SetTestTimeouts(this IBusRegistrationConfigurator configurator, TimeSpan? testTimeout = null,
        TimeSpan? testInactivityTimeout = null)
    {
        configurator.Services.AddOptions<TestHarnessOptions>()
            .Configure(options =>
            {
                if (testTimeout.HasValue)
                    options.TestTimeout = testTimeout.Value;
                if (testInactivityTimeout.HasValue)
                    options.TestInactivityTimeout = testInactivityTimeout.Value;
            });

        return configurator;
    }

    /// <summary>
    /// Controls how many observed contexts the test harness retains for later assertions.
    /// Activity/inactivity tracking is independent from context retention.
    /// </summary>
    public static IBusRegistrationConfigurator SetTestContextSaveMode(this IBusRegistrationConfigurator configurator,
        TestContextSaveMode saveMode, int maximumSavedContexts = 4096)
    {
        if (maximumSavedContexts <= 0)
            throw new ArgumentOutOfRangeException(nameof(maximumSavedContexts));

        configurator.Services.AddOptions<TestHarnessOptions>().Configure(options =>
        {
            options.ContextSaveMode = saveMode;
            options.MaximumSavedContexts = maximumSavedContexts;
        });

        return configurator;
    }

    /// <summary>
    /// Add the In-Memory test harness to the container, and configure it using the callback specified.
    /// </summary>
    public static IServiceCollection AddViciOneServiceBusInMemoryTestHarness(this IServiceCollection services,
        Action<IBusRegistrationConfigurator>? configure = null)
    {
        services.TryAddSingleton<TimeProvider>(_ => TimeProvider.System);
        services.AddViciOneServiceBus(cfg =>
        {
            configure?.Invoke(cfg);

            cfg.SetBusFactory(new InMemoryTestHarnessRegistrationBusFactory());
        });
        services.AddSingleton(provider =>
        {
            var busInstances = provider.GetService<IEnumerable<IBusInstance>>();
            if (busInstances == null)
            {
                var busInstance = provider.GetService<IBusInstance>();
                if (busInstance == null)
                    throw new ConfigurationException(global::ViciOne.ServiceBus.Providers.Configuration.ConfigurationMessages.Create("Test harness", "unknown", "No bus instances found", "Correct the named configuration before starting the host"));

                busInstances = [busInstance];
            }

            var testHarnessBusInstance = busInstances.FirstOrDefault(x => x is InMemoryTestHarnessBusInstance);
            if (testHarnessBusInstance is InMemoryTestHarnessBusInstance testInstance)
                return testInstance.Harness;

            throw new ConfigurationException(global::ViciOne.ServiceBus.Providers.Configuration.ConfigurationMessages.Create("Test harness", "unknown", "Test Harness configuration is invalid", "Correct the named configuration before starting the host"));
        });
        services.AddSingleton<BusTestHarness>(provider => provider.GetRequiredService<InMemoryTestHarness>());

        return services;
    }

    static (string? methodName, string? className) GetTestMethodInfo()
    {
        var stackTrace = new StackTrace(2);
        var frameCount = stackTrace.FrameCount;
        for (var i = 0; i < frameCount; i++)
        {
            var frame = stackTrace.GetFrame(i);
            if (frame == null)
                continue;

            var method = frame.GetMethod();
            if (method == null)
                continue;

            if (method.GetCustomAttributes(false).Any(x =>
                {
                    var name = x.GetType().Name;
                    return name.ToLower().Contains("test") || name.ToLower().Contains("fact");
                }))
                return (method.Name, method.DeclaringType?.Name);
        }

        return (null, null);
    }

    /// <summary>
    /// Add a consumer test harness for the specified consumer to the container
    /// </summary>
    public static void AddConsumerContainerTestHarness<T>(this IServiceCollection configurator)
        where T : class, IConsumer
    {
        configurator.TryAddSingleton<ConsumerContainerTestHarnessRegistration<T>>();
        configurator.TryAddSingleton<IConsumerFactoryDecoratorRegistration<T>>(provider =>
            provider.GetRequiredService<ConsumerContainerTestHarnessRegistration<T>>());
        configurator.TryAddSingleton<IConsumerTestHarness<T>, RegistrationConsumerTestHarness<T>>();
    }

    /// <summary>
    /// Add a saga test harness for the specified saga to the container. The saga must be added separately, including
    /// a valid saga repository.
    /// </summary>
    public static void AddSagaContainerTestHarness<T>(this IServiceCollection services)
        where T : class, ISaga
    {
        services.TryAddSingleton<SagaContainerTestHarnessRegistration<T>>();
        services.TryAddSingleton<ISagaRepositoryDecoratorRegistration<T>>(provider =>
            provider.GetRequiredService<SagaContainerTestHarnessRegistration<T>>());
        services.TryAddSingleton<ISagaTestHarness<T>, RegistrationSagaTestHarness<T>>();
    }

    /// <summary>
    /// Add a saga state machine test harness for the specified saga to the container. The saga must be added separately, including
    /// a valid saga repository.
    /// </summary>
    public static void AddSagaStateMachineContainerTestHarness<TStateMachine, T>(this IServiceCollection services)
        where TStateMachine : class, SagaStateMachine<T>
        where T : class, SagaStateMachineInstance
    {
        services.TryAddSingleton<SagaContainerTestHarnessRegistration<T>>();
        services.TryAddSingleton<ISagaRepositoryDecoratorRegistration<T>>(provider =>
            provider.GetRequiredService<SagaContainerTestHarnessRegistration<T>>());

        services.TryAddSingleton<RegistrationSagaStateMachineTestHarness<TStateMachine, T>>();
        services.TryAddSingleton<ISagaStateMachineTestHarness<TStateMachine, T>>(provider =>
            provider.GetRequiredService<RegistrationSagaStateMachineTestHarness<TStateMachine, T>>());
    }

}

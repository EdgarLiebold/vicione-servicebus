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
using ViciOne.ServiceBus.Internals;
using ViciOne.ServiceBus.Logging;
using ViciOne.ServiceBus.Testing;
using ViciOne.ServiceBus.Testing.Internal;

namespace Microsoft.Extensions.DependencyInjection;

/// <summary>Registers and configures ViciOne ServiceBus test-harness services.</summary>
public static class ViciOneServiceBusTestingServiceCollectionExtensions
{
    /// <summary>
    /// Registers ViciOne ServiceBus and its test harness in the service collection.
    /// A transport selected by <paramref name="configure"/> is used as configured; otherwise, the harness
    /// configures an in-memory bus and its registered endpoints.
    /// The registration fails when a bus has already been configured, because silently replacing production registrations
    /// would make the test container exercise a different topology.
    /// </summary>
    /// <param name="services">The dependency-injection service collection.</param>
    /// <param name="configure">An optional callback that configures consumers, sagas, endpoints, and transport.</param>
    /// <returns>The same service collection.</returns>
    public static IServiceCollection AddViciOneServiceBusTestHarness(this IServiceCollection services, Action<IBusRegistrationConfigurator>? configure = null)
    {
        return AddViciOneServiceBusTestHarness(services, Console.Out, configure);
    }

    /// <summary>
    /// Registers ViciOne ServiceBus, its test harness, and a text-writer logger in the service collection.
    /// A transport selected by <paramref name="configure"/> is used as configured; otherwise, the harness
    /// configures an in-memory bus and its registered endpoints.
    /// The registration fails when a bus has already been configured, because silently replacing production registrations
    /// would make the test container exercise a different topology.
    /// </summary>
    /// <param name="services">The dependency-injection service collection.</param>
    /// <param name="textWriter">The destination for test log output.</param>
    /// <param name="configure">An optional callback that configures consumers, sagas, endpoints, and transport.</param>
    /// <returns>The same service collection.</returns>
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


    /// <summary>Registers a logger factory that writes test output to a <see cref="TextWriter"/>.</summary>
    /// <param name="services">The dependency-injection service collection.</param>
    /// <param name="textWriter">The output destination, or <see langword="null"/> to use <see cref="Console.Out"/>.</param>
    /// <returns>The same service collection.</returns>
    public static IServiceCollection AddViciOneServiceBusTextWriterLogger(this IServiceCollection services, TextWriter? textWriter = null)
    {
        ArgumentNullException.ThrowIfNull(services);

        services.AddOptions<TextWriterLoggerOptions>()
            .Validate(
                static options => Enum.IsDefined(options.MinimumLevel),
                "Test logger for bus 'default': MinimumLevel is not defined. Select a valid LogLevel value.")
            .ValidateOnStart();
        services.TryAddSingleton<ILoggerFactory>(provider =>
            new TextWriterLoggerFactory(textWriter ?? Console.Out, provider.GetRequiredService<IOptions<TextWriterLoggerOptions>>().Value,
                provider.GetService<TimeProvider>()));
        services.TryAddSingleton(typeof(ILogger<>), typeof(Logger<>));

        return services;
    }

    /// <summary>Registers a telemetry listener that writes a timeline for the active test to <see cref="Console.Out"/>.</summary>
    /// <param name="services">The dependency-injection service collection.</param>
    /// <param name="includeDetails">Whether to include activity tags and events for each span.</param>
    /// <returns>The same service collection.</returns>
    public static IServiceCollection AddViciOneServiceBusTestTelemetry(this IServiceCollection services, bool includeDetails = false)
    {
        return services.AddViciOneServiceBusTestTelemetry(Console.Out, includeDetails);
    }

    /// <summary>Registers a telemetry listener that writes a timeline for the active test.</summary>
    /// <param name="services">The dependency-injection service collection.</param>
    /// <param name="textWriter">The destination for the activity timeline.</param>
    /// <param name="includeDetails">Whether to include activity tags and events for each span.</param>
    /// <returns>The same service collection.</returns>
    public static IServiceCollection AddViciOneServiceBusTestTelemetry(
        this IServiceCollection services,
        TextWriter textWriter,
        bool includeDetails = false)
    {
        ArgumentNullException.ThrowIfNull(services);
        ArgumentNullException.ThrowIfNull(textWriter);

        var (methodName, className) = GetTestMethodInfo();

        services.TryAddSingleton(_ => new TestActivityListener(textWriter, methodName, className, includeDetails));

        return services;
    }

    /// <summary>Configures assertion and inactivity timeouts for the test harness.</summary>
    /// <param name="configurator">The bus registration configurator.</param>
    /// <param name="testTimeout">The maximum assertion wait, or <see langword="null"/> to retain the default.</param>
    /// <param name="testInactivityTimeout">The interval without activity that indicates inactivity, or <see langword="null"/> to retain the default.</param>
    /// <returns>The same bus registration configurator.</returns>
    public static IBusRegistrationConfigurator SetTestTimeouts(this IBusRegistrationConfigurator configurator, TimeSpan? testTimeout = null,
        TimeSpan? testInactivityTimeout = null)
    {
        ArgumentNullException.ThrowIfNull(configurator);

        if (testTimeout is { } assertionTimeout && assertionTimeout <= TimeSpan.Zero)
            throw new ArgumentOutOfRangeException(nameof(testTimeout), testTimeout, "The test timeout must be greater than zero.");
        if (testInactivityTimeout is { } inactivityTimeout && inactivityTimeout <= TimeSpan.Zero)
        {
            throw new ArgumentOutOfRangeException(
                nameof(testInactivityTimeout),
                testInactivityTimeout,
                "The test inactivity timeout must be greater than zero.");
        }

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
    /// <param name="configurator">The bus registration configurator.</param>
    /// <param name="saveMode">The historical context retention policy.</param>
    /// <param name="maximumSavedContexts">The maximum contexts retained per list in bounded mode.</param>
    /// <returns>The same bus registration configurator.</returns>
    public static IBusRegistrationConfigurator SetTestContextRetention(this IBusRegistrationConfigurator configurator,
        TestContextSaveMode saveMode, int maximumSavedContexts = 4096)
    {
        ArgumentNullException.ThrowIfNull(configurator);

        if (!Enum.IsDefined(saveMode))
            throw new ArgumentOutOfRangeException(nameof(saveMode));
        if (maximumSavedContexts <= 0)
            throw new ArgumentOutOfRangeException(nameof(maximumSavedContexts));

        configurator.Services.AddOptions<TestHarnessOptions>().Configure(options =>
        {
            options.ContextSaveMode = saveMode;
            options.MaximumSavedContexts = maximumSavedContexts;
        });

        return configurator;
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
                    string name = x.GetType().Name;
                    return name.Contains("Test", StringComparison.OrdinalIgnoreCase)
                        || name.Contains("Fact", StringComparison.OrdinalIgnoreCase);
                }))
                return (method.Name, method.DeclaringType?.Name);
        }

        return (null, null);
    }

    /// <summary>Registers the factory decorator and harness for a consumer type.</summary>
    /// <typeparam name="TConsumer">The consumer implementation.</typeparam>
    /// <param name="services">The dependency-injection service collection.</param>
    internal static void AddConsumerContainerTestHarness<TConsumer>(this IServiceCollection services)
        where TConsumer : class, IConsumer
    {
        services.TryAddSingleton<ConsumerContainerTestHarnessRegistration<TConsumer>>();
        services.TryAddSingleton<IConsumerFactoryDecoratorRegistration<TConsumer>>(provider =>
            provider.GetRequiredService<ConsumerContainerTestHarnessRegistration<TConsumer>>());
        services.TryAddSingleton<IConsumerTestHarness<TConsumer>, RegistrationConsumerTestHarness<TConsumer>>();
    }

    /// <summary>
    /// Registers a test harness and repository decorator for an already registered saga and repository.
    /// </summary>
    /// <typeparam name="TSaga">The saga state type.</typeparam>
    /// <param name="services">The dependency-injection service collection.</param>
    internal static void AddSagaContainerTestHarness<TSaga>(this IServiceCollection services)
        where TSaga : class, ISaga
    {
        services.TryAddSingleton<SagaContainerTestHarnessRegistration<TSaga>>();
        services.TryAddSingleton<ISagaRepositoryDecoratorRegistration<TSaga>>(provider =>
            provider.GetRequiredService<SagaContainerTestHarnessRegistration<TSaga>>());
        services.TryAddSingleton<ISagaTestHarness<TSaga>, RegistrationSagaTestHarness<TSaga>>();
    }

    /// <summary>
    /// Registers a test harness and repository decorator for an already registered saga state machine.
    /// </summary>
    /// <typeparam name="TStateMachine">The state machine type.</typeparam>
    /// <typeparam name="TSaga">The saga state type.</typeparam>
    /// <param name="services">The dependency-injection service collection.</param>
    internal static void AddSagaStateMachineContainerTestHarness<TStateMachine, TSaga>(this IServiceCollection services)
        where TStateMachine : class, SagaStateMachine<TSaga>
        where TSaga : class, SagaStateMachineInstance
    {
        services.TryAddSingleton<SagaContainerTestHarnessRegistration<TSaga>>();
        services.TryAddSingleton<ISagaRepositoryDecoratorRegistration<TSaga>>(provider =>
            provider.GetRequiredService<SagaContainerTestHarnessRegistration<TSaga>>());

        services.TryAddSingleton<RegistrationSagaStateMachineTestHarness<TStateMachine, TSaga>>();
        services.TryAddSingleton<ISagaStateMachineTestHarness<TStateMachine, TSaga>>(provider =>
            provider.GetRequiredService<RegistrationSagaStateMachineTestHarness<TStateMachine, TSaga>>());
    }

}

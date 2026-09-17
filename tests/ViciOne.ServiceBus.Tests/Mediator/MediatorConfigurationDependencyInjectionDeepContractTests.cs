using System.Reflection;
using System.Runtime.CompilerServices;
using System.Diagnostics.CodeAnalysis;
using Microsoft.Extensions.DependencyInjection;
using ViciOne.ServiceBus.Advanced.Registration;
using ViciOne.ServiceBus.Clients;
using ViciOne.ServiceBus.Configuration;
using ViciOne.ServiceBus.DependencyInjection;
using ViciOne.ServiceBus.InMemoryTransport.Configuration;
using ViciOne.ServiceBus.Mediator;
using ViciOne.ServiceBus.Providers.Transports;
using ViciOne.ServiceBus.Tests.Infrastructure.Requirements;
using Xunit;

namespace ViciOne.ServiceBus.Tests.Mediator;

public sealed class MediatorConfigurationDependencyInjectionDeepContractTests
{
    [Fact]
    [RequirementCoverage("REQ-VSB-MEDIATOR-DI", "deep-registration-lifetimes-and-lazy-materialization")]
    public async Task Registration_OwnsOneLazySingletonRuntimeAndOneScopedWrapperPerScopeAsync()
    {
        var marker = new RegistrationMarker();
        var timeProvider = new TestTimeProvider();
        var services = new ServiceCollection()
            .AddSingleton(marker)
            .AddSingleton<TimeProvider>(timeProvider);
        var materializationCount = 0;
        IMediatorRegistrationContext? registrationContext = null;
        IMediatorConfigurator? runtimeConfiguration = null;

        IServiceCollection returned = services.AddMediator(new Uri("loopback://mediator-host/application"), configuration =>
        {
            configuration.Limits(MessageLimits.Conservative);
            configuration.ConfigureMediator((context, mediator) =>
            {
                Interlocked.Increment(ref materializationCount);
                registrationContext = context;
                runtimeConfiguration = mediator;
            });
        });

        Assert.Same(services, returned);
        Assert.Equal(0, Volatile.Read(ref materializationCount));
        Assert.Equal(ServiceLifetime.Singleton,
            Assert.Single(services, descriptor => descriptor.ServiceType == typeof(IMediator)).Lifetime);
        Assert.Equal(ServiceLifetime.Scoped,
            Assert.Single(services, descriptor => descriptor.ServiceType == typeof(IScopedMediator)).Lifetime);

        await using ServiceProvider provider = services.BuildServiceProvider(new ServiceProviderOptions
        {
            ValidateOnBuild = true,
            ValidateScopes = true,
        });
        Assert.Equal(0, Volatile.Read(ref materializationCount));

        IMediator firstRuntime = provider.GetRequiredService<IMediator>();
        IMediator secondRuntime = provider.GetRequiredService<IMediator>();

        Assert.Same(firstRuntime, secondRuntime);
        Assert.Equal(1, Volatile.Read(ref materializationCount));
        Assert.NotNull(runtimeConfiguration);
        Assert.Same(marker, registrationContext!.GetService(typeof(RegistrationMarker)));
        Assert.Same(timeProvider, firstRuntime.Context.TimeProvider);

        await using AsyncServiceScope firstScope = provider.CreateAsyncScope();
        await using AsyncServiceScope secondScope = provider.CreateAsyncScope();
        IScopedMediator firstScoped = firstScope.ServiceProvider.GetRequiredService<IScopedMediator>();
        IScopedMediator firstScopedAgain = firstScope.ServiceProvider.GetRequiredService<IScopedMediator>();
        IScopedMediator secondScoped = secondScope.ServiceProvider.GetRequiredService<IScopedMediator>();

        Assert.Same(firstScoped, firstScopedAgain);
        Assert.NotSame(firstScoped, secondScoped);
        Assert.Same(firstRuntime, Assert.IsType<ScopedMediator>(firstScoped).Endpoint);
        Assert.Same(firstRuntime, Assert.IsType<ScopedMediator>(secondScoped).Endpoint);
    }

    [Theory]
    [InlineData(false)]
    [InlineData(true)]
    [RequirementCoverage("REQ-VSB-MEDIATOR-DI", "deep-configuration-failure-preserves-exact-error-and-standard-mutation")]
    public void RegistrationCallbackFailure_PreservesTheExactFailureAndEstablishedServiceCollectionMutation(
        bool explicitBaseAddress)
    {
        var services = new ServiceCollection();
        var marker = new RegistrationMarker();
        var expected = new ConfigurationCallbackFailure();

        ConfigurationCallbackFailure actual = Assert.Throws<ConfigurationCallbackFailure>(() =>
        {
            void Configure(IMediatorRegistrationConfigurator configuration)
            {
                configuration.Services.AddSingleton(marker);
                throw expected;
            }

            if (explicitBaseAddress)
                services.AddMediator(new Uri("loopback://localhost/application"), Configure);
            else
                services.AddMediator(Configure);
        });

        Assert.Same(expected, actual);
        Assert.Contains(services, descriptor => descriptor.ServiceType == typeof(IMediator));
        Assert.Contains(services, descriptor => ReferenceEquals(descriptor.ImplementationInstance, marker));
    }

    [Fact]
    [RequirementCoverage("REQ-VSB-MEDIATOR-CONFIGURATION", "deep-constructor-unsupported-limits-and-observer-null-boundaries")]
    public async Task ConfigurationBoundaries_RejectMissingOwnersBeforeChangingRuntimeStateAsync()
    {
        Assert.Equal("registration", Assert.Throws<ArgumentNullException>(() =>
            new MediatorRegistrationContext(null!)).ParamName);

        IMediatorConfigurator unsupported = DispatchProxy.Create<IMediatorConfigurator, EmptyConfigurationProxy>();
        ConfigurationException unsupportedFailure = Assert.Throws<ConfigurationException>(() =>
            unsupported.Limits(MessageLimits.Conservative));
        Assert.Contains("cannot enforce receive limits", unsupportedFailure.Message, StringComparison.OrdinalIgnoreCase);

        ArgumentNullException? consumeFailure = null;
        ArgumentNullException? sendFailure = null;
        ArgumentNullException? publishFailure = null;
        await using IMediator mediator = MediatorFactory.Create(configuration =>
        {
            configuration.Limits(MessageLimits.Conservative);
            consumeFailure = Assert.Throws<ArgumentNullException>(() => configuration.ConnectConsumeObserver(null!));
            sendFailure = Assert.Throws<ArgumentNullException>(() => configuration.ConnectSendObserver(null!));
            publishFailure = Assert.Throws<ArgumentNullException>(() => configuration.ConnectPublishObserver(null!));
        });

        Assert.Equal("observer", consumeFailure!.ParamName);
        Assert.Equal("observer", sendFailure!.ParamName);
        Assert.Equal("observer", publishFailure!.ParamName);
    }

    [Fact]
    [RequirementCoverage("REQ-VSB-MEDIATOR-CONFIGURATION", "deep-message-limits-owner-state-transitions")]
    public void MessageLimitsOwner_RejectsDuplicateAndUnsupportedHostsWithoutReplacingTheAcceptedPolicy()
    {
        var topology = new InMemoryTopologyConfiguration(InMemoryBus.CreateMessageTopology());
        var bus = new InMemoryBusConfiguration(topology, new Uri("loopback://localhost/"));
        IReceiveEndpointConfiguration endpoint = bus.HostConfiguration.CreateReceiveEndpointConfiguration("limits-owner");
        var supported = new MediatorConfiguration(bus.HostConfiguration, endpoint);
        var limits = MessageLimits.Conservative;

        ((IMessageLimitsConfigurator)supported).SetMessageLimits(limits);

        ConfigurationException duplicate = Assert.Throws<ConfigurationException>(() =>
            ((IMessageLimitsConfigurator)supported).SetMessageLimits(new MessageLimits
            {
                WarnAboveBytes = 64,
                OffloadToMessageDataAboveBytes = 128,
                MaxBodyBytes = 256,
                MaxEnvelopeBytes = 512,
                MaxJsonDepth = 8,
            }));
        Assert.Contains("already declared", duplicate.Message, StringComparison.OrdinalIgnoreCase);
        Assert.Same(limits, ((IMessageLimitsHostConfiguration)bus.HostConfiguration).MessageLimits);

        IHostConfiguration unsupportedHost = DispatchProxy.Create<IHostConfiguration, EmptyConfigurationProxy>();
        var unsupported = new MediatorConfiguration(unsupportedHost, endpoint);

        ConfigurationException unsupportedFailure = Assert.Throws<ConfigurationException>(() =>
            ((IMessageLimitsConfigurator)unsupported).SetMessageLimits(limits));
        Assert.Contains("cannot enforce receive limits", unsupportedFailure.Message, StringComparison.OrdinalIgnoreCase);
    }

    [Fact]
    [RequirementCoverage("REQ-VSB-MEDIATOR-DI", "deep-registration-context-forwards-every-configuration-operation")]
    public void RegistrationContextAdapter_ForwardsEveryOperationToTheOwnedRegistrationBoundary()
    {
        var services = new ServiceCollection();
        using ServiceProvider provider = services.BuildServiceProvider();
        var selector = new DependencyInjectionContainerRegistrar(services);
        ISetScopedConsumeContext scopedContext = DispatchProxy.Create<ISetScopedConsumeContext, EmptyConfigurationProxy>();
        var registration = new RegistrationContext(provider, selector, scopedContext);
        var context = new MediatorRegistrationContext(registration);
        IReceiveEndpointConfigurator endpoint = DispatchProxy.Create<IReceiveEndpointConfigurator, EmptyConfigurationProxy>();
        ConsumeContext consumeContext = DispatchProxy.Create<ConsumeContext, EmptyConfigurationProxy>();

        Assert.Equal("serviceType", Assert.Throws<ArgumentNullException>(() => context.GetService(null!)).ParamName);
        Assert.Equal("consumerType", Assert.Throws<ArgumentNullException>(() =>
            context.ConfigureConsumer(null!, endpoint)).ParamName);
        Assert.Equal("configurator", Assert.Throws<ArgumentNullException>(() =>
            context.ConfigureConsumer<DelegatedConsumer>(null!)).ParamName);
        Assert.Equal("configurator", Assert.Throws<ArgumentNullException>(() =>
            context.ConfigureConsumers(null!)).ParamName);
        Assert.Equal("configurator", Assert.Throws<ArgumentNullException>(() =>
            context.ConfigureConsumerKinds(null!)).ParamName);
        Assert.Equal("sagaType", Assert.Throws<ArgumentNullException>(() =>
            context.ConfigureSaga(null!, endpoint)).ParamName);
        Assert.Equal("configurator", Assert.Throws<ArgumentNullException>(() =>
            context.ConfigureSaga<DelegatedMessage>(null!)).ParamName);
        Assert.Equal("configurator", Assert.Throws<ArgumentNullException>(() =>
            context.ConfigureSagas(null!)).ParamName);
        Assert.Equal("activityType", Assert.Throws<ArgumentNullException>(() =>
            context.ConfigureExecuteActivity(null!, endpoint)).ParamName);
        Assert.Equal("activityType", Assert.Throws<ArgumentNullException>(() =>
            context.ConfigureActivity(null!, endpoint, endpoint)).ParamName);
        Assert.Equal("activityType", Assert.Throws<ArgumentNullException>(() =>
            context.ConfigureActivityExecute(null!, endpoint, new Uri("loopback://localhost/compensate"))).ParamName);
        Assert.Equal("activityType", Assert.Throws<ArgumentNullException>(() =>
            context.ConfigureActivityCompensate(null!, endpoint)).ParamName);
        Assert.Equal("futureType", Assert.Throws<ArgumentNullException>(() =>
            context.ConfigureFuture(null!, endpoint)).ParamName);
        Assert.Equal("configurator", Assert.Throws<ArgumentNullException>(() =>
            context.ConfigureFuture<DelegatedMessage>(null!)).ParamName);
        Assert.Equal("scope", Assert.Throws<ArgumentNullException>(() =>
            context.PushContext(null!, consumeContext)).ParamName);
    }

    [Fact]
    [RequirementCoverage("REQ-VSB-MEDIATOR-DI", "deep-registration-context-completes-every-delegation-with-exact-inputs")]
    public void RegistrationContextAdapter_CompletesEveryDelegationAndPreservesExactInputs()
    {
        var marker = new RegistrationMarker();
        var consumer = new RecordingConsumerRegistration(typeof(DelegatedConsumer));
        var saga = new RecordingConsumerKind("Saga");
        var executeActivity = new RecordingConsumerKind("ExecuteActivity");
        var activity = new RecordingConsumerKind("Activity");
        var future = new RecordingConsumerKind("Future");
        var services = new ServiceCollection()
            .AddSingleton(marker)
            .AddSingleton<IConsumerKind>(saga)
            .AddSingleton<IConsumerKind>(executeActivity)
            .AddSingleton<IConsumerKind>(activity)
            .AddSingleton<IConsumerKind>(future);
        using ServiceProvider provider = services.BuildServiceProvider();
        var selector = new RecordingContainerSelector(consumer);
        var scopedContext = new RecordingScopedConsumeContext();
        var registration = new RegistrationContext(provider, selector, scopedContext);
        var context = new MediatorRegistrationContext(registration);
        IReceiveEndpointConfigurator endpoint = DispatchProxy.Create<IReceiveEndpointConfigurator, EmptyConfigurationProxy>();
        IReceiveEndpointConfigurator companionEndpoint = DispatchProxy.Create<IReceiveEndpointConfigurator, EmptyConfigurationProxy>();
        ConsumeContext consumeContext = DispatchProxy.Create<ConsumeContext, EmptyConfigurationProxy>();
        var companionAddress = new Uri("loopback://localhost/compensate");

        Assert.Same(marker, context.GetService(typeof(RegistrationMarker)));
        context.ConfigureConsumers(endpoint);
        context.ConfigureConsumer(typeof(DelegatedConsumer), endpoint);
        context.ConfigureConsumer<DelegatedConsumer>(endpoint);
        context.ConfigureConsumerKinds(endpoint);
        context.ConfigureSaga(typeof(DelegatedMessage), endpoint);
        context.ConfigureSaga<DelegatedMessage>(endpoint);
        context.ConfigureSagas(endpoint);
        context.ConfigureExecuteActivity(typeof(DelegatedMessage), endpoint);
        context.ConfigureActivity(typeof(DelegatedMessage), endpoint, companionEndpoint);
        context.ConfigureActivityExecute(typeof(DelegatedMessage), endpoint, companionAddress);
        context.ConfigureActivityCompensate(typeof(DelegatedMessage), companionEndpoint);
        context.ConfigureFuture(typeof(DelegatedMessage), endpoint);
        context.ConfigureFuture<DelegatedMessage>(endpoint);
        using IServiceScope scope = provider.CreateScope();

        IDisposable returnedHandle = context.PushContext(scope, consumeContext);

        Assert.Same(scopedContext.Handle, returnedHandle);
        Assert.Same(scope, scopedContext.Scope);
        Assert.Same(consumeContext, scopedContext.Context);
        Assert.Equal(3, consumer.ConfigureCount);
        Assert.Same(endpoint, consumer.Endpoint);
        Assert.Same(registration, consumer.Context);
        Assert.Equal((1, 1, 2, 0, 0, 0), saga.Calls);
        Assert.Equal((1, 0, 1, 0, 0, 0), executeActivity.Calls);
        Assert.Equal((0, 0, 1, 1, 1, 1), activity.Calls);
        Assert.Equal((1, 1, 1, 0, 0, 0), future.Calls);
        Assert.All(new[] { saga, executeActivity, activity, future }, kind =>
        {
            Assert.Same(endpoint, kind.Endpoint);
            Assert.Same(registration, kind.Context);
        });
        Assert.Same(companionEndpoint, activity.CompanionEndpoint);
        Assert.Equal(companionAddress, activity.CompanionAddress);
    }

    [Fact]
    [RequirementCoverage("REQ-VSB-SCOPED-MEDIATOR", "deep-pipe-publish-overloads-reject-each-missing-owner")]
    public async Task PipePublishOverloads_RejectNullPayloadsAndPipesAtTheScopedBoundaryAsync()
    {
        await using IMediator mediator = MediatorFactory.Create(configuration =>
            configuration.Limits(MessageLimits.Conservative));
        await using ServiceProvider provider = new ServiceCollection().BuildServiceProvider();
        await using var scoped = new ScopedMediator(mediator, provider);
        IAdvancedPublishEndpoint publish = scoped;
        CancellationToken token = TestContext.Current.CancellationToken;
        var message = new DelegatedMessage();
        IPipe<PublishContext<DelegatedMessage>> typedPipe = Pipe.Empty<PublishContext<DelegatedMessage>>();
        IPipe<PublishContext> untypedPipe = Pipe.Empty<PublishContext>();

        await AssertParameterAsync("message", () => publish.PublishAsync(
            (DelegatedMessage)null!, typedPipe, token));
        await AssertParameterAsync("publishPipe", () => publish.PublishAsync(
            message, (IPipe<PublishContext<DelegatedMessage>>)null!, token));
        await AssertParameterAsync("message", () => publish.PublishAsync(
            (DelegatedMessage)null!, untypedPipe, token));
        await AssertParameterAsync("publishPipe", () => publish.PublishAsync(
            message, (IPipe<PublishContext>)null!, token));
        await AssertParameterAsync("values", () => publish.PublishAsync<DelegatedMessage>(
            (object)null!, typedPipe, token));
        await AssertParameterAsync("publishPipe", () => publish.PublishAsync<DelegatedMessage>(
            new { Value = "valid" }, (IPipe<PublishContext<DelegatedMessage>>)null!, token));
        await AssertParameterAsync("values", () => publish.PublishAsync<DelegatedMessage>(
            (object)null!, untypedPipe, token));
        await AssertParameterAsync("publishPipe", () => publish.PublishAsync<DelegatedMessage>(
            new { Value = "valid" }, (IPipe<PublishContext>)null!, token));
    }

    [Fact]
    [RequirementCoverage("REQ-VSB-SCOPED-MEDIATOR", "deep-runtime-publish-and-constructor-null-branches")]
    public async Task RuntimePublishOverloadsAndConstructor_RejectEveryMissingOwnerAtTheScopedBoundaryAsync()
    {
        await using IMediator mediator = MediatorFactory.Create(configuration =>
            configuration.Limits(MessageLimits.Conservative));
        Assert.Equal("provider", Assert.Throws<ArgumentNullException>(() =>
            new ScopedMediator(mediator, null!)).ParamName);

        await using ServiceProvider provider = new ServiceCollection().BuildServiceProvider();
        await using var scoped = new ScopedMediator(mediator, provider);
        IAdvancedPublishEndpoint publish = scoped;
        CancellationToken token = TestContext.Current.CancellationToken;
        var message = new DelegatedMessage();
        IPipe<PublishContext> pipe = Pipe.Empty<PublishContext>();

        await AssertParameterAsync("message", () => publish.PublishAsync((object)null!, token));
        await AssertParameterAsync("message", () => publish.PublishAsync((object)null!, pipe, token));
        await AssertParameterAsync("publishPipe", () => publish.PublishAsync(
            (object)message, (IPipe<PublishContext>)null!, token));
        await AssertParameterAsync("message", () => publish.PublishAsync((object)null!, typeof(DelegatedMessage), token));
        await AssertParameterAsync("messageType", () => publish.PublishAsync(message, (Type)null!, token));
        await AssertParameterAsync("message", () => publish.PublishAsync((object)null!, typeof(DelegatedMessage), pipe, token));
        await AssertParameterAsync("messageType", () => publish.PublishAsync(message, (Type)null!, pipe, token));
        await AssertParameterAsync("publishPipe", () => publish.PublishAsync(
            message, typeof(DelegatedMessage), (IPipe<PublishContext>)null!, token));
    }

    [Fact]
    [RequirementCoverage("REQ-VSB-SCOPED-MEDIATOR", "deep-dispose-before-client-factory-creation-is-terminal")]
    public async Task DisposeBeforeClientFactoryCreation_PreventsLateOwnedFactoryCreationAsync()
    {
        await using IMediator mediator = MediatorFactory.Create(configuration =>
            configuration.Limits(MessageLimits.Conservative));
        await using ServiceProvider provider = new ServiceCollection().BuildServiceProvider();
        var scoped = new ScopedMediator(mediator, provider);

        await scoped.DisposeAsync();
        await scoped.DisposeAsync();

        ObjectDisposedException failure = Assert.Throws<ObjectDisposedException>(() => _ = scoped.Context);
        Assert.Equal(typeof(ScopedMediator).FullName, failure.ObjectName);
    }

    [Fact]
    [RequirementCoverage("REQ-VSB-SCOPED-MEDIATOR", "deep-dispose-racing-held-client-factory-creation-owns-cleanup")]
    public async Task DisposeRacingHeldClientFactoryCreation_AwaitsAndOwnsTheFactoryCleanupAsync()
    {
        await using IMediator mediator = MediatorFactory.Create(configuration =>
            configuration.Limits(MessageLimits.Conservative));
        await using ServiceProvider provider = new ServiceCollection().BuildServiceProvider();
        var scoped = new ScopedMediator(mediator, provider);
        var context = new DisposableClientFactoryContext();
        var factoryEntered = new TaskCompletionSource(TaskCreationOptions.RunContinuationsAsynchronously);
        var releaseFactory = new TaskCompletionSource(TaskCreationOptions.RunContinuationsAsynchronously);
        ClientFactoryField(scoped) = new Lazy<ClientFactory>(() =>
        {
            factoryEntered.TrySetResult();
            releaseFactory.Task.GetAwaiter().GetResult();
            return new ClientFactory(context);
        }, LazyThreadSafetyMode.ExecutionAndPublication);

        Task<ClientFactoryContext> access = Task.Run(() => scoped.Context, TestContext.Current.CancellationToken);
        await factoryEntered.Task.WaitAsync(TestContext.Current.CancellationToken);
        var disposeEntered = new TaskCompletionSource(TaskCreationOptions.RunContinuationsAsynchronously);
        Task disposal = Task.Run(async () =>
        {
            disposeEntered.TrySetResult();
            await scoped.DisposeAsync();
        }, TestContext.Current.CancellationToken);
        await disposeEntered.Task.WaitAsync(TestContext.Current.CancellationToken);
        await Task.Delay(TimeSpan.FromMilliseconds(25), TestContext.Current.CancellationToken);

        Assert.False(disposal.IsCompleted);
        Assert.Equal(0, context.DisposeCount);

        releaseFactory.TrySetResult();
        Assert.Same(context, await access.WaitAsync(TestContext.Current.CancellationToken));
        await disposal.WaitAsync(TestContext.Current.CancellationToken);
        await Task.WhenAll(Enumerable.Range(0, 16)
            .Select(_ => scoped.DisposeAsync().AsTask()));

        Assert.Equal(1, context.DisposeCount);
    }

    [UnsafeAccessor(UnsafeAccessorKind.Field, Name = "_clientFactory")]
    private static extern ref Lazy<ClientFactory> ClientFactoryField(ScopedMediator mediator);

    private class EmptyConfigurationProxy : DispatchProxy
    {
        protected override object? Invoke(MethodInfo? targetMethod, object?[]? args) =>
            targetMethod?.ReturnType != typeof(void) && targetMethod?.ReturnType.IsValueType == true
                ? Activator.CreateInstance(targetMethod.ReturnType)
                : null;
    }

    private static async Task AssertParameterAsync(string expected, Func<Task> action)
    {
        ArgumentNullException failure = await Assert.ThrowsAsync<ArgumentNullException>(action);
        Assert.Equal(expected, failure.ParamName);
    }

    private sealed class RecordingContainerSelector(params IConsumerRegistration[] registrations) : IContainerSelector
    {
        public bool TryGetRegistration<T>(IServiceProvider provider, Type type, [NotNullWhen(true)] out T? value)
            where T : class, IRegistration
        {
            value = registrations.FirstOrDefault(registration => registration.Type == type) as T;
            return value is not null;
        }

        public IEnumerable<T> GetRegistrations<T>(IServiceProvider provider)
            where T : class, IRegistration => registrations.OfType<T>();

        public T? GetDefinition<T>(IServiceProvider provider)
            where T : class, IDefinition => null;

        public IEndpointDefinition<T>? GetEndpointDefinition<T>(IServiceProvider provider)
            where T : class => null;

        public IConfigureReceiveEndpoint GetConfigureReceiveEndpoints(IServiceProvider provider) =>
            throw new NotSupportedException();

        public IEndpointNameFormatter GetEndpointNameFormatter(IServiceProvider provider) =>
            throw new NotSupportedException();
    }

    private sealed class RecordingConsumerRegistration(Type type) : IConsumerRegistration
    {
        public Type Type { get; } = type;
        public bool IncludeInConfigureEndpoints { get; set; } = true;
        public bool RequiresServiceInstance { get; set; }
        public int ConfigureCount { get; private set; }
        public IReceiveEndpointConfigurator? Endpoint { get; private set; }
        public IRegistrationContext? Context { get; private set; }

        public void AddConfigureAction<T>(Action<IRegistrationContext, IConsumerConfigurator<T>>? configure)
            where T : class, IConsumer
        {
        }

        public void Configure(IReceiveEndpointConfigurator configurator, IRegistrationContext context)
        {
            ConfigureCount++;
            Endpoint = configurator;
            Context = context;
        }

        public IConsumerDefinition GetDefinition(IRegistrationContext context) => throw new NotSupportedException();

        public IConsumerRegistrationConfigurator GetConsumerRegistrationConfigurator(IRegistrationConfigurator registrationConfigurator) =>
            throw new NotSupportedException();
    }

    private sealed class RecordingConsumerKind(string name) :
        IConsumerKind,
        IConsumerKindRuntimeConfigurator,
        IConsumerKindTypedConfigurator,
        IConsumerKindBulkConfigurator,
        IConsumerKindCompanionConfigurator
    {
        private int _runtimeCalls;
        private int _typedCalls;
        private int _bulkCalls;
        private int _pairCalls;
        private int _primaryCalls;
        private int _companionCalls;

        public bool IsFallback => false;
        public string Name { get; } = name;
        public int Order => 0;
        public IReceiveEndpointConfigurator? Endpoint { get; private set; }
        public IReceiveEndpointConfigurator? CompanionEndpoint { get; private set; }
        public IRegistrationContext? Context { get; private set; }
        public Uri? CompanionAddress { get; private set; }
        public (int Runtime, int Typed, int Bulk, int Pair, int Primary, int Companion) Calls =>
            (_runtimeCalls, _typedCalls, _bulkCalls, _pairCalls, _primaryCalls, _companionCalls);

        public IEnumerable<IConsumerKindRegistration> GetRegistrations(IConsumerKindContext context) => [];

        public void ConfigureTestHarness(IConsumerKindTestHarnessContext context)
        {
        }

        public bool TryConfigure(Type registrationType, IReceiveEndpointConfigurator endpointConfigurator,
            IRegistrationContext registrationContext)
        {
            _runtimeCalls++;
            Record(endpointConfigurator, registrationContext);
            return true;
        }

        public bool TryConfigure<TRegistration>(IReceiveEndpointConfigurator endpointConfigurator,
            IRegistrationContext registrationContext, Delegate? configure = null)
            where TRegistration : class
        {
            _typedCalls++;
            Record(endpointConfigurator, registrationContext);
            return true;
        }

        public IReadOnlyCollection<Type> ConfigureAll(IReceiveEndpointConfigurator endpointConfigurator,
            IRegistrationContext registrationContext, IReadOnlySet<Type> excludedRegistrationTypes)
        {
            _bulkCalls++;
            Record(endpointConfigurator, registrationContext);
            return [];
        }

        public bool TryConfigurePair(Type registrationType, IReceiveEndpointConfigurator primaryEndpointConfigurator,
            IReceiveEndpointConfigurator companionEndpointConfigurator, IRegistrationContext registrationContext)
        {
            _pairCalls++;
            Record(primaryEndpointConfigurator, registrationContext);
            CompanionEndpoint = companionEndpointConfigurator;
            return true;
        }

        public bool TryConfigurePrimary(Type registrationType, IReceiveEndpointConfigurator primaryEndpointConfigurator,
            Uri companionAddress, IRegistrationContext registrationContext)
        {
            _primaryCalls++;
            Record(primaryEndpointConfigurator, registrationContext);
            CompanionAddress = companionAddress;
            return true;
        }

        public bool TryConfigureCompanion(Type registrationType, IReceiveEndpointConfigurator companionEndpointConfigurator,
            IRegistrationContext registrationContext)
        {
            _companionCalls++;
            Context = registrationContext;
            CompanionEndpoint = companionEndpointConfigurator;
            return true;
        }

        private void Record(IReceiveEndpointConfigurator endpoint, IRegistrationContext context)
        {
            Endpoint = endpoint;
            Context = context;
        }
    }

    private sealed class RecordingScopedConsumeContext : ISetScopedConsumeContext
    {
        public IDisposable Handle { get; } = new RecordingDisposable();
        public IServiceScope? Scope { get; private set; }
        public ConsumeContext? Context { get; private set; }

        public IDisposable PushContext(IServiceScope serviceProvider, ConsumeContext context)
        {
            Scope = serviceProvider;
            Context = context;
            return Handle;
        }
    }

    private sealed class RecordingDisposable : IDisposable
    {
        public void Dispose()
        {
        }
    }

    private sealed class DisposableClientFactoryContext : ClientFactoryContext, IAsyncDisposable
    {
        private int _disposeCount;

        public int DisposeCount => Volatile.Read(ref _disposeCount);
        public RequestTimeout DefaultTimeout { get; } = new(TimeSpan.FromSeconds(10));
        public TimeProvider TimeProvider { get; } = TimeProvider.System;
        public IMessageRouteTable MessageRoutes { get; } = new MessageRouteTable();
        public Uri ResponseAddress { get; } = new("loopback://localhost/response");

        public ConnectHandle ConnectConsumePipe<T>(IPipe<ConsumeContext<T>> pipe) where T : class =>
            throw new NotSupportedException();

        public ConnectHandle ConnectConsumePipe<T>(IPipe<ConsumeContext<T>> pipe, ConnectPipeOptions options) where T : class =>
            throw new NotSupportedException();

        public ConnectHandle ConnectRequestPipe<T>(Guid requestId, IPipe<ConsumeContext<T>> pipe) where T : class =>
            throw new NotSupportedException();

        public IRequestSendEndpoint<T> GetRequestEndpoint<T>(ConsumeContext? consumeContext = default) where T : class =>
            throw new NotSupportedException();

        public IRequestSendEndpoint<T> GetRequestEndpoint<T>(Uri destinationAddress, ConsumeContext? consumeContext = default) where T : class =>
            throw new NotSupportedException();

        public ValueTask DisposeAsync()
        {
            Interlocked.Increment(ref _disposeCount);
            return default;
        }
    }

    private sealed class TestTimeProvider : TimeProvider;
    private sealed class RegistrationMarker;
    private sealed class ConfigurationCallbackFailure : Exception;
    private sealed record DelegatedMessage;

    private sealed class DelegatedConsumer : IConsumer<DelegatedMessage>
    {
        public Task ConsumeAsync(ConsumeContext<DelegatedMessage> context) => Task.CompletedTask;
    }
}

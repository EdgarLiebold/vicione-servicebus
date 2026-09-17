using System.Collections;
using System.Reflection;
using System.Text;
using Microsoft.Extensions.DependencyInjection;
using ViciOne.ServiceBus.Configuration;
using ViciOne.ServiceBus.Middleware;
using ViciOne.ServiceBus.RetryPolicies;
using ViciOne.ServiceBus.Saga;
using ViciOne.ServiceBus.Sagas.Configuration;
using ViciOne.ServiceBus.SagaStateMachine;
using ViciOne.ServiceBus.Tests.Infrastructure.Requirements;
using ViciOne.ServiceBus.Tests.InternalAccess.InMemoryOutbox;
using Xunit;

namespace ViciOne.ServiceBus.Tests.Sagas;

public sealed class SagaPipelineAndStateMachineConfigurationDeepContractTests
{
    [Fact]
    [RequirementCoverage("REQ-VSB-STATE-MACHINE-CONFIGURATION", "pipeline-required-and-invalid-boundary-matrix")]
    public void SagaPipelineExtensions_RejectEveryInvalidRequiredBoundaryBeforeCollaboratorEffects()
    {
        ISagaConfigurator<PipelineSaga> sagaConfigurator = StrictStub<ISagaConfigurator<PipelineSaga>>();
        var pipeConfigurator = new RecordingPipeConfigurator<SagaConsumeContext<PipelineSaga>>();
        IReceiveEndpointConfigurator endpointConfigurator = StrictStub<IReceiveEndpointConfigurator>();
        IRegistrationContext registrationContext = StrictStub<IRegistrationContext>();
        IBusFactoryConfigurator busFactoryConfigurator = StrictStub<IBusFactoryConfigurator>();
        IPipe<ExceptionSagaConsumeContext<PipelineSaga>> rescuePipe = StrictStub<IPipe<ExceptionSagaConsumeContext<PipelineSaga>>>();

        AssertArgument("configurator", () =>
            SagaPipelineConfigurationExtensions.UseConcurrencyLimit<PipelineSaga>(null!, 1));
        AssertRange("concurrencyLimit", () => sagaConfigurator.UseConcurrencyLimit(0));
        AssertArgument("configurator", () =>
            SagaPipelineConfigurationExtensions.UseConcurrencyLimit<PipelineSaga>(null!, 1, endpointConfigurator));
        AssertRange("concurrencyLimit", () => sagaConfigurator.UseConcurrencyLimit(0, endpointConfigurator));
        AssertArgument("managementEndpointConfigurator", () => sagaConfigurator.UseConcurrencyLimit(1, null!));
        AssertInvalidArgument("limiterId", () => sagaConfigurator.UseConcurrencyLimit(1, endpointConfigurator, " \t"));

        AssertArgument("configurator", () =>
            SagaPipelineConfigurationExtensions.UseDelayedRedelivery<PipelineSaga>(null!, _ => { }));
        AssertArgument("configure", () => sagaConfigurator.UseDelayedRedelivery(null!));
        AssertArgument("configurator", () =>
            SagaPipelineConfigurationExtensions.UseVolatileOutbox<PipelineSaga>(null!, registrationContext));
        AssertArgument("context", () => sagaConfigurator.UseVolatileOutbox(null!, null));
        AssertArgument("configurator", () =>
            SagaPipelineConfigurationExtensions.UseVolatileOutbox<PipelineSaga>(
                null!,
                (Action<IOutboxConfigurator>?)null));

        AssertArgument("configurator", () =>
            SagaPipelineConfigurationExtensions.UseMessageRetry<PipelineSaga>(null!, _ => { }));
        AssertArgument("configure", () => sagaConfigurator.UseMessageRetry(null!));
        AssertArgument("configurator", () =>
            SagaPipelineConfigurationExtensions.UseMessageRetry<PipelineSaga>(null!, busFactoryConfigurator, _ => { }));
        AssertArgument("busFactoryConfigurator", () => sagaConfigurator.UseMessageRetry(null!, _ => { }));
        AssertArgument("configure", () => sagaConfigurator.UseMessageRetry(busFactoryConfigurator, null!));
        AssertArgument("configurator", () =>
            SagaPipelineConfigurationExtensions.UseMessageRetry<PipelineSaga>(
                (IPipeConfigurator<SagaConsumeContext<PipelineSaga>>)null!,
                _ => { }));
        AssertArgument("configure", () => pipeConfigurator.UseMessageRetry(null!));
        AssertArgument("connector", () => pipeConfigurator.UseMessageRetry(null!, _ => { }));
        AssertArgument("configure", () => pipeConfigurator.UseMessageRetry(busFactoryConfigurator, null!));

        AssertArgument("configurator", () =>
            SagaPipelineConfigurationExtensions.UseScheduledRedelivery<PipelineSaga>(null!, _ => { }));
        AssertArgument("configure", () => sagaConfigurator.UseScheduledRedelivery(null!));
        AssertArgument("configurator", () =>
            SagaPipelineConfigurationExtensions.UseTimeout<PipelineSaga>(null!, _ => { }));
        AssertArgument("configure", () => sagaConfigurator.UseTimeout(null!));
        AssertArgument("configurator", () =>
            SagaPipelineConfigurationExtensions.UseRescue<PipelineSaga>(null!, rescuePipe));
        AssertArgument("rescuePipe", () => pipeConfigurator.UseRescue(null!));

        AssertArgument("configurator", () =>
            SagaPipelineConfigurationExtensions.UsePartitioner<PipelineSaga>(
                null!,
                1,
                context => context.Saga.CorrelationId));
        AssertArgument("keyProvider", () =>
            pipeConfigurator.UsePartitioner(1, (Func<SagaConsumeContext<PipelineSaga>, Guid>)null!));
        AssertRange("partitionCount", () =>
            pipeConfigurator.UsePartitioner(0, context => context.Saga.CorrelationId));
        AssertArgument("configurator", () =>
            SagaPipelineConfigurationExtensions.UsePartitioner<PipelineSaga>(
                null!,
                1,
                context => context.Saga.Key));
        AssertArgument("keyProvider", () =>
            pipeConfigurator.UsePartitioner(1, (Func<SagaConsumeContext<PipelineSaga>, string>)null!));
        AssertRange("partitionCount", () =>
            pipeConfigurator.UsePartitioner(0, context => context.Saga.Key));

        var minimumGuidPartitioner = new RecordingPipeConfigurator<SagaConsumeContext<PipelineSaga>>();
        var minimumTextPartitioner = new RecordingPipeConfigurator<SagaConsumeContext<PipelineSaga>>();
        minimumGuidPartitioner.UsePartitioner(1, context => context.Saga.CorrelationId);
        minimumTextPartitioner.UsePartitioner(1, context => context.Saga.Key);
        Assert.NotNull(minimumGuidPartitioner.Specification);
        Assert.NotNull(minimumTextPartitioner.Specification);

        AssertArgument("configurator", () =>
            SagaPipelineConfigurationExtensions.ConfigureSaga(null!, registrationContext, typeof(PipelineSaga)));
        AssertArgument("registration", () => endpointConfigurator.ConfigureSaga(null!, typeof(PipelineSaga)));
        AssertArgument("sagaType", () => endpointConfigurator.ConfigureSaga(registrationContext, null!));
        AssertArgument("configurator", () =>
            SagaPipelineConfigurationExtensions.ConfigureSaga<PipelineSaga>(null!, registrationContext));
        AssertArgument("registration", () => endpointConfigurator.ConfigureSaga<PipelineSaga>(null!));
        AssertArgument("configurator", () =>
            SagaPipelineConfigurationExtensions.ConfigureSagas(null!, registrationContext));
        AssertArgument("registration", () => endpointConfigurator.ConfigureSagas(null!));

        AssertArgument("services", () =>
            SagaPipelineConfigurationExtensions.AddEventObserver<ConnectorState, RecordingEventObserver>(null!));
        AssertArgument("factory", () =>
            new ServiceCollection().AddEventObserver<ConnectorState, RecordingEventObserver>(null!));
        AssertArgument("services", () =>
            SagaPipelineConfigurationExtensions.AddStateObserver<ConnectorState, RecordingStateObserver>(null!));
        AssertArgument("factory", () =>
            new ServiceCollection().AddStateObserver<ConnectorState, RecordingStateObserver>(null!));
    }

    [Fact]
    [RequirementCoverage("REQ-VSB-STATE-MACHINE-CONFIGURATION", "saga-observer-positive-connection-and-input-identity")]
    public void SagaObserverExtensions_ConnectExactlyOneObserverAndPreserveEveryInput()
    {
        ISagaConfigurator<PipelineSaga> sagaConfigurator = RecordingStub<ISagaConfigurator<PipelineSaga>>(
            out RecordingDispatchProxy sagaCalls);
        IBusFactoryConfigurator busFactoryConfigurator = RecordingStub<IBusFactoryConfigurator>(
            out RecordingDispatchProxy busCalls);
        IOutboxRegistrationContext registrationContext = RecordingStub<IOutboxRegistrationContext>(out _);
        Action<IRedeliveryConfigurator> delayedCallback = _ => { };
        Action<IOutboxConfigurator> registeredOutboxCallback = _ => { };
        Action<IOutboxConfigurator> unscopedOutboxCallback = _ => { };
        Action<IRetryConfigurator> retryCallback = _ => { };
        Action<IRetryConfigurator> scheduledCallback = _ => { };

        sagaConfigurator.UseDelayedRedelivery(delayedCallback);
        sagaConfigurator.UseVolatileOutbox(registrationContext, registeredOutboxCallback);
        sagaConfigurator.UseVolatileOutbox(unscopedOutboxCallback);
        sagaConfigurator.UseMessageRetry(busFactoryConfigurator, retryCallback);
        sagaConfigurator.UseScheduledRedelivery(scheduledCallback);

        object[] observers = sagaCalls.Invocations.Select(invocation =>
        {
            Assert.Equal(nameof(ISagaConfigurationObserverConnector.ConnectSagaConfigurationObserver), invocation.Method.Name);
            return Assert.Single(invocation.Arguments)!;
        }).ToArray();
        Assert.Equal(5, observers.Length);

        var delayed = Assert.IsType<DelayedRedeliverySagaConfigurationObserver<PipelineSaga>>(observers[0]);
        Assert.Same(sagaConfigurator, ReadField(delayed, "_configurator"));
        Assert.Same(delayedCallback, ReadField(delayed, "_configure"));

        var registeredOutbox = Assert.IsType<InMemoryOutboxSagaConfigurationObserver<PipelineSaga>>(observers[1]);
        Assert.Same(registrationContext, ReadField(registeredOutbox, "_setter"));
        Assert.Same(sagaConfigurator, ReadField(registeredOutbox, "_configurator"));
        Assert.Same(registeredOutboxCallback, ReadField(registeredOutbox, "_configure"));

        var unscopedOutbox = Assert.IsType<InMemoryOutboxSagaConfigurationObserver<PipelineSaga>>(observers[2]);
        Assert.Null(ReadField(unscopedOutbox, "_setter"));
        Assert.Same(sagaConfigurator, ReadField(unscopedOutbox, "_configurator"));
        Assert.Same(unscopedOutboxCallback, ReadField(unscopedOutbox, "_configure"));

        var retryBusObserver = Assert.IsType<RetryBusObserver>(AssertSingleArgumentCall(
            busCalls,
            nameof(IBusObserverConnector.ConnectBusObserver)));
        var retry = Assert.IsType<MessageRetrySagaConfigurationObserver<PipelineSaga>>(observers[3]);
        Assert.Same(sagaConfigurator, ReadField(retry, "_configurator"));
        Assert.Same(retryCallback, ReadField(retry, "_configure"));
        Assert.Equal(retryBusObserver.Stopping, Assert.IsType<CancellationToken>(ReadField(retry, "_cancellationToken")));

        var scheduled = Assert.IsType<ScheduledRedeliverySagaConfigurationObserver<PipelineSaga>>(observers[4]);
        Assert.Same(sagaConfigurator, ReadField(scheduled, "_configurator"));
        Assert.Same(scheduledCallback, ReadField(scheduled, "_configure"));
    }

    [Fact]
    [RequirementCoverage("REQ-VSB-STATE-MACHINE-CONFIGURATION", "saga-pipeline-retry-rescue-specification-and-context-factory")]
    public async Task SagaPipeExtensions_AddExactRetryAndRescueSpecificationsAndProjectTheSagaContextAsync()
    {
        var directRetryConfiguration = new RecordingPipeConfigurator<SagaConsumeContext<PipelineSaga>>();
        var busRetryConfiguration = new RecordingPipeConfigurator<SagaConsumeContext<PipelineSaga>>();
        IBusFactoryConfigurator busFactoryConfigurator = RecordingStub<IBusFactoryConfigurator>(
            out RecordingDispatchProxy busCalls);
        IRetryConfigurator? directRetryCallbackArgument = null;
        IRetryConfigurator? busRetryCallbackArgument = null;
        var directRetryCallbackCalls = 0;
        var busRetryCallbackCalls = 0;

        directRetryConfiguration.UseMessageRetry(configurator =>
        {
            directRetryCallbackCalls++;
            directRetryCallbackArgument = configurator;
            configurator.None();
        });
        busRetryConfiguration.UseMessageRetry(busFactoryConfigurator, configurator =>
        {
            busRetryCallbackCalls++;
            busRetryCallbackArgument = configurator;
            configurator.None();
        });

        Assert.Equal(1, directRetryCallbackCalls);
        Assert.Same(directRetryConfiguration.Specification, directRetryCallbackArgument);
        Assert.NotNull(directRetryConfiguration.Specification);
        Assert.Empty(directRetryConfiguration.Specification.Validate());
        Assert.Equal(1, busRetryCallbackCalls);
        Assert.Same(busRetryConfiguration.Specification, busRetryCallbackArgument);
        Assert.NotNull(busRetryConfiguration.Specification);
        Assert.Empty(busRetryConfiguration.Specification.Validate());

        var retryBusObserver = Assert.IsType<RetryBusObserver>(AssertSingleArgumentCall(
            busCalls,
            nameof(IBusObserverConnector.ConnectBusObserver)));
        Assert.Equal(
            retryBusObserver.Stopping,
            Assert.IsType<CancellationToken>(ReadField(busRetryConfiguration.Specification, "_cancellationToken")));

        var saga = new PipelineSaga { CorrelationId = Guid.NewGuid(), Key = "retry-key" };
        using InMemorySagaConsumeContext<PipelineSaga, PipelineMessage> context = await CreateContextAsync(saga);
        var contextFactory = Assert.IsAssignableFrom<Delegate>(ReadField(directRetryConfiguration.Specification, "_contextFactory"));
        object? retryContextValue = contextFactory.DynamicInvoke(context, Retry.None, null);
        Assert.NotNull(retryContextValue);
        object retryContext = retryContextValue;
        var projectedSagaContext = Assert.IsAssignableFrom<SagaConsumeContext<PipelineSaga>>(retryContext);
        Assert.Same(context, ReadField(retryContext, "_context"));
        Assert.Same(saga, projectedSagaContext.Saga);

        IPipe<ExceptionSagaConsumeContext<PipelineSaga>> rescuePipe = StrictStub<IPipe<ExceptionSagaConsumeContext<PipelineSaga>>>();
        var configuredRescue = new RecordingPipeConfigurator<SagaConsumeContext<PipelineSaga>>();
        var defaultRescue = new RecordingPipeConfigurator<SagaConsumeContext<PipelineSaga>>();
        IExceptionConfigurator? rescueCallbackArgument = null;
        var rescueCallbackCalls = 0;
        configuredRescue.UseRescue(rescuePipe, configurator =>
        {
            rescueCallbackCalls++;
            rescueCallbackArgument = configurator;
        });
        defaultRescue.UseRescue(rescuePipe);

        Assert.Equal(1, rescueCallbackCalls);
        Assert.Same(configuredRescue.Specification, rescueCallbackArgument);
        var configuredSpecification = Assert.IsType<SagaConsumeContextRescuePipeSpecification<PipelineSaga>>(
            configuredRescue.Specification);
        var defaultSpecification = Assert.IsType<SagaConsumeContextRescuePipeSpecification<PipelineSaga>>(
            defaultRescue.Specification);
        Assert.Same(rescuePipe, ReadField(configuredSpecification, "_rescuePipe"));
        Assert.Same(rescuePipe, ReadField(defaultSpecification, "_rescuePipe"));
        Assert.Empty(configuredSpecification.Validate());
        Assert.Empty(defaultSpecification.Validate());
    }

    [Fact]
    [RequirementCoverage("REQ-VSB-STATE-MACHINE-CONFIGURATION", "saga-registration-runtime-delegation")]
    public void SagaRegistrationExtensions_DelegateTheExactRuntimeTypeEndpointAndAllSagaRequest()
    {
        IReceiveEndpointConfigurator endpoint = StrictStub<IReceiveEndpointConfigurator>();
        IRegistrationContext registration = RecordingStub<IRegistrationContext>(out RecordingDispatchProxy calls);

        endpoint.ConfigureSaga(registration, typeof(PipelineSaga));
        endpoint.ConfigureSagas(registration);

        Assert.Collection(
            calls.Invocations,
            invocation =>
            {
                Assert.Equal(nameof(IRegistrationContext.ConfigureSaga), invocation.Method.Name);
                Assert.False(invocation.Method.IsGenericMethod);
                Assert.Equal(2, invocation.Arguments.Length);
                Assert.Same(typeof(PipelineSaga), invocation.Arguments[0]);
                Assert.Same(endpoint, invocation.Arguments[1]);
            },
            invocation =>
            {
                Assert.Equal(nameof(IRegistrationContext.ConfigureSagas), invocation.Method.Name);
                Assert.False(invocation.Method.IsGenericMethod);
                Assert.Same(endpoint, Assert.Single(invocation.Arguments));
            });
    }

    [Fact]
    [RequirementCoverage("REQ-VSB-STATE-MACHINE-PARTITION", "saga-partitioner-guid-text-encoding-and-null-key")]
    public async Task SagaPartitioners_PreserveGuidAndTextBytesAndRejectANullTextKeyAsync()
    {
        var saga = new PipelineSaga
        {
            CorrelationId = Guid.Parse("00112233-4455-6677-8899-aabbccddeeff"),
            Key = "Å",
        };
        using InMemorySagaConsumeContext<PipelineSaga, PipelineMessage> context = await CreateContextAsync(saga);
        var guidConfiguration = new RecordingPipeConfigurator<SagaConsumeContext<PipelineSaga>>();
        var defaultTextConfiguration = new RecordingPipeConfigurator<SagaConsumeContext<PipelineSaga>>();
        var explicitTextConfiguration = new RecordingPipeConfigurator<SagaConsumeContext<PipelineSaga>>();

        guidConfiguration.UsePartitioner(3, sagaContext => sagaContext.Saga.CorrelationId);
        defaultTextConfiguration.UsePartitioner(3, sagaContext => sagaContext.Saga.Key);
        explicitTextConfiguration.UsePartitioner(3, sagaContext => sagaContext.Saga.Key, Encoding.BigEndianUnicode);

        Assert.Equal(saga.CorrelationId.ToByteArray(), SelectPartitionKey(guidConfiguration.Specification, context));
        Assert.Equal(Encoding.UTF8.GetBytes(saga.Key), SelectPartitionKey(defaultTextConfiguration.Specification, context));
        Assert.Equal(Encoding.BigEndianUnicode.GetBytes(saga.Key), SelectPartitionKey(explicitTextConfiguration.Specification, context));

        saga.Key = null!;
        TargetInvocationException wrapper = Assert.Throws<TargetInvocationException>(() =>
            SelectPartitionKey(defaultTextConfiguration.Specification, context));
        InvalidOperationException exception = Assert.IsType<InvalidOperationException>(wrapper.InnerException);
        Assert.Equal("The partition key provider returned null.", exception.Message);
    }

    [Fact]
    [RequirementCoverage("REQ-VSB-STATE-MACHINE-OBSERVATION", "observer-registration-collection-dedup-and-singleton-identity")]
    public void ObserverRegistration_ReturnsTheInputCollectionAndPreservesExactSingletonIdentity()
    {
        var eventServices = new ServiceCollection();
        var eventObserver = new RecordingEventObserver();
        var eventFactoryCalls = 0;

        IServiceCollection eventResult = eventServices.AddEventObserver<ConnectorState, RecordingEventObserver>(_ =>
        {
            eventFactoryCalls++;
            return eventObserver;
        });
        eventServices.AddEventObserver<ConnectorState, RecordingEventObserver>(_ => new RecordingEventObserver());

        Assert.Same(eventServices, eventResult);
        ServiceDescriptor eventDescriptor = Assert.Single(eventServices);
        Assert.Equal(typeof(IEventObserver<ConnectorState>), eventDescriptor.ServiceType);
        Assert.Equal(ServiceLifetime.Singleton, eventDescriptor.Lifetime);
        using (ServiceProvider provider = eventServices.BuildServiceProvider())
        {
            Assert.Same(eventObserver, provider.GetRequiredService<IEventObserver<ConnectorState>>());
            Assert.Same(eventObserver, provider.GetRequiredService<IEventObserver<ConnectorState>>());
        }
        Assert.Equal(1, eventFactoryCalls);

        var eventTypeServices = new ServiceCollection();
        IServiceCollection eventTypeResult =
            eventTypeServices.AddEventObserver<ConnectorState, RecordingEventObserver>();
        Assert.Same(eventTypeServices, eventTypeResult);
        ServiceDescriptor eventTypeDescriptor = Assert.Single(eventTypeServices);
        Assert.Equal(typeof(IEventObserver<ConnectorState>), eventTypeDescriptor.ServiceType);
        Assert.Equal(typeof(RecordingEventObserver), eventTypeDescriptor.ImplementationType);
        Assert.Equal(ServiceLifetime.Singleton, eventTypeDescriptor.Lifetime);

        var stateServices = new ServiceCollection();
        IServiceCollection stateResult = stateServices.AddStateObserver<ConnectorState, RecordingStateObserver>();
        stateServices.AddStateObserver<ConnectorState, RecordingStateObserver>();

        Assert.Same(stateServices, stateResult);
        ServiceDescriptor stateDescriptor = Assert.Single(stateServices);
        Assert.Equal(typeof(IStateObserver<ConnectorState>), stateDescriptor.ServiceType);
        Assert.Equal(typeof(RecordingStateObserver), stateDescriptor.ImplementationType);
        Assert.Equal(ServiceLifetime.Singleton, stateDescriptor.Lifetime);
        using ServiceProvider stateProvider = stateServices.BuildServiceProvider();
        Assert.Same(
            stateProvider.GetRequiredService<IStateObserver<ConnectorState>>(),
            stateProvider.GetRequiredService<IStateObserver<ConnectorState>>());

        var stateFactoryServices = new ServiceCollection();
        var stateObserver = new RecordingStateObserver();
        var stateFactoryCalls = 0;
        IServiceCollection stateFactoryResult = stateFactoryServices.AddStateObserver<ConnectorState, RecordingStateObserver>(_ =>
        {
            stateFactoryCalls++;
            return stateObserver;
        });
        Assert.Same(stateFactoryServices, stateFactoryResult);
        ServiceDescriptor stateFactoryDescriptor = Assert.Single(stateFactoryServices);
        Assert.Equal(typeof(IStateObserver<ConnectorState>), stateFactoryDescriptor.ServiceType);
        Assert.Equal(ServiceLifetime.Singleton, stateFactoryDescriptor.Lifetime);
        using ServiceProvider stateFactoryProvider = stateFactoryServices.BuildServiceProvider();
        Assert.Same(stateObserver, stateFactoryProvider.GetRequiredService<IStateObserver<ConnectorState>>());
        Assert.Same(stateObserver, stateFactoryProvider.GetRequiredService<IStateObserver<ConnectorState>>());
        Assert.Equal(1, stateFactoryCalls);
    }

    [Fact]
    [RequirementCoverage("REQ-VSB-STATE-MACHINE-CORRELATION", "connector-validates-value-type-correlation-before-filter")]
    public void Connector_ValidatesValueTypeCorrelationsBeforeFilteringThem()
    {
        var machine = new ValueTypeCorrelationMachine();
        var correlation = new StubCorrelation(typeof(int));
        SetCorrelation(machine, machine.Signal, correlation);

        object connector = CreateConnector(machine);

        Assert.Equal(1, correlation.ValidateCalls);
        Assert.Empty(GetConnectorList(connector).Cast<object>());
    }

    [Fact]
    [RequirementCoverage("REQ-VSB-STATE-MACHINE-CORRELATION", "connector-wraps-correlation-validation-before-activation")]
    public void Connector_WrapsCorrelationValidationFailureBeforeAnyConnectorActivation()
    {
        var machine = new ValueTypeCorrelationMachine();
        var correlation = new StubCorrelation(typeof(int), failValidation: true);
        SetCorrelation(machine, machine.Signal, correlation);

        ConfigurationException exception = AssertConstructorThrows<ConfigurationException>(
            ConnectorType<ValueTypeCorrelationState>(),
            [machine]);

        Assert.Equal(1, correlation.ValidateCalls);
        Assert.Contains(nameof(ValueTypeCorrelationState), exception.Message, StringComparison.Ordinal);
        ConfigurationException validation = Assert.IsType<ConfigurationException>(exception.InnerException);
        Assert.Contains("The state machine was not properly configured:", validation.Message, StringComparison.Ordinal);
        Assert.Contains("synthetic correlation failure", validation.Message, StringComparison.Ordinal);
    }

    [Fact]
    [RequirementCoverage("REQ-VSB-STATE-MACHINE-CONFIGURATION", "connector-generic-saga-mismatch-diagnostic")]
    public void Connector_RejectsAGenericSagaMismatchWithTheGenericParameterName()
    {
        object connector = CreateConnector(new ConnectorMachine());

        ArgumentException exception = AssertInvocationThrows<ArgumentException>(() =>
            CreateSpecification<OtherSaga>(connector));

        Assert.Equal("T", exception.ParamName);
        Assert.Contains("did not match the connector type", exception.Message, StringComparison.Ordinal);
    }

    [Fact]
    [RequirementCoverage("REQ-VSB-SAGA-DIRECT-CONNECT", "connector-partial-failure-disposes-owned-handles")]
    public void Connector_DisposesEveryCompletedPartialConnectionAndStopsAtTheFailure()
    {
        object connector = CreateConnector(new ConnectorMachine());
        ISagaSpecification<ConnectorState> specification = CreateSpecification<ConnectorState>(connector);
        IList connectors = GetConnectorList(connector);
        connectors.Clear();
        var firstHandle = new RecordingConnectHandle();
        var secondHandle = new RecordingConnectHandle();
        var failure = new InvalidOperationException("synthetic connect failure");
        var first = new StubMessageConnector(() => firstHandle);
        var second = new StubMessageConnector(() => secondHandle);
        var failing = new StubMessageConnector(() => throw failure);
        var unreached = new StubMessageConnector(() => new RecordingConnectHandle());
        connectors.Add(first);
        connectors.Add(second);
        connectors.Add(failing);
        connectors.Add(unreached);
        var repository = new StubSagaRepository<ConnectorState>();
        IConsumePipeConnector consumePipe = StrictStub<IConsumePipeConnector>();

        InvalidOperationException exception = Assert.Throws<InvalidOperationException>(() =>
            ((ISagaConnector)connector).ConnectSaga(consumePipe, repository, specification));

        Assert.Same(failure, exception);
        Assert.Equal(1, first.ConnectCalls);
        Assert.Equal(1, second.ConnectCalls);
        Assert.Equal(1, failing.ConnectCalls);
        Assert.Equal(0, unreached.ConnectCalls);
        Assert.Equal(1, firstHandle.DisposeCalls);
        Assert.Equal(1, secondHandle.DisposeCalls);
    }

    [Fact]
    [RequirementCoverage("REQ-VSB-HANDLER-AND-SAGA-CONFIGURATION-OBSERVATION", "state-machine-specification-observer-exactly-once")]
    public void ConnectorSpecification_NotifiesEveryObserverExactlyOnceWithItsOwningMachine()
    {
        var machine = new ConnectorMachine();
        object connector = CreateConnector(machine);
        ISagaSpecification<ConnectorState> specification = CreateSpecification<ConnectorState>(connector);
        var first = new RecordingSagaConfigurationObserver();
        var second = new RecordingSagaConfigurationObserver();
        specification.ConnectSagaConfigurationObserver(first);
        specification.ConnectSagaConfigurationObserver(second);

        ValidationResult[] firstValidation = specification.Validate().ToArray();
        ValidationResult[] secondValidation = specification.Validate().ToArray();

        Assert.Empty(firstValidation);
        Assert.Empty(secondValidation);
        AssertObserverNotification(first, specification, machine);
        AssertObserverNotification(second, specification, machine);
    }

    [Fact]
    [RequirementCoverage("REQ-VSB-STATE-MACHINE-CONFIGURATION", "state-machine-configurator-delegates-concurrency-observer-and-options")]
    public void StateMachineSagaConfigurator_DelegatesConcurrencyObserverHandleAndEveryOptionsShape()
    {
        var machine = new ConnectorMachine();
        var initialObserver = new RecordingSagaConfigurationObserver();
        object owner = CreateNested<ConnectorState>(
            "StateMachineSagaConfigurator",
            machine,
            new StubSagaRepository<ConnectorState>(),
            initialObserver);
        var configurator = Assert.IsAssignableFrom<ISagaConfigurator<ConnectorState>>(owner);
        var specification = Assert.IsAssignableFrom<SagaSpecification<ConnectorState>>(ReadField(owner, "_specification"));

        configurator.ConcurrentMessageLimit = 17;

        Assert.Equal(17, specification.ConcurrentMessageLimit);

        var disconnectedObserver = new RecordingSagaConfigurationObserver();
        ConnectHandle observerHandle = configurator.ConnectSagaConfigurationObserver(disconnectedObserver);
        observerHandle.Dispose();

        ValidationResult[] validation = ((IReceiveEndpointSpecification)owner).Validate().ToArray();

        Assert.Empty(validation);
        AssertObserverNotification(initialObserver, specification, machine);
        Assert.Equal(0, disconnectedObserver.SagaCalls);
        Assert.Equal(0, disconnectedObserver.StateMachineCalls);
        Assert.Equal(0, disconnectedObserver.MessageCalls);

        CreatedPipelineOptions? createdCallbackArgument = null;
        var createdCallbackCalls = 0;
        CreatedPipelineOptions created = configurator.Options<CreatedPipelineOptions>(options =>
        {
            createdCallbackCalls++;
            createdCallbackArgument = options;
            options.Value = 23;
        });
        var supplied = new SuppliedPipelineOptions { Value = 29 };
        SuppliedPipelineOptions? suppliedCallbackArgument = null;
        var suppliedCallbackCalls = 0;
        SuppliedPipelineOptions suppliedResult = configurator.Options(supplied, options =>
        {
            suppliedCallbackCalls++;
            suppliedCallbackArgument = options;
            options.Value = 31;
        });

        Assert.Equal(1, createdCallbackCalls);
        Assert.Same(created, createdCallbackArgument);
        Assert.Equal(23, created.Value);
        Assert.Equal(1, suppliedCallbackCalls);
        Assert.Same(supplied, suppliedCallbackArgument);
        Assert.Same(supplied, suppliedResult);
        Assert.Equal(31, supplied.Value);

        Assert.True(configurator.TryGetOptions(out CreatedPipelineOptions selectedCreated));
        Assert.Same(created, selectedCreated);
        Assert.True(configurator.TryGetOptions(out SuppliedPipelineOptions selectedSupplied));
        Assert.Same(supplied, selectedSupplied);
        Assert.Collection(
            configurator.SelectOptions<IPipelineOptionMarker>(),
            selected => Assert.Same(created, selected),
            selected => Assert.Same(supplied, selected));
    }

    [Fact]
    [RequirementCoverage("REQ-VSB-STATE-MACHINE-CONFIGURATION", "configuration-owner-null-boundary-matrix")]
    public void StateMachineConfigurationOwners_RejectMissingDependenciesAtTheirImmediateBoundaries()
    {
        var machine = new ConnectorMachine();
        var repository = new StubSagaRepository<ConnectorState>();
        var observer = new RecordingSagaConfigurationObserver();
        object connector = CreateConnector(machine);
        ISagaSpecification<ConnectorState> specification = CreateSpecification<ConnectorState>(connector);

        AssertConstructorArgument("stateMachine", ConnectorType<ConnectorState>(), [null!]);
        Type configuratorType = NestedType<ConnectorState>("StateMachineSagaConfigurator");
        AssertConstructorArgument("stateMachine", configuratorType, [null!, repository, observer]);
        AssertConstructorArgument("repository", configuratorType, [machine, null!, observer]);
        AssertConstructorArgument("observer", configuratorType, [machine, repository, null!]);
        Type specificationType = NestedType<ConnectorState>("StateMachineSagaSpecification");
        AssertConstructorArgument(
            "stateMachine",
            specificationType,
            [null!, Array.Empty<ISagaMessageSpecification<ConnectorState>>()]);
        AssertConstructorArgument("messageSpecifications", specificationType, [machine, null!]);

        ISagaConnector sagaConnector = (ISagaConnector)connector;
        AssertArgument("consumePipe", () => sagaConnector.ConnectSaga<ConnectorState>(null!, repository, specification));
        AssertArgument("sagaRepository", () =>
            sagaConnector.ConnectSaga(StrictStub<IConsumePipeConnector>(), null!, specification));
        AssertArgument("specification", () =>
            sagaConnector.ConnectSaga(StrictStub<IConsumePipeConnector>(), repository, null!));

        object configurator = CreateNested<ConnectorState>(
            "StateMachineSagaConfigurator",
            machine,
            repository,
            observer);
        AssertArgument("builder", () => ((IReceiveEndpointSpecification)configurator).Configure(null!));
    }

    static async Task<InMemorySagaConsumeContext<PipelineSaga, PipelineMessage>> CreateContextAsync(PipelineSaga saga)
    {
        ConsumeContext<PipelineMessage> consumeContext = InMemoryOutboxTestContextFactory.Create(
            new PipelineMessage(),
            TestContext.Current.CancellationToken);
        var instance = new SagaInstance<PipelineSaga>(saga);
        await instance.MarkInUseAsync(TestContext.Current.CancellationToken);
        return new InMemorySagaConsumeContext<PipelineSaga, PipelineMessage>(consumeContext, instance);
    }

    static byte[] SelectPartitionKey(
        IPipeSpecification<SagaConsumeContext<PipelineSaga>>? specification,
        SagaConsumeContext<PipelineSaga> context)
    {
        Assert.NotNull(specification);
        FieldInfo field = specification.GetType().GetField("_keyProvider", BindingFlags.Instance | BindingFlags.NonPublic)
            ?? throw new InvalidOperationException("The saga partition-key provider field was not found.");
        var provider = Assert.IsAssignableFrom<Delegate>(field.GetValue(specification));
        return Assert.IsType<byte[]>(provider.DynamicInvoke(context));
    }

    static void SetCorrelation<TState>(
        ViciOneServiceBusStateMachine<TState> machine,
        IEvent @event,
        IEventCorrelation correlation)
        where TState : class, ISagaStateMachineInstance
    {
        FieldInfo field = typeof(ViciOneServiceBusStateMachine<TState>).GetField(
            "_eventCorrelations",
            BindingFlags.Instance | BindingFlags.NonPublic)
            ?? throw new InvalidOperationException("The state-machine correlation field was not found.");
        var correlations = Assert.IsAssignableFrom<IDictionary>(field.GetValue(machine));
        correlations[@event] = correlation;
    }

    static object CreateConnector<TState>(ISagaStateMachine<TState> machine)
        where TState : class, ISagaStateMachineInstance =>
        CreateNested<TState>("StateMachineConnector", machine);

    static ISagaSpecification<TSaga> CreateSpecification<TSaga>(object connector)
        where TSaga : class, ISaga
    {
        MethodInfo method = connector.GetType().GetMethod(nameof(ISagaConnector.CreateSagaSpecification))
            ?? throw new InvalidOperationException("The state-machine specification factory was not found.");
        return Assert.IsAssignableFrom<ISagaSpecification<TSaga>>(method.MakeGenericMethod(typeof(TSaga)).Invoke(connector, null));
    }

    static IList GetConnectorList(object connector)
    {
        FieldInfo field = connector.GetType().GetField("_connectors", BindingFlags.Instance | BindingFlags.NonPublic)
            ?? throw new InvalidOperationException("The state-machine connector list was not found.");
        return Assert.IsAssignableFrom<IList>(field.GetValue(connector));
    }

    static Type ConnectorType<TState>()
        where TState : class, ISagaStateMachineInstance => NestedType<TState>("StateMachineConnector");

    static Type NestedType<TState>(string name)
        where TState : class, ISagaStateMachineInstance
    {
        Type nestedType = typeof(ViciOneServiceBusStateMachine<TState>).GetNestedType(name, BindingFlags.NonPublic)
            ?? throw new InvalidOperationException($"The nested state-machine type '{name}' was not found.");
        return nestedType.ContainsGenericParameters
            ? nestedType.MakeGenericType(typeof(TState))
            : nestedType;
    }

    static object CreateNested<TState>(string name, params object?[] arguments)
        where TState : class, ISagaStateMachineInstance =>
        Activator.CreateInstance(
            NestedType<TState>(name),
            BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic,
            binder: null,
            args: arguments,
            culture: null)
        ?? throw new InvalidOperationException($"The nested state-machine type '{name}' could not be created.");

    static TException AssertConstructorThrows<TException>(Type type, object?[] arguments)
        where TException : Exception
    {
        TargetInvocationException wrapper = Assert.Throws<TargetInvocationException>(() =>
            Activator.CreateInstance(
                type,
                BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic,
                binder: null,
                args: arguments,
                culture: null));
        return Assert.IsType<TException>(wrapper.InnerException);
    }

    static TException AssertInvocationThrows<TException>(Action action)
        where TException : Exception
    {
        TargetInvocationException wrapper = Assert.Throws<TargetInvocationException>(action);
        return Assert.IsType<TException>(wrapper.InnerException);
    }

    static void AssertConstructorArgument(string parameterName, Type type, object?[] arguments)
    {
        ArgumentNullException exception = AssertConstructorThrows<ArgumentNullException>(type, arguments);
        Assert.Equal(parameterName, exception.ParamName);
    }

    static void AssertArgument(string parameterName, Action action)
    {
        ArgumentNullException exception = Assert.Throws<ArgumentNullException>(action);
        Assert.Equal(parameterName, exception.ParamName);
    }

    static void AssertRange(string parameterName, Action action)
    {
        ArgumentOutOfRangeException exception = Assert.Throws<ArgumentOutOfRangeException>(action);
        Assert.Equal(parameterName, exception.ParamName);
    }

    static void AssertInvalidArgument(string parameterName, Action action)
    {
        ArgumentException exception = Assert.Throws<ArgumentException>(action);
        Assert.Equal(parameterName, exception.ParamName);
    }

    static object? ReadField(object target, string fieldName)
    {
        FieldInfo field = target.GetType().GetField(fieldName, BindingFlags.Instance | BindingFlags.NonPublic)
            ?? throw new InvalidOperationException($"The field '{fieldName}' was not found on {target.GetType()}.");
        return field.GetValue(target);
    }

    static object? AssertSingleArgumentCall(RecordingDispatchProxy recording, string methodName)
    {
        RecordedInvocation invocation = Assert.Single(recording.Invocations);
        Assert.Equal(methodName, invocation.Method.Name);
        return Assert.Single(invocation.Arguments);
    }

    static void AssertObserverNotification(
        RecordingSagaConfigurationObserver observer,
        ISagaSpecification<ConnectorState> specification,
        ConnectorMachine machine)
    {
        Assert.Equal(1, observer.SagaCalls);
        Assert.Equal(1, observer.StateMachineCalls);
        Assert.Equal(1, observer.MessageCalls);
        Assert.Same(specification, observer.SagaConfigurator);
        Assert.Same(specification, observer.StateMachineConfigurator);
        Assert.Same(machine, observer.StateMachine);
    }

    static T StrictStub<T>()
        where T : class => DispatchProxy.Create<T, StrictDispatchProxy>();

    static T RecordingStub<T>(out RecordingDispatchProxy recording)
        where T : class
    {
        T instance = DispatchProxy.Create<T, RecordingDispatchProxy>();
        recording = (RecordingDispatchProxy)(object)instance;
        return instance;
    }

    class StrictDispatchProxy : DispatchProxy
    {
        protected override object? Invoke(MethodInfo? targetMethod, object?[]? args)
        {
            throw new InvalidOperationException($"Unexpected collaborator call: {targetMethod?.Name}.");
        }
    }

    sealed record RecordedInvocation(MethodInfo Method, object?[] Arguments);

    class RecordingDispatchProxy : DispatchProxy
    {
        public List<RecordedInvocation> Invocations { get; } = [];

        protected override object? Invoke(MethodInfo? targetMethod, object?[]? args)
        {
            Assert.NotNull(targetMethod);
            Invocations.Add(new RecordedInvocation(targetMethod, args?.ToArray() ?? []));

            return targetMethod.ReturnType != typeof(void) && targetMethod.ReturnType.IsValueType
                ? Activator.CreateInstance(targetMethod.ReturnType)
                : null;
        }
    }

    sealed class RecordingPipeConfigurator<TContext> : IPipeConfigurator<TContext>
        where TContext : class, PipeContext
    {
        public IPipeSpecification<TContext>? Specification { get; private set; }

        public void AddPipeSpecification(IPipeSpecification<TContext> specification)
        {
            Assert.Null(Specification);
            Specification = specification;
        }
    }

    sealed class StubCorrelation : IEventCorrelation
    {
        readonly bool _failValidation;

        public StubCorrelation(Type dataType, bool failValidation = false)
        {
            DataType = dataType;
            _failValidation = failValidation;
        }

        public Type DataType { get; }

        public bool ConfigureConsumeTopology => false;

        public int ValidateCalls { get; private set; }

        public IEnumerable<ValidationResult> Validate()
        {
            ValidateCalls++;
            return _failValidation
                ? [this.Failure("synthetic correlation failure")]
                : [];
        }
    }

    sealed class StubMessageConnector : ISagaMessageConnector<ConnectorState>
    {
        readonly Func<ConnectHandle> _connect;

        public StubMessageConnector(Func<ConnectHandle> connect)
        {
            _connect = connect;
        }

        public Type MessageType => typeof(ConnectorMessage);

        public int ConnectCalls { get; private set; }

        public ISagaMessageSpecification<ConnectorState> CreateSagaMessageSpecification() =>
            throw new NotSupportedException();

        public ConnectHandle ConnectSaga(
            IConsumePipeConnector consumePipe,
            ISagaRepository<ConnectorState> repository,
            ISagaSpecification<ConnectorState> specification)
        {
            ConnectCalls++;
            return _connect();
        }
    }

    sealed class RecordingConnectHandle : ConnectHandle
    {
        public int DisposeCalls { get; private set; }

        public void Disconnect()
        {
            DisposeCalls++;
        }

        public void Dispose()
        {
            Disconnect();
        }
    }

    sealed class StubSagaRepository<TSaga> : ISagaRepository<TSaga>
        where TSaga : class, ISaga
    {
        public void Probe(ProbeContext context)
        {
        }

        public Task SendAsync<T>(
            ConsumeContext<T> context,
            ISagaPolicy<TSaga, T> policy,
            IPipe<SagaConsumeContext<TSaga, T>> next)
            where T : class => throw new NotSupportedException();

        public Task SendQueryAsync<T>(
            ConsumeContext<T> context,
            ISagaQuery<TSaga> query,
            ISagaPolicy<TSaga, T> policy,
            IPipe<SagaConsumeContext<TSaga, T>> next)
            where T : class => throw new NotSupportedException();
    }

    sealed class RecordingSagaConfigurationObserver : ISagaConfigurationObserver
    {
        public int SagaCalls { get; private set; }

        public int StateMachineCalls { get; private set; }

        public int MessageCalls { get; private set; }

        public object? SagaConfigurator { get; private set; }

        public object? StateMachineConfigurator { get; private set; }

        public object? StateMachine { get; private set; }

        public void SagaConfigured<TSaga>(ISagaConfigurator<TSaga> configurator)
            where TSaga : class
        {
            SagaCalls++;
            SagaConfigurator = configurator;
        }

        public void StateMachineSagaConfigured<TInstance>(
            ISagaConfigurator<TInstance> configurator,
            object stateMachine)
            where TInstance : class
        {
            StateMachineCalls++;
            StateMachineConfigurator = configurator;
            StateMachine = stateMachine;
        }

        public void SagaMessageConfigured<TSaga, TMessage>(ISagaMessageConfigurator<TSaga, TMessage> configurator)
            where TSaga : class
            where TMessage : class
        {
            MessageCalls++;
        }
    }

    sealed class RecordingEventObserver : IEventObserver<ConnectorState>
    {
        public Task PreExecuteAsync(IBehaviorContext<ConnectorState> context) => Task.CompletedTask;

        public Task PreExecuteAsync<T>(IBehaviorContext<ConnectorState, T> context)
            where T : class => Task.CompletedTask;

        public Task PostExecuteAsync(IBehaviorContext<ConnectorState> context) => Task.CompletedTask;

        public Task PostExecuteAsync<T>(IBehaviorContext<ConnectorState, T> context)
            where T : class => Task.CompletedTask;

        public Task ExecuteFaultAsync(IBehaviorContext<ConnectorState> context, Exception exception) => Task.CompletedTask;

        public Task ExecuteFaultAsync<T>(IBehaviorContext<ConnectorState, T> context, Exception exception)
            where T : class => Task.CompletedTask;
    }

    sealed class RecordingStateObserver : IStateObserver<ConnectorState>
    {
        public Task StateChangedAsync(
            IBehaviorContext<ConnectorState> context,
            IState currentState,
            IState? previousState) => Task.CompletedTask;
    }

    interface IPipelineOptionMarker;

    sealed class CreatedPipelineOptions :
        IOptions,
        IPipelineOptionMarker
    {
        public int Value { get; set; }
    }

    sealed class SuppliedPipelineOptions :
        IOptions,
        IPipelineOptionMarker
    {
        public int Value { get; set; }
    }

    public sealed class PipelineSaga : ISaga
    {
        public Guid CorrelationId { get; set; }

        public string Key { get; set; } = string.Empty;
    }

    public interface IOutboxRegistrationContext :
        IRegistrationContext,
        ISetScopedConsumeContext;

    public sealed record PipelineMessage;

    public sealed record ConnectorMessage(Guid CorrelationId) : ICorrelatedBy<Guid>;

    public sealed class ConnectorState : ISagaStateMachineInstance
    {
        public Guid CorrelationId { get; set; }

        public string CurrentState { get; set; } = string.Empty;
    }

    public sealed class ConnectorMachine : ViciOneServiceBusStateMachine<ConnectorState>
    {
        public ConnectorMachine()
        {
            InstanceState(instance => instance.CurrentState);
            Initially(When(Start).TransitionTo(Running));
        }

        public IState Running { get; } = null!;

        public IEvent<ConnectorMessage> Start { get; } = null!;
    }

    public sealed class ValueTypeCorrelationState : ISagaStateMachineInstance
    {
        public Guid CorrelationId { get; set; }
    }

    public sealed class ValueTypeCorrelationMachine : ViciOneServiceBusStateMachine<ValueTypeCorrelationState>
    {
        public ValueTypeCorrelationMachine()
        {
            Initially(When(Signal));
        }

        public IEvent Signal { get; } = null!;
    }

    public sealed class OtherSaga : ISaga
    {
        public Guid CorrelationId { get; set; }
    }
}

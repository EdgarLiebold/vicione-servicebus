using System.Diagnostics.CodeAnalysis;
using System.Reflection;
using ViciOne.ServiceBus.Middleware;
using ViciOne.ServiceBus.Tests.Infrastructure.Requirements;
using Xunit;

namespace ViciOne.ServiceBus.Tests.Configuration;

public sealed class SagaConnectorPipelineDeepContractTests
{
    [Fact]
    [RequirementCoverage("REQ-VSB-SAGA-DIRECT-CONNECT", "message-connectors-constructor-null-precedence")]
    public void Constructors_RejectRequiredCollaboratorsInSignatureOrder()
    {
        var consumeFilter = new RecordingFilter<SagaConsumeContext<PipelineSaga, PipelineMessage>>();
        var policy = new RecordingSagaPolicy();
        Func<ConsumeContext<PipelineMessage>, Guid> selector = _ => Guid.Empty;
        var queryFactory = new RecordingSagaQueryFactory();

        AssertArgument("consumeFilter", () =>
            new SagaConnector<PipelineSaga, PipelineMessage>.CorrelatedSagaMessageConnector(null!, null!, null!));
        AssertArgument("policy", () =>
            new SagaConnector<PipelineSaga, PipelineMessage>.CorrelatedSagaMessageConnector(consumeFilter, null!, null!));
        AssertArgument("correlationIdSelector", () =>
            new SagaConnector<PipelineSaga, PipelineMessage>.CorrelatedSagaMessageConnector(consumeFilter, policy, null!));

        AssertArgument("consumeFilter", () =>
            new SagaConnector<PipelineSaga, PipelineMessage>.QuerySagaMessageConnector(null!, null!, null!));
        AssertArgument("policy", () =>
            new SagaConnector<PipelineSaga, PipelineMessage>.QuerySagaMessageConnector(consumeFilter, null!, null!));
        AssertArgument("queryFactory", () =>
            new SagaConnector<PipelineSaga, PipelineMessage>.QuerySagaMessageConnector(consumeFilter, policy, null!));

        _ = new SagaConnector<PipelineSaga, PipelineMessage>.CorrelatedSagaMessageConnector(consumeFilter, policy, selector);
        _ = new SagaConnector<PipelineSaga, PipelineMessage>.QuerySagaMessageConnector(consumeFilter, policy, queryFactory);
    }

    [Fact]
    [RequirementCoverage("REQ-VSB-SAGA-DIRECT-CONNECT", "connect-saga-argument-null-precedence-before-effects")]
    public void ConnectSaga_RejectsRequiredInputsInSignatureOrderBeforeAnyEffect()
    {
        var scenario = new ConnectionScenario();
        var connector = new RecordingSagaMessageConnector(scenario.ConsumeFilter, configureConsumeTopology: true);

        AssertArgument("consumePipe", () => connector.ConnectSaga(null!, null!, null!));
        AssertArgument("repository", () => connector.ConnectSaga(scenario.ConsumePipe, null!, null!));
        AssertArgument("specification", () => connector.ConnectSaga(scenario.ConsumePipe, scenario.Repository, null!));

        Assert.Equal(0, scenario.SagaSpecification.GetMessageSpecificationCalls);
        Assert.Equal(0, scenario.MessageSpecification.BuildConsumerPipeCalls);
        Assert.Equal(0, scenario.MessageSpecification.BuildMessagePipeCalls);
        Assert.Equal(0, connector.ConfigureMessagePipeCalls);
        Assert.Equal(0, scenario.ConsumePipe.TotalCalls);
    }

    [Fact]
    [RequirementCoverage("REQ-VSB-SAGA-DIRECT-CONNECT", "correlated-filter-order-identities-cardinality-default-topology")]
    public void CorrelatedConnector_ComposesExactFiltersOnceInOrderAndUsesDefaultTopologyOverload()
    {
        var scenario = new ConnectionScenario();
        var policy = new RecordingSagaPolicy();
        Func<ConsumeContext<PipelineMessage>, Guid> selector = _ => NewId.NextGuid();
        var connector = new SagaConnector<PipelineSaga, PipelineMessage>.CorrelatedSagaMessageConnector(
            scenario.ConsumeFilter,
            policy,
            selector);

        ConnectHandle result = scenario.Connect(connector);

        AssertSuccessfulConnection(scenario, result);
        Assert.Collection(
            scenario.MessageSpecification.MessageConfigurator.Specifications,
            specification => Assert.Same(scenario.SagaSpecification.SharedSpecification, specification),
            specification =>
            {
                var filter = Assert.IsType<CorrelationIdMessageFilter<PipelineMessage>>(ReadFilter(specification));
                Assert.Same(selector, ReadField<object>(filter, "_getCorrelationId"));
            },
            specification =>
            {
                var filter = Assert.IsType<CorrelatedSagaFilter<PipelineSaga, PipelineMessage>>(ReadFilter(specification));
                Assert.Same(scenario.Repository, ReadField<object>(filter, "_sagaRepository"));
                Assert.Same(policy, ReadField<object>(filter, "_policy"));
                Assert.Same(scenario.ConsumerPipe, ReadField<object>(filter, "_messagePipe"));
            });
    }

    [Fact]
    [RequirementCoverage("REQ-VSB-SAGA-DIRECT-CONNECT", "query-filter-order-identities-cardinality-default-topology")]
    public void QueryConnector_ComposesExactFilterOnceAfterSharedConfigurationAndUsesDefaultTopologyOverload()
    {
        var scenario = new ConnectionScenario();
        var policy = new RecordingSagaPolicy();
        var queryFactory = new RecordingSagaQueryFactory();
        var connector = new SagaConnector<PipelineSaga, PipelineMessage>.QuerySagaMessageConnector(
            scenario.ConsumeFilter,
            policy,
            queryFactory);

        ConnectHandle result = scenario.Connect(connector);

        AssertSuccessfulConnection(scenario, result);
        Assert.Collection(
            scenario.MessageSpecification.MessageConfigurator.Specifications,
            specification => Assert.Same(scenario.SagaSpecification.SharedSpecification, specification),
            specification =>
            {
                var filter = Assert.IsType<QuerySagaFilter<PipelineSaga, PipelineMessage>>(ReadFilter(specification));
                Assert.Same(scenario.Repository, ReadField<object>(filter, "_sagaRepository"));
                Assert.Same(policy, ReadField<object>(filter, "_policy"));
                Assert.Same(queryFactory, ReadField<object>(filter, "_queryFactory"));
                Assert.Same(scenario.ConsumerPipe, ReadField<object>(filter, "_messagePipe"));
            });
    }

    [Fact]
    [RequirementCoverage("REQ-VSB-SAGA-DIRECT-CONNECT", "disabled-topology-exact-options-handle-ownership")]
    public void DisabledTopology_UsesOnlyTheOptionsOverloadAndTransfersTheExactHandleWithoutDisposal()
    {
        var scenario = new ConnectionScenario();
        var connector = new RecordingSagaMessageConnector(scenario.ConsumeFilter, configureConsumeTopology: false);

        ConnectHandle result = scenario.Connect(connector);

        Assert.Same(scenario.Handle, result);
        Assert.Equal(0, scenario.ConsumePipe.DefaultCalls);
        Assert.Equal(1, scenario.ConsumePipe.OptionsCalls);
        Assert.Equal(ConnectPipeOptions.All & ~ConnectPipeOptions.ConfigureConsumeTopology, scenario.ConsumePipe.Options);
        Assert.Same(scenario.MessagePipe, scenario.ConsumePipe.ConnectedPipe);
        Assert.Equal(0, scenario.Handle.DisposeCalls);
        Assert.Equal(1, connector.ConfigureMessagePipeCalls);
        Assert.Same(scenario.Repository, connector.Repository);
        Assert.Same(scenario.ConsumerPipe, connector.SagaPipe);
    }

    [Theory]
    [InlineData(NullResultStage.MessageSpecification, "The saga specification returned a null message specification.")]
    [InlineData(NullResultStage.ConsumerPipe, "The saga message specification returned a null consumer pipe.")]
    [InlineData(NullResultStage.MessagePipe, "The saga message specification returned a null message pipe.")]
    [InlineData(NullResultStage.ConnectHandle, "The consume pipe connector returned a null connect handle.")]
    [RequirementCoverage("REQ-VSB-SAGA-DIRECT-CONNECT", "collaborator-null-results-stop-at-owning-boundary")]
    public void ConnectSaga_RejectsEveryCollaboratorNullResultAtItsOwningBoundary(
        NullResultStage stage,
        string expectedMessage)
    {
        var scenario = new ConnectionScenario();
        var connector = new RecordingSagaMessageConnector(scenario.ConsumeFilter, configureConsumeTopology: true);

        switch (stage)
        {
            case NullResultStage.MessageSpecification:
                scenario.SagaSpecification.MessageSpecification = null;
                break;
            case NullResultStage.ConsumerPipe:
                scenario.MessageSpecification.ConsumerPipe = null;
                break;
            case NullResultStage.MessagePipe:
                scenario.MessageSpecification.MessagePipe = null;
                break;
            case NullResultStage.ConnectHandle:
                scenario.ConsumePipe.Handle = null;
                break;
            default:
                throw new ArgumentOutOfRangeException(nameof(stage), stage, null);
        }

        InvalidOperationException exception = Assert.Throws<InvalidOperationException>(() => scenario.Connect(connector));

        Assert.Equal(expectedMessage, exception.Message);
        Assert.Equal(1, scenario.SagaSpecification.GetMessageSpecificationCalls);
        Assert.Equal(stage >= NullResultStage.ConsumerPipe ? 1 : 0, scenario.MessageSpecification.BuildConsumerPipeCalls);
        Assert.Equal(stage >= NullResultStage.MessagePipe ? 1 : 0, scenario.MessageSpecification.BuildMessagePipeCalls);
        Assert.Equal(stage >= NullResultStage.MessagePipe ? 1 : 0, scenario.MessageSpecification.ConfigureCallbackCalls);
        Assert.Equal(stage >= NullResultStage.MessagePipe ? 1 : 0, scenario.SagaSpecification.ConfigureMessagePipeCalls);
        Assert.Equal(stage >= NullResultStage.MessagePipe ? 1 : 0, connector.ConfigureMessagePipeCalls);
        Assert.Equal(stage >= NullResultStage.ConnectHandle ? 1 : 0, scenario.ConsumePipe.TotalCalls);
        Assert.Equal(0, scenario.Handle.DisposeCalls);
    }

    [Theory]
    [InlineData(FailureStage.GetMessageSpecification)]
    [InlineData(FailureStage.BuildConsumerPipe)]
    [InlineData(FailureStage.BuildMessagePipe)]
    [InlineData(FailureStage.ConfigureSharedMessagePipe)]
    [InlineData(FailureStage.ConfigureConnectorMessagePipe)]
    [InlineData(FailureStage.ConnectConsumePipe)]
    [RequirementCoverage("REQ-VSB-SAGA-DIRECT-CONNECT", "phase-failure-identity-cardinality-and-no-unowned-cleanup")]
    public void ConnectSaga_PropagatesEachPhaseFailureExactlyAndStopsWithoutDisposingUnownedHandles(FailureStage stage)
    {
        var scenario = new ConnectionScenario();
        var connector = new RecordingSagaMessageConnector(scenario.ConsumeFilter, configureConsumeTopology: true);
        var failure = new InvalidOperationException($"synthetic {stage} failure");

        switch (stage)
        {
            case FailureStage.GetMessageSpecification:
                scenario.SagaSpecification.GetMessageSpecificationFailure = failure;
                break;
            case FailureStage.BuildConsumerPipe:
                scenario.MessageSpecification.BuildConsumerPipeFailure = failure;
                break;
            case FailureStage.BuildMessagePipe:
                scenario.MessageSpecification.BuildMessagePipeFailure = failure;
                break;
            case FailureStage.ConfigureSharedMessagePipe:
                scenario.SagaSpecification.ConfigureMessagePipeFailure = failure;
                break;
            case FailureStage.ConfigureConnectorMessagePipe:
                connector.ConfigureMessagePipeFailure = failure;
                break;
            case FailureStage.ConnectConsumePipe:
                scenario.ConsumePipe.Failure = failure;
                break;
            default:
                throw new ArgumentOutOfRangeException(nameof(stage), stage, null);
        }

        InvalidOperationException exception = Assert.Throws<InvalidOperationException>(() => scenario.Connect(connector));

        Assert.Same(failure, exception);
        Assert.Equal(1, scenario.SagaSpecification.GetMessageSpecificationCalls);
        Assert.Equal(stage >= FailureStage.BuildConsumerPipe ? 1 : 0, scenario.MessageSpecification.BuildConsumerPipeCalls);
        Assert.Equal(stage >= FailureStage.BuildMessagePipe ? 1 : 0, scenario.MessageSpecification.BuildMessagePipeCalls);
        Assert.Equal(stage >= FailureStage.ConfigureSharedMessagePipe ? 1 : 0, scenario.MessageSpecification.ConfigureCallbackCalls);
        Assert.Equal(stage >= FailureStage.ConfigureSharedMessagePipe ? 1 : 0, scenario.SagaSpecification.ConfigureMessagePipeCalls);
        Assert.Equal(stage >= FailureStage.ConfigureConnectorMessagePipe ? 1 : 0, connector.ConfigureMessagePipeCalls);
        Assert.Equal(stage >= FailureStage.ConnectConsumePipe ? 1 : 0, scenario.ConsumePipe.TotalCalls);
        Assert.Equal(0, scenario.Handle.DisposeCalls);
    }

    [Fact]
    [RequirementCoverage("REQ-VSB-SAGA-DIRECT-CONNECT", "message-specification-factory-fresh-typed-identity")]
    public void CreateSagaMessageSpecification_ReturnsFreshSpecificationsForTheExactClosedMessagePair()
    {
        var connector = new RecordingSagaMessageConnector(
            new RecordingFilter<SagaConsumeContext<PipelineSaga, PipelineMessage>>(),
            configureConsumeTopology: true);

        ISagaMessageSpecification<PipelineSaga> first = connector.CreateSagaMessageSpecification();
        ISagaMessageSpecification<PipelineSaga> second = connector.CreateSagaMessageSpecification();

        Assert.NotSame(first, second);
        Assert.IsType<SagaConnector<PipelineSaga, PipelineMessage>.SagaMessageSpecification>(first);
        Assert.IsType<SagaConnector<PipelineSaga, PipelineMessage>.SagaMessageSpecification>(second);
        Assert.IsAssignableFrom<ISagaMessageSpecification<PipelineSaga, PipelineMessage>>(first);
        Assert.Equal(typeof(PipelineMessage), first.MessageType);
        Assert.Equal(typeof(PipelineMessage), connector.MessageType);
    }

    private static void AssertSuccessfulConnection(ConnectionScenario scenario, ConnectHandle result)
    {
        Assert.Same(scenario.Handle, result);
        Assert.Equal(1, scenario.SagaSpecification.GetMessageSpecificationCalls);
        Assert.Equal(1, scenario.MessageSpecification.BuildConsumerPipeCalls);
        Assert.Same(scenario.ConsumeFilter, scenario.MessageSpecification.ConsumeFilter);
        Assert.Equal(1, scenario.MessageSpecification.BuildMessagePipeCalls);
        Assert.Equal(1, scenario.MessageSpecification.ConfigureCallbackCalls);
        Assert.Equal(1, scenario.SagaSpecification.ConfigureMessagePipeCalls);
        Assert.Equal(1, scenario.ConsumePipe.DefaultCalls);
        Assert.Equal(0, scenario.ConsumePipe.OptionsCalls);
        Assert.Same(scenario.MessagePipe, scenario.ConsumePipe.ConnectedPipe);
        Assert.Equal(0, scenario.Handle.DisposeCalls);
    }

    private static object ReadFilter(IPipeSpecification<ConsumeContext<PipelineMessage>> specification)
    {
        var filterSpecification = Assert.IsType<FilterPipeSpecification<ConsumeContext<PipelineMessage>>>(specification);
        return ReadField<object>(filterSpecification, "_filter");
    }

    private static T ReadField<T>(object target, string fieldName)
    {
        for (Type? type = target.GetType(); type is not null; type = type.BaseType)
        {
            FieldInfo? field = type.GetField(fieldName, BindingFlags.Instance | BindingFlags.NonPublic | BindingFlags.DeclaredOnly);
            if (field is not null)
                return Assert.IsAssignableFrom<T>(field.GetValue(target));
        }

        throw new InvalidOperationException($"The field '{fieldName}' was not found on {target.GetType()}.");
    }

    private static void AssertArgument(string parameterName, Action action)
    {
        ArgumentNullException exception = Assert.Throws<ArgumentNullException>(action);
        Assert.Equal(parameterName, exception.ParamName);
    }

    public enum NullResultStage
    {
        MessageSpecification,
        ConsumerPipe,
        MessagePipe,
        ConnectHandle,
    }

    public enum FailureStage
    {
        GetMessageSpecification,
        BuildConsumerPipe,
        BuildMessagePipe,
        ConfigureSharedMessagePipe,
        ConfigureConnectorMessagePipe,
        ConnectConsumePipe,
    }

    public sealed class PipelineSaga : ISaga
    {
        public Guid CorrelationId { get; set; }
    }

    public sealed record PipelineMessage;

    sealed class ConnectionScenario
    {
        public ConnectionScenario()
        {
            ConsumerPipe = new RecordingPipe<SagaConsumeContext<PipelineSaga, PipelineMessage>>();
            MessagePipe = new RecordingPipe<ConsumeContext<PipelineMessage>>();
            MessageSpecification = new RecordingSagaMessageSpecification
            {
                ConsumerPipe = ConsumerPipe,
                MessagePipe = MessagePipe,
            };
            SagaSpecification = new RecordingSagaSpecification
            {
                MessageSpecification = MessageSpecification,
            };
            ConsumePipe.Handle = Handle;
        }

        public RecordingFilter<SagaConsumeContext<PipelineSaga, PipelineMessage>> ConsumeFilter { get; } = new();

        public RecordingPipe<SagaConsumeContext<PipelineSaga, PipelineMessage>> ConsumerPipe { get; }

        public RecordingPipe<ConsumeContext<PipelineMessage>> MessagePipe { get; }

        public RecordingSagaMessageSpecification MessageSpecification { get; }

        public RecordingSagaSpecification SagaSpecification { get; }

        public RecordingSagaRepository Repository { get; } = new();

        public RecordingConsumePipeConnector ConsumePipe { get; } = new();

        public RecordingConnectHandle Handle { get; } = new();

        public ConnectHandle Connect(SagaConnector<PipelineSaga, PipelineMessage>.SagaMessageConnector connector) =>
            connector.ConnectSaga(ConsumePipe, Repository, SagaSpecification);
    }

    sealed class RecordingSagaMessageConnector :
        SagaConnector<PipelineSaga, PipelineMessage>.SagaMessageConnector
    {
        readonly bool _configureConsumeTopology;

        public RecordingSagaMessageConnector(
            IFilter<SagaConsumeContext<PipelineSaga, PipelineMessage>> consumeFilter,
            bool configureConsumeTopology)
            : base(consumeFilter)
        {
            _configureConsumeTopology = configureConsumeTopology;
        }

        public int ConfigureMessagePipeCalls { get; private set; }

        public Exception? ConfigureMessagePipeFailure { get; set; }

        public ISagaRepository<PipelineSaga>? Repository { get; private set; }

        public IPipe<SagaConsumeContext<PipelineSaga, PipelineMessage>>? SagaPipe { get; private set; }

        public MarkerPipeSpecification<ConsumeContext<PipelineMessage>> ConnectorSpecification { get; } = new();

        protected override bool ConfigureConsumeTopology => _configureConsumeTopology;

        protected override void ConfigureMessagePipe(
            IPipeConfigurator<ConsumeContext<PipelineMessage>> configurator,
            ISagaRepository<PipelineSaga> repository,
            IPipe<SagaConsumeContext<PipelineSaga, PipelineMessage>> sagaPipe)
        {
            ConfigureMessagePipeCalls++;
            Repository = repository;
            SagaPipe = sagaPipe;

            if (ConfigureMessagePipeFailure is not null)
                throw ConfigureMessagePipeFailure;

            configurator.AddPipeSpecification(ConnectorSpecification);
        }
    }

    sealed class RecordingSagaSpecification : ISagaSpecification<PipelineSaga>
    {
        public ISagaMessageSpecification<PipelineSaga, PipelineMessage>? MessageSpecification { get; set; }

        public Exception? GetMessageSpecificationFailure { get; set; }

        public Exception? ConfigureMessagePipeFailure { get; set; }

        public int GetMessageSpecificationCalls { get; private set; }

        public int ConfigureMessagePipeCalls { get; private set; }

        public MarkerPipeSpecification<ConsumeContext<PipelineMessage>> SharedSpecification { get; } = new();

        public int? ConcurrentMessageLimit
        {
            set => throw new NotSupportedException();
        }

        public ISagaMessageSpecification<PipelineSaga, T> GetMessageSpecification<T>()
            where T : class
        {
            GetMessageSpecificationCalls++;

            if (GetMessageSpecificationFailure is not null)
                throw GetMessageSpecificationFailure;

            if (typeof(T) != typeof(PipelineMessage))
                throw new InvalidOperationException($"Unexpected message type: {typeof(T)}.");

            return (ISagaMessageSpecification<PipelineSaga, T>)(object)MessageSpecification!;
        }

        public void ConfigureMessagePipe<T>(IPipeConfigurator<ConsumeContext<T>> pipeConfigurator)
            where T : class
        {
            ConfigureMessagePipeCalls++;

            if (ConfigureMessagePipeFailure is not null)
                throw ConfigureMessagePipeFailure;

            if (pipeConfigurator is not IPipeConfigurator<ConsumeContext<PipelineMessage>> typedConfigurator)
                throw new InvalidOperationException($"Unexpected message configurator type: {typeof(T)}.");

            typedConfigurator.AddPipeSpecification(SharedSpecification);
        }

        public void AddPipeSpecification(IPipeSpecification<SagaConsumeContext<PipelineSaga>> specification) =>
            throw new NotSupportedException();

        public void Message<T>(Action<ISagaMessageConfigurator<T>> configure)
            where T : class => throw new NotSupportedException();

        public void SagaMessage<T>(Action<ISagaMessageConfigurator<PipelineSaga, T>> configure)
            where T : class => throw new NotSupportedException();

        public ConnectHandle ConnectSagaConfigurationObserver(ISagaConfigurationObserver observer) =>
            throw new NotSupportedException();

        public T Options<T>(Action<T>? configure = null)
            where T : IOptions, new() => throw new NotSupportedException();

        public T Options<T>(T options, Action<T>? configure = null)
            where T : IOptions => throw new NotSupportedException();

        public bool TryGetOptions<T>(out T options)
            where T : IOptions => throw new NotSupportedException();

        public IEnumerable<T> SelectOptions<T>()
            where T : class => throw new NotSupportedException();

        public IEnumerable<ValidationResult> Validate() => throw new NotSupportedException();
    }

    sealed class RecordingSagaMessageSpecification : ISagaMessageSpecification<PipelineSaga, PipelineMessage>
    {
        public IPipe<SagaConsumeContext<PipelineSaga, PipelineMessage>>? ConsumerPipe { get; set; }

        public IPipe<ConsumeContext<PipelineMessage>>? MessagePipe { get; set; }

        public Exception? BuildConsumerPipeFailure { get; set; }

        public Exception? BuildMessagePipeFailure { get; set; }

        public int BuildConsumerPipeCalls { get; private set; }

        public int BuildMessagePipeCalls { get; private set; }

        public int ConfigureCallbackCalls { get; private set; }

        public IFilter<SagaConsumeContext<PipelineSaga, PipelineMessage>>? ConsumeFilter { get; private set; }

        public RecordingPipeConfigurator<ConsumeContext<PipelineMessage>> MessageConfigurator { get; } = new();

        public Type MessageType => typeof(PipelineMessage);

        public ISagaMessageSpecification<PipelineSaga, T> GetMessageSpecification<T>()
            where T : class
        {
            if (typeof(T) != typeof(PipelineMessage))
                throw new InvalidOperationException($"Unexpected message type: {typeof(T)}.");

            return (ISagaMessageSpecification<PipelineSaga, T>)(object)this;
        }

        public IPipe<SagaConsumeContext<PipelineSaga, PipelineMessage>> BuildConsumerPipe(
            IFilter<SagaConsumeContext<PipelineSaga, PipelineMessage>> consumeFilter)
        {
            BuildConsumerPipeCalls++;
            ConsumeFilter = consumeFilter;

            if (BuildConsumerPipeFailure is not null)
                throw BuildConsumerPipeFailure;

            return ConsumerPipe!;
        }

        public IPipe<ConsumeContext<PipelineMessage>> BuildMessagePipe(
            Action<IPipeConfigurator<ConsumeContext<PipelineMessage>>> configure)
        {
            BuildMessagePipeCalls++;

            if (BuildMessagePipeFailure is not null)
                throw BuildMessagePipeFailure;

            ConfigureCallbackCalls++;
            configure(MessageConfigurator);
            return MessagePipe!;
        }

        public void AddPipeSpecification(IPipeSpecification<SagaConsumeContext<PipelineSaga, PipelineMessage>> specification) =>
            throw new NotSupportedException();

        public void AddPipeSpecification(IPipeSpecification<ConsumeContext<PipelineMessage>> specification) =>
            throw new NotSupportedException();

        public void AddPipeSpecification(IPipeSpecification<SagaConsumeContext<PipelineSaga>> specification) =>
            throw new NotSupportedException();

        public void Message(Action<ISagaMessageConfigurator<PipelineMessage>> configure) =>
            throw new NotSupportedException();

        public ConnectHandle ConnectSagaConfigurationObserver(ISagaConfigurationObserver observer) =>
            throw new NotSupportedException();

        public IEnumerable<ValidationResult> Validate() => throw new NotSupportedException();
    }

    sealed class RecordingPipeConfigurator<TContext> : IPipeConfigurator<TContext>
        where TContext : class, PipeContext
    {
        public List<IPipeSpecification<TContext>> Specifications { get; } = [];

        public void AddPipeSpecification(IPipeSpecification<TContext> specification)
        {
            Specifications.Add(specification);
        }
    }

    sealed class MarkerPipeSpecification<TContext> : IPipeSpecification<TContext>
        where TContext : class, PipeContext
    {
        public void Apply(IPipeBuilder<TContext> builder)
        {
        }

        public IEnumerable<ValidationResult> Validate() => [];
    }

    sealed class RecordingFilter<TContext> : IFilter<TContext>
        where TContext : class, PipeContext
    {
        public void Probe(ProbeContext context)
        {
        }

        public Task SendAsync(TContext context, IPipe<TContext> next) => Task.CompletedTask;
    }

    sealed class RecordingPipe<TContext> : IPipe<TContext>
        where TContext : class, PipeContext
    {
        public void Probe(ProbeContext context)
        {
        }

        public Task SendAsync(TContext context) => Task.CompletedTask;
    }

    sealed class RecordingSagaPolicy : ISagaPolicy<PipelineSaga, PipelineMessage>
    {
        public bool IsReadOnly => false;

        public bool PreInsertInstance(ConsumeContext<PipelineMessage> context, [NotNullWhen(true)] out PipelineSaga? instance)
        {
            instance = null;
            return false;
        }

        public Task ExistingAsync(
            SagaConsumeContext<PipelineSaga, PipelineMessage> context,
            IPipe<SagaConsumeContext<PipelineSaga, PipelineMessage>> next) => Task.CompletedTask;

        public Task MissingAsync(
            ConsumeContext<PipelineMessage> context,
            IPipe<SagaConsumeContext<PipelineSaga, PipelineMessage>> next) => Task.CompletedTask;
    }

    sealed class RecordingSagaQueryFactory : ISagaQueryFactory<PipelineSaga, PipelineMessage>
    {
        public void Probe(ProbeContext context)
        {
        }

        public bool TryCreateQuery(ConsumeContext<PipelineMessage> context, [NotNullWhen(true)] out ISagaQuery<PipelineSaga>? query)
        {
            query = null;
            return false;
        }
    }

    sealed class RecordingSagaRepository : ISagaRepository<PipelineSaga>
    {
        public void Probe(ProbeContext context)
        {
        }

        public Task SendAsync<T>(
            ConsumeContext<T> context,
            ISagaPolicy<PipelineSaga, T> policy,
            IPipe<SagaConsumeContext<PipelineSaga, T>> next)
            where T : class => throw new NotSupportedException();

        public Task SendQueryAsync<T>(
            ConsumeContext<T> context,
            ISagaQuery<PipelineSaga> query,
            ISagaPolicy<PipelineSaga, T> policy,
            IPipe<SagaConsumeContext<PipelineSaga, T>> next)
            where T : class => throw new NotSupportedException();
    }

    sealed class RecordingConsumePipeConnector : IConsumePipeConnector
    {
        public ConnectHandle? Handle { get; set; }

        public Exception? Failure { get; set; }

        public int DefaultCalls { get; private set; }

        public int OptionsCalls { get; private set; }

        public int TotalCalls => DefaultCalls + OptionsCalls;

        public object? ConnectedPipe { get; private set; }

        public ConnectPipeOptions? Options { get; private set; }

        public ConnectHandle ConnectConsumePipe<T>(IPipe<ConsumeContext<T>> pipe)
            where T : class
        {
            DefaultCalls++;
            ConnectedPipe = pipe;

            if (Failure is not null)
                throw Failure;

            return Handle!;
        }

        public ConnectHandle ConnectConsumePipe<T>(IPipe<ConsumeContext<T>> pipe, ConnectPipeOptions options)
            where T : class
        {
            OptionsCalls++;
            ConnectedPipe = pipe;
            Options = options;

            if (Failure is not null)
                throw Failure;

            return Handle!;
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
            DisposeCalls++;
        }
    }
}

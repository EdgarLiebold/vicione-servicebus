using System.Collections;
using System.Linq.Expressions;
using System.Reflection;
using ViciOne.ServiceBus.Middleware;
using ViciOne.ServiceBus.Sagas;
using ViciOne.ServiceBus.Tests.Infrastructure.Requirements;
using Xunit;

namespace ViciOne.ServiceBus.Tests.Configuration;

public sealed class SagaConnectorDiscoveryCacheDeepContractTests
{
    [Fact]
    [RequirementCoverage("REQ-VSB-SAGA-CONNECTOR-PUBLIC-CONTRACT", "iteration-212-concrete-connector-cache-shape")]
    public void ConcreteConnectorAndCache_ExposeOnlyTheSupportedConstructionAndAccessShape()
    {
        Type connector = typeof(SagaConnector<>);
        Assert.True(connector.IsPublic);
        Assert.True(connector.IsSealed);
        AssertSagaConstraint(connector);
        ConstructorInfo constructor = Assert.Single(connector.GetConstructors());
        Assert.Empty(constructor.GetParameters());
        PropertyInfo connectors = Assert.Single(connector.GetProperties(
            BindingFlags.Public | BindingFlags.Instance | BindingFlags.DeclaredOnly));
        Assert.Equal(nameof(SagaConnector<LifecycleSaga>.Connectors), connectors.Name);
        Assert.Equal(typeof(IEnumerable<ISagaMessageConnector>), connectors.PropertyType);
        Assert.False(connectors.CanWrite);

        Type cache = typeof(SagaConnectorCache<>);
        Assert.True(cache.IsPublic);
        Assert.False(cache.IsAbstract);
        AssertSagaConstraint(cache);
        Assert.Empty(cache.GetConstructors());
        PropertyInfo cachedConnector = Assert.Single(cache.GetProperties(
            BindingFlags.Public | BindingFlags.Static | BindingFlags.DeclaredOnly));
        Assert.Equal(nameof(SagaConnectorCache<LifecycleSaga>.Connector), cachedConnector.Name);
        Assert.Equal(typeof(ISagaConnector), cachedConnector.PropertyType);
        Assert.False(cachedConnector.CanWrite);
    }

    [Fact]
    [RequirementCoverage("REQ-VSB-SAGA-CONTRACT-DISCOVERY", "iteration-212-four-category-precedence-single-owner")]
    public void Discovery_UsesFourCategoryPrecedenceAndAssignsADuplicateMessageToOneOwner()
    {
        var connector = new SagaConnector<PrecedenceSaga>();
        ISagaMessageConnector[] connectors = connector.Connectors.ToArray();
        Type[] messages = connectors.Select(x => x.MessageType).ToArray();

        Assert.Equal(
            [
                typeof(DuplicateMessage),
                typeof(InitiatedMessage),
                typeof(OrchestratedMessage),
                typeof(CombinedMessage),
                typeof(ObservedMessage),
            ],
            messages);
        Assert.Equal(1, messages.Count(x => x == typeof(DuplicateMessage)));

        AssertConnectorFilter(connectors, typeof(DuplicateMessage), typeof(InitiatedBySagaMessageFilter<,>));
        AssertConnectorFilter(connectors, typeof(InitiatedMessage), typeof(InitiatedBySagaMessageFilter<,>));
        AssertConnectorFilter(connectors, typeof(OrchestratedMessage), typeof(OrchestratesSagaMessageFilter<,>));
        AssertConnectorFilter(connectors, typeof(CombinedMessage), typeof(InitiatedByOrOrchestratesSagaMessageFilter<,>));
        AssertConnectorFilter(connectors, typeof(ObservedMessage), typeof(ObservesSagaMessageFilter<,>));
    }

    [Fact]
    [RequirementCoverage("REQ-VSB-SAGA-CONNECTOR-PUBLIC-CONTRACT", "iteration-212-connector-view-read-only-mutation-resistant")]
    public void Connectors_ReturnAStableReadOnlyViewThatRejectsExternalMutation()
    {
        var connector = new SagaConnector<PrecedenceSaga>();
        IEnumerable<ISagaMessageConnector> view = connector.Connectors;
        var collection = Assert.IsAssignableFrom<IList>(view);
        Type[] expectedMessages = view.Select(x => x.MessageType).ToArray();

        Assert.True(collection.IsReadOnly);
        Assert.Throws<NotSupportedException>(() => collection.Clear());

        Assert.Same(view, connector.Connectors);
        Assert.Equal(expectedMessages, connector.Connectors.Select(x => x.MessageType));
    }

    [Fact]
    [RequirementCoverage("REQ-VSB-SAGA-CONTRACT-VALIDATION", "iteration-212-empty-and-reflection-failure-causality")]
    public void Discovery_ReportsEmptyAndReflectionFailuresWithoutLosingTheirCausality()
    {
        ConfigurationException empty = Assert.Throws<ConfigurationException>(() => new SagaConnector<EmptySaga>());
        Assert.Null(empty.InnerException);
        Assert.Contains(nameof(EmptySaga), empty.Message, StringComparison.Ordinal);
        Assert.Contains("does not declare a supported saga message contract", empty.Message, StringComparison.Ordinal);

        ConfigurationException reflection = Assert.Throws<ConfigurationException>(
            () => new SagaConnector<ReflectionFailureSaga>());
        Assert.Contains(nameof(ReflectionFailureSaga), reflection.Message, StringComparison.Ordinal);
        TargetInvocationException activation = Assert.IsType<TargetInvocationException>(reflection.InnerException);
        Assert.IsType<SyntheticReflectionFailure>(activation.InnerException);
    }

    [Fact]
    [RequirementCoverage("REQ-VSB-SAGA-CONTRACT-DISCOVERY", "iteration-212-cache-concurrent-single-publication")]
    public async Task Cache_PublishesOneConnectorIdentityDuringConcurrentFirstAccessAsync()
    {
        var start = new TaskCompletionSource(TaskCreationOptions.RunContinuationsAsynchronously);
        Task<ISagaConnector>[] reads = Enumerable.Range(0, 64)
            .Select(async _ =>
            {
                await start.Task;
                return SagaConnectorCache<ConcurrentSaga>.Connector;
            })
            .ToArray();
        Assert.All(reads, read => Assert.False(read.IsCompleted));

        start.SetResult();

        ISagaConnector[] connectors = await Task.WhenAll(reads);

        Assert.All(connectors, connector => Assert.Same(connectors[0], connector));
        Assert.Same(connectors[0], SagaConnectorCache<ConcurrentSaga>.Connector);
    }

    [Fact]
    [RequirementCoverage("REQ-VSB-SAGA-CONTRACT-VALIDATION", "iteration-212-cache-concurrent-failure-identity")]
    public async Task Cache_ReplaysTheSameConstructionFailureInstanceAcrossConcurrentAndLaterReadsAsync()
    {
        Task<Exception?>[] reads = Enumerable.Range(0, 64)
            .Select(_index => Task.Run(() => Record.Exception(
                () => _ = SagaConnectorCache<CachedEmptySaga>.Connector)))
            .ToArray();

        Exception?[] failures = await Task.WhenAll(reads);
        ConfigurationException first = Assert.IsType<ConfigurationException>(failures[0]);
        Assert.All(failures, failure => Assert.Same(first, failure));
        Assert.Same(first, Record.Exception(() => _ = SagaConnectorCache<CachedEmptySaga>.Connector));
    }

    [Fact]
    [RequirementCoverage("REQ-VSB-SAGA-DIRECT-CONNECT", "iteration-212-specification-exact-saga-before-collaborators")]
    public void CreateSagaSpecification_RequiresTheExactSagaTypeBeforeCreatingMessageSpecifications()
    {
        var connector = new SagaConnector<LifecycleSaga>();
        var messageConnector = new RecordingMessageConnector(typeof(LifecycleMessage), () => new RecordingConnectHandle());
        ReplaceConnectors(connector, messageConnector);
        ISagaConnector untyped = connector;

        ArgumentException mismatch = Assert.Throws<ArgumentException>(
            () => untyped.CreateSagaSpecification<OtherSaga>());

        Assert.Equal("T", mismatch.ParamName);
        Assert.Equal(0, messageConnector.CreateCalls);
        Assert.IsType<SagaSpecification<LifecycleSaga>>(untyped.CreateSagaSpecification<LifecycleSaga>());
        Assert.Equal(1, messageConnector.CreateCalls);
    }

    [Fact]
    [RequirementCoverage("REQ-VSB-SAGA-DIRECT-CONNECT", "iteration-212-connect-null-precedence-exact-saga")]
    public void ConnectSaga_ValidatesNullArgumentsInSignatureOrderThenRequiresTheExactSagaType()
    {
        var connector = new SagaConnector<LifecycleSaga>();
        var messageConnector = new RecordingMessageConnector(typeof(LifecycleMessage), () => new RecordingConnectHandle());
        ReplaceConnectors(connector, messageConnector);
        ISagaConnector untyped = connector;
        var pipe = new StubConsumePipeConnector();
        var repository = new StubSagaRepository<LifecycleSaga>();
        var specification = new SagaSpecification<LifecycleSaga>([]);

        Assert.Equal("consumePipe", Assert.Throws<ArgumentNullException>(
            () => untyped.ConnectSaga(null!, repository, specification)).ParamName);
        Assert.Equal("repository", Assert.Throws<ArgumentNullException>(
            () => untyped.ConnectSaga(pipe, null!, specification)).ParamName);
        Assert.Equal("specification", Assert.Throws<ArgumentNullException>(
            () => untyped.ConnectSaga(pipe, repository, null!)).ParamName);

        ArgumentException mismatch = Assert.Throws<ArgumentException>(() => untyped.ConnectSaga(
            pipe,
            new StubSagaRepository<OtherSaga>(),
            new SagaSpecification<OtherSaga>([])));
        Assert.Equal("T", mismatch.ParamName);
        Assert.Equal(0, messageConnector.ConnectCalls);
    }

    [Fact]
    [RequirementCoverage("REQ-VSB-SAGA-DIRECT-CONNECT", "iteration-212-mismatched-saga-still-honors-null-precedence")]
    public void ConnectSaga_WhenSagaTypeAlsoMismatches_StillValidatesNullArgumentsInSignatureOrder()
    {
        var connector = new SagaConnector<LifecycleSaga>();
        var messageConnector = new RecordingMessageConnector(typeof(LifecycleMessage), () => new RecordingConnectHandle());
        ReplaceConnectors(connector, messageConnector);
        ISagaConnector untyped = connector;
        var pipe = new StubConsumePipeConnector();
        var repository = new StubSagaRepository<OtherSaga>();
        var specification = new SagaSpecification<OtherSaga>([]);

        Assert.Equal("consumePipe", Assert.Throws<ArgumentNullException>(() =>
            untyped.ConnectSaga<OtherSaga>(null!, repository, specification)).ParamName);
        Assert.Equal("repository", Assert.Throws<ArgumentNullException>(() =>
            untyped.ConnectSaga<OtherSaga>(pipe, null!, specification)).ParamName);
        Assert.Equal("specification", Assert.Throws<ArgumentNullException>(() =>
            untyped.ConnectSaga<OtherSaga>(pipe, repository, null!)).ParamName);
        Assert.Equal(0, messageConnector.ConnectCalls);
    }

    [Fact]
    [RequirementCoverage("REQ-VSB-SAGA-DIRECT-CONNECT", "iteration-212-successful-aggregate-owns-every-child-in-order")]
    public void ConnectSaga_WhenEveryChildSucceeds_ReturnsHandleOwningEveryChildInOrder()
    {
        var connector = new SagaConnector<LifecycleSaga>();
        var disposalOrder = new List<string>();
        var first = new RecordingConnectHandle(onDispose: () => disposalOrder.Add("first"));
        var second = new RecordingConnectHandle(onDispose: () => disposalOrder.Add("second"));
        ReplaceConnectors(
            connector,
            new RecordingMessageConnector(typeof(FirstLifecycleMessage), () => first),
            new RecordingMessageConnector(typeof(SecondLifecycleMessage), () => second));

        ConnectHandle aggregate = Connect(connector);

        Assert.Equal(0, first.DisposeCalls);
        Assert.Equal(0, second.DisposeCalls);

        aggregate.Dispose();

        Assert.Equal(1, first.DisposeCalls);
        Assert.Equal(1, second.DisposeCalls);
        Assert.Equal(["first", "second"], disposalOrder);
    }

    [Fact]
    [RequirementCoverage("REQ-VSB-SAGA-DIRECT-CONNECT", "iteration-212-partial-handles-released-primary-rethrow")]
    public void ConnectSaga_ReleasesEveryOwnedPartialHandleAndRethrowsThePrimaryFailure()
    {
        var connector = new SagaConnector<LifecycleSaga>();
        var handle = new RecordingConnectHandle();
        var primary = new InvalidOperationException("primary");
        ReplaceConnectors(
            connector,
            new RecordingMessageConnector(typeof(FirstLifecycleMessage), () => handle),
            new RecordingMessageConnector(typeof(SecondLifecycleMessage), () => throw primary));

        Exception failure = Assert.Throws<InvalidOperationException>(() => Connect(connector));

        Assert.Same(primary, failure);
        Assert.Equal(1, handle.DisposeCalls);
    }

    [Fact]
    [RequirementCoverage("REQ-VSB-SAGA-DIRECT-CONNECT", "iteration-212-null-child-rejected-prior-ownership-released")]
    public void ConnectSaga_RejectsANullChildHandleAndReleasesPriorOwnership()
    {
        var connector = new SagaConnector<LifecycleSaga>();
        var handle = new RecordingConnectHandle();
        ReplaceConnectors(
            connector,
            new RecordingMessageConnector(typeof(FirstLifecycleMessage), () => handle),
            new RecordingMessageConnector(typeof(SecondLifecycleMessage), () => null!));

        InvalidOperationException failure = Assert.Throws<InvalidOperationException>(() => Connect(connector));

        Assert.Contains(nameof(SecondLifecycleMessage), failure.Message, StringComparison.Ordinal);
        Assert.Equal(1, handle.DisposeCalls);
    }

    [Fact]
    [RequirementCoverage("REQ-VSB-SAGA-DIRECT-CONNECT", "iteration-212-primary-and-all-cleanup-failures-preserved")]
    public void ConnectSaga_PreservesThePrimaryAndEveryCleanupFailureInCausalOrder()
    {
        var connector = new SagaConnector<LifecycleSaga>();
        var cleanupOne = new InvalidOperationException("cleanup-one");
        var cleanupTwo = new InvalidOperationException("cleanup-two");
        var first = new RecordingConnectHandle(cleanupOne);
        var second = new RecordingConnectHandle(cleanupTwo);
        var primary = new InvalidOperationException("primary");
        ReplaceConnectors(
            connector,
            new RecordingMessageConnector(typeof(FirstLifecycleMessage), () => first),
            new RecordingMessageConnector(typeof(SecondLifecycleMessage), () => second),
            new RecordingMessageConnector(typeof(ThirdLifecycleMessage), () => throw primary));

        AggregateException failure = Assert.Throws<AggregateException>(() => Connect(connector));

        Assert.Equal(1, first.DisposeCalls);
        Assert.Equal(1, second.DisposeCalls);
        Assert.Equal([primary, cleanupOne, cleanupTwo], failure.InnerExceptions);
    }

    private static ConnectHandle Connect(SagaConnector<LifecycleSaga> connector) =>
        ((ISagaConnector)connector).ConnectSaga(
            new StubConsumePipeConnector(),
            new StubSagaRepository<LifecycleSaga>(),
            new SagaSpecification<LifecycleSaga>([]));

    private static void ReplaceConnectors(
        SagaConnector<LifecycleSaga> connector,
        params RecordingMessageConnector[] replacements)
    {
        var connectors = Assert.IsType<List<ISagaMessageConnector<LifecycleSaga>>>(
            ReadField<object>(connector, "_connectors"));
        connectors.Clear();
        connectors.AddRange(replacements);
    }

    private static T ReadField<T>(object instance, string name)
    {
        for (Type? type = instance.GetType(); type != null; type = type.BaseType)
        {
            FieldInfo? field = type.GetField(name, BindingFlags.Instance | BindingFlags.NonPublic);
            if (field != null)
                return Assert.IsAssignableFrom<T>(field.GetValue(instance));
        }

        throw new Xunit.Sdk.XunitException($"Field '{name}' was not found.");
    }

    private static void AssertConnectorFilter(
        IEnumerable<ISagaMessageConnector> connectors,
        Type messageType,
        Type expectedFilterType)
    {
        ISagaMessageConnector connector = Assert.Single(connectors, x => x.MessageType == messageType);
        object filter = ReadField<object>(connector, "_consumeFilter");
        Assert.Equal(expectedFilterType, filter.GetType().GetGenericTypeDefinition());
    }

    private static void AssertSagaConstraint(Type genericType)
    {
        Type parameter = Assert.Single(genericType.GetGenericArguments());
        Assert.Equal(GenericParameterAttributes.ReferenceTypeConstraint,
            parameter.GenericParameterAttributes & GenericParameterAttributes.SpecialConstraintMask);
        Assert.Equal([typeof(ISaga)], parameter.GetGenericParameterConstraints());
    }

    public sealed record DuplicateMessage(Guid CorrelationId) : ICorrelatedBy<Guid>;
    public sealed record InitiatedMessage(Guid CorrelationId) : ICorrelatedBy<Guid>;
    public sealed record OrchestratedMessage(Guid CorrelationId) : ICorrelatedBy<Guid>;
    public sealed record CombinedMessage(Guid CorrelationId) : ICorrelatedBy<Guid>;
    public sealed record ObservedMessage(string Key);
    public sealed record ConcurrentMessage(Guid CorrelationId) : ICorrelatedBy<Guid>;
    public sealed record LifecycleMessage(Guid CorrelationId) : ICorrelatedBy<Guid>;
    public sealed record FirstLifecycleMessage(Guid CorrelationId) : ICorrelatedBy<Guid>;
    public sealed record SecondLifecycleMessage(Guid CorrelationId) : ICorrelatedBy<Guid>;
    public sealed record ThirdLifecycleMessage(Guid CorrelationId) : ICorrelatedBy<Guid>;

    public sealed class PrecedenceSaga :
        ISaga,
        IInitiatedBy<DuplicateMessage>,
        IOrchestrates<DuplicateMessage>,
        IInitiatedByOrOrchestrates<DuplicateMessage>,
        IObserves<DuplicateMessage, PrecedenceSaga>,
        IInitiatedBy<InitiatedMessage>,
        IOrchestrates<OrchestratedMessage>,
        IInitiatedByOrOrchestrates<CombinedMessage>,
        IObserves<ObservedMessage, PrecedenceSaga>
    {
        public PrecedenceSaga(Guid correlationId) => CorrelationId = correlationId;
        public Guid CorrelationId { get; set; }
        public string Key { get; private set; } = string.Empty;
        Expression<Func<PrecedenceSaga, DuplicateMessage, bool>> IObserves<DuplicateMessage, PrecedenceSaga>.CorrelationExpression =>
            (saga, message) => saga.CorrelationId == message.CorrelationId;
        Expression<Func<PrecedenceSaga, ObservedMessage, bool>> IObserves<ObservedMessage, PrecedenceSaga>.CorrelationExpression =>
            (saga, message) => saga.Key == message.Key;
        public Task ConsumeAsync(ConsumeContext<DuplicateMessage> context) => Task.CompletedTask;
        public Task ConsumeAsync(ConsumeContext<InitiatedMessage> context) => Task.CompletedTask;
        public Task ConsumeAsync(ConsumeContext<OrchestratedMessage> context) => Task.CompletedTask;
        public Task ConsumeAsync(ConsumeContext<CombinedMessage> context) => Task.CompletedTask;
        public Task ConsumeAsync(ConsumeContext<ObservedMessage> context) => Task.CompletedTask;
    }

    public sealed class EmptySaga : ISaga { public Guid CorrelationId { get; set; } }
    public sealed class CachedEmptySaga : ISaga { public Guid CorrelationId { get; set; } }
    public sealed class OtherSaga : ISaga { public Guid CorrelationId { get; set; } }

    public sealed class ReflectionFailureSaga : ISaga, IObserves<ObservedMessage, ReflectionFailureSaga>
    {
        public ReflectionFailureSaga(Guid correlationId) => CorrelationId = correlationId;
        public Guid CorrelationId { get; set; }
        public Expression<Func<ReflectionFailureSaga, ObservedMessage, bool>> CorrelationExpression =>
            throw new SyntheticReflectionFailure();
        public Task ConsumeAsync(ConsumeContext<ObservedMessage> context) => Task.CompletedTask;
    }

    public sealed class SyntheticReflectionFailure : Exception;

    public sealed class ConcurrentSaga : ISaga, IInitiatedBy<ConcurrentMessage>
    {
        public ConcurrentSaga(Guid correlationId) => CorrelationId = correlationId;
        public Guid CorrelationId { get; set; }
        public Task ConsumeAsync(ConsumeContext<ConcurrentMessage> context) => Task.CompletedTask;
    }

    public sealed class LifecycleSaga : ISaga, IInitiatedBy<LifecycleMessage>
    {
        public LifecycleSaga(Guid correlationId) => CorrelationId = correlationId;
        public Guid CorrelationId { get; set; }
        public Task ConsumeAsync(ConsumeContext<LifecycleMessage> context) => Task.CompletedTask;
    }

    private sealed class RecordingMessageConnector(Type messageType, Func<ConnectHandle> connect) :
        ISagaMessageConnector<LifecycleSaga>
    {
        public Type MessageType { get; } = messageType;
        public int CreateCalls { get; private set; }
        public int ConnectCalls { get; private set; }

        public ISagaMessageSpecification<LifecycleSaga> CreateSagaMessageSpecification()
        {
            CreateCalls++;
            return new SagaConnector<LifecycleSaga, LifecycleMessage>.SagaMessageSpecification();
        }

        public ConnectHandle ConnectSaga(
            IConsumePipeConnector consumePipe,
            ISagaRepository<LifecycleSaga> repository,
            ISagaSpecification<LifecycleSaga> specification)
        {
            ConnectCalls++;
            return connect();
        }
    }

    private sealed class RecordingConnectHandle(Exception? disposeFailure = null, Action? onDispose = null) : ConnectHandle
    {
        public int DisposeCalls { get; private set; }
        public void Disconnect() => Dispose();
        public void Dispose()
        {
            DisposeCalls++;
            onDispose?.Invoke();

            if (disposeFailure != null)
                throw disposeFailure;
        }
    }

    private sealed class StubConsumePipeConnector : IConsumePipeConnector
    {
        public ConnectHandle ConnectConsumePipe<T>(IPipe<ConsumeContext<T>> pipe) where T : class =>
            throw new NotSupportedException();

        public ConnectHandle ConnectConsumePipe<T>(IPipe<ConsumeContext<T>> pipe, ConnectPipeOptions options) where T : class =>
            throw new NotSupportedException();
    }

    private sealed class StubSagaRepository<TSaga> : ISagaRepository<TSaga> where TSaga : class, ISaga
    {
        public void Probe(ProbeContext context) { }
        public Task SendAsync<T>(ConsumeContext<T> context, ISagaPolicy<TSaga, T> policy,
            IPipe<SagaConsumeContext<TSaga, T>> next) where T : class => throw new NotSupportedException();
        public Task SendQueryAsync<T>(ConsumeContext<T> context, ISagaQuery<TSaga> query,
            ISagaPolicy<TSaga, T> policy, IPipe<SagaConsumeContext<TSaga, T>> next) where T : class =>
            throw new NotSupportedException();
    }
}

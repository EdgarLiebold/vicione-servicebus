using System.Linq.Expressions;
using System.Reflection;
using ViciOne.ServiceBus.Context;
using ViciOne.ServiceBus.Middleware;
using ViciOne.ServiceBus.Saga;
using ViciOne.ServiceBus.Sagas;
using ViciOne.ServiceBus.Tests.Infrastructure.Requirements;
using ViciOne.ServiceBus.Tests.InternalAccess.InMemoryOutbox;
using Xunit;

namespace ViciOne.ServiceBus.Tests.Configuration;

public sealed class SagaConnectorFactoryDeepContractTests
{
    private const string MismatchMessage = "The saga type did not match the connector type";

    [Fact]
    [RequirementCoverage("REQ-VSB-SAGA-CONNECTOR-FACTORY", "exact-four-public-factory-surfaces-constraints-and-explicit-method")]
    public void Surface_ExposesFourPublicFactoriesWithExactConstraintsAndExplicitInterfaceMethod()
    {
        AssertFactorySurface(
            typeof(InitiatedBySagaConnectorFactory<,>),
            typeof(IInitiatedBy<>),
            correlatedMessage: true,
            selfReferencingRole: false);
        AssertFactorySurface(
            typeof(InitiatedByOrOrchestratesSagaConnectorFactory<,>),
            typeof(IInitiatedByOrOrchestrates<>),
            correlatedMessage: true,
            selfReferencingRole: false);
        AssertFactorySurface(
            typeof(OrchestratesSagaConnectorFactory<,>),
            typeof(IOrchestrates<>),
            correlatedMessage: true,
            selfReferencingRole: false);
        AssertFactorySurface(
            typeof(ObservesSagaConnectorFactory<,>),
            typeof(IObserves<,>),
            correlatedMessage: false,
            selfReferencingRole: true);
    }

    [Fact]
    [RequirementCoverage("REQ-VSB-SAGA-CONNECTOR-FACTORY", "exact-saga-mismatch-without-cached-connector-replacement")]
    public void ExplicitInterfaceMethod_RejectsEverySagaMismatchExactlyWithoutReplacingTheCachedConnector()
    {
        AssertMismatchDoesNotReplaceConnector<InitiatedSaga>(
            new InitiatedBySagaConnectorFactory<InitiatedSaga, CorrelatedMessage>());
        AssertMismatchDoesNotReplaceConnector<CombinedSaga>(
            new InitiatedByOrOrchestratesSagaConnectorFactory<CombinedSaga, CorrelatedMessage>());
        AssertMismatchDoesNotReplaceConnector<OrchestratesSaga>(
            new OrchestratesSagaConnectorFactory<OrchestratesSaga, CorrelatedMessage>());
        AssertMismatchDoesNotReplaceConnector<ObservesSaga>(
            new ObservesSagaConnectorFactory<ObservesSaga, ObservedMessage>());
    }

    [Fact]
    [RequirementCoverage("REQ-VSB-SAGA-CONNECTOR-FACTORY", "cached-connector-identity-message-type-and-fresh-specification")]
    public void ExplicitInterfaceMethod_ReturnsOneCachedConnectorWithTheExactMessageType()
    {
        AssertCachedConnector<InitiatedSaga, CorrelatedMessage>(
            new InitiatedBySagaConnectorFactory<InitiatedSaga, CorrelatedMessage>(),
            typeof(SagaConnector<InitiatedSaga, CorrelatedMessage>.CorrelatedSagaMessageConnector));
        AssertCachedConnector<CombinedSaga, CorrelatedMessage>(
            new InitiatedByOrOrchestratesSagaConnectorFactory<CombinedSaga, CorrelatedMessage>(),
            typeof(SagaConnector<CombinedSaga, CorrelatedMessage>.CorrelatedSagaMessageConnector));
        AssertCachedConnector<OrchestratesSaga, CorrelatedMessage>(
            new OrchestratesSagaConnectorFactory<OrchestratesSaga, CorrelatedMessage>(),
            typeof(SagaConnector<OrchestratesSaga, CorrelatedMessage>.CorrelatedSagaMessageConnector));
        AssertCachedConnector<ObservesSaga, ObservedMessage>(
            new ObservesSagaConnectorFactory<ObservesSaga, ObservedMessage>(),
            typeof(SagaConnector<ObservesSaga, ObservedMessage>.QuerySagaMessageConnector));
    }

    [Fact]
    [RequirementCoverage("REQ-VSB-SAGA-CONNECTOR-FACTORY", "initiated-filter-new-policy-message-correlation-and-lifecycle")]
    public void InitiatedFactory_ComposesItsFilterPolicyAndMessageCorrelationExactly()
    {
        ISagaConnectorFactory factory = new InitiatedBySagaConnectorFactory<InitiatedSaga, CorrelatedMessage>();
        ISagaMessageConnector<InitiatedSaga> connector = factory.CreateMessageConnector<InitiatedSaga>();

        Assert.IsType<InitiatedBySagaMessageFilter<InitiatedSaga, CorrelatedMessage>>(
            ReadField<object>(connector, "_consumeFilter"));
        var policy = Assert.IsType<NewSagaPolicy<InitiatedSaga, CorrelatedMessage>>(
            ReadField<object>(connector, "_policy"));
        Assert.False(policy.IsReadOnly);
        Assert.False(ReadField<bool>(policy, "_insertOnInitial"));
        Assert.IsType<DefaultSagaFactory<InitiatedSaga, CorrelatedMessage>>(
            ReadField<object>(policy, "_sagaFactory"));

        Func<ConsumeContext<CorrelatedMessage>, Guid> selector =
            ReadField<Func<ConsumeContext<CorrelatedMessage>, Guid>>(connector, "_correlationIdSelector");
        Guid messageId = NewId.NextGuid();
        ConsumeContext<CorrelatedMessage> selectorContext =
            CreateContext(new CorrelatedMessage(messageId), NewId.NextGuid());
        Assert.Equal(messageId, selector(selectorContext));
    }

    [Fact]
    [RequirementCoverage("REQ-VSB-SAGA-CONNECTOR-FACTORY", "initiated-policy-missing-creates-and-existing-rejects")]
    public void InitiatedFactory_MissingCreatesTheSagaWhileExistingIsRejected()
    {
        ISagaConnectorFactory factory = new InitiatedBySagaConnectorFactory<InitiatedSaga, CorrelatedMessage>();
        var policy = Assert.IsAssignableFrom<ISagaPolicy<InitiatedSaga, CorrelatedMessage>>(
            ReadField<object>(factory.CreateMessageConnector<InitiatedSaga>(), "_policy"));
        Guid correlationId = NewId.NextGuid();
        var message = new CorrelatedMessage(correlationId);
        ConsumeContext<CorrelatedMessage> context = CreateContext(message, correlationId);
        var completion = new TaskCompletionSource(TaskCreationOptions.RunContinuationsAsynchronously);
        var missingPipe = new RecordingPipe<InitiatedSaga, CorrelatedMessage> { Result = completion.Task };

        Task result = policy.MissingAsync(context, missingPipe);

        Assert.Same(completion.Task, result);
        Assert.Equal(1, missingPipe.Count);
        Assert.NotNull(missingPipe.Context);
        Assert.Equal(correlationId, missingPipe.Context.Saga.CorrelationId);
        Assert.Same(message, missingPipe.Context.Message);

        var existing = new InitiatedSaga(correlationId);
        var existingContext = new DefaultSagaConsumeContext<InitiatedSaga, CorrelatedMessage>(context, existing);
        var existingPipe = new RecordingPipe<InitiatedSaga, CorrelatedMessage>();
        SagaException exception = Assert.Throws<SagaException>(() =>
        {
            _ = policy.ExistingAsync(existingContext, existingPipe);
        });

        Assert.Equal(
            $"{ShortName<InitiatedSaga>()}({correlationId}) Saga exception on receipt of "
            + $"{ShortName<CorrelatedMessage>()}: The message cannot be accepted by an existing saga",
            exception.Message);
        Assert.Equal(correlationId, exception.CorrelationId);
        Assert.Same(typeof(InitiatedSaga), exception.SagaType);
        Assert.Same(typeof(CorrelatedMessage), exception.MessageType);
        Assert.Equal(0, existingPipe.Count);
    }

    [Fact]
    [RequirementCoverage("REQ-VSB-SAGA-CONNECTOR-FACTORY", "combined-filter-new-or-existing-policy-correlation-and-both-lifecycles")]
    public void CombinedFactory_ComposesCorrelationAndSupportsBothMissingAndExistingLifecycles()
    {
        ISagaConnectorFactory factory =
            new InitiatedByOrOrchestratesSagaConnectorFactory<CombinedSaga, CorrelatedMessage>();
        ISagaMessageConnector<CombinedSaga> connector = factory.CreateMessageConnector<CombinedSaga>();
        Assert.IsType<InitiatedByOrOrchestratesSagaMessageFilter<CombinedSaga, CorrelatedMessage>>(
            ReadField<object>(connector, "_consumeFilter"));
        var policy = Assert.IsType<NewOrExistingSagaPolicy<CombinedSaga, CorrelatedMessage>>(
            ReadField<object>(connector, "_policy"));
        Assert.False(policy.IsReadOnly);
        Assert.False(ReadField<bool>(policy, "_insertOnInitial"));
        Assert.IsType<DefaultSagaFactory<CombinedSaga, CorrelatedMessage>>(
            ReadField<object>(policy, "_sagaFactory"));

        Func<ConsumeContext<CorrelatedMessage>, Guid> selector =
            ReadField<Func<ConsumeContext<CorrelatedMessage>, Guid>>(connector, "_correlationIdSelector");
        Guid correlationId = NewId.NextGuid();
        var message = new CorrelatedMessage(correlationId);
        ConsumeContext<CorrelatedMessage> context = CreateContext(message, correlationId);
        Assert.Equal(correlationId, selector(context));
        Assert.False(policy.PreInsertInstance(context, out CombinedSaga? preInserted));
        Assert.Null(preInserted);

        var missingCompletion = new TaskCompletionSource(TaskCreationOptions.RunContinuationsAsynchronously);
        var missingPipe = new RecordingPipe<CombinedSaga, CorrelatedMessage> { Result = missingCompletion.Task };
        Task missingResult = ((ISagaPolicy<CombinedSaga, CorrelatedMessage>)policy).MissingAsync(context, missingPipe);
        Assert.Same(missingCompletion.Task, missingResult);
        Assert.Equal(correlationId, Assert.IsType<CombinedSaga>(missingPipe.Context?.Saga).CorrelationId);

        var existing = new CombinedSaga(correlationId);
        var existingContext = new DefaultSagaConsumeContext<CombinedSaga, CorrelatedMessage>(context, existing);
        var existingCompletion = new TaskCompletionSource(TaskCreationOptions.RunContinuationsAsynchronously);
        var existingPipe = new RecordingPipe<CombinedSaga, CorrelatedMessage> { Result = existingCompletion.Task };
        Task existingResult = ((ISagaPolicy<CombinedSaga, CorrelatedMessage>)policy).ExistingAsync(existingContext, existingPipe);

        Assert.Same(existingCompletion.Task, existingResult);
        Assert.Equal(1, existingPipe.Count);
        Assert.NotNull(existingPipe.Context);
        Assert.Same(existingContext, existingPipe.Context);
        Assert.Same(existing, existingPipe.Context.Saga);
    }

    [Fact]
    [RequirementCoverage("REQ-VSB-SAGA-CONNECTOR-FACTORY", "orchestrates-filter-existing-policy-correlation-and-missing-diagnostic")]
    public async Task OrchestratesFactory_ComposesExistingPolicyAndReportsTheExactMissingSagaAsync()
    {
        ISagaConnectorFactory factory = new OrchestratesSagaConnectorFactory<OrchestratesSaga, CorrelatedMessage>();
        ISagaMessageConnector<OrchestratesSaga> connector = factory.CreateMessageConnector<OrchestratesSaga>();
        Assert.IsType<OrchestratesSagaMessageFilter<OrchestratesSaga, CorrelatedMessage>>(
            ReadField<object>(connector, "_consumeFilter"));
        var policy = Assert.IsType<AnyExistingSagaPolicy<OrchestratesSaga, CorrelatedMessage>>(
            ReadField<object>(connector, "_policy"));
        Assert.False(policy.IsReadOnly);
        Assert.NotNull(ReadField<IPipe<ConsumeContext<CorrelatedMessage>>>(policy, "_missingPipe"));

        Func<ConsumeContext<CorrelatedMessage>, Guid> selector =
            ReadField<Func<ConsumeContext<CorrelatedMessage>, Guid>>(connector, "_correlationIdSelector");
        Guid correlationId = NewId.NextGuid();
        var message = new CorrelatedMessage(correlationId);
        ConsumeContext<CorrelatedMessage> context = CreateContext(message, correlationId);
        Assert.Equal(correlationId, selector(context));
        Assert.False(policy.PreInsertInstance(context, out OrchestratesSaga? preInserted));
        Assert.Null(preInserted);

        var missingPipe = new RecordingPipe<OrchestratesSaga, CorrelatedMessage>();
        SagaException exception = await Assert.ThrowsAsync<SagaException>(() =>
            ((ISagaPolicy<OrchestratesSaga, CorrelatedMessage>)policy).MissingAsync(context, missingPipe));

        Assert.Equal(
            $"{ShortName<OrchestratesSaga>()}({correlationId}) Saga exception on receipt of "
            + $"{ShortName<CorrelatedMessage>()}: An existing saga instance was not found",
            exception.Message);
        Assert.Equal(correlationId, exception.CorrelationId);
        Assert.Same(typeof(OrchestratesSaga), exception.SagaType);
        Assert.Same(typeof(CorrelatedMessage), exception.MessageType);
        Assert.Equal(0, missingPipe.Count);

        var saga = new OrchestratesSaga(correlationId);
        var existingContext = new DefaultSagaConsumeContext<OrchestratesSaga, CorrelatedMessage>(context, saga);
        var existingCompletion = new TaskCompletionSource(TaskCreationOptions.RunContinuationsAsynchronously);
        var existingPipe = new RecordingPipe<OrchestratesSaga, CorrelatedMessage> { Result = existingCompletion.Task };
        Task existingResult = ((ISagaPolicy<OrchestratesSaga, CorrelatedMessage>)policy).ExistingAsync(existingContext, existingPipe);
        Assert.Same(existingCompletion.Task, existingResult);
        Assert.Same(existingContext, existingPipe.Context);
    }

    [Fact]
    [RequirementCoverage("REQ-VSB-SAGA-CONNECTOR-FACTORY", "observes-filter-query-policy-expression-and-empty-missing-path")]
    public async Task ObservesFactory_ComposesOneInstanceExpressionQueryAndIgnoresAMissingSagaAsync()
    {
        ObservesSaga.Reset();
        ISagaConnectorFactory factory = new ObservesSagaConnectorFactory<ObservesSaga, ObservedMessage>();
        ISagaMessageConnector<ObservesSaga> connector = factory.CreateMessageConnector<ObservesSaga>();

        Assert.Equal(1, ObservesSaga.ConstructorCount);
        Assert.Equal(1, ObservesSaga.ExpressionCount);
        Assert.NotEqual(Guid.Empty, ObservesSaga.FactoryCorrelationId);
        Assert.IsType<ObservesSagaMessageFilter<ObservesSaga, ObservedMessage>>(
            ReadField<object>(connector, "_consumeFilter"));
        var policy = Assert.IsType<AnyExistingSagaPolicy<ObservesSaga, ObservedMessage>>(
            ReadField<object>(connector, "_policy"));
        Assert.False(policy.IsReadOnly);
        var queryFactory = Assert.IsType<ExpressionSagaQueryFactory<ObservesSaga, ObservedMessage>>(
            ReadField<object>(connector, "_queryFactory"));
        Assert.Same(ObservesSaga.LastExpression, ReadField<object>(queryFactory, "_filterExpression"));

        var message = new ObservedMessage("matching-key");
        ConsumeContext<ObservedMessage> context = CreateContext(message);
        Assert.True(((ISagaQueryFactory<ObservesSaga, ObservedMessage>)queryFactory).TryCreateQuery(
            context, out ISagaQuery<ObservesSaga>? query));
        Assert.NotNull(query);
        Func<ObservesSaga, bool> filter = query.GetFilter();
        Assert.True(filter(new ObservesSaga(NewId.NextGuid()) { Key = message.Key }));
        Assert.False(filter(new ObservesSaga(NewId.NextGuid()) { Key = "different" }));

        var missingPipe = new RecordingPipe<ObservesSaga, ObservedMessage>();
        await ((ISagaPolicy<ObservesSaga, ObservedMessage>)policy).MissingAsync(context, missingPipe);
        Assert.Equal(0, missingPipe.Count);
    }

    [Fact]
    [RequirementCoverage("REQ-VSB-SAGA-CONNECTOR-FACTORY", "all-composed-policies-context-before-next-preconditions")]
    public void ComposedPolicies_ValidateContextBeforeNextAndBeforeTheirMissingBehavior()
    {
        AssertMissingPreconditions(
            GetPolicy<InitiatedSaga, CorrelatedMessage>(
                new InitiatedBySagaConnectorFactory<InitiatedSaga, CorrelatedMessage>()),
            CreateContext(new CorrelatedMessage(NewId.NextGuid())));
        AssertMissingPreconditions(
            GetPolicy<CombinedSaga, CorrelatedMessage>(
                new InitiatedByOrOrchestratesSagaConnectorFactory<CombinedSaga, CorrelatedMessage>()),
            CreateContext(new CorrelatedMessage(NewId.NextGuid())));
        AssertMissingPreconditions(
            GetPolicy<OrchestratesSaga, CorrelatedMessage>(
                new OrchestratesSagaConnectorFactory<OrchestratesSaga, CorrelatedMessage>()),
            CreateContext(new CorrelatedMessage(NewId.NextGuid())));
        AssertMissingPreconditions(
            GetPolicy<ObservesSaga, ObservedMessage>(
                new ObservesSagaConnectorFactory<ObservesSaga, ObservedMessage>()),
            CreateContext(new ObservedMessage("key")));
    }

    [Fact]
    [RequirementCoverage("REQ-VSB-SAGA-CONNECTOR-FACTORY", "observes-expression-prerequisite-before-connector-publication")]
    public void ObservesFactory_RequiresTheCorrelationExpressionBeforePublishingAConnector()
    {
        NullExpressionObservesSaga.Reset();

        ArgumentNullException exception = Assert.Throws<ArgumentNullException>(() =>
            new ObservesSagaConnectorFactory<NullExpressionObservesSaga, ObservedMessage>());

        Assert.Equal("filterExpression", exception.ParamName);
        Assert.Equal(1, NullExpressionObservesSaga.ConstructorCount);
        Assert.Equal(1, NullExpressionObservesSaga.ExpressionCount);
    }

    private static void AssertFactorySurface(
        Type factory,
        Type roleDefinition,
        bool correlatedMessage,
        bool selfReferencingRole)
    {
        Assert.True(factory.IsPublic && factory.IsClass);
        Assert.False(factory.IsAbstract);
        Assert.False(factory.IsSealed);
        Assert.Equal(typeof(ISagaConnectorFactory), Assert.Single(factory.GetInterfaces()));

        Type[] arguments = factory.GetGenericArguments();
        Assert.Equal(["TSaga", "TMessage"], arguments.Select(argument => argument.Name));
        Type role = selfReferencingRole
            ? roleDefinition.MakeGenericType(arguments[1], arguments[0])
            : roleDefinition.MakeGenericType(arguments[1]);
        AssertReferenceConstraints(arguments[0], typeof(ISaga), role);
        AssertReferenceConstraints(
            arguments[1],
            correlatedMessage ? [typeof(ICorrelatedBy<Guid>)] : []);

        ConstructorInfo constructor = Assert.Single(factory.GetConstructors(
            BindingFlags.Public | BindingFlags.Instance | BindingFlags.DeclaredOnly));
        Assert.Empty(constructor.GetParameters());
        FieldInfo connector = Assert.Single(factory.GetFields(
            BindingFlags.NonPublic | BindingFlags.Instance | BindingFlags.DeclaredOnly));
        Assert.Equal("_connector", connector.Name);
        Assert.True(connector.IsInitOnly);
        Assert.Equal(typeof(ISagaMessageConnector<>).MakeGenericType(arguments[0]), connector.FieldType);
        Assert.DoesNotContain(
            factory.GetMethods(BindingFlags.Public | BindingFlags.Instance | BindingFlags.DeclaredOnly),
            method => method.Name.Contains(nameof(ISagaConnectorFactory.CreateMessageConnector), StringComparison.Ordinal));

        MethodInfo explicitMethod = Assert.Single(factory.GetMethods(
            BindingFlags.NonPublic | BindingFlags.Instance | BindingFlags.DeclaredOnly));
        Assert.EndsWith(".CreateMessageConnector", explicitMethod.Name, StringComparison.Ordinal);
        Assert.True(explicitMethod.IsPrivate && explicitMethod.IsFinal && explicitMethod.IsVirtual);
        Assert.True(explicitMethod.IsGenericMethodDefinition);
        Assert.Empty(explicitMethod.GetParameters());
        Type methodSaga = Assert.Single(explicitMethod.GetGenericArguments());
        Assert.Equal("T", methodSaga.Name);
        AssertReferenceConstraints(methodSaga, typeof(ISaga));
        Assert.Equal(typeof(ISagaMessageConnector<>).MakeGenericType(methodSaga), explicitMethod.ReturnType);

        InterfaceMapping mapping = factory.GetInterfaceMap(typeof(ISagaConnectorFactory));
        Assert.Equal(explicitMethod, Assert.Single(mapping.TargetMethods));
        Assert.Equal(nameof(ISagaConnectorFactory.CreateMessageConnector), Assert.Single(mapping.InterfaceMethods).Name);
    }

    private static void AssertReferenceConstraints(Type parameter, params Type[] expectedTypeConstraints)
    {
        GenericParameterAttributes special =
            parameter.GenericParameterAttributes & GenericParameterAttributes.SpecialConstraintMask;
        Assert.Equal(GenericParameterAttributes.ReferenceTypeConstraint, special);

        Type[] actual = parameter.GetGenericParameterConstraints();
        Assert.Equal(expectedTypeConstraints.Length, actual.Length);
        Assert.All(expectedTypeConstraints, constraint => Assert.Contains(constraint, actual));
    }

    private static void AssertMismatchDoesNotReplaceConnector<TSaga>(ISagaConnectorFactory factory)
        where TSaga : class, ISaga
    {
        object cached = ReadField<object>(factory, "_connector");

        ArgumentException exception = Assert.Throws<ArgumentException>(() =>
            factory.CreateMessageConnector<OtherSaga>());

        Assert.Equal(MismatchMessage, exception.Message);
        Assert.Null(exception.ParamName);
        Assert.Same(cached, ReadField<object>(factory, "_connector"));
        Assert.Same(cached, factory.CreateMessageConnector<TSaga>());
    }

    private static void AssertCachedConnector<TSaga, TMessage>(ISagaConnectorFactory factory, Type expectedConnectorType)
        where TSaga : class, ISaga
        where TMessage : class
    {
        ISagaMessageConnector<TSaga> first = factory.CreateMessageConnector<TSaga>();
        ISagaMessageConnector<TSaga> second = factory.CreateMessageConnector<TSaga>();

        Assert.Same(first, second);
        Assert.Same(ReadField<object>(factory, "_connector"), first);
        Assert.Equal(expectedConnectorType, first.GetType());
        Assert.Equal(typeof(TMessage), first.MessageType);

        ISagaMessageSpecification<TSaga> firstSpecification = first.CreateSagaMessageSpecification();
        ISagaMessageSpecification<TSaga> secondSpecification = first.CreateSagaMessageSpecification();
        Assert.NotSame(firstSpecification, secondSpecification);
        Assert.Equal(typeof(TMessage), firstSpecification.MessageType);
        Assert.IsAssignableFrom<ISagaMessageSpecification<TSaga, TMessage>>(firstSpecification);
    }

    private static ISagaPolicy<TSaga, TMessage> GetPolicy<TSaga, TMessage>(ISagaConnectorFactory factory)
        where TSaga : class, ISaga
        where TMessage : class =>
        Assert.IsAssignableFrom<ISagaPolicy<TSaga, TMessage>>(
            ReadField<object>(factory.CreateMessageConnector<TSaga>(), "_policy"));

    private static void AssertMissingPreconditions<TSaga, TMessage>(
        ISagaPolicy<TSaga, TMessage> policy,
        ConsumeContext<TMessage> context)
        where TSaga : class, ISaga
        where TMessage : class
    {
        ArgumentNullException missingContext = Assert.Throws<ArgumentNullException>(() =>
        {
            _ = policy.MissingAsync(null!, null!);
        });
        ArgumentNullException missingNext = Assert.Throws<ArgumentNullException>(() =>
        {
            _ = policy.MissingAsync(context, null!);
        });

        Assert.Equal("context", missingContext.ParamName);
        Assert.Equal("next", missingNext.ParamName);
    }

    private static T ReadField<T>(object instance, string fieldName)
    {
        for (Type? type = instance.GetType(); type is not null; type = type.BaseType)
        {
            FieldInfo? field = type.GetField(
                fieldName, BindingFlags.NonPublic | BindingFlags.Instance | BindingFlags.DeclaredOnly);
            if (field is not null)
                return Assert.IsAssignableFrom<T>(field.GetValue(instance));
        }

        throw new InvalidOperationException(
            $"Field '{fieldName}' was not found on {TypeCache.GetShortName(instance.GetType())}.");
    }

    private static string ShortName<T>() => TypeCache.GetShortName(typeof(T));

    private static ConsumeContext<TMessage> CreateContext<TMessage>(TMessage message, Guid? correlationId = null)
        where TMessage : class =>
        InMemoryOutboxTestContextFactory.Create(
            message,
            TestContext.Current.CancellationToken,
            correlationId: correlationId);

    private sealed class RecordingPipe<TSaga, TMessage> : IPipe<SagaConsumeContext<TSaga, TMessage>>
        where TSaga : class, ISaga
        where TMessage : class
    {
        private int _count;

        public SagaConsumeContext<TSaga, TMessage>? Context { get; private set; }
        public int Count => Volatile.Read(ref _count);
        public Task? Result { get; init; } = Task.CompletedTask;

        public Task SendAsync(SagaConsumeContext<TSaga, TMessage> context)
        {
            Context = context;
            Interlocked.Increment(ref _count);
            return Result!;
        }

        public void Probe(ProbeContext context)
        {
        }
    }

    public sealed record CorrelatedMessage(Guid CorrelationId) : ICorrelatedBy<Guid>;

    public sealed record ObservedMessage(string Key);

    public sealed class InitiatedSaga : ISaga, IInitiatedBy<CorrelatedMessage>
    {
        public InitiatedSaga(Guid correlationId) => CorrelationId = correlationId;

        public Guid CorrelationId { get; set; }

        public Task ConsumeAsync(ConsumeContext<CorrelatedMessage> context) => Task.CompletedTask;
    }

    public sealed class CombinedSaga : ISaga, IInitiatedByOrOrchestrates<CorrelatedMessage>
    {
        public CombinedSaga(Guid correlationId) => CorrelationId = correlationId;

        public Guid CorrelationId { get; set; }

        public Task ConsumeAsync(ConsumeContext<CorrelatedMessage> context) => Task.CompletedTask;
    }

    public sealed class OrchestratesSaga : ISaga, IOrchestrates<CorrelatedMessage>
    {
        public OrchestratesSaga(Guid correlationId) => CorrelationId = correlationId;

        public Guid CorrelationId { get; set; }

        public Task ConsumeAsync(ConsumeContext<CorrelatedMessage> context) => Task.CompletedTask;
    }

    public sealed class ObservesSaga : ISaga, IObserves<ObservedMessage, ObservesSaga>
    {
        private static int _constructorCount;
        private static int _expressionCount;

        public ObservesSaga(Guid correlationId)
        {
            Interlocked.Increment(ref _constructorCount);
            CorrelationId = correlationId;
            FactoryCorrelationId = correlationId;
        }

        public static int ConstructorCount => Volatile.Read(ref _constructorCount);
        public static int ExpressionCount => Volatile.Read(ref _expressionCount);
        public static Guid FactoryCorrelationId { get; private set; }
        public static Expression<Func<ObservesSaga, ObservedMessage, bool>>? LastExpression { get; private set; }
        public Guid CorrelationId { get; set; }
        public string Key { get; set; } = string.Empty;

        public Expression<Func<ObservesSaga, ObservedMessage, bool>> CorrelationExpression
        {
            get
            {
                Interlocked.Increment(ref _expressionCount);
                Expression<Func<ObservesSaga, ObservedMessage, bool>> expression =
                    (saga, message) => saga.Key == message.Key;
                LastExpression = expression;
                return expression;
            }
        }

        public Task ConsumeAsync(ConsumeContext<ObservedMessage> context) => Task.CompletedTask;

        public static void Reset()
        {
            Volatile.Write(ref _constructorCount, 0);
            Volatile.Write(ref _expressionCount, 0);
            FactoryCorrelationId = Guid.Empty;
            LastExpression = null;
        }
    }

    public sealed class NullExpressionObservesSaga : ISaga, IObserves<ObservedMessage, NullExpressionObservesSaga>
    {
        private static int _constructorCount;
        private static int _expressionCount;

        public NullExpressionObservesSaga(Guid correlationId)
        {
            Interlocked.Increment(ref _constructorCount);
            CorrelationId = correlationId;
        }

        public static int ConstructorCount => Volatile.Read(ref _constructorCount);
        public static int ExpressionCount => Volatile.Read(ref _expressionCount);
        public Guid CorrelationId { get; set; }

        public Expression<Func<NullExpressionObservesSaga, ObservedMessage, bool>> CorrelationExpression
        {
            get
            {
                Interlocked.Increment(ref _expressionCount);
                return null!;
            }
        }

        public Task ConsumeAsync(ConsumeContext<ObservedMessage> context) => Task.CompletedTask;

        public static void Reset()
        {
            Volatile.Write(ref _constructorCount, 0);
            Volatile.Write(ref _expressionCount, 0);
        }
    }

    public sealed class OtherSaga : ISaga
    {
        public Guid CorrelationId { get; set; }
    }
}

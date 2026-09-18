using System.Linq.Expressions;
using System.Reflection;
using ViciOne.ServiceBus.Saga;
using ViciOne.ServiceBus.SagaStateMachine;
using ViciOne.ServiceBus.Tests.Infrastructure.Requirements;
using ViciOne.ServiceBus.Tests.InternalAccess.InMemoryOutbox;
using Xunit;

namespace ViciOne.ServiceBus.Tests.SagaStateMachine;

public sealed class EventCorrelationExpressionQueryDeepContractTests
{
    private const BindingFlags DeclaredMembers =
        BindingFlags.Public | BindingFlags.NonPublic | BindingFlags.Instance | BindingFlags.Static | BindingFlags.DeclaredOnly;

    private static readonly NullabilityInfoContext Nullability = new();

    [Fact]
    [RequirementCoverage("REQ-VSB-STATE-MACHINE-CORRELATION", "iteration-214-expression-query-public-surface")]
    public void PublicSurface_PreservesExactConverterAndQueryFactoryContracts()
    {
        Type converter = typeof(EventCorrelationExpressionConverter<CorrelationState, CorrelationMessage>);
        Assert.True(converter.IsPublic);
        Assert.False(converter.IsAbstract);
        Assert.False(converter.IsSealed);
        Assert.Equal(typeof(ExpressionVisitor), converter.BaseType);
        AssertDirectInterfaces(converter);
        Type[] converterArguments = typeof(EventCorrelationExpressionConverter<,>).GetGenericArguments();
        AssertGenericParameter(
            converterArguments[0],
            "TInstance",
            GenericParameterAttributes.ReferenceTypeConstraint,
            typeof(ISagaStateMachineInstance));
        AssertGenericParameter(
            converterArguments[1],
            "TMessage",
            GenericParameterAttributes.ReferenceTypeConstraint);

        ConstructorInfo converterConstructor = Assert.Single(converter.GetConstructors());
        AssertParameters(converterConstructor, ("context", typeof(ConsumeContext<CorrelationMessage>)));
        MethodInfo convert = Assert.Single(converter.GetMethods(DeclaredMembers), method => method.Name == "Convert");
        Assert.True(convert.IsPublic);
        Assert.False(convert.IsStatic);
        Assert.False(convert.IsVirtual);
        Assert.False(convert.IsFinal);
        Assert.Same(convert, convert.GetBaseDefinition());
        Assert.Equal(typeof(Expression<Func<CorrelationState, bool>>), convert.ReturnType);
        Assert.Equal(NullabilityState.NotNull, Nullability.Create(convert.ReturnParameter).ReadState);
        AssertParameters(
            convert,
            ("expression", typeof(Expression<Func<CorrelationState, ConsumeContext<CorrelationMessage>, bool>>)));
        MethodInfo visitMember = Assert.Single(converter.GetMethods(DeclaredMembers), method => method.Name == "VisitMember");
        Assert.True(visitMember.IsFamily);
        Assert.True(visitMember.IsVirtual);
        Assert.False(visitMember.IsFinal);
        Assert.Equal(typeof(ExpressionVisitor), visitMember.GetBaseDefinition().DeclaringType);
        Assert.Equal(typeof(Expression), visitMember.ReturnType);
        Assert.Equal(NullabilityState.NotNull, Nullability.Create(visitMember.ReturnParameter).ReadState);
        AssertParameters(visitMember, ("m", typeof(MemberExpression)));
        AssertDeclaredPublicOrProtectedSurface(
            converter,
            [converterConstructor],
            [convert, visitMember]);

        Type factory = typeof(ExpressionCorrelationSagaQueryFactory<CorrelationState, CorrelationMessage>);
        Assert.True(factory.IsPublic);
        Assert.False(factory.IsAbstract);
        Assert.False(factory.IsSealed);
        Assert.Equal(typeof(object), factory.BaseType);
        AssertDirectInterfaces(factory, typeof(ISagaQueryFactory<CorrelationState, CorrelationMessage>));
        Type[] factoryArguments = typeof(ExpressionCorrelationSagaQueryFactory<,>).GetGenericArguments();
        AssertGenericParameter(
            factoryArguments[0],
            "TInstance",
            GenericParameterAttributes.ReferenceTypeConstraint,
            typeof(ISagaStateMachineInstance));
        AssertGenericParameter(
            factoryArguments[1],
            "TData",
            GenericParameterAttributes.ReferenceTypeConstraint);

        ConstructorInfo factoryConstructor = Assert.Single(factory.GetConstructors());
        AssertParameters(
            factoryConstructor,
            ("correlationExpression", typeof(Expression<Func<CorrelationState, ConsumeContext<CorrelationMessage>, bool>>)));
        MethodInfo tryCreateQuery = Assert.Single(factory.GetMethods(DeclaredMembers), method => method.Name == "TryCreateQuery");
        Assert.True(tryCreateQuery.IsPublic);
        Assert.True(tryCreateQuery.IsVirtual);
        Assert.True(tryCreateQuery.IsFinal);
        Assert.Same(factory, tryCreateQuery.GetBaseDefinition().DeclaringType);
        Assert.Equal(typeof(bool), tryCreateQuery.ReturnType);
        ParameterInfo[] queryParameters = tryCreateQuery.GetParameters();
        Assert.Equal(2, queryParameters.Length);
        AssertParameter(queryParameters[0], "context", typeof(ConsumeContext<CorrelationMessage>), NullabilityState.NotNull);
        Assert.Equal("query", queryParameters[1].Name);
        Assert.True(queryParameters[1].IsOut);
        Assert.Equal(typeof(ISagaQuery<CorrelationState>).MakeByRefType(), queryParameters[1].ParameterType);
        Assert.Equal(NullabilityState.NotNull, Nullability.Create(queryParameters[1]).ReadState);
        Assert.Equal(NullabilityState.NotNull, Nullability.Create(queryParameters[1]).WriteState);
        MethodInfo probe = Assert.Single(factory.GetMethods(DeclaredMembers), method => method.Name == "Probe");
        Assert.True(probe.IsPublic);
        Assert.True(probe.IsVirtual);
        Assert.True(probe.IsFinal);
        Assert.Same(factory, probe.GetBaseDefinition().DeclaringType);
        Assert.Equal(typeof(void), probe.ReturnType);
        AssertParameters(probe, ("context", typeof(ProbeContext)));
        AssertDeclaredPublicOrProtectedSurface(
            factory,
            [factoryConstructor],
            [tryCreateQuery, probe]);
    }

    [Fact]
    [RequirementCoverage("REQ-VSB-STATE-MACHINE-CORRELATION", "iteration-214-expression-query-required-null-boundaries")]
    public void RequiredInputs_AreRejectedAtTheirExactOwningBoundaries()
    {
        ConsumeContext<CorrelationMessage> context = CreateContext(
            new CorrelationMessage(NewId.NextGuid(), 5),
            NewId.NextGuid());
        Expression<Func<CorrelationState, ConsumeContext<CorrelationMessage>, bool>> expression =
            (state, consumeContext) => state.BusinessId == consumeContext.Message.BusinessId;

        AssertParameter("context", () =>
            new EventCorrelationExpressionConverter<CorrelationState, CorrelationMessage>(null!));

        var converter = new EventCorrelationExpressionConverter<CorrelationState, CorrelationMessage>(context);
        AssertParameter("expression", () => converter.Convert(null!));

        AssertParameter("correlationExpression", () =>
            new ExpressionCorrelationSagaQueryFactory<CorrelationState, CorrelationMessage>(null!));

        var factory = new ExpressionCorrelationSagaQueryFactory<CorrelationState, CorrelationMessage>(expression);
        AssertParameter("context", () => factory.TryCreateQuery(null!, out _));
        AssertParameter("context", () => factory.Probe(null!));
    }

    [Fact]
    [RequirementCoverage("REQ-VSB-STATE-MACHINE-CORRELATION", "iteration-214-expression-converter-context-binding")]
    public void Converter_BindsOnlyItsContextParameterAndSnapshotsContextRootedMemberValues()
    {
        Guid originalBusinessId = NewId.NextGuid();
        Guid changedBusinessId = NewId.NextGuid();
        Guid envelopeId = NewId.NextGuid();
        var message = new CorrelationMessage(originalBusinessId, 5);
        ConsumeContext<CorrelationMessage> context = CreateContext(message, envelopeId);
        var capturedOffset = 3;
        Expression<Func<CorrelationState, ConsumeContext<CorrelationMessage>, bool>> source =
            (state, consumeContext) =>
                state.BusinessId == consumeContext.Message.BusinessId
                && state.Sequence == consumeContext.Message.Sequence
                && state.EnvelopeId == consumeContext.CorrelationId
                && state.Offset == StaticOffset + capturedOffset;
        var converter = new EventCorrelationExpressionConverter<CorrelationState, CorrelationMessage>(context);

        Expression<Func<CorrelationState, bool>> converted = converter.Convert(source);
        Func<CorrelationState, bool> predicate = converted.Compile();
        message.BusinessId = changedBusinessId;
        message.Sequence = 17;
        capturedOffset = 9;

        Assert.Single(converted.Parameters);
        Assert.Same(source.Parameters[0], converted.Parameters[0]);
        Assert.Equal(2, source.Parameters.Count);
        Assert.DoesNotContain(source.Parameters[1], CollectParameters(converted.Body));
        Assert.True(predicate(new CorrelationState
        {
            BusinessId = originalBusinessId,
            Sequence = 5,
            EnvelopeId = envelopeId,
            Offset = StaticOffset + capturedOffset,
        }));
        Assert.False(predicate(new CorrelationState
        {
            BusinessId = changedBusinessId,
            Sequence = 17,
            EnvelopeId = envelopeId,
            Offset = StaticOffset + capturedOffset,
        }));

        message.Sequence = null;
        Expression<Func<CorrelationState, ConsumeContext<CorrelationMessage>, bool>> nullableSource =
            (state, consumeContext) => state.Sequence == consumeContext.Message.Sequence;
        Expression<Func<CorrelationState, bool>> nullableConverted = converter.Convert(nullableSource);
        message.Sequence = 23;
        Assert.True(nullableConverted.Compile()(new CorrelationState { Sequence = null }));
        Assert.False(nullableConverted.Compile()(new CorrelationState { Sequence = 23 }));

        ParameterExpression stateParameter = Expression.Parameter(typeof(CorrelationState), "state");
        ParameterExpression outerContext = Expression.Parameter(typeof(ConsumeContext<CorrelationMessage>), "outerContext");
        ParameterExpression nestedContext = Expression.Parameter(typeof(ConsumeContext<CorrelationMessage>), "nestedContext");
        MemberExpression outerBusinessId = Expression.Property(
            Expression.Property(outerContext, nameof(ConsumeContext<CorrelationMessage>.Message)),
            nameof(CorrelationMessage.BusinessId));
        MemberExpression nestedBusinessId = Expression.Property(
            Expression.Property(nestedContext, nameof(ConsumeContext<CorrelationMessage>.Message)),
            nameof(CorrelationMessage.BusinessId));
        Expression<Func<ConsumeContext<CorrelationMessage>, Guid>> nestedLambda =
            Expression.Lambda<Func<ConsumeContext<CorrelationMessage>, Guid>>(nestedBusinessId, nestedContext);
        BinaryExpression nestedLambdaIsPresent = Expression.NotEqual(
            Expression.Quote(nestedLambda),
            Expression.Constant(null, typeof(Expression<Func<ConsumeContext<CorrelationMessage>, Guid>>)));
        var distinctParameterSource = Expression.Lambda<Func<CorrelationState, ConsumeContext<CorrelationMessage>, bool>>(
            Expression.AndAlso(
                Expression.Equal(Expression.Property(stateParameter, nameof(CorrelationState.BusinessId)), outerBusinessId),
                nestedLambdaIsPresent),
            stateParameter,
            outerContext);

        message.BusinessId = originalBusinessId;
        Expression<Func<CorrelationState, bool>> distinctParameterConverted = converter.Convert(distinctParameterSource);
        LambdaExpression preservedNestedLambda = Assert.IsAssignableFrom<LambdaExpression>(
            Assert.IsType<UnaryExpression>(
                Assert.IsAssignableFrom<BinaryExpression>(
                    Assert.IsAssignableFrom<BinaryExpression>(distinctParameterConverted.Body).Right).Left).Operand);
        Assert.Contains(nestedContext, CollectParameters(preservedNestedLambda.Body));
        Assert.DoesNotContain(outerContext, CollectParameters(distinctParameterConverted.Body));
        Assert.True(distinctParameterConverted.Compile()(new CorrelationState { BusinessId = originalBusinessId }));
    }

    [Fact]
    [RequirementCoverage("REQ-VSB-STATE-MACHINE-CORRELATION", "iteration-214-expression-query-factory-behavior-and-probe")]
    public void QueryFactory_CreatesIndependentQueriesAndReportsItsExactExpression()
    {
        Guid firstId = NewId.NextGuid();
        Guid secondId = NewId.NextGuid();
        Expression<Func<CorrelationState, ConsumeContext<CorrelationMessage>, bool>> expression =
            (state, context) => state.BusinessId == context.Message.BusinessId;
        var factory = new ExpressionCorrelationSagaQueryFactory<CorrelationState, CorrelationMessage>(expression);
        ConsumeContext<CorrelationMessage> firstContext = CreateContext(
            new CorrelationMessage(firstId, 1),
            NewId.NextGuid());
        ConsumeContext<CorrelationMessage> secondContext = CreateContext(
            new CorrelationMessage(secondId, 2),
            NewId.NextGuid());

        bool firstCreated = factory.TryCreateQuery(firstContext, out ISagaQuery<CorrelationState> firstQuery);
        bool secondCreated = factory.TryCreateQuery(secondContext, out ISagaQuery<CorrelationState> secondQuery);

        Assert.True(firstCreated);
        Assert.True(secondCreated);
        var first = Assert.IsType<SagaQuery<CorrelationState>>(firstQuery);
        var second = Assert.IsType<SagaQuery<CorrelationState>>(secondQuery);
        Assert.NotSame(first, second);
        Assert.Single(first.FilterExpression.Parameters);
        Assert.Single(second.FilterExpression.Parameters);
        Assert.True(first.GetFilter()(new CorrelationState { BusinessId = firstId }));
        Assert.False(first.GetFilter()(new CorrelationState { BusinessId = secondId }));
        Assert.True(second.GetFilter()(new CorrelationState { BusinessId = secondId }));
        Assert.False(second.GetFilter()(new CorrelationState { BusinessId = firstId }));

        var probe = new RecordingProbeContext();
        factory.Probe(probe);

        Assert.Equal(1, probe.StringAddCalls);
        Assert.Equal("expression", probe.Key);
        Assert.Equal(expression.ToString(), probe.Value);
    }

    [Fact]
    [RequirementCoverage("REQ-VSB-STATE-MACHINE-CORRELATION", "iteration-214-expression-query-failure-identity")]
    public void ConversionAndProbeFailures_PreserveTheExactOriginatingExceptionIdentity()
    {
        var expectedEvaluationFailure = new SyntheticExpressionException();
        ConsumeContext<ThrowingMessage> context = CreateContext(
            new ThrowingMessage(expectedEvaluationFailure),
            NewId.NextGuid());
        Expression<Func<ThrowingState, ConsumeContext<ThrowingMessage>, bool>> expression =
            (state, consumeContext) => state.Value == consumeContext.Message.Value;
        var converter = new EventCorrelationExpressionConverter<ThrowingState, ThrowingMessage>(context);
        var factory = new ExpressionCorrelationSagaQueryFactory<ThrowingState, ThrowingMessage>(expression);

        SyntheticExpressionException converterFailure = Assert.Throws<SyntheticExpressionException>(() =>
            converter.Convert(expression));
        SyntheticExpressionException factoryFailure = Assert.Throws<SyntheticExpressionException>(() =>
            factory.TryCreateQuery(context, out _));

        Assert.Same(expectedEvaluationFailure, converterFailure);
        Assert.Same(expectedEvaluationFailure, factoryFailure);

        var nonLambdaConverter = new NonLambdaConverter(context);
        InvalidOperationException conversionFailure = Assert.Throws<InvalidOperationException>(() =>
            nonLambdaConverter.Convert(expression));
        Assert.Equal("The correlation expression could not be converted to a lambda expression.", conversionFailure.Message);

        var expectedProbeFailure = new SyntheticProbeException();
        var failingProbe = new RecordingProbeContext(expectedProbeFailure);
        SyntheticProbeException probeFailure = Assert.Throws<SyntheticProbeException>(() => factory.Probe(failingProbe));
        Assert.Same(expectedProbeFailure, probeFailure);
        Assert.Equal(1, failingProbe.StringAddCalls);
    }

    [Fact]
    [RequirementCoverage("REQ-VSB-STATE-MACHINE-CORRELATION", "iteration-214-expression-converter-concurrent-parameter-isolation")]
    public async Task Convert_ConcurrentDistinctExpressionsRemainBoundToTheirOwnParametersAsync()
    {
        Guid businessId = NewId.NextGuid();
        var message = new CorrelationMessage(businessId, 11);
        ConsumeContext<CorrelationMessage> context = CreateContext(message, NewId.NextGuid());
        var converter = new EventCorrelationExpressionConverter<CorrelationState, CorrelationMessage>(context);
        Expression<Func<CorrelationState, ConsumeContext<CorrelationMessage>, bool>> businessExpression =
            (businessState, businessContext) => businessState.BusinessId == businessContext.Message.BusinessId;
        Expression<Func<CorrelationState, ConsumeContext<CorrelationMessage>, bool>> sequenceExpression =
            (sequenceState, sequenceContext) => sequenceState.Sequence == sequenceContext.Message.Sequence;
        Assert.NotSame(businessExpression.Parameters[0], sequenceExpression.Parameters[0]);
        Assert.NotSame(businessExpression.Parameters[1], sequenceExpression.Parameters[1]);
        using var start = new Barrier(2);
        CancellationToken cancellationToken = TestContext.Current.CancellationToken;

        Task<Expression<Func<CorrelationState, bool>>> businessConversion = Task.Run(() =>
        {
            start.SignalAndWait(cancellationToken);
            return converter.Convert(businessExpression);
        }, cancellationToken);
        Task<Expression<Func<CorrelationState, bool>>> sequenceConversion = Task.Run(() =>
        {
            start.SignalAndWait(cancellationToken);
            return converter.Convert(sequenceExpression);
        }, cancellationToken);

        Expression<Func<CorrelationState, bool>>[] converted = await Task.WhenAll(
            businessConversion,
            sequenceConversion);

        Assert.Same(businessExpression.Parameters[0], converted[0].Parameters[0]);
        Assert.Same(sequenceExpression.Parameters[0], converted[1].Parameters[0]);
        Assert.DoesNotContain(businessExpression.Parameters[1], CollectParameters(converted[0].Body));
        Assert.DoesNotContain(sequenceExpression.Parameters[1], CollectParameters(converted[1].Body));
        Assert.True(converted[0].Compile()(new CorrelationState { BusinessId = businessId }));
        Assert.False(converted[0].Compile()(new CorrelationState { BusinessId = NewId.NextGuid() }));
        Assert.True(converted[1].Compile()(new CorrelationState { Sequence = 11 }));
        Assert.False(converted[1].Compile()(new CorrelationState { Sequence = 12 }));

        var nestedFailure = new SyntheticExpressionException();
        Expression<Func<CorrelationState, ConsumeContext<CorrelationMessage>, bool>> nestedExpression =
            (nestedState, nestedContext) => nestedState.Sequence == nestedContext.Message.Sequence;
        var reentrantConverter = new ReentrantFailureConverter(context, nestedExpression, nestedFailure);
        Expression<Func<CorrelationState, ConsumeContext<CorrelationMessage>, bool>> outerExpression =
            (outerState, outerContext) => outerState.BusinessId == outerContext.Message.BusinessId;
        Expression<Func<CorrelationState, bool>> reentrantResult = reentrantConverter.Convert(outerExpression);
        Assert.Same(nestedFailure, reentrantConverter.ObservedFailure);
        Assert.True(reentrantResult.Compile()(new CorrelationState { BusinessId = businessId }));

        var serializedConverter = new SerializedVisitConverter(context);
        Task<Expression<Func<CorrelationState, bool>>> firstSerialized = Task.Factory.StartNew(
            () => serializedConverter.Convert(businessExpression),
            cancellationToken,
            TaskCreationOptions.LongRunning,
            TaskScheduler.Default);
        try
        {
            Assert.True(serializedConverter.FirstVisitEntered.Wait(TimeSpan.FromSeconds(5), cancellationToken));
            FieldInfo conversionLockField = Assert.IsAssignableFrom<FieldInfo>(
                typeof(EventCorrelationExpressionConverter<CorrelationState, CorrelationMessage>)
                    .GetField("_conversionLock", BindingFlags.Instance | BindingFlags.NonPublic));
            object conversionLock = Assert.IsType<object>(conversionLockField.GetValue(serializedConverter));
            bool unexpectedlyEnteredLock = Monitor.TryEnter(conversionLock);
            if (unexpectedlyEnteredLock)
                Monitor.Exit(conversionLock);
            Assert.False(unexpectedlyEnteredLock);
        }
        finally
        {
            serializedConverter.ReleaseFirstVisit.Set();
        }

        Task<Expression<Func<CorrelationState, bool>>> secondSerialized = Task.Factory.StartNew(
            () => serializedConverter.Convert(sequenceExpression),
            cancellationToken,
            TaskCreationOptions.LongRunning,
            TaskScheduler.Default);
        await Task.WhenAll(firstSerialized, secondSerialized);
        Assert.True(serializedConverter.SecondVisitEntered.IsSet);
    }

    private static int StaticOffset => 4;

    private static ConsumeContext<T> CreateContext<T>(T message, Guid correlationId)
        where T : class =>
        InMemoryOutboxTestContextFactory.Create(
            message,
            TestContext.Current.CancellationToken,
            correlationId: correlationId);

    private static IReadOnlyList<ParameterExpression> CollectParameters(Expression expression)
    {
        var collector = new ParameterCollector();
        collector.Visit(expression);
        return collector.Parameters;
    }

    private static void AssertDeclaredPublicOrProtectedSurface(
        Type type,
        IReadOnlyCollection<ConstructorInfo> expectedConstructors,
        IReadOnlyCollection<MethodInfo> expectedMethods)
    {
        Assert.Equal(
            expectedConstructors.OrderBy(member => member.MetadataToken),
            type.GetConstructors(DeclaredMembers).Where(IsPublicOrProtected).OrderBy(member => member.MetadataToken));
        Assert.Equal(
            expectedMethods.OrderBy(member => member.MetadataToken),
            type.GetMethods(DeclaredMembers).Where(IsPublicOrProtected).OrderBy(member => member.MetadataToken));
        Assert.DoesNotContain(type.GetFields(DeclaredMembers), IsPublicOrProtected);
        Assert.DoesNotContain(type.GetProperties(DeclaredMembers), property =>
            property.GetAccessors(true).Any(IsPublicOrProtected));
        Assert.DoesNotContain(type.GetEvents(DeclaredMembers), @event =>
            IsPublicOrProtected(@event.AddMethod)
            || IsPublicOrProtected(@event.RemoveMethod)
            || IsPublicOrProtected(@event.RaiseMethod));
        Assert.DoesNotContain(type.GetNestedTypes(BindingFlags.Public | BindingFlags.NonPublic), nested =>
            nested.IsNestedPublic
            || nested.IsNestedFamily
            || nested.IsNestedFamORAssem
            || nested.IsNestedFamANDAssem);
    }

    private static void AssertParameters(MethodBase member, params (string Name, Type Type)[] expected)
    {
        ParameterInfo[] parameters = member.GetParameters();
        Assert.Equal(expected.Length, parameters.Length);
        for (var index = 0; index < expected.Length; index++)
            AssertParameter(parameters[index], expected[index].Name, expected[index].Type, NullabilityState.NotNull);
    }

    private static void AssertParameter(
        ParameterInfo parameter,
        string expectedName,
        Type expectedType,
        NullabilityState expectedNullability)
    {
        Assert.Equal(expectedName, parameter.Name);
        Assert.Equal(expectedType, parameter.ParameterType);
        Assert.Equal(expectedNullability, Nullability.Create(parameter).ReadState);
    }

    private static void AssertDirectInterfaces(Type type, params Type[] expected)
    {
        Type[] all = type.GetInterfaces();
        Type[] direct = all.Where(candidate => !all.Any(
            other => other != candidate && other.GetInterfaces().Contains(candidate))).ToArray();
        Assert.Equal(
            expected.OrderBy(TypeIdentity, StringComparer.Ordinal),
            direct.OrderBy(TypeIdentity, StringComparer.Ordinal));
    }

    private static void AssertGenericParameter(
        Type parameter,
        string name,
        GenericParameterAttributes attributes,
        params Type[] constraints)
    {
        Assert.True(parameter.IsGenericParameter);
        Assert.Equal(name, parameter.Name);
        Assert.Equal(attributes, parameter.GenericParameterAttributes & GenericParameterAttributes.SpecialConstraintMask);
        Assert.Equal(
            constraints.OrderBy(TypeIdentity, StringComparer.Ordinal),
            parameter.GetGenericParameterConstraints().OrderBy(TypeIdentity, StringComparer.Ordinal));
    }

    private static bool IsPublicOrProtected(MethodBase? member) =>
        member is not null
        && (member.IsPublic || member.IsFamily || member.IsFamilyOrAssembly || member.IsFamilyAndAssembly);

    private static bool IsPublicOrProtected(FieldInfo member) =>
        member.IsPublic || member.IsFamily || member.IsFamilyOrAssembly || member.IsFamilyAndAssembly;

    private static string TypeIdentity(Type type) => type.AssemblyQualifiedName ?? type.FullName ?? type.Name;

    private static void AssertParameter(string name, Action action) =>
        Assert.Equal(name, Assert.Throws<ArgumentNullException>(action).ParamName);

    private sealed class ParameterCollector : ExpressionVisitor
    {
        public List<ParameterExpression> Parameters { get; } = [];

        protected override Expression VisitParameter(ParameterExpression node)
        {
            Parameters.Add(node);
            return node;
        }
    }

    private sealed class RecordingProbeContext(Exception? failure = null) : ProbeContext
    {
        public int StringAddCalls { get; private set; }
        public string? Key { get; private set; }
        public string? Value { get; private set; }
        public CancellationToken CancellationToken => default;

        public void Add(string key, string? value)
        {
            StringAddCalls++;
            if (failure != null)
                throw failure;

            Key = key;
            Value = value;
        }

        public void Add(string key, object? value) => throw new InvalidOperationException("Unexpected object probe value.");
        public void Set(object values) => throw new InvalidOperationException("Unexpected probe values.");
        public void Set(IEnumerable<KeyValuePair<string, object?>> values) =>
            throw new InvalidOperationException("Unexpected probe values.");
        public ProbeContext CreateScope(string key) => throw new InvalidOperationException("Unexpected probe scope.");
    }

    private sealed class NonLambdaConverter(ConsumeContext<ThrowingMessage> context) :
        EventCorrelationExpressionConverter<ThrowingState, ThrowingMessage>(context)
    {
        protected override Expression VisitLambda<T>(Expression<T> node) => Expression.Constant(true);
    }

    private sealed class ReentrantFailureConverter(
        ConsumeContext<CorrelationMessage> context,
        Expression<Func<CorrelationState, ConsumeContext<CorrelationMessage>, bool>> nestedExpression,
        Exception failure) : EventCorrelationExpressionConverter<CorrelationState, CorrelationMessage>(context)
    {
        private bool _nestedAttempted;

        public Exception? ObservedFailure { get; private set; }

        protected override Expression VisitMember(MemberExpression node)
        {
            if (!_nestedAttempted && node.Member.DeclaringType == typeof(CorrelationState))
            {
                _nestedAttempted = true;
                ObservedFailure = Assert.Throws<SyntheticExpressionException>(() => Convert(nestedExpression));
            }

            return base.VisitMember(node);
        }

        protected override Expression VisitLambda<T>(Expression<T> node)
        {
            if (ReferenceEquals(node, nestedExpression))
                throw failure;

            return base.VisitLambda(node);
        }
    }

    private sealed class SerializedVisitConverter(ConsumeContext<CorrelationMessage> context) :
        EventCorrelationExpressionConverter<CorrelationState, CorrelationMessage>(context)
    {
        private readonly AsyncLocal<int> _call = new();
        private int _callCount;
        private int _firstVisitSeen;

        public ManualResetEventSlim FirstVisitEntered { get; } = new();
        public ManualResetEventSlim ReleaseFirstVisit { get; } = new();
        public ManualResetEventSlim SecondVisitEntered { get; } = new();

        public new Expression<Func<CorrelationState, bool>> Convert(
            Expression<Func<CorrelationState, ConsumeContext<CorrelationMessage>, bool>> expression)
        {
            int call = Interlocked.Increment(ref _callCount);
            _call.Value = call;

            try
            {
                return base.Convert(expression);
            }
            finally
            {
                _call.Value = 0;
            }
        }

        protected override Expression VisitMember(MemberExpression node)
        {
            if (_call.Value == 1 && Interlocked.Exchange(ref _firstVisitSeen, 1) == 0)
            {
                FirstVisitEntered.Set();
                ReleaseFirstVisit.Wait(TestContext.Current.CancellationToken);
            }
            else if (_call.Value == 2)
                SecondVisitEntered.Set();

            return base.VisitMember(node);
        }
    }

    public sealed class CorrelationState : ISagaStateMachineInstance
    {
        public Guid CorrelationId { get; set; }
        public Guid BusinessId { get; set; }
        public int? Sequence { get; set; }
        public Guid? EnvelopeId { get; set; }
        public int Offset { get; set; }
    }

    public sealed class CorrelationMessage(Guid businessId, int? sequence)
    {
        public Guid BusinessId { get; set; } = businessId;
        public int? Sequence { get; set; } = sequence;
    }

    private sealed class ThrowingState : ISagaStateMachineInstance
    {
        public Guid CorrelationId { get; set; }
        public int Value { get; set; }
    }

    public sealed class ThrowingMessage(Exception failure)
    {
        public int Value => throw failure;
    }

    private sealed class SyntheticExpressionException : Exception;
    private sealed class SyntheticProbeException : Exception;
}

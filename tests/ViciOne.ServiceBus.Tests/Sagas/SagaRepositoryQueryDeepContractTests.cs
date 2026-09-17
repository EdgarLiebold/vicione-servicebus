using System.Collections;
using System.Linq.Expressions;
using System.Reflection;
using ViciOne.ServiceBus.Advanced;
using ViciOne.ServiceBus.Saga;
using ViciOne.ServiceBus.Tests.Infrastructure.Requirements;
using ViciOne.ServiceBus.Tests.InternalAccess.InMemoryOutbox;
using Xunit;

namespace ViciOne.ServiceBus.Tests.Sagas;

public sealed class SagaRepositoryQueryDeepContractTests
{
    [Fact]
    [RequirementCoverage("REQ-VSB-SAGA-REPOSITORY-QUERY", "query-components-reject-every-missing-required-owner")]
    public void QueryComponents_RejectEveryMissingRequiredOwnerWithExactParameterNames()
    {
        IList<Guid> ids = [];
        ISagaRepositoryContext<QuerySaga, QueryMessage> repository = CreateRepositoryContext(new QueryMessage());
        IQuerySagaRepositoryContext<QuerySaga> queryRepository = CreateQueryRepositoryContext();
        var selector = new RecordingPropertySelector(true, "order-42");
        Expression<Func<QueryState, string>> propertyExpression = instance => instance.OrderNumber;

        AssertParameter("context", () => new DefaultSagaRepositoryQueryContext<QuerySaga, QueryMessage>(null!, ids));
        AssertParameter("results", () => new DefaultSagaRepositoryQueryContext<QuerySaga, QueryMessage>(repository, null!));
        AssertParameter("queryContext", () => new DefaultSagaRepositoryQueryContext<QuerySaga>(null!, ids));
        AssertParameter("results", () => new DefaultSagaRepositoryQueryContext<QuerySaga>(queryRepository, null!));
        AssertParameter("repositoryContext", () => new LoadedSagaRepositoryQueryContext<QuerySaga, QueryMessage>(null!, []));
        AssertParameter("instances", () => new LoadedSagaRepositoryQueryContext<QuerySaga, QueryMessage>(repository, null!));
        AssertParameter("querySagaRepositoryContext", () => new LoadedSagaRepositoryQueryContext<QuerySaga>(null!, []));
        AssertParameter("instances", () => new LoadedSagaRepositoryQueryContext<QuerySaga>(queryRepository, null!));
        AssertParameter("filterExpression", () => new ExpressionSagaQueryFactory<QuerySaga, QueryMessage>(null!));
        AssertParameter("propertyExpression", () => new PropertyExpressionSagaQueryFactory<QueryState, QueryMessage, string>(null!, selector));
        AssertParameter("selector", () => new PropertyExpressionSagaQueryFactory<QueryState, QueryMessage, string>(propertyExpression, null!));
        AssertParameter("message", () => new SagaFilterExpressionConverter<QuerySaga, QueryMessage>(null!));
        AssertParameter("expression", () => new SagaFilterExpressionConverter<QuerySaga, QueryMessage>(new QueryMessage()).Convert(null!));
        AssertParameter("expression", () => new StateExpressionVisitor<QueryState>(null!));
        AssertParameter("expression", () => StateExpressionVisitor<QueryState>.Combine(null!, instance => instance.IsEnabled));
        AssertParameter("stateExpression", () => StateExpressionVisitor<QueryState>.Combine(instance => true, null!));
    }

    [Fact]
    [RequirementCoverage("REQ-VSB-SAGA-REPOSITORY-QUERY", "default-query-contexts-preserve-results-and-forward-every-operation")]
    public async Task DefaultQueryContexts_PreserveResultsAndForwardEveryOperationAsync()
    {
        var message = new QueryMessage { Threshold = 7 };
        var calls = new List<Invocation>();
        SagaConsumeContext<QuerySaga, QueryMessage> sagaContext = CreateProxy<SagaConsumeContext<QuerySaga, QueryMessage>>();
        SagaConsumeContext<QuerySaga> baseSagaContext = CreateProxy<SagaConsumeContext<QuerySaga>>();
        ISagaRepositoryContext<QuerySaga, QueryMessage> repository = CreateRepositoryContext(message, calls, sagaContext);
        Guid first = Guid.NewGuid();
        Guid second = Guid.NewGuid();
        IList<Guid> ids = [first, second];
        var context = new DefaultSagaRepositoryQueryContext<QuerySaga, QueryMessage>(repository, ids);
        var instance = new QuerySaga { CorrelationId = first };
        using var cancellation = new CancellationTokenSource();
        CancellationToken token = cancellation.Token;

        Assert.Equal(2, context.Count);
        Assert.Equal(ids, context.ToArray());
        Assert.Equal(ids, EnumerateUntyped(context).Cast<Guid>().ToArray());
        Assert.Same(sagaContext, await context.AddAsync(instance, token));
        Assert.Same(sagaContext, await context.InsertAsync(instance, token));
        Assert.Same(sagaContext, await context.LoadAsync(first, token));
        await context.SaveAsync(baseSagaContext, token);
        await context.DiscardAsync(baseSagaContext, token);
        await context.UndoAsync(baseSagaContext, token);
        await context.UpdateAsync(baseSagaContext, token);
        await context.DeleteAsync(baseSagaContext, token);
        Assert.Same(sagaContext, await context.CreateSagaConsumeContextAsync(repository, instance, SagaConsumeContextMode.Load));

        Assert.Equal(
            [
                "AddAsync", "InsertAsync", "LoadAsync", "SaveAsync", "DiscardAsync", "UndoAsync",
                "UpdateAsync", "DeleteAsync", "CreateSagaConsumeContextAsync",
            ],
            calls.Select(call => call.Method.Name));
        Assert.All(calls.Take(8), call => Assert.Equal(token, Assert.IsType<CancellationToken>(call.Arguments[^1])));
        Assert.Same(instance, calls[0].Arguments[0]);
        Assert.Equal(first, calls[2].Arguments[0]);
        Assert.Same(repository, calls[8].Arguments[0]);
        Assert.Same(instance, calls[8].Arguments[1]);
        Assert.Equal(SagaConsumeContextMode.Load, calls[8].Arguments[2]);

        var queryCalls = new List<Invocation>();
        ISagaRepositoryQueryContext<QuerySaga> downstream = CreateProxy<ISagaRepositoryQueryContext<QuerySaga>>();
        IQuerySagaRepositoryContext<QuerySaga> queryRepository = CreateQueryRepositoryContext(queryCalls, downstream);
        var queryContext = new DefaultSagaRepositoryQueryContext<QuerySaga>(queryRepository, ids);
        var query = new SagaQuery<QuerySaga>(saga => saga.Score >= 7);

        Assert.Equal(2, queryContext.Count);
        Assert.Equal(ids, queryContext.ToArray());
        Assert.Equal(ids, EnumerateUntyped(queryContext).Cast<Guid>().ToArray());
        Assert.Same(downstream, await queryContext.QueryAsync(query, token));
        Invocation queryCall = Assert.Single(queryCalls);
        Assert.Same(query, queryCall.Arguments[0]);
        Assert.Equal(token, queryCall.Arguments[1]);
    }

    [Fact]
    [RequirementCoverage("REQ-VSB-SAGA-REPOSITORY-QUERY", "loaded-query-context-cache-miss-cancellation-and-forwarding")]
    public async Task LoadedQueryContexts_DistinguishCacheMissHonorCancellationAndForwardOperationsAsync()
    {
        var calls = new List<Invocation>();
        SagaConsumeContext<QuerySaga, QueryMessage> cachedContext = CreateProxy<SagaConsumeContext<QuerySaga, QueryMessage>>();
        SagaConsumeContext<QuerySaga, QueryMessage> loadedContext = CreateProxy<SagaConsumeContext<QuerySaga, QueryMessage>>();
        SagaConsumeContext<QuerySaga> baseSagaContext = CreateProxy<SagaConsumeContext<QuerySaga>>();
        var message = new QueryMessage();
        ISagaRepositoryContext<QuerySaga, QueryMessage> repository = CreateRepositoryContext(
            message,
            calls,
            cachedContext,
            loadedContext);
        var cached = new QuerySaga { CorrelationId = Guid.NewGuid(), Score = 11 };
        var context = new LoadedSagaRepositoryQueryContext<QuerySaga, QueryMessage>(repository, [cached]);
        Guid missingId = Guid.NewGuid();
        using var cancellation = new CancellationTokenSource();
        CancellationToken token = cancellation.Token;

        Assert.Equal(1, context.Count);
        Assert.Equal([cached.CorrelationId], context.ToArray());
        Assert.Equal([cached.CorrelationId], EnumerateUntyped(context).Cast<Guid>().ToArray());
        Assert.Same(cachedContext, await context.LoadAsync(cached.CorrelationId, token));
        Assert.Same(loadedContext, await context.LoadAsync(missingId, token));
        Assert.Same(cachedContext, await context.AddAsync(cached, token));
        Assert.Same(cachedContext, await context.InsertAsync(cached, token));
        await context.SaveAsync(baseSagaContext, token);
        await context.DiscardAsync(baseSagaContext, token);
        await context.UndoAsync(baseSagaContext, token);
        await context.UpdateAsync(baseSagaContext, token);
        await context.DeleteAsync(baseSagaContext, token);
        Assert.Same(cachedContext, await context.CreateSagaConsumeContextAsync(repository, cached, SagaConsumeContextMode.Load));

        Invocation[] createCalls = calls.Where(call => call.Method.Name == "CreateSagaConsumeContextAsync"
            && ReferenceEquals(call.Arguments[0], repository)).ToArray();
        Assert.Equal(2, createCalls.Length);
        Invocation cachedLoad = createCalls[0];
        Assert.Same(cached, cachedLoad.Arguments[1]);
        Assert.Equal(SagaConsumeContextMode.Load, cachedLoad.Arguments[2]);
        Invocation repositoryLoad = Assert.Single(calls, call => call.Method.Name == "LoadAsync");
        Assert.Equal(missingId, repositoryLoad.Arguments[0]);
        Assert.Equal(token, repositoryLoad.Arguments[1]);

        using var canceled = new CancellationTokenSource();
        canceled.Cancel();
        int callsBeforeCancellation = calls.Count;
        OperationCanceledException canceledFailure = await Assert.ThrowsAnyAsync<OperationCanceledException>(() =>
            context.LoadAsync(cached.CorrelationId, canceled.Token));
        Assert.Equal(canceled.Token, canceledFailure.CancellationToken);
        Assert.Equal(callsBeforeCancellation, calls.Count);

        var queryCalls = new List<Invocation>();
        ISagaRepositoryQueryContext<QuerySaga> downstream = CreateProxy<ISagaRepositoryQueryContext<QuerySaga>>();
        IQuerySagaRepositoryContext<QuerySaga> queryRepository = CreateQueryRepositoryContext(queryCalls, downstream);
        var loadedQuery = new LoadedSagaRepositoryQueryContext<QuerySaga>(queryRepository, [cached]);
        var query = new SagaQuery<QuerySaga>(saga => saga.Score == cached.Score);

        Assert.Equal(1, loadedQuery.Count);
        Assert.Equal([cached.CorrelationId], loadedQuery.ToArray());
        Assert.Equal([cached.CorrelationId], EnumerateUntyped(loadedQuery).Cast<Guid>().ToArray());
        Assert.Same(downstream, await loadedQuery.QueryAsync(query, token));
        Invocation queryCall = Assert.Single(queryCalls);
        Assert.Same(query, queryCall.Arguments[0]);
        Assert.Equal(token, queryCall.Arguments[1]);

        ArgumentException duplicate = Assert.Throws<ArgumentException>(() =>
            new LoadedSagaRepositoryQueryContext<QuerySaga>(queryRepository,
                [cached, new QuerySaga { CorrelationId = cached.CorrelationId }]));
        Assert.Contains("same key", duplicate.Message, StringComparison.OrdinalIgnoreCase);
    }

    [Fact]
    [RequirementCoverage("REQ-VSB-SAGA-QUERY-EXPRESSION", "filter-converter-binds-message-by-parameter-identity")]
    public void FilterConverter_BindsWholeMessagesAndKeepsSameTypedSagaParametersIndependent()
    {
        var message = new QueryMessage { Threshold = 10 };
        var converter = new SagaFilterExpressionConverter<QuerySaga, QueryMessage>(message);
        Expression<Func<QuerySaga, bool>> memberFilter = converter.Convert((saga, consumed) =>
            saga.Score >= consumed.Threshold);
        Expression<Func<QuerySaga, bool>> wholeMessageFilter = converter.Convert((saga, consumed) =>
            ReferenceEquals(saga.LastMessage, consumed));

        Assert.False(memberFilter.Compile()(new QuerySaga { Score = 9 }));
        Assert.True(memberFilter.Compile()(new QuerySaga { Score = 10 }));
        Assert.True(wholeMessageFilter.Compile()(new QuerySaga { LastMessage = message }));
        Assert.False(wholeMessageFilter.Compile()(new QuerySaga { LastMessage = new QueryMessage { Threshold = 10 } }));
        Assert.DoesNotContain(memberFilter.Parameters, parameter => parameter.Type == typeof(QueryMessage));

        var sameTypedMessage = new RecursiveSaga { CorrelationId = Guid.NewGuid() };
        var sameTypedConverter = new SagaFilterExpressionConverter<RecursiveSaga, RecursiveSaga>(sameTypedMessage);
        Expression<Func<RecursiveSaga, bool>> sameTypedFilter = sameTypedConverter.Convert((saga, consumed) =>
            saga.CorrelationId != consumed.CorrelationId);
        Func<RecursiveSaga, bool> predicate = sameTypedFilter.Compile();

        Assert.False(predicate(new RecursiveSaga { CorrelationId = sameTypedMessage.CorrelationId }));
        Assert.True(predicate(new RecursiveSaga { CorrelationId = Guid.NewGuid() }));
        Assert.Same(sameTypedFilter.Parameters[0],
            Assert.IsAssignableFrom<MemberExpression>(Assert.IsAssignableFrom<BinaryExpression>(sameTypedFilter.Body).Left).Expression);
    }

    [Fact]
    [RequirementCoverage("REQ-VSB-SAGA-QUERY-EXPRESSION", "query-factories-create-snapshot-filters-and-probe-diagnostics")]
    public void QueryFactories_CreateSnapshotFiltersReportFalseAndProbeDiagnostics()
    {
        var message = new QueryMessage { Threshold = 5 };
        ConsumeContext<QueryMessage> consumeContext = CreateConsumeContext(message);
        ISagaQueryFactory<QuerySaga, QueryMessage> expressionFactory =
            new ExpressionSagaQueryFactory<QuerySaga, QueryMessage>((saga, consumed) => saga.Score == consumed.Threshold);

        Assert.True(expressionFactory.TryCreateQuery(consumeContext, out ISagaQuery<QuerySaga>? expressionQuery));
        Assert.NotNull(expressionQuery);
        Func<QuerySaga, bool> expressionPredicate = expressionQuery.FilterExpression.Compile();
        Assert.True(expressionPredicate(new QuerySaga { Score = 5 }));
        Assert.False(expressionPredicate(new QuerySaga { Score = 4 }));
        AssertParameter("context", () => expressionFactory.TryCreateQuery(null!, out _));
        AssertParameter("context", () => ((IProbeSite)expressionFactory).Probe(null!));
        IReadOnlyList<Invocation> expressionProbe = Probe((IProbeSite)expressionFactory);
        Assert.Contains(expressionProbe.SelectMany(call => call.Arguments), argument =>
            argument?.ToString()?.Contains("Threshold", StringComparison.Ordinal) == true);

        var selector = new RecordingPropertySelector(true, "order-42");
        var propertyFactory = new PropertyExpressionSagaQueryFactory<QueryState, QueryMessage, string>(
            instance => instance.OrderNumber,
            selector);

        Assert.True(propertyFactory.TryCreateQuery(consumeContext, out ISagaQuery<QueryState>? propertyQuery));
        Assert.NotNull(propertyQuery);
        selector.Value = "changed-after-query";
        Func<QueryState, bool> propertyPredicate = propertyQuery.FilterExpression.Compile();
        Assert.True(propertyPredicate(new QueryState { OrderNumber = "order-42" }));
        Assert.False(propertyPredicate(new QueryState { OrderNumber = "changed-after-query" }));
        selector.Found = false;
        Assert.False(propertyFactory.TryCreateQuery(consumeContext, out ISagaQuery<QueryState>? missingQuery));
        Assert.Null(missingQuery);
        AssertParameter("context", () => propertyFactory.TryCreateQuery(null!, out _));
        AssertParameter("context", () => propertyFactory.Probe(null!));
        IReadOnlyList<Invocation> propertyProbe = Probe(propertyFactory);
        Assert.Contains(propertyProbe.SelectMany(call => call.Arguments), argument =>
            argument?.ToString()?.Contains("OrderNumber", StringComparison.Ordinal) == true);

        var propertyValue = new PropertyExpressionPropertyValue<string?> { Value = null };
        Assert.Null(propertyValue.GetValue());
        propertyValue.Value = "owned-value";
        Assert.Equal("owned-value", propertyValue.GetValue());
    }

    [Fact]
    [RequirementCoverage("REQ-VSB-SAGA-QUERY-EXPRESSION", "state-composition-preserves-nested-lambda-parameter-scope")]
    public void StateComposition_PreservesNestedLambdaParameterScopeAndShortCircuitSemantics()
    {
        Expression<Func<QueryState, bool>> expression = instance => instance.IsEnabled;
        Expression<Func<QueryState, bool>> stateExpression = instance =>
            instance.Children.Any(child => child.IsActive && instance.IsActive);

        Expression<Func<QueryState, bool>> combined = StateExpressionVisitor<QueryState>.Combine(expression, stateExpression);
        Func<QueryState, bool> predicate = combined.Compile();
        var enabledInactiveRoot = new QueryState
        {
            IsEnabled = true,
            IsActive = false,
            Children = [new QueryState { IsActive = true }],
        };

        Assert.False(predicate(enabledInactiveRoot));
        enabledInactiveRoot.IsActive = true;
        Assert.True(predicate(enabledInactiveRoot));
        enabledInactiveRoot.IsEnabled = false;
        Assert.False(predicate(enabledInactiveRoot));
        Assert.Single(combined.Parameters);
        Assert.Same(expression.Parameters[0], combined.Parameters[0]);
    }

    private static ISagaRepositoryContext<QuerySaga, QueryMessage> CreateRepositoryContext(
        QueryMessage message,
        List<Invocation>? calls = null,
        SagaConsumeContext<QuerySaga, QueryMessage>? defaultContext = null,
        SagaConsumeContext<QuerySaga, QueryMessage>? loadContext = null)
    {
        calls ??= [];
        defaultContext ??= CreateProxy<SagaConsumeContext<QuerySaga, QueryMessage>>();
        loadContext ??= defaultContext;
        ConsumeContext<QueryMessage> consumeContext = InMemoryOutboxTestContextFactory.Create(message);
        ConsumeContext advancedContext = consumeContext.Advanced();

        return CreateProxy<AdvancedRepositoryContext>((method, arguments) =>
        {
            if (method.Name == "get_Message")
                return message;
            if (method.Name == "get_ReceiveContext")
                return advancedContext.ReceiveContext;
            if (method.Name == "get_SerializerContext")
                return advancedContext.SerializerContext;

            calls.Add(new Invocation(method, arguments ?? []));
            return method.Name switch
            {
                "AddAsync" => Task.FromResult(defaultContext),
                "InsertAsync" => Task.FromResult<SagaConsumeContext<QuerySaga, QueryMessage>?>(defaultContext),
                "LoadAsync" => Task.FromResult<SagaConsumeContext<QuerySaga, QueryMessage>?>(loadContext),
                "CreateSagaConsumeContextAsync" => Task.FromResult(defaultContext),
                "SaveAsync" or "DiscardAsync" or "UndoAsync" or "UpdateAsync" or "DeleteAsync" => Task.CompletedTask,
                _ => DefaultReturn(method.ReturnType),
            };
        });
    }

    private static IQuerySagaRepositoryContext<QuerySaga> CreateQueryRepositoryContext(
        List<Invocation>? calls = null,
        ISagaRepositoryQueryContext<QuerySaga>? result = null)
    {
        calls ??= [];
        result ??= CreateProxy<ISagaRepositoryQueryContext<QuerySaga>>();

        return CreateProxy<IQuerySagaRepositoryContext<QuerySaga>>((method, arguments) =>
        {
            calls.Add(new Invocation(method, arguments ?? []));
            return method.Name == "QueryAsync"
                ? Task.FromResult(result)
                : DefaultReturn(method.ReturnType);
        });
    }

    private static ConsumeContext<QueryMessage> CreateConsumeContext(QueryMessage message) =>
        CreateProxy<ConsumeContext<QueryMessage>>((method, _) => method.Name == "get_Message"
            ? message
            : DefaultReturn(method.ReturnType));

    private static IReadOnlyList<Invocation> Probe(IProbeSite site)
    {
        var calls = new List<Invocation>();
        ProbeContext? context = null;
        context = CreateProxy<ProbeContext>((method, arguments) =>
        {
            calls.Add(new Invocation(method, arguments ?? []));
            return DefaultReturn(method.ReturnType, context);
        });

        site.Probe(context);
        return calls;
    }

    private static IReadOnlyList<object?> EnumerateUntyped(IEnumerable source)
    {
        var values = new List<object?>();
        IEnumerator enumerator = source.GetEnumerator();

        while (enumerator.MoveNext())
            values.Add(enumerator.Current);

        return values;
    }

    private static T CreateProxy<T>(Func<MethodInfo, object?[]?, object?>? handler = null)
        where T : class
    {
        T proxy = DispatchProxy.Create<T, ContractProxy>();
        ((ContractProxy)(object)proxy).Handler = handler;
        return proxy;
    }

    private static object? DefaultReturn(Type returnType, object? self = null)
    {
        if (returnType == typeof(void))
            return null;
        if (self != null && returnType.IsInstanceOfType(self))
            return self;
        if (returnType == typeof(Task))
            return Task.CompletedTask;
        if (returnType.IsGenericType && returnType.GetGenericTypeDefinition() == typeof(Task<>))
        {
            Type resultType = returnType.GetGenericArguments()[0];
            object? result = resultType.IsValueType ? Activator.CreateInstance(resultType) : null;
            return typeof(Task).GetMethod(nameof(Task.FromResult))!
                .MakeGenericMethod(resultType)
                .Invoke(null, [result]);
        }

        return returnType.IsValueType ? Activator.CreateInstance(returnType) : null;
    }

    private static void AssertParameter(string expected, Action action)
    {
        ArgumentNullException failure = Assert.Throws<ArgumentNullException>(action);
        Assert.Equal(expected, failure.ParamName);
    }

    private class ContractProxy : DispatchProxy
    {
        public Func<MethodInfo, object?[]?, object?>? Handler { get; set; }

        protected override object? Invoke(MethodInfo? targetMethod, object?[]? args)
        {
            ArgumentNullException.ThrowIfNull(targetMethod);
            return Handler?.Invoke(targetMethod, args) ?? DefaultReturn(targetMethod.ReturnType, this);
        }
    }

    private sealed class RecordingPropertySelector(bool found, string? value) :
        ISagaQueryPropertySelector<QueryMessage, string>
    {
        public bool Found { get; set; } = found;
        public string Value { get; set; } = value!;

        public bool TryGetProperty(ConsumeContext<QueryMessage> context, out string propertyValue)
        {
            propertyValue = Value;
            return Found;
        }
    }

    private interface AdvancedRepositoryContext :
        ISagaRepositoryContext<QuerySaga, QueryMessage>,
        PipeContext,
        MessageContext,
        ConsumeContext,
        IPublishEndpoint,
        ViciOne.ServiceBus.Advanced.Observers.IPublishObserverConnector,
        IAdvancedPublishEndpoint,
        ISendEndpointProvider,
        ViciOne.ServiceBus.Advanced.Observers.ISendObserverConnector;

    private sealed record Invocation(MethodInfo Method, object?[] Arguments);

    private sealed class QuerySaga : ISaga
    {
        public Guid CorrelationId { get; set; }
        public int Score { get; set; }
        public QueryMessage? LastMessage { get; set; }
    }

    private sealed class RecursiveSaga : ISaga
    {
        public Guid CorrelationId { get; set; }
    }

    private sealed class QueryState : ISagaStateMachineInstance
    {
        public Guid CorrelationId { get; set; }
        public string CurrentState { get; set; } = "Initial";
        public string OrderNumber { get; set; } = "";
        public bool IsEnabled { get; set; }
        public bool IsActive { get; set; }
        public List<QueryState> Children { get; set; } = [];
    }

    private sealed class QueryMessage
    {
        public int Threshold { get; set; }
    }
}

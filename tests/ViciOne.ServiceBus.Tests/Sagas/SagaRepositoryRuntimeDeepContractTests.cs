using System.Collections;
using System.Diagnostics.CodeAnalysis;
using ViciOne.ServiceBus.Middleware;
using ViciOne.ServiceBus.Saga;
using ViciOne.ServiceBus.Tests.Infrastructure.Requirements;
using ViciOne.ServiceBus.Tests.InternalAccess.InMemoryOutbox;
using Xunit;

namespace ViciOne.ServiceBus.Tests.Sagas;

public sealed class SagaRepositoryRuntimeDeepContractTests
{
    [Fact]
    [RequirementCoverage("REQ-VSB-SAGA-REPOSITORY-CAPABILITY", "runtime-load-null-result-and-canceled-token-forwarding")]
    public async Task LoadAsync_PreservesTheMissingSagaResultAndForwardsAnAlreadyCanceledTokenAsync()
    {
        using var cancellation = new CancellationTokenSource();
        cancellation.Cancel();
        var factory = new RecordingLoadFactory(new RecordingLoadContext(null, cancellation.Token));
        var repository = new LoadSagaRepository<RuntimeSaga>(factory);
        Guid correlationId = Guid.NewGuid();

        RuntimeSaga? result = await repository.LoadAsync(correlationId, cancellation.Token);

        Assert.Null(result);
        Assert.Equal(1, factory.InvocationCount);
        Assert.Equal(cancellation.Token, factory.CancellationToken);
        Assert.Equal(correlationId, factory.Context.CorrelationId);
        Assert.Equal(cancellation.Token, factory.Context.OperationCancellationToken);
    }

    [Theory]
    [InlineData(false, "load context returned a null task")]
    [InlineData(true, "load context factory returned a null task")]
    [RequirementCoverage("REQ-VSB-SAGA-REPOSITORY-CAPABILITY", "runtime-load-null-task-boundaries")]
    public async Task LoadAsync_ConvertsEveryCollaboratorNullTaskIntoADeterministicFaultAsync(
        bool factoryReturnsNullTask,
        string expectedMessage)
    {
        var context = new RecordingLoadContext(new RuntimeSaga { CorrelationId = Guid.NewGuid() }, default)
        {
            ReturnNullTask = !factoryReturnsNullTask,
        };
        var factory = new RecordingLoadFactory(context) { ReturnNullTask = factoryReturnsNullTask };
        var repository = new LoadSagaRepository<RuntimeSaga>(factory);

        InvalidOperationException exception = await Assert.ThrowsAsync<InvalidOperationException>(
            () => repository.LoadAsync(Guid.NewGuid(), TestContext.Current.CancellationToken));

        Assert.Contains(expectedMessage, exception.Message, StringComparison.OrdinalIgnoreCase);
        Assert.Equal(1, factory.InvocationCount);
        Assert.Equal(factoryReturnsNullTask ? 0 : 1, context.InvocationCount);
    }

    [Fact]
    [RequirementCoverage("REQ-VSB-SAGA-REPOSITORY-CAPABILITY", "runtime-load-failure-task-identity")]
    public async Task LoadAsync_PreservesFactoryFaultAndCancellationTaskIdentityAsync()
    {
        var expectedFault = new InvalidOperationException("load failed");
        Task<RuntimeSaga?> faulted = Task.FromException<RuntimeSaga?>(expectedFault);
        using var cancellation = new CancellationTokenSource();
        cancellation.Cancel();
        Task<RuntimeSaga?> canceled = Task.FromCanceled<RuntimeSaga?>(cancellation.Token);
        var context = new RecordingLoadContext(null, default);
        var faultFactory = new RecordingLoadFactory(context) { ExecutionTask = faulted };
        var cancellationFactory = new RecordingLoadFactory(context) { ExecutionTask = canceled };

        Task<RuntimeSaga?> faultOperation = new LoadSagaRepository<RuntimeSaga>(faultFactory)
            .LoadAsync(Guid.NewGuid(), TestContext.Current.CancellationToken);
        Task<RuntimeSaga?> cancellationOperation = new LoadSagaRepository<RuntimeSaga>(cancellationFactory)
            .LoadAsync(Guid.NewGuid(), TestContext.Current.CancellationToken);

        Assert.Same(faulted, faultOperation);
        Assert.Same(canceled, cancellationOperation);
        Assert.Same(expectedFault, await Assert.ThrowsAsync<InvalidOperationException>(() => faultOperation));
        OperationCanceledException canceledException =
            await Assert.ThrowsAnyAsync<OperationCanceledException>(() => cancellationOperation);
        Assert.Equal(cancellation.Token, canceledException.CancellationToken);
        Assert.Equal(0, context.InvocationCount);
    }

    [Theory]
    [InlineData(QueryFailureMode.ContextNullTask, "query context returned a null task")]
    [InlineData(QueryFailureMode.ContextNullResult, "query context returned a null result")]
    [InlineData(QueryFailureMode.FactoryNullTask, "query context factory returned a null task")]
    [InlineData(QueryFailureMode.FactoryNullResult, "query context factory returned a null result")]
    [RequirementCoverage("REQ-VSB-SAGA-REPOSITORY-CAPABILITY", "runtime-query-null-boundary-matrix")]
    public async Task FindAsync_RejectsEveryImpossibleNullFromTheQueryPipelineAsync(
        QueryFailureMode failureMode,
        string expectedMessage)
    {
        var query = new SagaQuery<RuntimeSaga>(_ => true);
        var factory = new RecordingQueryFactory([], failureMode);
        var repository = new QuerySagaRepository<RuntimeSaga>(factory);

        InvalidOperationException exception = await Assert.ThrowsAsync<InvalidOperationException>(
            () => repository.FindAsync(query, TestContext.Current.CancellationToken));

        Assert.Contains(expectedMessage, exception.Message, StringComparison.OrdinalIgnoreCase);
        Assert.Equal(1, factory.InvocationCount);
        Assert.Equal(
            failureMode is QueryFailureMode.ContextNullTask or QueryFailureMode.ContextNullResult ? 1 : 0,
            factory.Context.InvocationCount);
    }

    [Fact]
    [RequirementCoverage("REQ-VSB-SAGA-REPOSITORY-CAPABILITY", "runtime-query-identity-order-and-canceled-token-forwarding")]
    public async Task FindAsync_PreservesQueryResultOrderIdentityAndAlreadyCanceledTokenAsync()
    {
        Guid[] matches = [Guid.NewGuid(), Guid.NewGuid(), Guid.NewGuid()];
        var query = new SagaQuery<RuntimeSaga>(_ => true);
        using var cancellation = new CancellationTokenSource();
        cancellation.Cancel();
        var factory = new RecordingQueryFactory(matches, QueryFailureMode.None);
        var repository = new QuerySagaRepository<RuntimeSaga>(factory);

        IEnumerable<Guid> result = await repository.FindAsync(query, cancellation.Token);

        Assert.Same(factory.Context, result);
        Assert.Equal(matches, result);
        Assert.Same(query, factory.Context.Query);
        Assert.Equal(cancellation.Token, factory.CancellationToken);
        Assert.Equal(cancellation.Token, factory.Context.OperationCancellationToken);
    }

    [Theory]
    [InlineData(false)]
    [InlineData(true)]
    [RequirementCoverage("REQ-VSB-SAGA-REPOSITORY-CAPABILITY", "runtime-composite-dispatch-forwarding")]
    public async Task CompositeRepositories_ForwardBothDispatchFormsWithoutReplacingInputsAsync(bool queryable)
    {
        var dispatchFactory = new RecordingDispatchFactory();
        var loadFactory = new RecordingLoadFactory(new RecordingLoadContext(null, default));
        var queryFactory = new RecordingQueryFactory([], QueryFailureMode.None);
        ISagaRepository<RuntimeSaga> repository = queryable
            ? SagaRepository<RuntimeSaga>.CreateQueryable(dispatchFactory, queryFactory, loadFactory)
            : SagaRepository<RuntimeSaga>.CreateLoadable(dispatchFactory, loadFactory);
        Guid correlationId = Guid.NewGuid();
        ConsumeContext<RuntimeMessage> context = CreateContext(correlationId);
        var query = new SagaQuery<RuntimeSaga>(_ => true);
        var policy = new RuntimeSagaPolicy();
        var next = new DelegatePipe<SagaConsumeContext<RuntimeSaga, RuntimeMessage>>(_ => Task.CompletedTask);

        await repository.SendAsync(context, policy, next);
        await repository.SendQueryAsync(context, query, policy, next);

        Assert.Equal(1, dispatchFactory.SendInvocationCount);
        Assert.Equal(1, dispatchFactory.QueryInvocationCount);
        Assert.Same(context, dispatchFactory.SendContext);
        Assert.Same(context, dispatchFactory.QueryContext);
        Assert.Same(query, dispatchFactory.Query);
        Assert.NotNull(dispatchFactory.SendPipe);
        Assert.NotNull(dispatchFactory.QueryPipe);
    }

    [Theory]
    [InlineData(false)]
    [InlineData(true)]
    [RequirementCoverage("REQ-VSB-SAGA-REPOSITORY-CAPABILITY", "runtime-composite-dispatch-failure-task-identity")]
    public async Task CompositeRepositories_PreserveDispatchFailureTaskIdentityAsync(bool queryable)
    {
        var expectedFault = new InvalidOperationException("composite dispatch failed");
        Task faulted = Task.FromException(expectedFault);
        using var cancellation = new CancellationTokenSource();
        cancellation.Cancel();
        Task canceled = Task.FromCanceled(cancellation.Token);
        var dispatchFactory = new RecordingDispatchFactory
        {
            SendTask = faulted,
            QueryTask = canceled,
        };
        var loadFactory = new RecordingLoadFactory(new RecordingLoadContext(null, default));
        var queryFactory = new RecordingQueryFactory([], QueryFailureMode.None);
        ISagaRepository<RuntimeSaga> repository = queryable
            ? SagaRepository<RuntimeSaga>.CreateQueryable(dispatchFactory, queryFactory, loadFactory)
            : SagaRepository<RuntimeSaga>.CreateLoadable(dispatchFactory, loadFactory);
        ConsumeContext<RuntimeMessage> context = CreateContext(Guid.NewGuid());
        var query = new SagaQuery<RuntimeSaga>(_ => true);
        var policy = new RuntimeSagaPolicy();
        var next = new DelegatePipe<SagaConsumeContext<RuntimeSaga, RuntimeMessage>>(_ => Task.CompletedTask);

        Task send = repository.SendAsync(context, policy, next);
        Task sendQuery = repository.SendQueryAsync(context, query, policy, next);

        Assert.Same(faulted, send);
        Assert.Same(canceled, sendQuery);
        Assert.Same(expectedFault, await Assert.ThrowsAsync<InvalidOperationException>(() => send));
        OperationCanceledException canceledException = await Assert.ThrowsAnyAsync<OperationCanceledException>(() => sendQuery);
        Assert.Equal(cancellation.Token, canceledException.CancellationToken);
    }

    [Fact]
    [RequirementCoverage("REQ-VSB-SAGA-REPOSITORY-CAPABILITY", "runtime-missing-correlation-is-typed-and-side-effect-free")]
    public void SendAsync_RejectsMissingCorrelationBeforeOpeningARepositoryContext()
    {
        var factory = new RecordingDispatchFactory();
        var repository = new SagaRepository<RuntimeSaga>(factory);
        ConsumeContext<RuntimeMessage> context = CreateContext(null);
        var policy = new RuntimeSagaPolicy();
        var next = new DelegatePipe<SagaConsumeContext<RuntimeSaga, RuntimeMessage>>(_ => Task.CompletedTask);

        SagaException exception = Assert.Throws<SagaException>(() =>
        {
            _ = repository.SendAsync(context, policy, next);
        });

        Assert.Equal(typeof(RuntimeSaga), exception.SagaType);
        Assert.Equal(typeof(RuntimeMessage), exception.MessageType);
        Assert.Null(exception.CorrelationId);
        Assert.Contains("CorrelationId was not specified", exception.Message, StringComparison.Ordinal);
        Assert.Equal(0, factory.SendInvocationCount);
    }

    [Theory]
    [InlineData(false)]
    [InlineData(true)]
    [RequirementCoverage("REQ-VSB-SAGA-REPOSITORY-CAPABILITY", "runtime-dispatch-null-task-boundaries")]
    public async Task DispatchAsync_ConvertsAFactoryNullTaskIntoADeterministicFaultAsync(bool queryDispatch)
    {
        var factory = new RecordingDispatchFactory
        {
            ReturnNullSendTask = !queryDispatch,
            ReturnNullQueryTask = queryDispatch,
        };
        var repository = new SagaRepository<RuntimeSaga>(factory);
        ConsumeContext<RuntimeMessage> context = CreateContext(Guid.NewGuid());
        var query = new SagaQuery<RuntimeSaga>(_ => true);
        var policy = new RuntimeSagaPolicy();
        var next = new DelegatePipe<SagaConsumeContext<RuntimeSaga, RuntimeMessage>>(_ => Task.CompletedTask);

        Task operation = queryDispatch
            ? repository.SendQueryAsync(context, query, policy, next)
            : repository.SendAsync(context, policy, next);
        InvalidOperationException exception = await Assert.ThrowsAsync<InvalidOperationException>(() => operation);

        Assert.Contains("repository context factory returned a null task", exception.Message, StringComparison.OrdinalIgnoreCase);
        Assert.Equal(queryDispatch ? 0 : 1, factory.SendInvocationCount);
        Assert.Equal(queryDispatch ? 1 : 0, factory.QueryInvocationCount);
    }

    [Fact]
    [RequirementCoverage("REQ-VSB-SAGA-REPOSITORY-CAPABILITY", "runtime-dispatch-fault-and-cancellation-identity")]
    public async Task DispatchAsync_PreservesFactoryFaultAndCancellationIdentityAsync()
    {
        var expectedFault = new InvalidOperationException("dispatch failed");
        using var cancellation = new CancellationTokenSource();
        cancellation.Cancel();
        Task faulted = Task.FromException(expectedFault);
        Task canceled = Task.FromCanceled(cancellation.Token);
        var factory = new RecordingDispatchFactory
        {
            SendTask = faulted,
            QueryTask = canceled,
        };
        var repository = new SagaRepository<RuntimeSaga>(factory);
        ConsumeContext<RuntimeMessage> context = CreateContext(Guid.NewGuid());
        var query = new SagaQuery<RuntimeSaga>(_ => true);
        var policy = new RuntimeSagaPolicy();
        var next = new DelegatePipe<SagaConsumeContext<RuntimeSaga, RuntimeMessage>>(_ => Task.CompletedTask);

        Task send = repository.SendAsync(context, policy, next);
        Task sendQuery = repository.SendQueryAsync(context, query, policy, next);

        Assert.Same(faulted, send);
        Assert.Same(canceled, sendQuery);
        Assert.Same(expectedFault, await Assert.ThrowsAsync<InvalidOperationException>(() => send));
        OperationCanceledException canceledException = await Assert.ThrowsAnyAsync<OperationCanceledException>(() => sendQuery);
        Assert.Equal(cancellation.Token, canceledException.CancellationToken);
    }

    [Fact]
    [RequirementCoverage("REQ-VSB-SAGA-REPOSITORY-CAPABILITY", "runtime-composite-probe-order-and-scope")]
    public void CompositeRepositories_ProbeEveryOwnedCapabilityInStableOrder()
    {
        var standaloneDispatch = new RecordingDispatchFactory();
        var standalone = new SagaRepository<RuntimeSaga>(standaloneDispatch);

        ArgumentNullException exception = Assert.Throws<ArgumentNullException>(() => standalone.Probe(null!));

        Assert.Equal("context", exception.ParamName);
        Assert.Equal(0, standaloneDispatch.ProbeInvocationCount);

        var loadableProbe = new RecordingProbeContext();
        var loadableDispatch = new RecordingDispatchFactory();
        var loadableLoad = new RecordingLoadFactory(new RecordingLoadContext(null, default));
        ILoadableSagaRepository<RuntimeSaga> loadable = SagaRepository<RuntimeSaga>.CreateLoadable(loadableDispatch, loadableLoad);

        loadable.Probe(loadableProbe);

        Assert.Equal(["sagaRepository", "loadSagaRepository"], loadableProbe.ScopeKeys);
        Assert.Equal(1, loadableDispatch.ProbeInvocationCount);
        Assert.Equal(1, loadableLoad.ProbeInvocationCount);

        var queryableProbe = new RecordingProbeContext();
        var queryableDispatch = new RecordingDispatchFactory();
        var queryableQuery = new RecordingQueryFactory([], QueryFailureMode.None);
        var queryableLoad = new RecordingLoadFactory(new RecordingLoadContext(null, default));
        IQueryableSagaRepository<RuntimeSaga> queryable = SagaRepository<RuntimeSaga>.CreateQueryable(
            queryableDispatch,
            queryableQuery,
            queryableLoad);

        queryable.Probe(queryableProbe);

        Assert.Equal(["sagaRepository", "querySagaRepository", "loadSagaRepository"], queryableProbe.ScopeKeys);
        Assert.Equal(1, queryableDispatch.ProbeInvocationCount);
        Assert.Equal(1, queryableQuery.ProbeInvocationCount);
        Assert.Equal(1, queryableLoad.ProbeInvocationCount);
    }

    private static ConsumeContext<RuntimeMessage> CreateContext(Guid? correlationId) =>
        InMemoryOutboxTestContextFactory.Create(
            new RuntimeMessage(),
            TestContext.Current.CancellationToken,
            correlationId: correlationId);

    public enum QueryFailureMode
    {
        None,
        ContextNullTask,
        ContextNullResult,
        FactoryNullTask,
        FactoryNullResult,
    }

    private sealed class RecordingLoadFactory(RecordingLoadContext context) : ILoadSagaRepositoryContextFactory<RuntimeSaga>
    {
        public RecordingLoadContext Context { get; } = context;

        public bool ReturnNullTask { get; init; }

        public Task<RuntimeSaga?>? ExecutionTask { get; init; }

        public int InvocationCount { get; private set; }

        public int ProbeInvocationCount { get; private set; }

        public CancellationToken CancellationToken { get; private set; }

        public Task<T?> ExecuteAsync<T>(
            Func<ILoadSagaRepositoryContext<RuntimeSaga>, Task<T?>> asyncMethod,
            CancellationToken cancellationToken = default)
            where T : class
        {
            InvocationCount++;
            CancellationToken = cancellationToken;
            if (ReturnNullTask)
                return null!;

            return ExecutionTask is null
                ? asyncMethod(Context)
                : (Task<T?>)(object)ExecutionTask;
        }

        public void Probe(ProbeContext context)
        {
            ArgumentNullException.ThrowIfNull(context);
            ProbeInvocationCount++;
        }
    }

    private sealed class RecordingLoadContext(RuntimeSaga? saga, CancellationToken cancellationToken) :
        BasePipeContext(cancellationToken),
        ILoadSagaRepositoryContext<RuntimeSaga>
    {
        public bool ReturnNullTask { get; init; }

        public int InvocationCount { get; private set; }

        public Guid? CorrelationId { get; private set; }

        public CancellationToken OperationCancellationToken { get; private set; }

        public Task<RuntimeSaga?> LoadAsync(Guid correlationId, CancellationToken cancellationToken = default)
        {
            InvocationCount++;
            CorrelationId = correlationId;
            OperationCancellationToken = cancellationToken;
            return ReturnNullTask ? null! : Task.FromResult(saga);
        }
    }

    private sealed class RecordingQueryFactory(IEnumerable<Guid> matches, QueryFailureMode failureMode) :
        IQuerySagaRepositoryContextFactory<RuntimeSaga>
    {
        public RecordingQueryContext Context { get; } = new(matches, failureMode);

        public int InvocationCount { get; private set; }

        public int ProbeInvocationCount { get; private set; }

        public CancellationToken CancellationToken { get; private set; }

        public Task<T> ExecuteAsync<T>(
            Func<IQuerySagaRepositoryContext<RuntimeSaga>, Task<T>> asyncMethod,
            CancellationToken cancellationToken = default)
            where T : class
        {
            InvocationCount++;
            CancellationToken = cancellationToken;
            return failureMode switch
            {
                QueryFailureMode.FactoryNullTask => null!,
                QueryFailureMode.FactoryNullResult => Task.FromResult<T>(null!),
                _ => asyncMethod(Context),
            };
        }

        public void Probe(ProbeContext context)
        {
            ArgumentNullException.ThrowIfNull(context);
            ProbeInvocationCount++;
        }
    }

    private sealed class RecordingQueryContext(IEnumerable<Guid> matches, QueryFailureMode failureMode) :
        BasePipeContext,
        ISagaRepositoryQueryContext<RuntimeSaga>
    {
        readonly IReadOnlyList<Guid> _matches = matches.ToArray();

        public int Count => _matches.Count;

        public int InvocationCount { get; private set; }

        public ISagaQuery<RuntimeSaga>? Query { get; private set; }

        public CancellationToken OperationCancellationToken { get; private set; }

        public Task<ISagaRepositoryQueryContext<RuntimeSaga>> QueryAsync(
            ISagaQuery<RuntimeSaga> query,
            CancellationToken cancellationToken = default)
        {
            InvocationCount++;
            Query = query;
            OperationCancellationToken = cancellationToken;
            return failureMode switch
            {
                QueryFailureMode.ContextNullTask => null!,
                QueryFailureMode.ContextNullResult => Task.FromResult<ISagaRepositoryQueryContext<RuntimeSaga>>(null!),
                _ => Task.FromResult<ISagaRepositoryQueryContext<RuntimeSaga>>(this),
            };
        }

        public IEnumerator<Guid> GetEnumerator() => _matches.GetEnumerator();

        IEnumerator IEnumerable.GetEnumerator() => GetEnumerator();
    }

    private sealed class RecordingDispatchFactory : ISagaRepositoryContextFactory<RuntimeSaga>
    {
        public bool ReturnNullSendTask { get; init; }

        public bool ReturnNullQueryTask { get; init; }

        public Task SendTask { get; init; } = Task.CompletedTask;

        public Task QueryTask { get; init; } = Task.CompletedTask;

        public int SendInvocationCount { get; private set; }

        public int QueryInvocationCount { get; private set; }

        public int ProbeInvocationCount { get; private set; }

        public object? SendContext { get; private set; }

        public object? QueryContext { get; private set; }

        public object? Query { get; private set; }

        public object? SendPipe { get; private set; }

        public object? QueryPipe { get; private set; }

        public Task SendAsync<T>(ConsumeContext<T> context, IPipe<ISagaRepositoryContext<RuntimeSaga, T>> next)
            where T : class
        {
            SendInvocationCount++;
            SendContext = context;
            SendPipe = next;
            return ReturnNullSendTask ? null! : SendTask;
        }

        public Task SendQueryAsync<T>(
            ConsumeContext<T> context,
            ISagaQuery<RuntimeSaga> query,
            IPipe<ISagaRepositoryQueryContext<RuntimeSaga, T>> next)
            where T : class
        {
            QueryInvocationCount++;
            QueryContext = context;
            Query = query;
            QueryPipe = next;
            return ReturnNullQueryTask ? null! : QueryTask;
        }

        public void Probe(ProbeContext context)
        {
            ArgumentNullException.ThrowIfNull(context);
            ProbeInvocationCount++;
        }
    }

    private sealed class RuntimeSagaPolicy : ISagaPolicy<RuntimeSaga, RuntimeMessage>
    {
        public bool IsReadOnly => false;

        public bool PreInsertInstance(ConsumeContext<RuntimeMessage> context, [NotNullWhen(true)] out RuntimeSaga? instance)
        {
            instance = null;
            return false;
        }

        public Task ExistingAsync(
            SagaConsumeContext<RuntimeSaga, RuntimeMessage> context,
            IPipe<SagaConsumeContext<RuntimeSaga, RuntimeMessage>> next) =>
            throw new NotSupportedException();

        public Task MissingAsync(
            ConsumeContext<RuntimeMessage> context,
            IPipe<SagaConsumeContext<RuntimeSaga, RuntimeMessage>> next) =>
            throw new NotSupportedException();
    }

    private sealed class DelegatePipe<TContext>(Func<TContext, Task> callback) : IPipe<TContext>
        where TContext : class, PipeContext
    {
        public Task SendAsync(TContext context) => callback(context);

        public void Probe(ProbeContext context) => ArgumentNullException.ThrowIfNull(context);
    }

    private sealed class RecordingProbeContext : ProbeContext
    {
        public CancellationToken CancellationToken => default;

        public List<string> ScopeKeys { get; } = [];

        public void Add(string key, string? value) => throw new NotSupportedException();

        public void Add(string key, object? value) => throw new NotSupportedException();

        public void Set(object values) => throw new NotSupportedException();

        public void Set(IEnumerable<KeyValuePair<string, object?>> values) => throw new NotSupportedException();

        public ProbeContext CreateScope(string key)
        {
            ScopeKeys.Add(key);
            return this;
        }
    }

    public sealed class RuntimeSaga : ISaga
    {
        public Guid CorrelationId { get; set; }
    }

    public sealed record RuntimeMessage;
}

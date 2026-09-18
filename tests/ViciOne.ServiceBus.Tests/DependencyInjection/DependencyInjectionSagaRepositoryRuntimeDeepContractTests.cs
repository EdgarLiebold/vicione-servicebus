using System.Collections;
using System.Diagnostics.CodeAnalysis;
using System.Reflection;
using Microsoft.Extensions.DependencyInjection;
using ViciOne.ServiceBus.Advanced;
using ViciOne.ServiceBus.Configuration;
using ViciOne.ServiceBus.Context;
using ViciOne.ServiceBus.DependencyInjection;
using ViciOne.ServiceBus.Middleware;
using ViciOne.ServiceBus.Saga;
using ViciOne.ServiceBus.Tests.Infrastructure.Requirements;
using Xunit;

namespace ViciOne.ServiceBus.Tests.DependencyInjection;

public sealed class DependencyInjectionSagaRepositoryRuntimeDeepContractTests
{
    [Fact]
    [RequirementCoverage("REQ-VSB-DI-SAGA-REPOSITORY-RUNTIME", "public-boundaries-reject-every-missing-owner")]
    public async Task PublicBoundaries_RejectEveryMissingOwnerBeforeScopeEffectsAsync()
    {
        var setter = new RecordingSetter([]);
        var provider = new RecordingServiceProvider();
        var factory = new DependencyInjectionSagaRepositoryContextFactory<TestSaga>(provider, setter);
        ConsumeContext<TestMessage> context = CreateConsumeContext();
        IPipe<ISagaRepositoryContext<TestSaga, TestMessage>> next = Proxy<IPipe<ISagaRepositoryContext<TestSaga, TestMessage>>>();
        IPipe<ISagaRepositoryQueryContext<TestSaga, TestMessage>> queryNext =
            Proxy<IPipe<ISagaRepositoryQueryContext<TestSaga, TestMessage>>>();

        AssertParameter("provider", () => new DependencyInjectionLoadSagaRepository<TestSaga>(null!));
        AssertParameter("provider", () => new DependencyInjectionQuerySagaRepository<TestSaga>(null!));
        AssertParameter("context", () => new DependencyInjectionSagaRepository<TestSaga>((IRegistrationContext)null!));
        AssertParameter("serviceProvider", () => new DependencyInjectionSagaRepository<TestSaga>(null!, setter));
        AssertParameter("setter", () => new DependencyInjectionSagaRepository<TestSaga>(provider, null!));
        AssertParameter("serviceProvider", () => new DependencyInjectionSagaRepositoryContextFactory<TestSaga>(null!, setter));
        AssertParameter("setter", () => new DependencyInjectionSagaRepositoryContextFactory<TestSaga>(provider, null!));
        AssertParameter("context", () => factory.Probe(null!));

        var repository = new DependencyInjectionSagaRepository<TestSaga>(provider, setter);
        ISagaPolicy<TestSaga, TestMessage> policy = Proxy<ISagaPolicy<TestSaga, TestMessage>>();
        IPipe<SagaConsumeContext<TestSaga, TestMessage>> sagaNext = Proxy<IPipe<SagaConsumeContext<TestSaga, TestMessage>>>();
        ISagaQuery<TestSaga> query = Proxy<ISagaQuery<TestSaga>>();
        AssertParameter("context", () => repository.Probe(null!));
        AssertParameter("context", () => repository.SendAsync<TestMessage>(null!, policy, sagaNext));
        AssertParameter("policy", () => repository.SendAsync(context, null!, sagaNext));
        AssertParameter("next", () => repository.SendAsync(context, policy, null!));
        AssertParameter("context", () => repository.SendQueryAsync<TestMessage>(null!, query, policy, sagaNext));
        AssertParameter("query", () => repository.SendQueryAsync(context, null!, policy, sagaNext));
        AssertParameter("policy", () => repository.SendQueryAsync(context, query, null!, sagaNext));
        AssertParameter("next", () => repository.SendQueryAsync(context, query, policy, null!));

        await AssertParameterAsync("context", () => factory.SendAsync<TestMessage>(null!, next));
        await AssertParameterAsync("next", () => factory.SendAsync(context, null!));
        await AssertParameterAsync("context", () => factory.SendQueryAsync<TestMessage>(null!, Proxy<ISagaQuery<TestSaga>>(), queryNext));
        await AssertParameterAsync("query", () => factory.SendQueryAsync(context, null!, queryNext));
        await AssertParameterAsync("next", () => factory.SendQueryAsync(context, Proxy<ISagaQuery<TestSaga>>(), null!));
        Assert.Equal(0, setter.PushCount);
    }

    [Fact]
    [RequirementCoverage("REQ-VSB-DI-SAGA-REPOSITORY-RUNTIME", "load-distinct-scopes-gated-success-fault-and-exactly-once-cleanup")]
    public async Task LoadRepository_UsesDistinctScopesUntilGatedSuccessAndFaultCompleteAsync()
    {
        Guid correlationId = Guid.NewGuid();
        using var cancellation = new CancellationTokenSource();
        var saga = new TestSaga(correlationId);
        var successGate = new TaskCompletionSource<TestSaga?>(TaskCreationOptions.RunContinuationsAsynchronously);
        var successContext = DispatchProxy.Create<ILoadSagaRepositoryContext<TestSaga>, LoadContextProxy>();
        var successRecorder = (LoadContextProxy)(object)successContext;
        successRecorder.Result = successGate.Task;
        var successFactory = new RecordingLoadFactory(successContext);
        var successEvents = new List<string>();
        var successScope = CreateScope(successEvents, successFactory);

        var failure = new ExpectedFailure("load");
        var faultGate = new TaskCompletionSource<TestSaga?>(TaskCreationOptions.RunContinuationsAsynchronously);
        var faultContext = DispatchProxy.Create<ILoadSagaRepositoryContext<TestSaga>, LoadContextProxy>();
        var faultRecorder = (LoadContextProxy)(object)faultContext;
        faultRecorder.Result = faultGate.Task;
        var faultFactory = new RecordingLoadFactory(faultContext);
        var faultEvents = new List<string>();
        var faultScope = CreateScope(faultEvents, faultFactory);

        Queue<IServiceScope> scopes = new([successScope, faultScope]);
        var scopeFactory = new RecordingScopeFactory(() => scopes.Dequeue());
        var root = Provider((typeof(IServiceScopeFactory), scopeFactory));
        var repository = new DependencyInjectionLoadSagaRepository<TestSaga>(root);

        Task<TestSaga?> successOperation = repository.LoadAsync(correlationId, cancellation.Token);

        Assert.False(successOperation.IsCompleted);
        Assert.Empty(successEvents);
        Assert.Equal(0, successScope.DisposeCount);

        successGate.SetResult(saga);
        TestSaga? result = await successOperation;
        Assert.Same(saga, result);
        Assert.Equal(cancellation.Token, successFactory.Token);
        Assert.Equal(correlationId, successRecorder.CorrelationId);
        Assert.Equal(cancellation.Token, successRecorder.Token);
        Assert.Equal(["scope-async"], successEvents);
        Assert.Equal(1, successScope.DisposeCount);

        Task<TestSaga?> faultOperation = repository.LoadAsync(correlationId, cancellation.Token);

        Assert.False(faultOperation.IsCompleted);
        Assert.Empty(faultEvents);
        Assert.Equal(0, faultScope.DisposeCount);

        faultGate.SetException(failure);
        Exception observed = await Assert.ThrowsAsync<ExpectedFailure>(() => faultOperation);
        Assert.Same(failure, observed);
        Assert.Equal(cancellation.Token, faultFactory.Token);
        Assert.Equal(correlationId, faultRecorder.CorrelationId);
        Assert.Equal(cancellation.Token, faultRecorder.Token);
        Assert.Equal(2, scopeFactory.CreateCount);
        Assert.NotSame(successScope, faultScope);
        Assert.Equal(["scope-async"], faultEvents);
        Assert.Equal(1, faultScope.DisposeCount);
    }

    [Fact]
    [RequirementCoverage("REQ-VSB-DI-SAGA-REPOSITORY-RUNTIME", "query-distinct-scopes-gated-success-fault-and-exactly-once-cleanup")]
    public async Task QueryRepository_UsesDistinctScopesUntilGatedSuccessAndFaultCompleteAsync()
    {
        using var cancellation = new CancellationTokenSource();
        ISagaQuery<TestSaga> query = Proxy<ISagaQuery<TestSaga>>();
        ISagaRepositoryQueryContext<TestSaga> queryResult = CreateQueryResult(Guid.NewGuid(), Guid.NewGuid());
        var successGate = new TaskCompletionSource<ISagaRepositoryQueryContext<TestSaga>>(
            TaskCreationOptions.RunContinuationsAsynchronously);
        var successContext = DispatchProxy.Create<IQuerySagaRepositoryContext<TestSaga>, QueryContextProxy>();
        var successRecorder = (QueryContextProxy)(object)successContext;
        successRecorder.Result = successGate.Task;
        var successFactory = new RecordingQueryFactory(successContext);
        var successEvents = new List<string>();
        var successScope = CreateScope(successEvents, successFactory);

        var failure = new ExpectedFailure("query");
        var faultGate = new TaskCompletionSource<ISagaRepositoryQueryContext<TestSaga>>(
            TaskCreationOptions.RunContinuationsAsynchronously);
        var faultContext = DispatchProxy.Create<IQuerySagaRepositoryContext<TestSaga>, QueryContextProxy>();
        var faultRecorder = (QueryContextProxy)(object)faultContext;
        faultRecorder.Result = faultGate.Task;
        var faultFactory = new RecordingQueryFactory(faultContext);
        var faultEvents = new List<string>();
        var faultScope = CreateScope(faultEvents, faultFactory);

        Queue<IServiceScope> scopes = new([successScope, faultScope]);
        var scopeFactory = new RecordingScopeFactory(() => scopes.Dequeue());
        var repository = new DependencyInjectionQuerySagaRepository<TestSaga>(
            Provider((typeof(IServiceScopeFactory), scopeFactory)));

        Task<IEnumerable<Guid>> successOperation = repository.FindAsync(query, cancellation.Token);

        Assert.False(successOperation.IsCompleted);
        Assert.Empty(successEvents);
        Assert.Equal(0, successScope.DisposeCount);

        successGate.SetResult(queryResult);
        IEnumerable<Guid> result = await successOperation;
        Assert.Same(queryResult, result);
        Assert.Same(query, successRecorder.Query);
        Assert.Equal(cancellation.Token, successRecorder.Token);
        Assert.Equal(cancellation.Token, successFactory.Token);
        Assert.Equal(["scope-async"], successEvents);
        Assert.Equal(1, successScope.DisposeCount);

        Task<IEnumerable<Guid>> faultOperation = repository.FindAsync(query, cancellation.Token);

        Assert.False(faultOperation.IsCompleted);
        Assert.Empty(faultEvents);
        Assert.Equal(0, faultScope.DisposeCount);

        faultGate.SetException(failure);
        Exception observed = await Assert.ThrowsAsync<ExpectedFailure>(() => faultOperation);
        Assert.Same(failure, observed);
        Assert.Same(query, faultRecorder.Query);
        Assert.Equal(cancellation.Token, faultRecorder.Token);
        Assert.Equal(cancellation.Token, faultFactory.Token);
        Assert.Equal(2, scopeFactory.CreateCount);
        Assert.NotSame(successScope, faultScope);
        Assert.Equal(["scope-async"], faultEvents);
        Assert.Equal(1, faultScope.DisposeCount);
    }

    [Fact]
    [RequirementCoverage("REQ-VSB-DI-SAGA-REPOSITORY-RUNTIME", "correlation-precondition-and-behavioral-send-query-forwarding")]
    public async Task Repository_EnforcesCorrelationAndBehaviorallyForwardsExactContinuationsAsync()
    {
        var scopedFactory = new RecordingSagaFactory();
        var events = new List<string>();
        var scope = CreateScope(events, scopedFactory);
        var root = Provider((typeof(IServiceScopeFactory), new RecordingScopeFactory(() => scope)));
        var setter = new RecordingSetter(events);
        var repository = new DependencyInjectionSagaRepository<TestSaga>(root, setter);
        IPipe<SagaConsumeContext<TestSaga, TestMessage>> next = Proxy<IPipe<SagaConsumeContext<TestSaga, TestMessage>>>();
        ConsumeContext<TestMessage> missingCorrelation = CreateConsumeContext();

        var missingPolicy = new RecordingSagaPolicy();
        Action sendWithoutCorrelation = () => _ = repository.SendAsync(missingCorrelation, missingPolicy, next);
        SagaException missing = Assert.Throws<SagaException>(sendWithoutCorrelation);
        Assert.Contains("CorrelationId", missing.Message, StringComparison.Ordinal);
        Assert.Equal(0, setter.PushCount);

        Guid correlationId = Guid.NewGuid();
        ConsumeContext<TestMessage> context = CreateConsumeContext(correlationId, payloads: [scope]);
        var sendPolicy = new RecordingSagaPolicy();
        await repository.SendAsync(context, sendPolicy, next);

        var sagaContext = DispatchProxy.Create<SagaConsumeContext<TestSaga, TestMessage>, SagaConsumeContextProxy>();
        var sagaRecorder = (SagaConsumeContextProxy)(object)sagaContext;
        sagaRecorder.CorrelationId = correlationId;
        sagaRecorder.Saga = new TestSaga(correlationId);
        var sendRepositoryContext = DispatchProxy.Create<ISagaRepositoryContext<TestSaga, TestMessage>, SagaRepositoryContextProxy>();
        var sendRepositoryRecorder = (SagaRepositoryContextProxy)(object)sendRepositoryContext;
        sendRepositoryRecorder.SagaContext = sagaContext;
        IPipe<ISagaRepositoryContext<TestSaga, TestMessage>> sendPipe =
            Assert.IsAssignableFrom<IPipe<ISagaRepositoryContext<TestSaga, TestMessage>>>(scopedFactory.Pipe);

        await sendPipe.SendAsync(sendRepositoryContext);

        Assert.Equal(correlationId, sendRepositoryRecorder.LoadCorrelationId);
        Assert.Same(sagaContext, sendPolicy.ExistingContext);
        Assert.Same(next, sendPolicy.Next);
        Assert.Same(sagaContext, sendRepositoryRecorder.UpdatedContext);

        ISagaQuery<TestSaga> query = Proxy<ISagaQuery<TestSaga>>();
        var queryPolicy = new RecordingSagaPolicy();
        await repository.SendQueryAsync(context, query, queryPolicy, next);

        var queryRepositoryContext =
            DispatchProxy.Create<ISagaRepositoryQueryContext<TestSaga, TestMessage>, SagaRepositoryContextProxy>();
        var queryRepositoryRecorder = (SagaRepositoryContextProxy)(object)queryRepositoryContext;
        queryRepositoryRecorder.SagaContext = sagaContext;
        queryRepositoryRecorder.Values = [correlationId];
        IPipe<ISagaRepositoryQueryContext<TestSaga, TestMessage>> queryPipe =
            Assert.IsAssignableFrom<IPipe<ISagaRepositoryQueryContext<TestSaga, TestMessage>>>(scopedFactory.QueryPipe);

        await queryPipe.SendAsync(queryRepositoryContext);

        Assert.Same(query, scopedFactory.Query);
        Assert.Equal(correlationId, queryRepositoryRecorder.LoadCorrelationId);
        Assert.Same(sagaContext, queryPolicy.ExistingContext);
        Assert.Same(next, queryPolicy.Next);
        Assert.Same(sagaContext, queryRepositoryRecorder.UpdatedContext);
    }

    [Fact]
    [RequirementCoverage("REQ-VSB-DI-SAGA-REPOSITORY-RUNTIME", "borrowed-scope-preserves-context-and-restores-without-disposal")]
    public async Task ExistingScope_IsBorrowedAndRestoredWithoutReplacementOrDisposalAsync()
    {
        var events = new List<string>();
        var scopedFactory = new RecordingSagaFactory();
        var scope = CreateScope(events, scopedFactory);
        var rootScopeFactory = new RecordingScopeFactory(() => throw new InvalidOperationException("must not create"));
        var root = Provider((typeof(IServiceScopeFactory), rootScopeFactory));
        var setter = new RecordingSetter(events);
        var factory = new DependencyInjectionSagaRepositoryContextFactory<TestSaga>(root, setter);
        ConsumeContext<TestMessage> context = CreateConsumeContext(payloads: [scope]);
        IPipe<ISagaRepositoryContext<TestSaga, TestMessage>> next = Proxy<IPipe<ISagaRepositoryContext<TestSaga, TestMessage>>>();

        await factory.SendAsync(context, next);

        Assert.Equal(0, rootScopeFactory.CreateCount);
        Assert.Same(scope, setter.Scope);
        Assert.Same(context, setter.Context);
        Assert.Same(context, scopedFactory.Context);
        Assert.Same(next, scopedFactory.Pipe);
        Assert.Equal(["restore"], events);
        Assert.Equal(0, scope.DisposeCount);

        var failure = new ExpectedFailure("borrowed operation");
        scopedFactory.Result = Task.FromException(failure);
        Exception observed = await Assert.ThrowsAsync<ExpectedFailure>(() => factory.SendAsync(context, next));
        Assert.Same(failure, observed);
        Assert.Equal(["restore", "restore"], events);
        Assert.Equal(0, scope.DisposeCount);
    }

    [Fact]
    [RequirementCoverage("REQ-VSB-DI-SAGA-REPOSITORY-RUNTIME", "created-scope-rebinds-scheduler-and-releases-after-completion")]
    public async Task CreatedScope_RebindsSchedulerAndReleasesOnlyAfterOperationAsync()
    {
        var events = new List<string>();
        var completion = new TaskCompletionSource(TaskCreationOptions.RunContinuationsAsynchronously);
        var scopedFactory = new RecordingSagaFactory { Result = completion.Task };
        var scope = CreateScope(events, scopedFactory);
        var root = Provider((typeof(IServiceScopeFactory), new RecordingScopeFactory(() => scope)));
        var setter = new RecordingSetter(events);
        var factory = new DependencyInjectionSagaRepositoryContextFactory<TestSaga>(root, setter);
        MessageSchedulerContext scheduler = CreateSchedulerContext();
        ConsumeContext<TestMessage> source = CreateConsumeContext(payloads: [scheduler]);

        Task operation = factory.SendAsync(
            source,
            Proxy<IPipe<ISagaRepositoryContext<TestSaga, TestMessage>>>());

        Assert.False(operation.IsCompleted);
        Assert.Empty(events);
        ConsumeContext<TestMessage> scopedContext = Assert.IsAssignableFrom<ConsumeContext<TestMessage>>(scopedFactory.Context);
        Assert.NotSame(source, scopedContext);
        Assert.Same(((TestConsumeContextProxy)(object)source).Message, scopedContext.Message);
        Assert.True(scopedContext.TryGetPayload(out MessageSchedulerContext? rebound));
        Assert.NotNull(rebound);
        Assert.NotSame(scheduler, rebound);
        Assert.Same(scheduler.SchedulerFactory, rebound.SchedulerFactory);

        completion.SetResult();
        await operation;

        Assert.Equal(["restore", "scope-async"], events);
        Assert.Equal(1, setter.PushCount);
        Assert.Equal(1, scope.DisposeCount);
    }

    [Fact]
    [RequirementCoverage("REQ-VSB-DI-SAGA-REPOSITORY-RUNTIME", "created-inner-fault-preserves-identity-before-ordered-cleanup")]
    public async Task CreatedScope_InnerFaultPreservesExactFailureThenRestoresAndReleasesAsync()
    {
        var events = new List<string>();
        var failure = new ExpectedFailure("inner operation");
        var completion = new TaskCompletionSource(TaskCreationOptions.RunContinuationsAsynchronously);
        var scopedFactory = new RecordingSagaFactory { Result = completion.Task };
        var scope = CreateScope(events, scopedFactory);
        var factory = new DependencyInjectionSagaRepositoryContextFactory<TestSaga>(
            Provider((typeof(IServiceScopeFactory), new RecordingScopeFactory(() => scope))),
            new RecordingSetter(events));

        Task operation = factory.SendAsync(
            CreateConsumeContext(),
            Proxy<IPipe<ISagaRepositoryContext<TestSaga, TestMessage>>>());

        Assert.False(operation.IsCompleted);
        Assert.Empty(events);
        Assert.Equal(0, scope.DisposeCount);

        completion.SetException(failure);
        Exception observed = await Assert.ThrowsAsync<ExpectedFailure>(() => operation);

        Assert.Same(failure, observed);
        Assert.Equal(["restore", "scope-async"], events);
        Assert.Equal(1, scope.DisposeCount);
    }

    [Fact]
    [RequirementCoverage("REQ-VSB-DI-SAGA-REPOSITORY-RUNTIME", "created-scope-attempts-both-cleanups-and-preserves-both-failures")]
    public async Task CreatedScope_AttemptsBothCleanupStagesAndPreservesBothFailuresAsync()
    {
        var events = new List<string>();
        var restoreFailure = new ExpectedFailure("restore");
        var scopeFailure = new ExpectedFailure("scope");
        var scopedFactory = new RecordingSagaFactory();
        var scope = CreateScope(events, scopedFactory, scopeFailure);
        var root = Provider((typeof(IServiceScopeFactory), new RecordingScopeFactory(() => scope)));
        var factory = new DependencyInjectionSagaRepositoryContextFactory<TestSaga>(
            root,
            new RecordingSetter(events, restoreFailure));

        AggregateException observed = await Assert.ThrowsAsync<AggregateException>(() => factory.SendAsync(
            CreateConsumeContext(),
            Proxy<IPipe<ISagaRepositoryContext<TestSaga, TestMessage>>>()));

        Assert.Equal([restoreFailure, scopeFailure], observed.InnerExceptions);
        Assert.Equal(["restore", "scope-async"], events);
        Assert.Equal(1, scope.DisposeCount);
    }

    [Fact]
    [RequirementCoverage("REQ-VSB-DI-SAGA-REPOSITORY-RUNTIME", "borrowed-body-and-restore-failures-preserve-operation-first")]
    public async Task BorrowedScope_BodyAndRestoreFailuresAreAggregatedInCausalOrderAsync()
    {
        var events = new List<string>();
        var bodyFailure = new ExpectedFailure("borrowed body");
        var restoreFailure = new ExpectedFailure("borrowed restore");
        var completion = new TaskCompletionSource(TaskCreationOptions.RunContinuationsAsynchronously);
        var scopedFactory = new RecordingSagaFactory { Result = completion.Task };
        var scope = CreateScope(events, scopedFactory);
        var factory = new DependencyInjectionSagaRepositoryContextFactory<TestSaga>(
            Provider((typeof(IServiceScopeFactory), new RecordingScopeFactory(() => throw new InvalidOperationException("must not create")))),
            new RecordingSetter(events, restoreFailure));

        Task operation = factory.SendAsync(
            CreateConsumeContext(payloads: [scope]),
            Proxy<IPipe<ISagaRepositoryContext<TestSaga, TestMessage>>>());

        Assert.False(operation.IsCompleted);
        Assert.Empty(events);

        completion.SetException(bodyFailure);
        AggregateException observed = await Assert.ThrowsAsync<AggregateException>(() => operation);

        Assert.Equal([bodyFailure, restoreFailure], observed.InnerExceptions);
        Assert.Equal(["restore"], events);
        Assert.Equal(0, scope.DisposeCount);
    }

    [Fact]
    [RequirementCoverage("REQ-VSB-DI-SAGA-REPOSITORY-RUNTIME", "created-body-restore-scope-failures-preserve-causal-order")]
    public async Task CreatedScope_BodyRestoreAndScopeFailuresAreAggregatedInCausalOrderAsync()
    {
        var events = new List<string>();
        var bodyFailure = new ExpectedFailure("created body");
        var restoreFailure = new ExpectedFailure("created restore");
        var scopeFailure = new ExpectedFailure("created scope");
        var completion = new TaskCompletionSource(TaskCreationOptions.RunContinuationsAsynchronously);
        var scopedFactory = new RecordingSagaFactory { Result = completion.Task };
        var scope = CreateScope(events, scopedFactory, scopeFailure);
        var factory = new DependencyInjectionSagaRepositoryContextFactory<TestSaga>(
            Provider((typeof(IServiceScopeFactory), new RecordingScopeFactory(() => scope))),
            new RecordingSetter(events, restoreFailure));

        Task operation = factory.SendAsync(
            CreateConsumeContext(),
            Proxy<IPipe<ISagaRepositoryContext<TestSaga, TestMessage>>>());

        Assert.False(operation.IsCompleted);
        Assert.Empty(events);
        Assert.Equal(0, scope.DisposeCount);

        completion.SetException(bodyFailure);
        AggregateException observed = await Assert.ThrowsAsync<AggregateException>(() => operation);

        Assert.Equal([bodyFailure, restoreFailure, scopeFailure], observed.InnerExceptions);
        Assert.Equal(["restore", "scope-async"], events);
        Assert.Equal(1, scope.DisposeCount);
    }

    [Fact]
    [RequirementCoverage("REQ-VSB-DI-SAGA-REPOSITORY-RUNTIME", "load-body-and-scope-failures-preserve-operation-first")]
    public async Task LoadRepository_BodyAndScopeFailuresAreAggregatedInCausalOrderAsync()
    {
        var events = new List<string>();
        var bodyFailure = new ExpectedFailure("load body");
        var scopeFailure = new ExpectedFailure("load scope");
        var completion = new TaskCompletionSource<TestSaga?>(TaskCreationOptions.RunContinuationsAsynchronously);
        var loadContext = DispatchProxy.Create<ILoadSagaRepositoryContext<TestSaga>, LoadContextProxy>();
        ((LoadContextProxy)(object)loadContext).Result = completion.Task;
        var scope = CreateScope(events, new RecordingLoadFactory(loadContext), scopeFailure);
        var repository = new DependencyInjectionLoadSagaRepository<TestSaga>(Provider(
            (typeof(IServiceScopeFactory), new RecordingScopeFactory(() => scope))));

        Task<TestSaga?> operation = repository.LoadAsync(Guid.NewGuid(), TestContext.Current.CancellationToken);

        Assert.False(operation.IsCompleted);
        Assert.Empty(events);
        Assert.Equal(0, scope.DisposeCount);

        completion.SetException(bodyFailure);
        AggregateException observed = await Assert.ThrowsAsync<AggregateException>(() => operation);

        Assert.Equal([bodyFailure, scopeFailure], observed.InnerExceptions);
        Assert.Equal(["scope-async"], events);
        Assert.Equal(1, scope.DisposeCount);
    }

    [Fact]
    [RequirementCoverage("REQ-VSB-DI-SAGA-REPOSITORY-RUNTIME", "query-body-and-scope-failures-preserve-operation-first")]
    public async Task QueryRepository_BodyAndScopeFailuresAreAggregatedInCausalOrderAsync()
    {
        var events = new List<string>();
        var bodyFailure = new ExpectedFailure("query body");
        var scopeFailure = new ExpectedFailure("query scope");
        var completion = new TaskCompletionSource<ISagaRepositoryQueryContext<TestSaga>>(
            TaskCreationOptions.RunContinuationsAsynchronously);
        var queryContext = DispatchProxy.Create<IQuerySagaRepositoryContext<TestSaga>, QueryContextProxy>();
        ((QueryContextProxy)(object)queryContext).Result = completion.Task;
        var scope = CreateScope(events, new RecordingQueryFactory(queryContext), scopeFailure);
        var repository = new DependencyInjectionQuerySagaRepository<TestSaga>(Provider(
            (typeof(IServiceScopeFactory), new RecordingScopeFactory(() => scope))));

        Task<IEnumerable<Guid>> operation = repository.FindAsync(
            Proxy<ISagaQuery<TestSaga>>(),
            TestContext.Current.CancellationToken);

        Assert.False(operation.IsCompleted);
        Assert.Empty(events);
        Assert.Equal(0, scope.DisposeCount);

        completion.SetException(bodyFailure);
        AggregateException observed = await Assert.ThrowsAsync<AggregateException>(() => operation);

        Assert.Equal([bodyFailure, scopeFailure], observed.InnerExceptions);
        Assert.Equal(["scope-async"], events);
        Assert.Equal(1, scope.DisposeCount);
    }

    [Fact]
    [RequirementCoverage("REQ-VSB-DI-SAGA-REPOSITORY-RUNTIME", "restore-only-cleanup-failure-retains-exact-identity")]
    public async Task CreatedScope_RestoreOnlyFailureRetainsExactIdentityAfterScopeCleanupAsync()
    {
        var events = new List<string>();
        var restoreFailure = new ExpectedFailure("restore only");
        var scope = CreateScope(events, new RecordingSagaFactory());
        var factory = new DependencyInjectionSagaRepositoryContextFactory<TestSaga>(
            Provider((typeof(IServiceScopeFactory), new RecordingScopeFactory(() => scope))),
            new RecordingSetter(events, restoreFailure));

        Exception observed = await Assert.ThrowsAsync<ExpectedFailure>(() => factory.SendAsync(
            CreateConsumeContext(),
            Proxy<IPipe<ISagaRepositoryContext<TestSaga, TestMessage>>>()));

        Assert.Same(restoreFailure, observed);
        Assert.Equal(["restore", "scope-async"], events);
        Assert.Equal(1, scope.DisposeCount);
    }

    [Fact]
    [RequirementCoverage("REQ-VSB-DI-SAGA-REPOSITORY-RUNTIME", "scope-only-cleanup-failure-retains-exact-identity")]
    public async Task CreatedScope_ScopeOnlyFailureRetainsExactIdentityAfterRestoreAsync()
    {
        var events = new List<string>();
        var scopeFailure = new ExpectedFailure("scope only");
        var scope = CreateScope(events, new RecordingSagaFactory(), scopeFailure);
        var factory = new DependencyInjectionSagaRepositoryContextFactory<TestSaga>(
            Provider((typeof(IServiceScopeFactory), new RecordingScopeFactory(() => scope))),
            new RecordingSetter(events));

        Exception observed = await Assert.ThrowsAsync<ExpectedFailure>(() => factory.SendAsync(
            CreateConsumeContext(),
            Proxy<IPipe<ISagaRepositoryContext<TestSaga, TestMessage>>>()));

        Assert.Same(scopeFailure, observed);
        Assert.Equal(["restore", "scope-async"], events);
        Assert.Equal(1, scope.DisposeCount);
    }

    [Fact]
    [RequirementCoverage("REQ-VSB-DI-SAGA-REPOSITORY-RUNTIME", "load-query-context-factories-use-sync-disposal-fallback")]
    public async Task AllOwnedScopePaths_FallBackToSynchronousDisposalAsync()
    {
        var loadEvents = new List<string>();
        var loadContext = DispatchProxy.Create<ILoadSagaRepositoryContext<TestSaga>, LoadContextProxy>();
        ((LoadContextProxy)(object)loadContext).Result = Task.FromResult<TestSaga?>(null);
        var loadFactory = new RecordingLoadFactory(loadContext);
        var loadScope = new RecordingSyncScope(
            loadEvents,
            Provider((typeof(ILoadSagaRepositoryContextFactory<TestSaga>), loadFactory)));
        var loadRepository = new DependencyInjectionLoadSagaRepository<TestSaga>(Provider(
            (typeof(IServiceScopeFactory), new RecordingScopeFactory(() => loadScope))));

        TestSaga? loadResult = await loadRepository.LoadAsync(Guid.NewGuid(), TestContext.Current.CancellationToken);

        Assert.Null(loadResult);
        Assert.Equal(["scope-sync"], loadEvents);
        Assert.Equal(1, loadScope.DisposeCount);

        var queryEvents = new List<string>();
        ISagaRepositoryQueryContext<TestSaga> queryResult = CreateQueryResult(Guid.NewGuid());
        var queryContext = DispatchProxy.Create<IQuerySagaRepositoryContext<TestSaga>, QueryContextProxy>();
        ((QueryContextProxy)(object)queryContext).Result = Task.FromResult(queryResult);
        var queryFactory = new RecordingQueryFactory(queryContext);
        var queryScope = new RecordingSyncScope(
            queryEvents,
            Provider((typeof(IQuerySagaRepositoryContextFactory<TestSaga>), queryFactory)));
        var queryRepository = new DependencyInjectionQuerySagaRepository<TestSaga>(Provider(
            (typeof(IServiceScopeFactory), new RecordingScopeFactory(() => queryScope))));

        IEnumerable<Guid> queryValues = await queryRepository.FindAsync(
            Proxy<ISagaQuery<TestSaga>>(),
            TestContext.Current.CancellationToken);

        Assert.Same(queryResult, queryValues);
        Assert.Equal(["scope-sync"], queryEvents);
        Assert.Equal(1, queryScope.DisposeCount);

        var contextEvents = new List<string>();
        var sagaFactory = new RecordingSagaFactory();
        var contextScope = new RecordingSyncScope(
            contextEvents,
            Provider((typeof(ISagaRepositoryContextFactory<TestSaga>), sagaFactory)));
        var contextFactory = new DependencyInjectionSagaRepositoryContextFactory<TestSaga>(
            Provider((typeof(IServiceScopeFactory), new RecordingScopeFactory(() => contextScope))),
            new RecordingSetter(contextEvents));

        await contextFactory.SendAsync(
            CreateConsumeContext(),
            Proxy<IPipe<ISagaRepositoryContext<TestSaga, TestMessage>>>());

        Assert.Equal(["restore", "scope-sync"], contextEvents);
        Assert.Equal(1, contextScope.DisposeCount);
    }

    [Fact]
    [RequirementCoverage("REQ-VSB-DI-SAGA-REPOSITORY-RUNTIME", "repository-and-context-factory-publish-probe-metadata")]
    public void Probe_RepositoryAndContextFactoryPublishExpectedMetadata()
    {
        var provider = new RecordingServiceProvider();
        var setter = new RecordingSetter([]);
        var factory = new DependencyInjectionSagaRepositoryContextFactory<TestSaga>(provider, setter);
        var factoryProbe = new RecordingProbeContext();

        factory.Probe(factoryProbe);

        Assert.Equal("dependencyInjection", factoryProbe.Values["provider"]);
        Assert.Empty(factoryProbe.Children);

        var repository = new DependencyInjectionSagaRepository<TestSaga>(provider, setter);
        var repositoryProbe = new RecordingProbeContext();

        repository.Probe(repositoryProbe);

        RecordingProbeContext child = Assert.Single(repositoryProbe.Children);
        Assert.Equal("dependencyInjectionSagaRepository", child.ScopeKey);
        Assert.Equal("dependencyInjection", child.Values["provider"]);

        var loadRepository = new DependencyInjectionLoadSagaRepository<TestSaga>(provider);
        var loadProbe = new RecordingProbeContext();

        loadRepository.Probe(loadProbe);

        RecordingProbeContext loadChild = Assert.Single(loadProbe.Children);
        Assert.Equal("loadSagaRepository", loadChild.ScopeKey);
        Assert.Equal("dependencyInjection", loadChild.Values["provider"]);

        var queryRepository = new DependencyInjectionQuerySagaRepository<TestSaga>(provider);
        var queryProbe = new RecordingProbeContext();

        queryRepository.Probe(queryProbe);

        RecordingProbeContext queryChild = Assert.Single(queryProbe.Children);
        Assert.Equal("querySagaRepository", queryChild.ScopeKey);
        Assert.Equal("dependencyInjection", queryChild.Values["provider"]);
    }

    [Fact]
    [RequirementCoverage("REQ-VSB-DI-SAGA-REPOSITORY-RUNTIME", "consume-context-provider-payload-overrides-constructor-root")]
    public async Task CreatedScope_ConsumeContextProviderPayloadOverridesConstructorRootAsync()
    {
        var events = new List<string>();
        var scopedFactory = new RecordingSagaFactory();
        var scope = CreateScope(events, scopedFactory);
        var overrideScopeFactory = new RecordingScopeFactory(() => scope);
        var overrideProvider = Provider((typeof(IServiceScopeFactory), overrideScopeFactory));
        var rootScopeFactory = new RecordingScopeFactory(() => throw new InvalidOperationException("constructor root must not create"));
        var factory = new DependencyInjectionSagaRepositoryContextFactory<TestSaga>(
            Provider((typeof(IServiceScopeFactory), rootScopeFactory)),
            new RecordingSetter(events));

        await factory.SendAsync(
            CreateConsumeContext(payloads: [overrideProvider]),
            Proxy<IPipe<ISagaRepositoryContext<TestSaga, TestMessage>>>());

        Assert.Equal(0, rootScopeFactory.CreateCount);
        Assert.Equal(1, overrideScopeFactory.CreateCount);
        Assert.Equal(["restore", "scope-async"], events);
        Assert.Equal(1, scope.DisposeCount);
    }

    [Fact]
    [RequirementCoverage("REQ-VSB-DI-SAGA-REPOSITORY-RUNTIME", "null-restore-handles-fail-stably-for-borrowed-and-created-scopes")]
    public async Task NullRestoreHandles_FailStablyForBorrowedAndCreatedScopesAsync()
    {
        const string expectedMessage = "The scoped consume context setter returned a null restore handle.";

        var borrowedEvents = new List<string>();
        var borrowedScope = CreateScope(borrowedEvents, new RecordingSagaFactory());
        var borrowedFactory = new DependencyInjectionSagaRepositoryContextFactory<TestSaga>(
            Provider((typeof(IServiceScopeFactory), new RecordingScopeFactory(() => throw new InvalidOperationException("must not create")))),
            new RecordingSetter(borrowedEvents) { ReturnNull = true });

        InvalidOperationException borrowedFailure = await Assert.ThrowsAsync<InvalidOperationException>(() => borrowedFactory.SendAsync(
            CreateConsumeContext(payloads: [borrowedScope]),
            Proxy<IPipe<ISagaRepositoryContext<TestSaga, TestMessage>>>()));

        Assert.Equal(expectedMessage, borrowedFailure.Message);
        Assert.Empty(borrowedEvents);
        Assert.Equal(0, borrowedScope.DisposeCount);

        var createdEvents = new List<string>();
        var createdScope = CreateScope(createdEvents, new RecordingSagaFactory());
        var createdFactory = new DependencyInjectionSagaRepositoryContextFactory<TestSaga>(
            Provider((typeof(IServiceScopeFactory), new RecordingScopeFactory(() => createdScope))),
            new RecordingSetter(createdEvents) { ReturnNull = true });

        InvalidOperationException createdFailure = await Assert.ThrowsAsync<InvalidOperationException>(() => createdFactory.SendAsync(
            CreateConsumeContext(),
            Proxy<IPipe<ISagaRepositoryContext<TestSaga, TestMessage>>>()));

        Assert.Equal(expectedMessage, createdFailure.Message);
        Assert.Equal(["scope-async"], createdEvents);
        Assert.Equal(1, createdScope.DisposeCount);
    }

    [Fact]
    [RequirementCoverage("REQ-VSB-DI-SAGA-REPOSITORY-RUNTIME", "unsupported-registration-context-has-stable-actionable-diagnostic")]
    public void RegistrationContextWithoutScopedOwnership_IsRejectedWithStableDiagnostic()
    {
        IRegistrationContext registrationContext = Proxy<IRegistrationContext>();

        ArgumentException factoryFailure = Assert.Throws<ArgumentException>(
            () => new DependencyInjectionSagaRepositoryContextFactory<TestSaga>(registrationContext));
        ArgumentException repositoryFailure = Assert.Throws<ArgumentException>(
            () => new DependencyInjectionSagaRepository<TestSaga>(registrationContext));

        Assert.Equal("context", factoryFailure.ParamName);
        Assert.StartsWith(
            "The registration context must support scoped consume-context ownership.",
            factoryFailure.Message,
            StringComparison.Ordinal);
        Assert.Equal("context", repositoryFailure.ParamName);
        Assert.StartsWith(
            "The registration context must support scoped consume-context ownership.",
            repositoryFailure.Message,
            StringComparison.Ordinal);
    }

    [Fact]
    [RequirementCoverage("REQ-VSB-DI-SAGA-REPOSITORY-RUNTIME", "scoped-null-tasks-have-stable-diagnostics-and-still-clean-up")]
    public async Task ScopedFactories_NullTasksHaveStableDiagnosticsAndStillCleanUpAsync()
    {
        var loadEvents = new List<string>();
        var loadFactory = new RecordingLoadFactory(Proxy<ILoadSagaRepositoryContext<TestSaga>>()) { ReturnNull = true };
        var loadScope = CreateScope(loadEvents, loadFactory);
        var loadRepository = new DependencyInjectionLoadSagaRepository<TestSaga>(Provider(
            (typeof(IServiceScopeFactory), new RecordingScopeFactory(() => loadScope))));

        InvalidOperationException loadFailure = await Assert.ThrowsAsync<InvalidOperationException>(
            () => loadRepository.LoadAsync(Guid.NewGuid(), TestContext.Current.CancellationToken));
        Assert.Equal("The scoped saga load context factory returned a null task.", loadFailure.Message);
        Assert.Equal(["scope-async"], loadEvents);

        var queryEvents = new List<string>();
        var queryFactory = new RecordingQueryFactory(Proxy<IQuerySagaRepositoryContext<TestSaga>>()) { ReturnNull = true };
        var queryScope = CreateScope(queryEvents, queryFactory);
        var queryRepository = new DependencyInjectionQuerySagaRepository<TestSaga>(Provider(
            (typeof(IServiceScopeFactory), new RecordingScopeFactory(() => queryScope))));

        InvalidOperationException queryFailure = await Assert.ThrowsAsync<InvalidOperationException>(
            () => queryRepository.FindAsync(Proxy<ISagaQuery<TestSaga>>(), TestContext.Current.CancellationToken));
        Assert.Equal("The scoped saga query context factory returned a null task.", queryFailure.Message);
        Assert.Equal(["scope-async"], queryEvents);

        var sagaEvents = new List<string>();
        var sagaFactory = new RecordingSagaFactory { ReturnNull = true };
        var sagaScope = CreateScope(sagaEvents, sagaFactory);
        var contextFactory = new DependencyInjectionSagaRepositoryContextFactory<TestSaga>(
            Provider((typeof(IServiceScopeFactory), new RecordingScopeFactory(() => sagaScope))),
            new RecordingSetter(sagaEvents));

        InvalidOperationException sagaFailure = await Assert.ThrowsAsync<InvalidOperationException>(() => contextFactory.SendAsync(
            CreateConsumeContext(),
            Proxy<IPipe<ISagaRepositoryContext<TestSaga, TestMessage>>>()));
        Assert.Equal("The scoped saga repository context factory returned a null task.", sagaFailure.Message);
        Assert.Equal(["restore", "scope-async"], sagaEvents);
    }

    private static ConsumeContext<TestMessage> CreateConsumeContext(Guid? correlationId = null, object[]? payloads = null)
    {
        TestConsumeContext context = DispatchProxy.Create<TestConsumeContext, TestConsumeContextProxy>();
        var source = (TestConsumeContextProxy)(object)context;
        source.Message = new TestMessage();
        source.CorrelationId = correlationId;
        source.Payloads.AddRange(payloads ?? []);
        source.ReceiveContext = CreateReceiveContext();
        source.SerializerContext = Proxy<SerializerContext>();
        return context;
    }

    private static ReceiveContext CreateReceiveContext()
    {
        ReceiveContext context = DispatchProxy.Create<ReceiveContext, ReceiveContextProxy>();
        var source = (ReceiveContextProxy)(object)context;
        source.InputAddress = new Uri("loopback://localhost/input");
        source.PublishEndpointProvider = Proxy<IPublishEndpointProvider>();
        return context;
    }

    private static MessageSchedulerContext CreateSchedulerContext()
    {
        MessageSchedulerContext context = DispatchProxy.Create<MessageSchedulerContext, SchedulerContextProxy>();
        var source = (SchedulerContextProxy)(object)context;
        source.Factory = _ => Proxy<IMessageScheduler>();
        return context;
    }

    private static ISagaRepositoryQueryContext<TestSaga> CreateQueryResult(params Guid[] values)
    {
        ISagaRepositoryQueryContext<TestSaga> context =
            DispatchProxy.Create<ISagaRepositoryQueryContext<TestSaga>, QueryResultProxy>();
        ((QueryResultProxy)(object)context).Values = values;
        return context;
    }

    private static RecordingAsyncScope CreateScope(List<string> events, object service, Exception? failure = null) =>
        new(events, Provider((service.GetType().GetInterfaces().Single(type => type.IsGenericType &&
            (type.GetGenericTypeDefinition() == typeof(ILoadSagaRepositoryContextFactory<>) ||
             type.GetGenericTypeDefinition() == typeof(IQuerySagaRepositoryContextFactory<>) ||
             type.GetGenericTypeDefinition() == typeof(ISagaRepositoryContextFactory<>))), service)), failure);

    private static RecordingServiceProvider Provider(params (Type Type, object Service)[] services)
    {
        var provider = new RecordingServiceProvider();
        foreach ((Type type, object service) in services)
            provider.Add(type, service);
        return provider;
    }

    private static T Proxy<T>() where T : class => DispatchProxy.Create<T, PassiveProxy>();

    private static void AssertParameter(string parameterName, Action action)
    {
        ArgumentNullException exception = Assert.Throws<ArgumentNullException>(action);
        Assert.Equal(parameterName, exception.ParamName);
    }

    private static async Task AssertParameterAsync(string parameterName, Func<Task> action)
    {
        ArgumentNullException exception = await Assert.ThrowsAsync<ArgumentNullException>(action);
        Assert.Equal(parameterName, exception.ParamName);
    }

    private interface TestConsumeContext : ConsumeContext<TestMessage>, ConsumeContext;

    private sealed class TestMessage;

    private sealed class TestSaga(Guid correlationId) : ISaga
    {
        public Guid CorrelationId { get; set; } = correlationId;
    }

    private sealed class ExpectedFailure(string message) : Exception(message);

    private sealed class RecordingLoadFactory(ILoadSagaRepositoryContext<TestSaga> context) :
        ILoadSagaRepositoryContextFactory<TestSaga>
    {
        public CancellationToken Token { get; private set; }
        public bool ReturnNull { get; init; }

        public Task<T?> ExecuteAsync<T>(Func<ILoadSagaRepositoryContext<TestSaga>, Task<T?>> asyncMethod,
            CancellationToken cancellationToken = default) where T : class
        {
            Token = cancellationToken;
            return ReturnNull ? null! : asyncMethod(context);
        }

        public void Probe(ProbeContext context) { }
    }

    private sealed class RecordingQueryFactory(IQuerySagaRepositoryContext<TestSaga> context) :
        IQuerySagaRepositoryContextFactory<TestSaga>
    {
        public CancellationToken Token { get; private set; }
        public bool ReturnNull { get; init; }

        public Task<T> ExecuteAsync<T>(Func<IQuerySagaRepositoryContext<TestSaga>, Task<T>> asyncMethod,
            CancellationToken cancellationToken) where T : class
        {
            Token = cancellationToken;
            return ReturnNull ? null! : asyncMethod(context);
        }

        public void Probe(ProbeContext context) { }
    }

    private sealed class RecordingSagaFactory : ISagaRepositoryContextFactory<TestSaga>
    {
        public ConsumeContext? Context { get; private set; }
        public object? Pipe { get; private set; }
        public ISagaQuery<TestSaga>? Query { get; private set; }
        public object? QueryPipe { get; private set; }
        public Task Result { get; set; } = Task.CompletedTask;
        public bool ReturnNull { get; init; }

        public Task SendAsync<T>(ConsumeContext<T> context, IPipe<ISagaRepositoryContext<TestSaga, T>> next) where T : class
        {
            Context = (ConsumeContext)context;
            Pipe = next;
            return ReturnNull ? null! : Result;
        }

        public Task SendQueryAsync<T>(ConsumeContext<T> context, ISagaQuery<TestSaga> query,
            IPipe<ISagaRepositoryQueryContext<TestSaga, T>> next) where T : class
        {
            Context = (ConsumeContext)context;
            Query = query;
            QueryPipe = next;
            return ReturnNull ? null! : Result;
        }

        public void Probe(ProbeContext context) { }
    }

    private sealed class RecordingSetter(List<string> events, Exception? failure = null) : ISetScopedConsumeContext
    {
        public int PushCount { get; private set; }
        public IServiceScope? Scope { get; private set; }
        public ConsumeContext? Context { get; private set; }
        public bool ReturnNull { get; init; }

        public IDisposable PushContext(IServiceScope serviceProvider, ConsumeContext context)
        {
            PushCount++;
            Scope = serviceProvider;
            Context = context;
            return ReturnNull ? null! : new RecordingDisposable(events, failure);
        }
    }

    private sealed class RecordingDisposable(List<string> events, Exception? failure) : IDisposable
    {
        public void Dispose()
        {
            events.Add("restore");
            if (failure is not null)
                throw failure;
        }
    }

    private sealed class RecordingAsyncScope(List<string> events, IServiceProvider provider, Exception? failure) :
        IServiceScope, IAsyncDisposable
    {
        public int DisposeCount { get; private set; }
        public IServiceProvider ServiceProvider { get; } = provider;

        public void Dispose() => throw new InvalidOperationException("Async disposal is required.");

        public ValueTask DisposeAsync()
        {
            DisposeCount++;
            events.Add("scope-async");
            return failure is null ? default : ValueTask.FromException(failure);
        }
    }

    private sealed class RecordingSyncScope(List<string> events, IServiceProvider provider, Exception? failure = null) :
        IServiceScope
    {
        public int DisposeCount { get; private set; }
        public IServiceProvider ServiceProvider { get; } = provider;

        public void Dispose()
        {
            DisposeCount++;
            events.Add("scope-sync");
            if (failure is not null)
                throw failure;
        }
    }

    private sealed class RecordingScopeFactory(Func<IServiceScope> createScope) : IServiceScopeFactory
    {
        public int CreateCount { get; private set; }

        public IServiceScope CreateScope()
        {
            CreateCount++;
            return createScope();
        }
    }

    private sealed class RecordingServiceProvider : IServiceProvider
    {
        readonly Dictionary<Type, object> _services = [];

        public void Add(Type type, object service) => _services.Add(type, service);

        public object? GetService(Type serviceType) => _services.GetValueOrDefault(serviceType);
    }

    private sealed class RecordingSagaPolicy : ISagaPolicy<TestSaga, TestMessage>
    {
        public bool IsReadOnly => false;
        public SagaConsumeContext<TestSaga, TestMessage>? ExistingContext { get; private set; }
        public IPipe<SagaConsumeContext<TestSaga, TestMessage>>? Next { get; private set; }

        public bool PreInsertInstance(
            ConsumeContext<TestMessage> context,
            [NotNullWhen(true)] out TestSaga? instance)
        {
            instance = null;
            return false;
        }

        public Task ExistingAsync(
            SagaConsumeContext<TestSaga, TestMessage> context,
            IPipe<SagaConsumeContext<TestSaga, TestMessage>> next)
        {
            ExistingContext = context;
            Next = next;
            return Task.CompletedTask;
        }

        public Task MissingAsync(
            ConsumeContext<TestMessage> context,
            IPipe<SagaConsumeContext<TestSaga, TestMessage>> next) =>
            throw new InvalidOperationException("The behavioral forwarding test requires an existing saga.");
    }

    private sealed class RecordingProbeContext(string? scopeKey = null) : ProbeContext
    {
        public CancellationToken CancellationToken => default;
        public string? ScopeKey { get; } = scopeKey;
        public Dictionary<string, object?> Values { get; } = [];
        public List<RecordingProbeContext> Children { get; } = [];

        public void Add(string key, string? value) => Values[key] = value;

        public void Add(string key, object? value) => Values[key] = value;

        public void Set(object values) => throw new NotSupportedException();

        public void Set(IEnumerable<KeyValuePair<string, object?>> values)
        {
            foreach ((string key, object? value) in values)
                Values[key] = value;
        }

        public ProbeContext CreateScope(string key)
        {
            var child = new RecordingProbeContext(key);
            Children.Add(child);
            return child;
        }
    }

    private class PassiveProxy : DispatchProxy
    {
        protected override object? Invoke(MethodInfo? targetMethod, object?[]? args) =>
            targetMethod?.ReturnType == typeof(void)
                ? null
                : targetMethod?.ReturnType.IsValueType == true
                    ? Activator.CreateInstance(targetMethod.ReturnType)
                    : null;
    }

    private class TestConsumeContextProxy : PassiveProxy
    {
        public TestMessage Message { get; set; } = null!;
        public Guid? CorrelationId { get; set; }
        public List<object> Payloads { get; } = [];
        public ReceiveContext ReceiveContext { get; set; } = null!;
        public SerializerContext SerializerContext { get; set; } = null!;

        protected override object? Invoke(MethodInfo? targetMethod, object?[]? args)
        {
            if (targetMethod?.Name == nameof(PipeContext.TryGetPayload) && targetMethod.IsGenericMethod)
            {
                Type type = targetMethod.GetGenericArguments()[0];
                object? payload = Payloads.FirstOrDefault(type.IsInstanceOfType);
                args![0] = payload;
                return payload is not null;
            }

            if (targetMethod?.Name == nameof(PipeContext.HasPayloadType))
                return args![0] is Type type && Payloads.Any(type.IsInstanceOfType);

            return targetMethod?.Name switch
            {
                "get_Message" => Message,
                "get_CorrelationId" => CorrelationId,
                "get_CancellationToken" => CancellationToken.None,
                "get_ReceiveContext" => ReceiveContext,
                "get_SerializerContext" => SerializerContext,
                _ => base.Invoke(targetMethod, args),
            };
        }
    }

    private class ReceiveContextProxy : PassiveProxy
    {
        public Uri InputAddress { get; set; } = null!;
        public IPublishEndpointProvider PublishEndpointProvider { get; set; } = null!;

        protected override object? Invoke(MethodInfo? targetMethod, object?[]? args) => targetMethod?.Name switch
        {
            "get_InputAddress" => InputAddress,
            "get_PublishEndpointProvider" => PublishEndpointProvider,
            _ => base.Invoke(targetMethod, args),
        };
    }

    private class SchedulerContextProxy : PassiveProxy
    {
        public MessageSchedulerFactory Factory { get; set; } = null!;

        protected override object? Invoke(MethodInfo? targetMethod, object?[]? args) =>
            targetMethod?.Name == "get_SchedulerFactory" ? Factory : base.Invoke(targetMethod, args);
    }

    private class LoadContextProxy : PassiveProxy
    {
        public Task<TestSaga?> Result { get; set; } = null!;
        public Guid CorrelationId { get; private set; }
        public CancellationToken Token { get; private set; }

        protected override object? Invoke(MethodInfo? targetMethod, object?[]? args)
        {
            if (targetMethod?.Name == nameof(ILoadSagaRepositoryContext<TestSaga>.LoadAsync))
            {
                CorrelationId = Assert.IsType<Guid>(args![0]);
                Token = Assert.IsType<CancellationToken>(args[1]);
                return Result;
            }

            return base.Invoke(targetMethod, args);
        }
    }

    private class QueryContextProxy : PassiveProxy
    {
        public Task<ISagaRepositoryQueryContext<TestSaga>> Result { get; set; } = null!;
        public ISagaQuery<TestSaga>? Query { get; private set; }
        public CancellationToken Token { get; private set; }

        protected override object? Invoke(MethodInfo? targetMethod, object?[]? args)
        {
            if (targetMethod?.Name == nameof(IQuerySagaRepositoryContext<TestSaga>.QueryAsync))
            {
                Query = Assert.IsAssignableFrom<ISagaQuery<TestSaga>>(args![0]);
                Token = Assert.IsType<CancellationToken>(args[1]);
                return Result;
            }

            return base.Invoke(targetMethod, args);
        }
    }

    private class QueryResultProxy : PassiveProxy
    {
        public Guid[] Values { get; set; } = [];

        protected override object? Invoke(MethodInfo? targetMethod, object?[]? args)
        {
            if (targetMethod?.Name == "get_Count")
                return Values.Length;

            if (targetMethod?.Name == nameof(IEnumerable.GetEnumerator))
            {
                return targetMethod.ReturnType == typeof(IEnumerator<Guid>)
                    ? ((IEnumerable<Guid>)Values).GetEnumerator()
                    : Values.GetEnumerator();
            }

            return base.Invoke(targetMethod, args);
        }
    }

    private class SagaConsumeContextProxy : PassiveProxy
    {
        public Guid? CorrelationId { get; set; }
        public TestSaga Saga { get; set; } = null!;
        public bool IsCompleted { get; set; }

        protected override object? Invoke(MethodInfo? targetMethod, object?[]? args) => targetMethod?.Name switch
        {
            "get_CorrelationId" => CorrelationId,
            "get_Saga" => Saga,
            "get_IsCompleted" => IsCompleted,
            "get_CancellationToken" => CancellationToken.None,
            _ => base.Invoke(targetMethod, args),
        };
    }

    private class SagaRepositoryContextProxy : PassiveProxy
    {
        public SagaConsumeContext<TestSaga, TestMessage> SagaContext { get; set; } = null!;
        public Guid[] Values { get; set; } = [];
        public Guid? LoadCorrelationId { get; private set; }
        public SagaConsumeContext<TestSaga, TestMessage>? UpdatedContext { get; private set; }

        protected override object? Invoke(MethodInfo? targetMethod, object?[]? args)
        {
            if (targetMethod?.Name == nameof(ISagaRepositoryContext<TestSaga, TestMessage>.LoadAsync))
            {
                LoadCorrelationId = Assert.IsType<Guid>(args![0]);
                return Task.FromResult<SagaConsumeContext<TestSaga, TestMessage>?>(SagaContext);
            }

            if (targetMethod?.Name == nameof(ISagaRepositoryContext<TestSaga, TestMessage>.UpdateAsync))
            {
                UpdatedContext = Assert.IsAssignableFrom<SagaConsumeContext<TestSaga, TestMessage>>(args![0]);
                return Task.CompletedTask;
            }

            if (targetMethod?.Name == "get_Count")
                return Values.Length;

            if (targetMethod?.Name == nameof(IEnumerable.GetEnumerator))
            {
                return targetMethod.ReturnType == typeof(IEnumerator<Guid>)
                    ? ((IEnumerable<Guid>)Values).GetEnumerator()
                    : Values.GetEnumerator();
            }

            return base.Invoke(targetMethod, args);
        }
    }
}

using System.Reflection;
using Microsoft.Extensions.DependencyInjection;
using ViciOne.ServiceBus.Courier;
using ViciOne.ServiceBus.DependencyInjection;
using ViciOne.ServiceBus.Tests.Infrastructure.Requirements;
using Xunit;

namespace ViciOne.ServiceBus.Tests.Courier;

public sealed class CourierActivityFactoryContractTests
{
    [Fact]
    [RequirementCoverage("REQ-VSB-COURIER-ACTIVITY-LIFETIME", "factory-method-requires-creation-delegate")]
    public void FactoryMethodConstructors_RejectMissingCreationDelegates()
    {
        Assert.Equal("executeFactory", Assert.Throws<ArgumentNullException>(() =>
            new FactoryMethodExecuteActivityFactory<TestActivity, TestArguments>(null!)).ParamName);
        Assert.Equal("compensateFactory", Assert.Throws<ArgumentNullException>(() =>
            new FactoryMethodCompensateActivityFactory<TestActivity, TestLog>(null!)).ParamName);
        Assert.Equal("executeFactory", Assert.Throws<ArgumentNullException>(() =>
            new FactoryMethodActivityFactory<TestActivity, TestArguments, TestLog>(null!, _ => new TestActivity())).ParamName);
        Assert.Equal("compensateFactory", Assert.Throws<ArgumentNullException>(() =>
            new FactoryMethodActivityFactory<TestActivity, TestArguments, TestLog>(_ => new TestActivity(), null!)).ParamName);
    }

    [Fact]
    [RequirementCoverage("REQ-VSB-COURIER-ACTIVITY-LIFETIME", "default-constructor-factories-are-cached-per-activity-contract")]
    public async Task DefaultConstructorFactories_AreCachedAndCreateFreshActivitiesAsync()
    {
        IExecuteActivityFactory<TestActivity, TestArguments> firstExecute =
            DefaultConstructorExecuteActivityFactory<TestActivity, TestArguments>.ExecuteFactory;
        IExecuteActivityFactory<TestActivity, TestArguments> secondExecute =
            DefaultConstructorExecuteActivityFactory<TestActivity, TestArguments>.ExecuteFactory;
        ICompensateActivityFactory<TestActivity, TestLog> firstCompensate =
            DefaultConstructorCompensateActivityFactory<TestActivity, TestLog>.CompensateFactory;
        ICompensateActivityFactory<TestActivity, TestLog> secondCompensate =
            DefaultConstructorCompensateActivityFactory<TestActivity, TestLog>.CompensateFactory;
        TestActivity? executed = null;
        TestActivity? compensated = null;

        await firstExecute.ExecuteAsync(
            CreateExecuteContext(new TestArguments("execute")),
            new DelegatePipe<ExecuteActivityContext<TestActivity, TestArguments>>(context =>
            {
                executed = context.Activity;
                return Task.CompletedTask;
            }),
            TestContext.Current.CancellationToken);
        await firstCompensate.CompensateAsync(
            CreateCompensateContext(new TestLog("compensate")),
            new DelegatePipe<CompensateActivityContext<TestActivity, TestLog>>(context =>
            {
                compensated = context.Activity;
                return Task.CompletedTask;
            }),
            TestContext.Current.CancellationToken);

        Assert.Same(firstExecute, secondExecute);
        Assert.Same(firstCompensate, secondCompensate);
        Assert.NotNull(executed);
        Assert.NotNull(compensated);
        Assert.NotSame(executed, compensated);
        Assert.True(executed.IsDisposed);
        Assert.True(compensated.IsDisposed);
    }

    [Fact]
    [RequirementCoverage("REQ-VSB-COURIER-ACTIVITY-LIFETIME", "execute-factory-binds-arguments-and-disposes-after-pipeline")]
    public async Task ExecuteFactory_CreatesFromExactArgumentsAndDisposesAfterThePipelineAsync()
    {
        var arguments = new TestArguments("north");
        var activity = new TestActivity();
        ExecuteContext<TestArguments> context = CreateExecuteContext(arguments);
        var observations = new List<string>();
        var factory = new FactoryMethodExecuteActivityFactory<TestActivity, TestArguments>(value =>
        {
            Assert.Same(arguments, value);
            observations.Add("created");
            return activity;
        });

        await factory.ExecuteAsync(context, new DelegatePipe<ExecuteActivityContext<TestActivity, TestArguments>>(activityContext =>
        {
            Assert.Same(activity, activityContext.Activity);
            Assert.False(activity.IsDisposed);
            observations.Add("pipeline");
            return Task.CompletedTask;
        }), TestContext.Current.CancellationToken);

        Assert.True(activity.IsDisposed);
        Assert.Equal(["created", "pipeline"], observations);
    }

    [Fact]
    [RequirementCoverage("REQ-VSB-COURIER-ACTIVITY-LIFETIME", "compensate-factory-binds-log-and-disposes-after-pipeline")]
    public async Task CompensateFactory_CreatesFromExactLogAndDisposesAfterThePipelineAsync()
    {
        var log = new TestLog("charged");
        var activity = new TestActivity();
        CompensateContext<TestLog> context = CreateCompensateContext(log);
        var observations = new List<string>();
        var factory = new FactoryMethodCompensateActivityFactory<TestActivity, TestLog>(value =>
        {
            Assert.Same(log, value);
            observations.Add("created");
            return activity;
        });

        await factory.CompensateAsync(context, new DelegatePipe<CompensateActivityContext<TestActivity, TestLog>>(activityContext =>
        {
            Assert.Same(activity, activityContext.Activity);
            Assert.False(activity.IsDisposed);
            observations.Add("pipeline");
            return Task.CompletedTask;
        }), TestContext.Current.CancellationToken);

        Assert.True(activity.IsDisposed);
        Assert.Equal(["created", "pipeline"], observations);
    }

    [Fact]
    [RequirementCoverage("REQ-VSB-COURIER-ACTIVITY-LIFETIME", "factory-method-disposes-after-downstream-failure")]
    public async Task FactoryMethods_DisposeActivitiesWhenThePipelineFailsAsync()
    {
        var executeActivity = new TestActivity();
        var compensateActivity = new TestActivity();
        var expectedExecute = new ExpectedFactoryException("execute");
        var expectedCompensate = new ExpectedFactoryException("compensate");
        var execute = new FactoryMethodExecuteActivityFactory<TestActivity, TestArguments>(_ => executeActivity);
        var compensate = new FactoryMethodCompensateActivityFactory<TestActivity, TestLog>(_ => compensateActivity);

        Exception executeFailure = await Assert.ThrowsAsync<ExpectedFactoryException>(() => execute.ExecuteAsync(
            CreateExecuteContext(new TestArguments("value")),
            new DelegatePipe<ExecuteActivityContext<TestActivity, TestArguments>>(_ => Task.FromException(expectedExecute)),
            TestContext.Current.CancellationToken));
        Exception compensateFailure = await Assert.ThrowsAsync<ExpectedFactoryException>(() => compensate.CompensateAsync(
            CreateCompensateContext(new TestLog("value")),
            new DelegatePipe<CompensateActivityContext<TestActivity, TestLog>>(_ => Task.FromException(expectedCompensate)),
            TestContext.Current.CancellationToken));

        Assert.Same(expectedExecute, executeFailure);
        Assert.Same(expectedCompensate, compensateFailure);
        Assert.True(executeActivity.IsDisposed);
        Assert.True(compensateActivity.IsDisposed);
    }

    [Fact]
    [RequirementCoverage("REQ-VSB-COURIER-ACTIVITY-LIFETIME", "async-disposal-is-preferred-and-awaited")]
    public async Task FactoryMethods_PreferAndAwaitAsynchronousDisposalAsync()
    {
        var activity = new DualDisposableActivity();
        var factory = new FactoryMethodExecuteActivityFactory<DualDisposableActivity, TestArguments>(_ => activity);

        await factory.ExecuteAsync(
            CreateExecuteContext(new TestArguments("value")),
            new DelegatePipe<ExecuteActivityContext<DualDisposableActivity, TestArguments>>(_ => Task.CompletedTask),
            TestContext.Current.CancellationToken);

        Assert.Equal(0, activity.SynchronousDisposeCount);
        Assert.Equal(1, activity.AsynchronousDisposeCount);
    }

    [Fact]
    [RequirementCoverage("REQ-VSB-COURIER-ACTIVITY-LIFETIME", "invalid-factory-output-has-explicit-diagnostic")]
    public async Task FactoryMethods_RejectNullActivitiesWithExplicitDiagnosticsAsync()
    {
        var execute = new FactoryMethodExecuteActivityFactory<TestActivity, TestArguments>(_ => null!);
        var compensate = new FactoryMethodCompensateActivityFactory<TestActivity, TestLog>(_ => null!);

        InvalidOperationException executeFailure = await Assert.ThrowsAsync<InvalidOperationException>(() => execute.ExecuteAsync(
            CreateExecuteContext(new TestArguments("value")),
            new DelegatePipe<ExecuteActivityContext<TestActivity, TestArguments>>(_ => Task.CompletedTask),
            TestContext.Current.CancellationToken));
        InvalidOperationException compensateFailure = await Assert.ThrowsAsync<InvalidOperationException>(() => compensate.CompensateAsync(
            CreateCompensateContext(new TestLog("value")),
            new DelegatePipe<CompensateActivityContext<TestActivity, TestLog>>(_ => Task.CompletedTask),
            TestContext.Current.CancellationToken));

        Assert.Contains("returned null", executeFailure.Message, StringComparison.OrdinalIgnoreCase);
        Assert.Contains("returned null", compensateFailure.Message, StringComparison.OrdinalIgnoreCase);
    }

    [Fact]
    [RequirementCoverage("REQ-VSB-COURIER-CANCELLATION", "pre-canceled-factory-skips-creation-provider-and-pipeline")]
    public async Task ActivityFactories_WithPreCanceledTokenHaveNoSideEffectsAsync()
    {
        using var cancellation = new CancellationTokenSource();
        await cancellation.CancelAsync();
        int creationCount = 0;
        int pipelineCount = 0;
        var direct = new FactoryMethodExecuteActivityFactory<TestActivity, TestArguments>(_ =>
        {
            creationCount++;
            return new TestActivity();
        });
        var directCompensate = new FactoryMethodCompensateActivityFactory<TestActivity, TestLog>(_ =>
        {
            creationCount++;
            return new TestActivity();
        });
        var executeScopeProvider = new RecordingExecuteScopeProvider();
        var compensateScopeProvider = new RecordingCompensateScopeProvider();
        var scopedExecute = new ScopeExecuteActivityFactory<TestActivity, TestArguments>(executeScopeProvider);
        var scopedCompensate = new ScopeCompensateActivityFactory<TestActivity, TestLog>(compensateScopeProvider);
        var executePipe = new DelegatePipe<ExecuteActivityContext<TestActivity, TestArguments>>(_ =>
        {
            pipelineCount++;
            return Task.CompletedTask;
        });
        var compensatePipe = new DelegatePipe<CompensateActivityContext<TestActivity, TestLog>>(_ =>
        {
            pipelineCount++;
            return Task.CompletedTask;
        });
        ExecuteContext<TestArguments> executeContext = CreateExecuteContext(new TestArguments("value"));
        CompensateContext<TestLog> compensateContext = CreateCompensateContext(new TestLog("value"));

        await Assert.ThrowsAnyAsync<OperationCanceledException>(() => direct.ExecuteAsync(executeContext, executePipe, cancellation.Token));
        await Assert.ThrowsAnyAsync<OperationCanceledException>(() => directCompensate.CompensateAsync(compensateContext, compensatePipe, cancellation.Token));
        await Assert.ThrowsAnyAsync<OperationCanceledException>(() => scopedExecute.ExecuteAsync(executeContext, executePipe, cancellation.Token));
        await Assert.ThrowsAnyAsync<OperationCanceledException>(() => scopedCompensate.CompensateAsync(compensateContext, compensatePipe, cancellation.Token));

        Assert.Equal(0, creationCount);
        Assert.Equal(0, executeScopeProvider.GetActivityScopeCount);
        Assert.Equal(0, compensateScopeProvider.GetActivityScopeCount);
        Assert.Equal(0, pipelineCount);
    }

    [Fact]
    [RequirementCoverage("REQ-VSB-COURIER-ACTIVITY-LIFETIME", "scoped-factory-disposes-scope-after-success-and-failure")]
    public async Task ScopedFactory_DisposesTheAcquiredScopeAfterSuccessAndFailureAsync()
    {
        var firstScope = new RecordingExecuteScopeContext(CreateExecuteActivityContext(new TestActivity(), new TestArguments("first")));
        var secondScope = new RecordingExecuteScopeContext(CreateExecuteActivityContext(new TestActivity(), new TestArguments("second")));
        var provider = new RecordingExecuteScopeProvider(firstScope, secondScope);
        var factory = new ScopeExecuteActivityFactory<TestActivity, TestArguments>(provider);
        var expected = new ExpectedFactoryException("pipeline");

        await factory.ExecuteAsync(
            CreateExecuteContext(new TestArguments("first")),
            new DelegatePipe<ExecuteActivityContext<TestActivity, TestArguments>>(_ => Task.CompletedTask),
            TestContext.Current.CancellationToken);
        Exception failure = await Assert.ThrowsAsync<ExpectedFactoryException>(() => factory.ExecuteAsync(
            CreateExecuteContext(new TestArguments("second")),
            new DelegatePipe<ExecuteActivityContext<TestActivity, TestArguments>>(_ => Task.FromException(expected)),
            TestContext.Current.CancellationToken));

        Assert.Same(expected, failure);
        Assert.Equal(1, firstScope.DisposeCount);
        Assert.Equal(1, secondScope.DisposeCount);
    }

    [Fact]
    [RequirementCoverage("REQ-VSB-COURIER-ACTIVITY-LIFETIME", "scoped-factory-rejects-null-scope-result")]
    public async Task ScopedFactory_RejectsANullScopeWithAnExplicitDiagnosticAsync()
    {
        var executeProvider = new RecordingExecuteScopeProvider((IExecuteActivityScopeContext<TestActivity, TestArguments>)null!);
        var compensateProvider = new RecordingCompensateScopeProvider((ICompensateActivityScopeContext<TestActivity, TestLog>)null!);
        var execute = new ScopeExecuteActivityFactory<TestActivity, TestArguments>(executeProvider);
        var compensate = new ScopeCompensateActivityFactory<TestActivity, TestLog>(compensateProvider);

        InvalidOperationException executeException = await Assert.ThrowsAsync<InvalidOperationException>(() => execute.ExecuteAsync(
            CreateExecuteContext(new TestArguments("value")),
            new DelegatePipe<ExecuteActivityContext<TestActivity, TestArguments>>(_ => Task.CompletedTask),
            TestContext.Current.CancellationToken));
        InvalidOperationException compensateException = await Assert.ThrowsAsync<InvalidOperationException>(() => compensate.CompensateAsync(
            CreateCompensateContext(new TestLog("value")),
            new DelegatePipe<CompensateActivityContext<TestActivity, TestLog>>(_ => Task.CompletedTask),
            TestContext.Current.CancellationToken));

        Assert.Contains("scope provider returned null", executeException.Message, StringComparison.OrdinalIgnoreCase);
        Assert.Contains("scope provider returned null", compensateException.Message, StringComparison.OrdinalIgnoreCase);
    }

    [Fact]
    [RequirementCoverage("REQ-VSB-COURIER-ACTIVITY-LIFETIME", "created-activity-scopes-resolve-services-and-release-owned-scope")]
    public async Task CreatedActivityScopeContexts_ResolveServicesAndReleaseBothOwnedResourcesAsync()
    {
        var executeDependency = new TrackedScopedDependency();
        var compensateDependency = new TrackedScopedDependency();
        await using ServiceProvider executeProvider = new ServiceCollection()
            .AddScoped(_ => executeDependency)
            .BuildServiceProvider(validateScopes: true);
        await using ServiceProvider compensateProvider = new ServiceCollection()
            .AddScoped(_ => compensateDependency)
            .BuildServiceProvider(validateScopes: true);
        IServiceScope executeScope = executeProvider.CreateScope();
        IServiceScope compensateScope = compensateProvider.CreateScope();
        var executeRestore = new RecordingDisposable();
        var compensateRestore = new RecordingDisposable();
        var execute = new CreatedExecuteActivityScopeContext<TestActivity, TestArguments>(
            CreateExecuteActivityContext(new TestActivity(), new TestArguments("execute")),
            executeScope,
            executeRestore);
        var compensate = new CreatedCompensateActivityScopeContext<TestActivity, TestLog>(
            CreateCompensateActivityContext(new TestActivity(), new TestLog("compensate")),
            compensateScope,
            compensateRestore);

        Assert.Same(executeDependency, execute.GetService<TrackedScopedDependency>());
        Assert.Same(compensateDependency, compensate.GetService<TrackedScopedDependency>());
        Assert.NotNull(execute.GetService<UnregisteredDependency>());
        Assert.NotNull(compensate.GetService<UnregisteredDependency>());
        await execute.DisposeAsync();
        await compensate.DisposeAsync();

        Assert.Equal(1, executeRestore.DisposeCount);
        Assert.Equal(1, compensateRestore.DisposeCount);
        Assert.Equal(1, executeDependency.DisposeCount);
        Assert.Equal(1, compensateDependency.DisposeCount);
    }

    [Fact]
    [RequirementCoverage("REQ-VSB-COURIER-ACTIVITY-LIFETIME", "existing-activity-scopes-restore-context-without-owning-scope")]
    public async Task ExistingActivityScopeContexts_RestoreContextWithoutDisposingTheBorrowedScopeAsync()
    {
        var executeDependency = new TrackedScopedDependency();
        var compensateDependency = new TrackedScopedDependency();
        await using ServiceProvider executeProvider = new ServiceCollection()
            .AddScoped(_ => executeDependency)
            .BuildServiceProvider(validateScopes: true);
        await using ServiceProvider compensateProvider = new ServiceCollection()
            .AddScoped(_ => compensateDependency)
            .BuildServiceProvider(validateScopes: true);
        AsyncServiceScope executeScope = executeProvider.CreateAsyncScope();
        AsyncServiceScope compensateScope = compensateProvider.CreateAsyncScope();
        var executeRestore = new RecordingDisposable();
        var compensateRestore = new RecordingDisposable();
        var execute = new ExistingExecuteActivityScopeContext<TestActivity, TestArguments>(
            CreateExecuteActivityContext(new TestActivity(), new TestArguments("execute")),
            executeScope,
            executeRestore);
        var compensate = new ExistingCompensateActivityScopeContext<TestActivity, TestLog>(
            CreateCompensateActivityContext(new TestActivity(), new TestLog("compensate")),
            compensateScope,
            compensateRestore);

        Assert.Same(executeDependency, execute.GetService<TrackedScopedDependency>());
        Assert.Same(compensateDependency, compensate.GetService<TrackedScopedDependency>());
        await execute.DisposeAsync();
        await compensate.DisposeAsync();

        Assert.Equal(1, executeRestore.DisposeCount);
        Assert.Equal(1, compensateRestore.DisposeCount);
        Assert.Equal(0, executeDependency.DisposeCount);
        Assert.Equal(0, compensateDependency.DisposeCount);

        await executeScope.DisposeAsync();
        await compensateScope.DisposeAsync();
        Assert.Equal(1, executeDependency.DisposeCount);
        Assert.Equal(1, compensateDependency.DisposeCount);
    }

    [Fact]
    [RequirementCoverage("REQ-VSB-COURIER-ACTIVITY-LIFETIME", "activity-scope-contexts-require-context-scope-and-restoration")]
    public void ActivityScopeContexts_RejectMissingRequiredState()
    {
        using ServiceProvider provider = new ServiceCollection().BuildServiceProvider();
        using IServiceScope scope = provider.CreateScope();
        var restoration = new RecordingDisposable();
        ExecuteActivityContext<TestActivity, TestArguments> executeContext =
            CreateExecuteActivityContext(new TestActivity(), new TestArguments("value"));
        CompensateActivityContext<TestActivity, TestLog> compensateContext =
            CreateCompensateActivityContext(new TestActivity(), new TestLog("value"));

        Assert.Equal("context", Assert.Throws<ArgumentNullException>(() =>
            new CreatedExecuteActivityScopeContext<TestActivity, TestArguments>(null!, scope, restoration)).ParamName);
        Assert.Equal("scope", Assert.Throws<ArgumentNullException>(() =>
            new CreatedExecuteActivityScopeContext<TestActivity, TestArguments>(executeContext, null!, restoration)).ParamName);
        Assert.Equal("disposable", Assert.Throws<ArgumentNullException>(() =>
            new CreatedExecuteActivityScopeContext<TestActivity, TestArguments>(executeContext, scope, null!)).ParamName);
        Assert.Equal("context", Assert.Throws<ArgumentNullException>(() =>
            new ExistingCompensateActivityScopeContext<TestActivity, TestLog>(null!, scope, restoration)).ParamName);
        Assert.Equal("scope", Assert.Throws<ArgumentNullException>(() =>
            new ExistingCompensateActivityScopeContext<TestActivity, TestLog>(compensateContext, null!, restoration)).ParamName);
        Assert.Equal("disposable", Assert.Throws<ArgumentNullException>(() =>
            new ExistingCompensateActivityScopeContext<TestActivity, TestLog>(compensateContext, scope, null!)).ParamName);
    }

    [Fact]
    [RequirementCoverage("REQ-VSB-COURIER-ACTIVITY-LIFETIME", "owned-scope-is-released-when-context-restoration-fails")]
    public async Task CreatedActivityScopeContexts_ReleaseOwnedScopesWhenRestorationFailsAsync()
    {
        var executeScope = new RecordingServiceScope();
        var compensateScope = new RecordingServiceScope();
        var expectedExecute = new ExpectedFactoryException("execute restoration");
        var expectedCompensate = new ExpectedFactoryException("compensate restoration");
        var execute = new CreatedExecuteActivityScopeContext<TestActivity, TestArguments>(
            CreateExecuteActivityContext(new TestActivity(), new TestArguments("execute")),
            executeScope,
            new ThrowingDisposable(expectedExecute));
        var compensate = new CreatedCompensateActivityScopeContext<TestActivity, TestLog>(
            CreateCompensateActivityContext(new TestActivity(), new TestLog("compensate")),
            compensateScope,
            new ThrowingDisposable(expectedCompensate));

        Exception executeFailure = await Assert.ThrowsAsync<ExpectedFactoryException>(async () =>
            await execute.DisposeAsync());
        Exception compensateFailure = await Assert.ThrowsAsync<ExpectedFactoryException>(async () =>
            await compensate.DisposeAsync());

        Assert.Same(expectedExecute, executeFailure);
        Assert.Same(expectedCompensate, compensateFailure);
        Assert.Equal(1, executeScope.DisposeCount);
        Assert.Equal(1, compensateScope.DisposeCount);
    }

    [Fact]
    [RequirementCoverage("REQ-VSB-COURIER-ACTIVITY-LIFETIME", "all-owned-scope-cleanup-failures-remain-observable")]
    public async Task CreatedActivityScopeContext_PreservesRestorationAndScopeFailuresAsync()
    {
        var restorationFailure = new ExpectedFactoryException("restoration");
        var scopeFailure = new ExpectedFactoryException("scope");
        var scope = new ThrowingServiceScope(scopeFailure);
        var context = new CreatedExecuteActivityScopeContext<TestActivity, TestArguments>(
            CreateExecuteActivityContext(new TestActivity(), new TestArguments("execute")),
            scope,
            new ThrowingDisposable(restorationFailure));

        AggregateException failure = await Assert.ThrowsAsync<AggregateException>(async () =>
            await context.DisposeAsync());

        Assert.Equal([restorationFailure, scopeFailure], failure.InnerExceptions);
        Assert.Equal(1, scope.DisposeCount);
    }

    private static ExecuteContext<TestArguments> CreateExecuteContext(TestArguments arguments)
    {
        TestExecuteContext context = DispatchProxy.Create<TestExecuteContext, ExecuteContextProxy>();
        ((ExecuteContextProxy)(object)context).Arguments = arguments;
        return context;
    }

    private static CompensateContext<TestLog> CreateCompensateContext(TestLog log)
    {
        TestCompensateContext context = DispatchProxy.Create<TestCompensateContext, CompensateContextProxy>();
        ((CompensateContextProxy)(object)context).Log = log;
        return context;
    }

    private static ExecuteActivityContext<TActivity, TestArguments> CreateExecuteActivityContext<TActivity>(TActivity activity, TestArguments arguments)
        where TActivity : class
    {
        TestExecuteActivityContext<TActivity> context = DispatchProxy.Create<TestExecuteActivityContext<TActivity>, ExecuteActivityContextProxy<TActivity>>();
        var proxy = (ExecuteActivityContextProxy<TActivity>)(object)context;
        proxy.Activity = activity;
        proxy.Arguments = arguments;
        return context;
    }

    private static CompensateActivityContext<TActivity, TestLog> CreateCompensateActivityContext<TActivity>(TActivity activity, TestLog log)
        where TActivity : class
    {
        TestCompensateActivityContext<TActivity> context = DispatchProxy.Create<TestCompensateActivityContext<TActivity>, CompensateActivityContextProxy<TActivity>>();
        var proxy = (CompensateActivityContextProxy<TActivity>)(object)context;
        proxy.Activity = activity;
        proxy.Log = log;
        return context;
    }

    private interface TestExecuteContext : ExecuteContext<TestArguments>;

    private interface TestCompensateContext : CompensateContext<TestLog>;

    private interface TestExecuteActivityContext<out TActivity> : ExecuteActivityContext<TActivity, TestArguments>
        where TActivity : class;

    private interface TestCompensateActivityContext<out TActivity> : CompensateActivityContext<TActivity, TestLog>
        where TActivity : class;

    private class ExecuteContextProxy : DispatchProxy
    {
        private static readonly MethodInfo CreateContextMethod = typeof(ExecuteContextProxy)
            .GetMethod(nameof(CreateExecuteActivityContext), BindingFlags.NonPublic | BindingFlags.Static)!;

        public TestArguments Arguments { get; set; } = null!;

        protected override object? Invoke(MethodInfo? targetMethod, object?[]? args) => targetMethod?.Name switch
        {
            "get_Arguments" => Arguments,
            "CreateActivityContext" => CreateContextMethod
                .MakeGenericMethod(targetMethod.GetGenericArguments()[0])
                .Invoke(null, [args![0]!, Arguments]),
            _ => throw new NotSupportedException($"Unexpected execute-context member: {targetMethod?.Name}"),
        };

        private static object CreateExecuteActivityContext<TActivity>(object activity, TestArguments arguments)
            where TActivity : class
        {
            TestExecuteActivityContext<TActivity> context = DispatchProxy.Create<TestExecuteActivityContext<TActivity>, ExecuteActivityContextProxy<TActivity>>();
            var proxy = (ExecuteActivityContextProxy<TActivity>)(object)context;
            proxy.Activity = Assert.IsType<TActivity>(activity);
            proxy.Arguments = arguments;
            return context;
        }
    }

    private class CompensateContextProxy : DispatchProxy
    {
        private static readonly MethodInfo CreateContextMethod = typeof(CompensateContextProxy)
            .GetMethod(nameof(CreateCompensateActivityContext), BindingFlags.NonPublic | BindingFlags.Static)!;

        public TestLog Log { get; set; } = null!;

        protected override object? Invoke(MethodInfo? targetMethod, object?[]? args) => targetMethod?.Name switch
        {
            "get_Log" => Log,
            "CreateActivityContext" => CreateContextMethod
                .MakeGenericMethod(targetMethod.GetGenericArguments()[0])
                .Invoke(null, [args![0]!, Log]),
            _ => throw new NotSupportedException($"Unexpected compensate-context member: {targetMethod?.Name}"),
        };

        private static object CreateCompensateActivityContext<TActivity>(object activity, TestLog log)
            where TActivity : class
        {
            TestCompensateActivityContext<TActivity> context = DispatchProxy.Create<TestCompensateActivityContext<TActivity>, CompensateActivityContextProxy<TActivity>>();
            var proxy = (CompensateActivityContextProxy<TActivity>)(object)context;
            proxy.Activity = Assert.IsType<TActivity>(activity);
            proxy.Log = log;
            return context;
        }
    }

    private class ExecuteActivityContextProxy<TActivity> : DispatchProxy
        where TActivity : class
    {
        public TActivity Activity { get; set; } = null!;
        public TestArguments Arguments { get; set; } = null!;

        protected override object? Invoke(MethodInfo? targetMethod, object?[]? args) => targetMethod?.Name switch
        {
            "get_Activity" => Activity,
            "get_Arguments" => Arguments,
            _ => throw new NotSupportedException($"Unexpected execute-activity-context member: {targetMethod?.Name}"),
        };
    }

    private class CompensateActivityContextProxy<TActivity> : DispatchProxy
        where TActivity : class
    {
        public TActivity Activity { get; set; } = null!;
        public TestLog Log { get; set; } = null!;

        protected override object? Invoke(MethodInfo? targetMethod, object?[]? args) => targetMethod?.Name switch
        {
            "get_Activity" => Activity,
            "get_Log" => Log,
            _ => throw new NotSupportedException($"Unexpected compensate-activity-context member: {targetMethod?.Name}"),
        };
    }

    private sealed class DelegatePipe<TContext>(Func<TContext, Task> callback) : IPipe<TContext>
        where TContext : class, PipeContext
    {
        public Task SendAsync(TContext context) => callback(context);

        public void Probe(ProbeContext context) => ArgumentNullException.ThrowIfNull(context);
    }

    private sealed class RecordingExecuteScopeProvider(params IExecuteActivityScopeContext<TestActivity, TestArguments>[] scopes) :
        IExecuteActivityScopeProvider<TestActivity, TestArguments>
    {
        private readonly Queue<IExecuteActivityScopeContext<TestActivity, TestArguments>> _scopes = new(scopes);

        public int GetActivityScopeCount { get; private set; }

        public ValueTask<IExecuteScopeContext<TestArguments>> GetScopeAsync(
            ExecuteContext<TestArguments> context,
            CancellationToken cancellationToken = default) => throw new NotSupportedException();

        public ValueTask<IExecuteActivityScopeContext<TestActivity, TestArguments>> GetActivityScopeAsync(
            ExecuteContext<TestArguments> context,
            CancellationToken cancellationToken = default)
        {
            GetActivityScopeCount++;
            return ValueTask.FromResult(_scopes.Dequeue());
        }

        public void Probe(ProbeContext context) => ArgumentNullException.ThrowIfNull(context);
    }

    private sealed class RecordingExecuteScopeContext(ExecuteActivityContext<TestActivity, TestArguments> context) :
        IExecuteActivityScopeContext<TestActivity, TestArguments>
    {
        public int DisposeCount { get; private set; }

        public ExecuteActivityContext<TestActivity, TestArguments> Context { get; } = context;

        public ValueTask DisposeAsync()
        {
            DisposeCount++;
            return ValueTask.CompletedTask;
        }

        public T GetService<T>() where T : class => throw new NotSupportedException();
    }

    private sealed class RecordingCompensateScopeProvider(params ICompensateActivityScopeContext<TestActivity, TestLog>[] scopes) :
        ICompensateActivityScopeProvider<TestActivity, TestLog>
    {
        private readonly Queue<ICompensateActivityScopeContext<TestActivity, TestLog>> _scopes = new(scopes);

        public int GetActivityScopeCount { get; private set; }

        public ValueTask<ICompensateScopeContext<TestLog>> GetScopeAsync(
            CompensateContext<TestLog> context,
            CancellationToken cancellationToken = default) => throw new NotSupportedException();

        public ValueTask<ICompensateActivityScopeContext<TestActivity, TestLog>> GetActivityScopeAsync(
            CompensateContext<TestLog> context,
            CancellationToken cancellationToken = default)
        {
            GetActivityScopeCount++;
            return ValueTask.FromResult(_scopes.Dequeue());
        }

        public void Probe(ProbeContext context) => ArgumentNullException.ThrowIfNull(context);
    }

    private sealed record TestArguments(string Value);

    private sealed record TestLog(string Value);

    private class TestActivity : IExecuteActivity<TestArguments>, ICompensateActivity<TestLog>, IDisposable
    {
        public bool IsDisposed { get; private set; }

        public Task<ExecutionResult> ExecuteAsync(ExecuteContext<TestArguments> context) => throw new NotSupportedException();

        public Task<CompensationResult> CompensateAsync(CompensateContext<TestLog> context) => throw new NotSupportedException();

        public void Dispose() => IsDisposed = true;
    }

    private sealed class DualDisposableActivity : IExecuteActivity<TestArguments>, IDisposable, IAsyncDisposable
    {
        public int SynchronousDisposeCount { get; private set; }
        public int AsynchronousDisposeCount { get; private set; }

        public Task<ExecutionResult> ExecuteAsync(ExecuteContext<TestArguments> context) => throw new NotSupportedException();

        public void Dispose() => SynchronousDisposeCount++;

        public ValueTask DisposeAsync()
        {
            AsynchronousDisposeCount++;
            return ValueTask.CompletedTask;
        }
    }

    private sealed class RecordingDisposable : IDisposable
    {
        public int DisposeCount { get; private set; }

        public void Dispose() => DisposeCount++;
    }

    private sealed class ThrowingDisposable(Exception exception) : IDisposable
    {
        public void Dispose() => throw exception;
    }

    private sealed class RecordingServiceScope : IServiceScope
    {
        public int DisposeCount { get; private set; }

        public IServiceProvider ServiceProvider { get; } = new ServiceCollection().BuildServiceProvider();

        public void Dispose() => DisposeCount++;
    }

    private sealed class ThrowingServiceScope(Exception exception) : IServiceScope
    {
        public int DisposeCount { get; private set; }

        public IServiceProvider ServiceProvider { get; } = new ServiceCollection().BuildServiceProvider();

        public void Dispose()
        {
            DisposeCount++;
            throw exception;
        }
    }

    private sealed class TrackedScopedDependency : IAsyncDisposable
    {
        public int DisposeCount { get; private set; }

        public ValueTask DisposeAsync()
        {
            DisposeCount++;
            return ValueTask.CompletedTask;
        }
    }

    private sealed class UnregisteredDependency;

    private sealed class ExpectedFactoryException(string message) : Exception(message);
}

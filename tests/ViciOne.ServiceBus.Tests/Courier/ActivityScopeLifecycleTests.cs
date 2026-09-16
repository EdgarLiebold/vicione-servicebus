using System.Reflection;
using Microsoft.Extensions.DependencyInjection;
using ViciOne.ServiceBus.DependencyInjection;
using ViciOne.ServiceBus.Tests.Infrastructure.Requirements;
using Xunit;

namespace ViciOne.ServiceBus.Tests.Courier;

public sealed class ActivityScopeLifecycleTests
{
    [Fact]
    [RequirementCoverage("REQ-VSB-COURIER-ACTIVITY-SCOPE-LIFECYCLE", "constructors-reject-missing-dependencies")]
    public void Constructors_RejectMissingDependencies()
    {
        var events = new List<string>();
        var scope = new RecordingAsyncScope(events);
        var restore = new RecordingDisposable(events);
        ExecuteActivityContext<TestActivity, Arguments> execute = Proxy<ExecuteActivityContext<TestActivity, Arguments>>();
        CompensateActivityContext<TestActivity, Log> compensate = Proxy<CompensateActivityContext<TestActivity, Log>>();

        AssertParameter("restoreContext", () => new ActivityScopeLifetime(null!));
        AssertParameter("context", () => new CreatedExecuteActivityScopeContext<TestActivity, Arguments>(null!, scope, restore));
        AssertParameter("scope", () => new CreatedExecuteActivityScopeContext<TestActivity, Arguments>(execute, null!, restore));
        AssertParameter("disposable", () => new CreatedExecuteActivityScopeContext<TestActivity, Arguments>(execute, scope, null!));
        AssertParameter("context", () => new ExistingExecuteActivityScopeContext<TestActivity, Arguments>(null!, scope, restore));
        AssertParameter("scope", () => new ExistingExecuteActivityScopeContext<TestActivity, Arguments>(execute, null!, restore));
        AssertParameter("disposable", () => new ExistingExecuteActivityScopeContext<TestActivity, Arguments>(execute, scope, null!));
        AssertParameter("context", () => new CreatedCompensateActivityScopeContext<TestActivity, Log>(null!, scope, restore));
        AssertParameter("scope", () => new CreatedCompensateActivityScopeContext<TestActivity, Log>(compensate, null!, restore));
        AssertParameter("disposable", () => new CreatedCompensateActivityScopeContext<TestActivity, Log>(compensate, scope, null!));
        AssertParameter("context", () => new ExistingCompensateActivityScopeContext<TestActivity, Log>(null!, scope, restore));
        AssertParameter("scope", () => new ExistingCompensateActivityScopeContext<TestActivity, Log>(compensate, null!, restore));
        AssertParameter("disposable", () => new ExistingCompensateActivityScopeContext<TestActivity, Log>(compensate, scope, null!));
    }

    [Fact]
    [RequirementCoverage("REQ-VSB-COURIER-ACTIVITY-SCOPE-LIFECYCLE", "created-scopes-restore-then-release-once")]
    public async Task CreatedScopes_RestoreThenReleaseExactlyOnceAsync()
    {
        var service = new Service();
        var services = new RecordingServiceProvider(service);
        var compensateEvents = new List<string>();
        var compensateScope = new RecordingAsyncScope(compensateEvents, services);
        var compensateRestore = new RecordingDisposable(compensateEvents);
        var compensate = new CreatedCompensateActivityScopeContext<TestActivity, Log>(
            Proxy<CompensateActivityContext<TestActivity, Log>>(),
            compensateScope,
            compensateRestore);

        Assert.Same(service, compensate.GetService<Service>());
        await Task.WhenAll(Enumerable.Range(0, 16).Select(_ => compensate.DisposeAsync().AsTask()));
        Assert.Equal(["restore", "scope-async"], compensateEvents);
        Assert.Equal(1, compensateRestore.DisposeCount);
        Assert.Equal(1, compensateScope.DisposeCount);

        var executeEvents = new List<string>();
        var executeScope = new RecordingSyncScope(executeEvents, services);
        var executeRestore = new RecordingDisposable(executeEvents);
        var execute = new CreatedExecuteActivityScopeContext<TestActivity, Arguments>(
            Proxy<ExecuteActivityContext<TestActivity, Arguments>>(),
            executeScope,
            executeRestore);

        Assert.Same(service, execute.GetService<Service>());
        await execute.DisposeAsync();
        await execute.DisposeAsync();
        Assert.Equal(["restore", "scope-sync"], executeEvents);
    }

    [Fact]
    [RequirementCoverage("REQ-VSB-COURIER-ACTIVITY-SCOPE-LIFECYCLE", "borrowed-scopes-restore-once-without-release")]
    public async Task ExistingScopes_RestoreOnceWithoutReleasingBorrowedScopeAsync()
    {
        var service = new Service();
        var services = new RecordingServiceProvider(service);
        var compensateEvents = new List<string>();
        var compensateScope = new RecordingAsyncScope(compensateEvents, services);
        var compensateRestore = new RecordingDisposable(compensateEvents);
        var compensate = new ExistingCompensateActivityScopeContext<TestActivity, Log>(
            Proxy<CompensateActivityContext<TestActivity, Log>>(),
            compensateScope,
            compensateRestore);

        Assert.Same(service, compensate.GetService<Service>());
        await compensate.DisposeAsync();
        await compensate.DisposeAsync();
        Assert.Equal(["restore"], compensateEvents);
        Assert.Equal(0, compensateScope.DisposeCount);

        var executeEvents = new List<string>();
        var executeScope = new RecordingAsyncScope(executeEvents, services);
        var executeRestore = new RecordingDisposable(executeEvents);
        var execute = new ExistingExecuteActivityScopeContext<TestActivity, Arguments>(
            Proxy<ExecuteActivityContext<TestActivity, Arguments>>(),
            executeScope,
            executeRestore);

        Assert.Same(service, execute.GetService<Service>());
        await Task.WhenAll(Enumerable.Range(0, 16).Select(_ => execute.DisposeAsync().AsTask()));
        Assert.Equal(["restore"], executeEvents);
        Assert.Equal(0, executeScope.DisposeCount);
    }

    [Fact]
    [RequirementCoverage("REQ-VSB-COURIER-ACTIVITY-SCOPE-LIFECYCLE", "cleanup-attempts-both-and-preserves-failures")]
    public async Task Cleanup_AttemptsBothStagesAndPreservesFailuresAsync()
    {
        var restoreFailure = new RestoreFailure();
        var scopeFailure = new ScopeFailure();
        var scope = new ThrowingAsyncScope(scopeFailure);
        var lifetime = new ActivityScopeLifetime(new ThrowingDisposable(restoreFailure), scope);

        AggregateException aggregate = await Assert.ThrowsAsync<AggregateException>(async () => await lifetime.DisposeAsync());

        Assert.Equal([restoreFailure, scopeFailure], aggregate.InnerExceptions);
        Assert.Equal(1, scope.DisposeCount);
        await lifetime.DisposeAsync();

        var ownedRestoreFailure = new RestoreFailure();
        var ownedRestoreOnly = new ActivityScopeLifetime(
            new ThrowingDisposable(ownedRestoreFailure),
            new RecordingAsyncScope([]));
        Assert.Same(ownedRestoreFailure,
            await Assert.ThrowsAsync<RestoreFailure>(async () => await ownedRestoreOnly.DisposeAsync()));

        var loneRestoreFailure = new RestoreFailure();
        var restoreOnly = new ActivityScopeLifetime(new ThrowingDisposable(loneRestoreFailure));
        Assert.Same(loneRestoreFailure, await Assert.ThrowsAsync<RestoreFailure>(async () => await restoreOnly.DisposeAsync()));

        var loneScopeFailure = new ScopeFailure();
        var scopeOnly = new ActivityScopeLifetime(new RecordingDisposable([]), new ThrowingAsyncScope(loneScopeFailure));
        Assert.Same(loneScopeFailure, await Assert.ThrowsAsync<ScopeFailure>(async () => await scopeOnly.DisposeAsync()));
    }

    [Fact]
    [RequirementCoverage("REQ-VSB-COURIER-ACTIVITY-SCOPE-LIFECYCLE", "providers-and-factories-enforce-boundaries-and-forward")]
    public async Task ProvidersAndFactories_EnforceBoundariesAndForwardAsync()
    {
        TestRegistrationContext registrationContext = Proxy<TestRegistrationContext>();
        _ = new ExecuteActivityScopeProvider<TestActivity, Arguments>(registrationContext);
        _ = new CompensateActivityScopeProvider<TestActivity, Log>(registrationContext);

        var setter = new RecordingSetter();
        var executeProvider = new ExecuteActivityScopeProvider<TestActivity, Arguments>(new RecordingServiceProvider(), setter);
        var compensateProvider = new CompensateActivityScopeProvider<TestActivity, Log>(new RecordingServiceProvider(), setter);
        using var cancellation = new CancellationTokenSource();
        cancellation.Cancel();
        CancellationToken token = cancellation.Token;

        AssertParameter("context", () => executeProvider.Probe(null!));
        AssertParameter("context", () => compensateProvider.Probe(null!));
        ProbeContext probe = Proxy<ProbeContext>();
        executeProvider.Probe(probe);
        compensateProvider.Probe(probe);
        await AssertCanceledAsync(executeProvider.GetScopeAsync(Proxy<ExecuteContext<Arguments>>(), token), token);
        await AssertCanceledAsync(executeProvider.GetActivityScopeAsync(Proxy<ExecuteContext<Arguments>>(), token), token);
        await AssertCanceledAsync(compensateProvider.GetScopeAsync(Proxy<CompensateContext<Log>>(), token), token);
        await AssertCanceledAsync(compensateProvider.GetActivityScopeAsync(Proxy<CompensateContext<Log>>(), token), token);

        AssertParameter("scopeProvider", () => new ScopeExecuteActivityFactory<TestActivity, Arguments>(null!));
        AssertParameter("scopeProvider", () => new ScopeCompensateActivityFactory<TestActivity, Log>(null!));
        var executeFactory = new ScopeExecuteActivityFactory<TestActivity, Arguments>(
            Proxy<IExecuteActivityScopeProvider<TestActivity, Arguments>>());
        AssertParameter("context", () => executeFactory.Probe(null!));
        executeFactory.Probe(probe);

        CompensateActivityContext<TestActivity, Log> activityContext = Proxy<CompensateActivityContext<TestActivity, Log>>();
        var activityScope = new RecordingCompensateActivityScope(activityContext);
        var activityProvider = new RecordingCompensateActivityScopeProvider(activityScope);
        var compensateFactory = new ScopeCompensateActivityFactory<TestActivity, Log>(activityProvider);
        var next = new RecordingPipe<CompensateActivityContext<TestActivity, Log>>();

        await compensateFactory.CompensateAsync(Proxy<CompensateContext<Log>>(), next, TestContext.Current.CancellationToken);
        Assert.Same(activityContext, next.Context);
        Assert.Equal(1, activityScope.DisposeCount);
        AssertParameter("context", () => compensateFactory.Probe(null!));
        compensateFactory.Probe(probe);
        Assert.Equal(1, activityProvider.ProbeCount);
    }

    private static T Proxy<T>() where T : class => DispatchProxy.Create<T, EmptyProxy>();

    private static void AssertParameter(string expected, Action action) =>
        Assert.Equal(expected, Assert.Throws<ArgumentNullException>(action).ParamName);

    private static async Task AssertCanceledAsync<T>(ValueTask<T> task, CancellationToken expected)
    {
        OperationCanceledException failure = await Assert.ThrowsAnyAsync<OperationCanceledException>(() => task.AsTask());
        Assert.Equal(expected, failure.CancellationToken);
    }

    private sealed class RecordingDisposable(List<string> events) : IDisposable
    {
        int _disposeCount;

        public int DisposeCount => Volatile.Read(ref _disposeCount);

        public void Dispose()
        {
            Interlocked.Increment(ref _disposeCount);
            events.Add("restore");
        }
    }

    private sealed class RecordingAsyncScope(List<string> events, IServiceProvider? services = null) : IServiceScope, IAsyncDisposable
    {
        int _disposeCount;

        public int DisposeCount => Volatile.Read(ref _disposeCount);
        public IServiceProvider ServiceProvider { get; } = services ?? new RecordingServiceProvider();

        public void Dispose() => throw new InvalidOperationException("The asynchronous path must be used.");

        public ValueTask DisposeAsync()
        {
            Interlocked.Increment(ref _disposeCount);
            events.Add("scope-async");
            return default;
        }
    }

    private sealed class RecordingSyncScope(List<string> events, IServiceProvider services) : IServiceScope
    {
        public IServiceProvider ServiceProvider { get; } = services;

        public void Dispose() => events.Add("scope-sync");
    }

    private sealed class ThrowingDisposable(Exception failure) : IDisposable
    {
        public void Dispose() => throw failure;
    }

    private sealed class ThrowingAsyncScope(Exception failure) : IServiceScope, IAsyncDisposable
    {
        public int DisposeCount { get; private set; }
        public IServiceProvider ServiceProvider { get; } = new RecordingServiceProvider();

        public void Dispose() => throw new InvalidOperationException("The asynchronous path must be used.");

        public ValueTask DisposeAsync()
        {
            DisposeCount++;
            return ValueTask.FromException(failure);
        }
    }

    private sealed class RecordingServiceProvider(params object[] services) : IServiceProvider
    {
        public object? GetService(Type serviceType) => services.FirstOrDefault(serviceType.IsInstanceOfType);
    }

    private sealed class RecordingSetter : ISetScopedConsumeContext
    {
        public IDisposable PushContext(IServiceScope serviceProvider, ConsumeContext context) => new RecordingDisposable([]);
    }

    private sealed class RecordingCompensateActivityScope(CompensateActivityContext<TestActivity, Log> context) :
        ICompensateActivityScopeContext<TestActivity, Log>
    {
        public int DisposeCount { get; private set; }
        public CompensateActivityContext<TestActivity, Log> Context { get; } = context;

        public ValueTask DisposeAsync()
        {
            DisposeCount++;
            return default;
        }

        public T GetService<T>() where T : class => throw new NotSupportedException();
    }

    private sealed class RecordingCompensateActivityScopeProvider(RecordingCompensateActivityScope scope) :
        ICompensateActivityScopeProvider<TestActivity, Log>
    {
        public int ProbeCount { get; private set; }

        public ValueTask<ICompensateScopeContext<Log>> GetScopeAsync(CompensateContext<Log> context,
            CancellationToken cancellationToken = default) => throw new NotSupportedException();

        public ValueTask<ICompensateActivityScopeContext<TestActivity, Log>> GetActivityScopeAsync(
            CompensateContext<Log> context,
            CancellationToken cancellationToken = default) => new(scope);

        public void Probe(ProbeContext context) => ProbeCount++;
    }

    private sealed class RecordingPipe<TContext> : IPipe<TContext> where TContext : class, PipeContext
    {
        public TContext? Context { get; private set; }

        public Task SendAsync(TContext context)
        {
            Context = context;
            return Task.CompletedTask;
        }

        public void Probe(ProbeContext context)
        {
        }
    }

    private class EmptyProxy : DispatchProxy
    {
        protected override object? Invoke(MethodInfo? targetMethod, object?[]? args) =>
            targetMethod?.ReturnType != typeof(void) && targetMethod?.ReturnType.IsValueType == true
                ? Activator.CreateInstance(targetMethod.ReturnType)
                : null;
    }

    private sealed class TestActivity : IExecuteActivity<Arguments>, ICompensateActivity<Log>
    {
        public Task<ExecutionResult> ExecuteAsync(ExecuteContext<Arguments> context) => throw new NotSupportedException();
        public Task<CompensationResult> CompensateAsync(CompensateContext<Log> context) => throw new NotSupportedException();
    }

    private sealed class Arguments;
    private sealed class Log;
    private sealed class Service;
    private interface TestRegistrationContext : IRegistrationContext, ISetScopedConsumeContext;
    private sealed class RestoreFailure : Exception;
    private sealed class ScopeFailure : Exception;
}

using System.Reflection;
using Microsoft.Extensions.DependencyInjection;
using ViciOne.ServiceBus.DependencyInjection;
using ViciOne.ServiceBus.Tests.Infrastructure.Requirements;
using Xunit;

namespace ViciOne.ServiceBus.Tests.Courier;

public sealed class CourierActivityScopeContextDeepContractTests
{
    [Fact]
    [RequirementCoverage("REQ-VSB-COURIER-ACTIVITY-SCOPE-LIFECYCLE", "deep-scope-context-api-is-internal-sealed-async-disposable-and-read-only")]
    public void ScopeContextApis_AreInternalSealedAsyncDisposableOnlyAndExposeReadOnlyContext()
    {
        Type[] contextTypes =
        [
            typeof(CreatedExecuteActivityScopeContext<,>),
            typeof(ExistingExecuteActivityScopeContext<,>),
            typeof(CreatedCompensateActivityScopeContext<,>),
            typeof(ExistingCompensateActivityScopeContext<,>),
        ];

        foreach (Type contextType in contextTypes)
        {
            Assert.True(contextType.IsNotPublic);
            Assert.True(contextType.IsSealed);
            Assert.Contains(typeof(IAsyncDisposable), contextType.GetInterfaces());
            Assert.DoesNotContain(typeof(IDisposable), contextType.GetInterfaces());
            PropertyInfo context = Assert.Single(
                contextType.GetProperties(BindingFlags.Instance | BindingFlags.Public),
                property => property.Name == "Context");
            Assert.NotNull(context.GetMethod);
            Assert.Null(context.SetMethod);
            MethodInfo dispose = Assert.Single(
                contextType.GetMethods(BindingFlags.Instance | BindingFlags.Public | BindingFlags.DeclaredOnly),
                method => method.Name == nameof(IAsyncDisposable.DisposeAsync));
            Assert.Equal(typeof(ValueTask), dispose.ReturnType);
            Assert.Empty(dispose.GetParameters());
        }

        Assert.True(typeof(ActivityScopeLifetime).IsNotPublic);
        Assert.True(typeof(ActivityScopeLifetime).IsSealed);
    }

    [Fact]
    [RequirementCoverage("REQ-VSB-COURIER-ACTIVITY-SCOPE-LIFECYCLE", "deep-all-scope-contexts-forward-exact-context-service-and-ownership")]
    public async Task AllScopeContexts_ForwardExactContextAndServiceWhileOnlyCreatedContextsOwnTheirScopeAsync()
    {
        var service = new Service();
        var provider = new RecordingServiceProvider(service);
        ExecuteActivityContext<TestActivity, Arguments> executeContext = Proxy<ExecuteActivityContext<TestActivity, Arguments>>();
        CompensateActivityContext<TestActivity, Log> compensateContext = Proxy<CompensateActivityContext<TestActivity, Log>>();
        var createdExecuteScope = new RecordingSyncScope(provider);
        var existingExecuteScope = new RecordingAsyncScope(provider);
        var createdCompensateScope = new RecordingAsyncScope(provider);
        var existingCompensateScope = new RecordingSyncScope(provider);
        var createdExecuteRestore = new RecordingDisposable();
        var existingExecuteRestore = new RecordingDisposable();
        var createdCompensateRestore = new RecordingDisposable();
        var existingCompensateRestore = new RecordingDisposable();
        var createdExecute = new CreatedExecuteActivityScopeContext<TestActivity, Arguments>(
            executeContext, createdExecuteScope, createdExecuteRestore);
        var existingExecute = new ExistingExecuteActivityScopeContext<TestActivity, Arguments>(
            executeContext, existingExecuteScope, existingExecuteRestore);
        var createdCompensate = new CreatedCompensateActivityScopeContext<TestActivity, Log>(
            compensateContext, createdCompensateScope, createdCompensateRestore);
        var existingCompensate = new ExistingCompensateActivityScopeContext<TestActivity, Log>(
            compensateContext, existingCompensateScope, existingCompensateRestore);

        Assert.Same(executeContext, createdExecute.Context);
        Assert.Same(executeContext, existingExecute.Context);
        Assert.Same(compensateContext, createdCompensate.Context);
        Assert.Same(compensateContext, existingCompensate.Context);
        Assert.Same(service, createdExecute.GetService<Service>());
        Assert.Same(service, existingExecute.GetService<Service>());
        Assert.Same(service, createdCompensate.GetService<Service>());
        Assert.Same(service, existingCompensate.GetService<Service>());

        await createdExecute.DisposeAsync();
        await existingExecute.DisposeAsync();
        await createdCompensate.DisposeAsync();
        await existingCompensate.DisposeAsync();

        Assert.Equal(1, createdExecuteRestore.DisposeCount);
        Assert.Equal(1, existingExecuteRestore.DisposeCount);
        Assert.Equal(1, createdCompensateRestore.DisposeCount);
        Assert.Equal(1, existingCompensateRestore.DisposeCount);
        Assert.Equal(1, createdExecuteScope.SyncDisposeCount);
        Assert.Equal(1, createdCompensateScope.AsyncDisposeCount);
        Assert.Equal(0, createdCompensateScope.SyncDisposeCount);
        Assert.Equal(0, existingExecuteScope.AsyncDisposeCount);
        Assert.Equal(0, existingExecuteScope.SyncDisposeCount);
        Assert.Equal(0, existingCompensateScope.SyncDisposeCount);
    }

    [Fact]
    [RequirementCoverage("REQ-VSB-COURIER-ACTIVITY-SCOPE-LIFECYCLE", "deep-concurrent-dispose-callers-share-held-owned-cleanup")]
    public async Task CreatedScope_ConcurrentDisposeCallersShareTheSameHeldCleanupAsync()
    {
        var events = new List<string>();
        var scope = new HoldingAsyncScope(events);
        var restore = new RecordingDisposable(events);
        var context = new CreatedExecuteActivityScopeContext<TestActivity, Arguments>(
            Proxy<ExecuteActivityContext<TestActivity, Arguments>>(),
            scope,
            restore);

        Task first = context.DisposeAsync().AsTask();
        await scope.Entered.Task.WaitAsync(TestContext.Current.CancellationToken);
        Task[] concurrent = Enumerable.Range(0, 16).Select(_ => context.DisposeAsync().AsTask()).ToArray();

        Assert.False(first.IsCompleted);
        Assert.All(concurrent, task => Assert.False(task.IsCompleted));
        Assert.Equal(["restore", "scope-enter"], events);
        Assert.Equal(1, restore.DisposeCount);
        Assert.Equal(1, scope.AsyncDisposeCount);

        scope.Release.TrySetResult();
        await Task.WhenAll(concurrent.Prepend(first));

        Assert.Equal(["restore", "scope-enter", "scope-exit"], events);
        await context.DisposeAsync();
        Assert.Equal(1, restore.DisposeCount);
        Assert.Equal(1, scope.AsyncDisposeCount);
    }

    [Fact]
    [RequirementCoverage("REQ-VSB-COURIER-ACTIVITY-SCOPE-LIFECYCLE", "deep-concurrent-callers-share-dual-failure-then-later-dispose-is-no-op")]
    public async Task ActivityScopeLifetime_ConcurrentCallersShareDualFailureThenLaterDisposeIsANoOpAsync()
    {
        var restoreFailure = new RestoreFailure();
        var scopeFailure = new ScopeFailure();
        var scope = new HoldingAsyncScope([], scopeFailure);
        var lifetime = new ActivityScopeLifetime(new ThrowingDisposable(restoreFailure), scope);

        Task first = lifetime.DisposeAsync().AsTask();
        await scope.Entered.Task.WaitAsync(TestContext.Current.CancellationToken);
        Task second = lifetime.DisposeAsync().AsTask();
        Assert.False(first.IsCompleted);
        Assert.False(second.IsCompleted);

        scope.Release.TrySetResult();
        AggregateException firstFailure = await Assert.ThrowsAsync<AggregateException>(() => first);
        AggregateException secondFailure = await Assert.ThrowsAsync<AggregateException>(() => second);

        Assert.Same(firstFailure, secondFailure);
        Assert.Equal([restoreFailure, scopeFailure], firstFailure.InnerExceptions);
        Assert.Equal(1, scope.AsyncDisposeCount);
        await lifetime.DisposeAsync();
        Assert.Equal(1, scope.AsyncDisposeCount);
    }

    [Fact]
    [RequirementCoverage("REQ-VSB-COURIER-CANCELLATION", "deep-canceled-wait-does-not-cancel-or-duplicate-owned-cleanup")]
    public async Task CreatedScope_CancelingAWaitDoesNotCancelOrDuplicateOwnedCleanupAsync()
    {
        var scope = new HoldingAsyncScope([]);
        var restore = new RecordingDisposable();
        var context = new CreatedCompensateActivityScopeContext<TestActivity, Log>(
            Proxy<CompensateActivityContext<TestActivity, Log>>(),
            scope,
            restore);
        Task disposal = context.DisposeAsync().AsTask();
        await scope.Entered.Task.WaitAsync(TestContext.Current.CancellationToken);
        using var cancellation = new CancellationTokenSource();
        await cancellation.CancelAsync();

        OperationCanceledException canceled = await Assert.ThrowsAnyAsync<OperationCanceledException>(() =>
            disposal.WaitAsync(cancellation.Token));
        Task concurrent = context.DisposeAsync().AsTask();

        Assert.Equal(cancellation.Token, canceled.CancellationToken);
        Assert.False(disposal.IsCompleted);
        Assert.False(concurrent.IsCompleted);
        Assert.Equal(1, restore.DisposeCount);
        Assert.Equal(1, scope.AsyncDisposeCount);

        scope.Release.TrySetResult();
        await Task.WhenAll(disposal, concurrent);
        Assert.Equal(1, restore.DisposeCount);
        Assert.Equal(1, scope.AsyncDisposeCount);
    }

    private static T Proxy<T>()
        where T : class => DispatchProxy.Create<T, EmptyProxy>();

    private class EmptyProxy : DispatchProxy
    {
        protected override object? Invoke(MethodInfo? targetMethod, object?[]? args) =>
            targetMethod?.ReturnType != typeof(void) && targetMethod?.ReturnType.IsValueType == true
                ? Activator.CreateInstance(targetMethod.ReturnType)
                : null;
    }

    private sealed class RecordingDisposable(List<string>? events = null) : IDisposable
    {
        public int DisposeCount { get; private set; }

        public void Dispose()
        {
            DisposeCount++;
            events?.Add("restore");
        }
    }

    private sealed class ThrowingDisposable(Exception failure) : IDisposable
    {
        public void Dispose() => throw failure;
    }

    private sealed class RecordingSyncScope(IServiceProvider provider) : IServiceScope
    {
        public int SyncDisposeCount { get; private set; }
        public IServiceProvider ServiceProvider { get; } = provider;

        public void Dispose() => SyncDisposeCount++;
    }

    private sealed class RecordingAsyncScope(IServiceProvider provider) : IServiceScope, IAsyncDisposable
    {
        public int AsyncDisposeCount { get; private set; }
        public int SyncDisposeCount { get; private set; }
        public IServiceProvider ServiceProvider { get; } = provider;

        public void Dispose() => SyncDisposeCount++;

        public ValueTask DisposeAsync()
        {
            AsyncDisposeCount++;
            return default;
        }
    }

    private sealed class HoldingAsyncScope(List<string> events, Exception? failure = null) : IServiceScope, IAsyncDisposable
    {
        public int AsyncDisposeCount { get; private set; }
        public TaskCompletionSource Entered { get; } = new(TaskCreationOptions.RunContinuationsAsynchronously);
        public TaskCompletionSource Release { get; } = new(TaskCreationOptions.RunContinuationsAsynchronously);
        public IServiceProvider ServiceProvider { get; } = new RecordingServiceProvider();

        public void Dispose() => throw new InvalidOperationException("The asynchronous scope path must be used.");

        public async ValueTask DisposeAsync()
        {
            AsyncDisposeCount++;
            events.Add("scope-enter");
            Entered.TrySetResult();
            await Release.Task.ConfigureAwait(false);
            events.Add("scope-exit");
            if (failure is not null)
                throw failure;
        }
    }

    private sealed class RecordingServiceProvider(params object[] services) : IServiceProvider
    {
        public object? GetService(Type serviceType) => services.FirstOrDefault(serviceType.IsInstanceOfType);
    }

    private sealed class TestActivity : IExecuteActivity<Arguments>, ICompensateActivity<Log>
    {
        public Task<ExecutionResult> ExecuteAsync(ExecuteContext<Arguments> context) => throw new NotSupportedException();
        public Task<CompensationResult> CompensateAsync(CompensateContext<Log> context) => throw new NotSupportedException();
    }

    private sealed class Arguments;

    private sealed class Log;

    private sealed class Service;

    private sealed class RestoreFailure : Exception;

    private sealed class ScopeFailure : Exception;
}

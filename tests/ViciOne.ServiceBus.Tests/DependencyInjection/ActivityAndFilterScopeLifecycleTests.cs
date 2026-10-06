using System.Reflection;
using Microsoft.Extensions.DependencyInjection;
using ViciOne.ServiceBus.Configuration;
using ViciOne.ServiceBus.Courier;
using ViciOne.ServiceBus.Courier.Contracts;
using ViciOne.ServiceBus.DependencyInjection;
using ViciOne.ServiceBus.Middleware;
using ViciOne.ServiceBus.Tests.Infrastructure.Requirements;
using ViciOne.ServiceBus.Tests.InternalAccess.InMemoryOutbox;
using Xunit;

namespace ViciOne.ServiceBus.Tests.DependencyInjection;

public sealed class ActivityAndFilterScopeLifecycleTests
{
    [Fact]
    [RequirementCoverage("REQ-VSB-CONTAINER-ACTIVITY-FILTER-SCOPE", "public-boundaries-reject-missing-dependencies")]
    public void PublicBoundaries_RejectMissingDependenciesBeforeUsingCollaborators()
    {
        var events = new List<string>();
        var scope = new RecordingAsyncScope(events);
        var restore = new RecordingDisposable(events);
        CompensateContext<Log> compensate = Proxy<CompensateContext<Log>>();
        ExecuteContext<Arguments> execute = Proxy<ExecuteContext<Arguments>>();

        AssertParameter("scope", () => new ServiceScopeLifetime(null!));
        AssertParameter("serviceProvider", () => new FilterScopeProvider<TestFilter, TestPipeContext>(null!));
        var filterProvider = new FilterScopeProvider<TestFilter, TestPipeContext>(new NullServiceProvider());
        AssertParameter("context", () => filterProvider.Create(null!));
        AssertParameter("context", () => filterProvider.Probe(null!));
        TestPipeContext malformedContext = Proxy<TestPipeContext>();
        ((PayloadProxy)(object)malformedContext).ReturnNullServiceProvider = true;
        AssertParameter("serviceProvider", () => filterProvider.Create(malformedContext));
        Assert.Throws<ArgumentNullException>(() => new CompensateScopeProvider<Log>((IRegistrationContext)null!));
        Assert.Throws<ArgumentNullException>(() => new ExecuteScopeProvider<Arguments>((IRegistrationContext)null!));
        TestRegistrationContext registrationContext = Proxy<TestRegistrationContext>();
        _ = new CompensateScopeProvider<Log>(registrationContext);
        _ = new ExecuteScopeProvider<Arguments>(registrationContext);

        AssertParameter("scope", () => new CreatedCompensateScopeContext<Log>(null!, compensate, restore));
        AssertParameter("context", () => new CreatedCompensateScopeContext<Log>(scope, null!, restore));
        AssertParameter("disposable", () => new CreatedCompensateScopeContext<Log>(scope, compensate, null!));
        AssertParameter("context", () => new ExistingCompensateScopeContext<Log>(null!, scope, restore));
        AssertParameter("scope", () => new ExistingCompensateScopeContext<Log>(compensate, null!, restore));
        AssertParameter("disposable", () => new ExistingCompensateScopeContext<Log>(compensate, scope, null!));

        AssertParameter("context", () => new CreatedExecuteScopeContext<Arguments>(null!, scope, restore));
        AssertParameter("scope", () => new CreatedExecuteScopeContext<Arguments>(execute, null!, restore));
        AssertParameter("disposable", () => new CreatedExecuteScopeContext<Arguments>(execute, scope, null!));
        AssertParameter("context", () => new ExistingExecuteScopeContext<Arguments>(null!, scope, restore));
        AssertParameter("scope", () => new ExistingExecuteScopeContext<Arguments>(execute, null!, restore));
        AssertParameter("disposable", () => new ExistingExecuteScopeContext<Arguments>(execute, scope, null!));
    }

    [Fact]
    [RequirementCoverage("REQ-VSB-CONTAINER-ACTIVITY-FILTER-SCOPE", "filter-created-and-borrowed-provider-ownership")]
    public async Task FilterScope_UsesTheNearestProviderAndReleasesOnlyCreatedScopesAsync()
    {
        var ownedEvents = new List<string>();
        var ownedFilter = new TestFilter();
        var ownedServices = new RecordingServiceProvider();
        ownedServices.Add(ownedFilter);
        var ownedScope = new RecordingAsyncScope(ownedEvents, ownedServices);
        var scopeFactory = new RecordingScopeFactory(() => ownedScope);
        var rootServices = new RecordingServiceProvider();
        rootServices.Add<IServiceScopeFactory>(scopeFactory);
        var provider = new FilterScopeProvider<TestFilter, TestPipeContext>(rootServices);
        TestPipeContext context = PayloadContext<TestPipeContext>();

        IFilterScopeContext<TestPipeContext> owned = provider.Create(context);

        Assert.Same(context, owned.Context);
        Assert.Same(ownedFilter, owned.Filter);
        Assert.Same(ownedFilter, owned.Filter);
        await Task.WhenAll(Enumerable.Range(0, 16).Select(_ => owned.DisposeAsync().AsTask()));
        Assert.Equal(1, ownedScope.DisposeCount);
        Assert.Equal(1, scopeFactory.CreateCount);

        var syncEvents = new List<string>();
        var syncFilter = new TestFilter();
        var syncServices = new RecordingServiceProvider();
        syncServices.Add(syncFilter);
        var syncScope = new RecordingSyncScope(syncEvents, syncServices);
        var syncFactory = new RecordingScopeFactory(() => syncScope);
        var syncRootServices = new RecordingServiceProvider();
        syncRootServices.Add<IServiceScopeFactory>(syncFactory);
        var syncProvider = new FilterScopeProvider<TestFilter, TestPipeContext>(syncRootServices);
        IFilterScopeContext<TestPipeContext> synchronous = syncProvider.Create(PayloadContext<TestPipeContext>());

        Assert.Same(syncFilter, synchronous.Filter);
        await synchronous.DisposeAsync();
        await synchronous.DisposeAsync();
        Assert.Equal(["scope-sync"], syncEvents);

        var borrowedFilter = new TestFilter();
        var borrowedServices = new RecordingServiceProvider();
        borrowedServices.Add(borrowedFilter);
        TestPipeContext borrowedContext = PayloadContext<TestPipeContext>(borrowedServices);

        IFilterScopeContext<TestPipeContext> borrowed = provider.Create(borrowedContext);

        Assert.Same(borrowedFilter, borrowed.Filter);
        await borrowed.DisposeAsync();
        await borrowed.DisposeAsync();
        Assert.Equal(1, scopeFactory.CreateCount);

        var nestedFilter = new TestFilter();
        var nestedServices = new RecordingServiceProvider();
        nestedServices.Add(nestedFilter);
        ConsumeContext consumeContext = PayloadContext<ConsumeContext>(nestedServices);
        IFilterScopeContext<TestPipeContext> nested = provider.Create(PayloadContext<TestPipeContext>(consumeContext));

        Assert.Same(nestedFilter, nested.Filter);
        await nested.DisposeAsync();
        provider.Probe(Proxy<ProbeContext>());
    }

    [Fact]
    [RequirementCoverage("REQ-VSB-CONTAINER-ACTIVITY-FILTER-SCOPE", "created-activity-scopes-restore-then-release-once")]
    public async Task CreatedActivityScopes_RestoreThenReleaseExactlyOnceAsync()
    {
        var compensateEvents = new List<string>();
        var compensateServices = new RecordingServiceProvider();
        var service = new Service();
        compensateServices.Add(service);
        var compensateScope = new RecordingAsyncScope(compensateEvents, compensateServices);
        var compensateRestore = new RecordingDisposable(compensateEvents);
        var compensate = new CreatedCompensateScopeContext<Log>(
            compensateScope,
            Proxy<CompensateContext<Log>>(),
            compensateRestore);

        Assert.Same(service, compensate.GetService<Service>());
        await compensate.DisposeAsync();
        await compensate.DisposeAsync();
        Assert.Equal(["restore", "scope-async"], compensateEvents);

        var executeEvents = new List<string>();
        var executeScope = new RecordingAsyncScope(executeEvents);
        var executeRestore = new RecordingDisposable(executeEvents);
        var execute = new CreatedExecuteScopeContext<Arguments>(
            Proxy<ExecuteContext<Arguments>>(),
            executeScope,
            executeRestore);

        Assert.Same(service, new CreatedExecuteScopeContext<Arguments>(
            Proxy<ExecuteContext<Arguments>>(),
            new RecordingAsyncScope([], compensateServices),
            new RecordingDisposable([])).GetService<Service>());
        await Task.WhenAll(Enumerable.Range(0, 16).Select(_ => execute.DisposeAsync().AsTask()));
        Assert.Equal(["restore", "scope-async"], executeEvents);
        Assert.Equal(1, executeRestore.DisposeCount);
        Assert.Equal(1, executeScope.DisposeCount);
    }

    [Fact]
    [RequirementCoverage("REQ-VSB-CONTAINER-ACTIVITY-FILTER-SCOPE", "existing-activity-scopes-never-release-borrowed-scope")]
    public async Task ExistingActivityScopes_RestoreWithoutReleasingBorrowedScopesAsync()
    {
        var compensateEvents = new List<string>();
        var service = new Service();
        var services = new RecordingServiceProvider();
        services.Add(service);
        var compensateScope = new RecordingAsyncScope(compensateEvents, services);
        var compensateRestore = new RecordingDisposable(compensateEvents);
        var compensate = new ExistingCompensateScopeContext<Log>(
            Proxy<CompensateContext<Log>>(),
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
        var execute = new ExistingExecuteScopeContext<Arguments>(
            Proxy<ExecuteContext<Arguments>>(),
            executeScope,
            executeRestore);

        Assert.Same(service, execute.GetService<Service>());
        await execute.DisposeAsync();
        await execute.DisposeAsync();
        Assert.Equal(["restore"], executeEvents);
        Assert.Equal(0, executeScope.DisposeCount);
    }

    [Fact]
    [RequirementCoverage("REQ-VSB-CONTAINER-ACTIVITY-FILTER-SCOPE", "providers-transfer-created-and-borrowed-ownership")]
    public async Task ActivityProviders_TransferCreatedAndBorrowedOwnershipAsync()
    {
        await AssertProviderOwnershipAsync<CompensateContext<Log>, ICompensateScopeContext<Log>>(
            (services, setter) => new CompensateScopeProvider<Log>(services, setter).GetScopeAsync,
            CreateCompensateContext);
        await AssertProviderOwnershipAsync<ExecuteContext<Arguments>, IExecuteScopeContext<Arguments>>(
            (services, setter) => new ExecuteScopeProvider<Arguments>(services, setter).GetScopeAsync,
            CreateExecuteContext);
    }

    [Fact]
    [RequirementCoverage("REQ-VSB-CONTAINER-ACTIVITY-FILTER-SCOPE", "activity-provider-validates-context-before-cancellation")]
    public async Task ActivityProviders_ValidateContextBeforeReturningCallerCancellationAsync()
    {
        var setter = new RecordingSetter();
        var compensateProvider = new CompensateScopeProvider<Log>(new NullServiceProvider(), setter);
        var executeProvider = new ExecuteScopeProvider<Arguments>(new NullServiceProvider(), setter);
        using var cancellation = new CancellationTokenSource();
        cancellation.Cancel();
        CancellationToken token = cancellation.Token;

        AssertParameter("context", () => compensateProvider.GetScopeAsync(null!, token));
        AssertParameter("context", () => executeProvider.GetScopeAsync(null!, token));
        AssertParameter("context", () => compensateProvider.Probe(null!));
        AssertParameter("context", () => executeProvider.Probe(null!));

        await AssertCanceledAsync(compensateProvider.GetScopeAsync(Proxy<CompensateContext<Log>>(), token), token);
        await AssertCanceledAsync(executeProvider.GetScopeAsync(Proxy<ExecuteContext<Arguments>>(), token), token);

        ProbeContext probe = Proxy<ProbeContext>();
        compensateProvider.Probe(probe);
        executeProvider.Probe(probe);
    }

    [Theory]
    [InlineData(false, 0)]
    [InlineData(true, 0)]
    [InlineData(false, 1)]
    [InlineData(true, 1)]
    [InlineData(false, 2)]
    [InlineData(true, 2)]
    [InlineData(false, 3)]
    [InlineData(true, 3)]
    [InlineData(false, 4)]
    [InlineData(true, 4)]
    [InlineData(false, 5)]
    [InlineData(true, 5)]
    [RequirementCoverage("REQ-VSB-CONTAINER-ACTIVITY-FILTER-SCOPE", "scope-only-filters-drain-release-and-retain-original-outcomes")]
    public async Task ScopeOnlyFilters_DrainReleaseAndRetainOriginalOutcomesAsync(bool compensate, int outcome)
    {
        using var cancellation = new CancellationTokenSource();
        cancellation.Cancel();
        Exception? operationFailure = outcome switch
        {
            1 or 3 => new InvalidOperationException("unique pipeline failure"),
            4 or 5 => new OperationCanceledException("unique pipeline cancellation", cancellation.Token),
            _ => null
        };
        Exception? releaseFailure = outcome is 2 or 3 or 5
            ? new InvalidOperationException("unique scope release failure")
            : null;
        var events = new List<string>();
        var release = new TaskCompletionSource(TaskCreationOptions.RunContinuationsAsynchronously);
        var scope = new HeldReleaseScope(events, release.Task, releaseFailure);
        var factory = new RecordingScopeFactory(() => scope);
        var services = new RecordingServiceProvider();
        services.Add<IServiceScopeFactory>(factory);
        var setter = new RecordingSetter(events);
        int nextCalls = 0;
        Task operation;
        if (compensate)
        {
            CompensateContext<Log> original = CreateCompensateContext(null);
            var filter = new ScopeCompensateFilter<Log>(new CompensateScopeProvider<Log>(services, setter));
            operation = filter.SendAsync(original, new CallbackPipe<CompensateContext<Log>>(context =>
            {
                Assert.NotSame(original, context);
                Assert.Same(scope, context.GetPayload<IServiceScope>());
                nextCalls++;
                return operationFailure is null ? Task.CompletedTask : Task.FromException(operationFailure);
            }));
        }
        else
        {
            ExecuteContext<Arguments> original = CreateExecuteContext(null);
            var filter = new ScopeExecuteFilter<Arguments>(new ExecuteScopeProvider<Arguments>(services, setter));
            operation = filter.SendAsync(original, new CallbackPipe<ExecuteContext<Arguments>>(context =>
            {
                Assert.NotSame(original, context);
                Assert.Same(scope, context.GetPayload<IServiceScope>());
                nextCalls++;
                return operationFailure is null ? Task.CompletedTask : Task.FromException(operationFailure);
            }));
        }

        try
        {
            await scope.Started.WaitAsync(TimeSpan.FromSeconds(5), TestContext.Current.CancellationToken);
            Assert.False(operation.IsCompleted);
            Assert.Equal(1, nextCalls);
            Assert.Equal(1, factory.CreateCount);
            Assert.Equal(1, scope.DisposeCount);
            Assert.Equal(["restore", "scope-release-started"], events);
            release.SetResult();
            Exception? actual = await Record.ExceptionAsync(() => operation.WaitAsync(TimeSpan.FromSeconds(5), TestContext.Current.CancellationToken));
            if (operationFailure is not null && releaseFailure is not null)
            {
                AggregateException aggregate = Assert.IsType<AggregateException>(actual);
                Assert.Collection(aggregate.InnerExceptions,
                    first => Assert.Same(operationFailure, first),
                    second => Assert.Same(releaseFailure, second));
            }
            else
                Assert.Same(operationFailure ?? releaseFailure, actual);

            if (operationFailure is OperationCanceledException canceled)
                Assert.Equal(cancellation.Token, canceled.CancellationToken);
            Assert.Equal(["restore", "scope-release-started", "scope-release-completed"], events);
            Assert.Equal(1, scope.DisposeCount);
        }
        finally
        {
            release.TrySetResult();
            _ = await Record.ExceptionAsync(() => operation.WaitAsync(TimeSpan.FromSeconds(5), TestContext.Current.CancellationToken));
        }
    }

    [Theory]
    [InlineData(false)]
    [InlineData(true)]
    [RequirementCoverage("REQ-VSB-CONTAINER-ACTIVITY-FILTER-SCOPE", "scope-only-filters-restore-without-releasing-borrowed-scope")]
    public async Task ScopeOnlyFilters_DoNotReleaseBorrowedScopeAsync(bool compensate)
    {
        var events = new List<string>();
        var scope = new RecordingAsyncScope(events);
        var setter = new RecordingSetter(events);
        var primary = new InvalidOperationException("unique borrowed pipeline failure");
        int calls = 0;
        Task operation;
        if (compensate)
        {
            CompensateContext<Log> original = CreateCompensateContext(scope);
            var filter = new ScopeCompensateFilter<Log>(new CompensateScopeProvider<Log>(new NullServiceProvider(), setter));
            operation = filter.SendAsync(original, new CallbackPipe<CompensateContext<Log>>(context =>
            {
                Assert.Same(original, context);
                calls++;
                return Task.FromException(primary);
            }));
        }
        else
        {
            ExecuteContext<Arguments> original = CreateExecuteContext(scope);
            var filter = new ScopeExecuteFilter<Arguments>(new ExecuteScopeProvider<Arguments>(new NullServiceProvider(), setter));
            operation = filter.SendAsync(original, new CallbackPipe<ExecuteContext<Arguments>>(context =>
            {
                Assert.Same(original, context);
                calls++;
                return Task.FromException(primary);
            }));
        }

        Assert.Same(primary, await Record.ExceptionAsync(() => operation.WaitAsync(TimeSpan.FromSeconds(5), TestContext.Current.CancellationToken)));
        Assert.Equal(1, calls);
        Assert.Equal(["restore"], events);
        Assert.Equal(0, scope.DisposeCount);
    }

    private sealed class CallbackPipe<TContext>(Func<TContext, Task> send) : IPipe<TContext>
        where TContext : class, PipeContext
    {
        public Task SendAsync(TContext context) => send(context);
        public void Probe(ProbeContext context) => throw new NotSupportedException("Probe is outside this oracle.");
    }

    private sealed class HeldReleaseScope(List<string> events, Task release, Exception? failure) : IServiceScope, IAsyncDisposable
    {
        readonly TaskCompletionSource _started = new(TaskCreationOptions.RunContinuationsAsynchronously);

        public Task Started => _started.Task;
        public int DisposeCount { get; private set; }
        public IServiceProvider ServiceProvider { get; } = new NullServiceProvider();
        public void Dispose() => throw new InvalidOperationException("Only asynchronous owned release is supported.");

        public async ValueTask DisposeAsync()
        {
            DisposeCount++;
            events.Add("scope-release-started");
            _started.TrySetResult();
            await release.ConfigureAwait(false);
            events.Add("scope-release-completed");
            if (failure is not null)
                throw failure;
        }
    }

    private static async Task AssertProviderOwnershipAsync<TContext, TScopeContext>(
        Func<IServiceProvider, ISetScopedConsumeContext, ScopeProvider<TContext, TScopeContext>> createProvider,
        Func<IServiceScope?, TContext> createContext)
        where TContext : class, ConsumeContext
        where TScopeContext : IAsyncDisposable
    {
        var borrowedEvents = new List<string>();
        var borrowedScope = new RecordingAsyncScope(borrowedEvents);
        var borrowedSetter = new RecordingSetter(borrowedEvents);
        ScopeProvider<TContext, TScopeContext> borrowedProvider = createProvider(new NullServiceProvider(), borrowedSetter);
        TContext borrowedContext = createContext(borrowedScope);

        TScopeContext borrowed = await borrowedProvider(borrowedContext);
        await borrowed.DisposeAsync();
        await borrowed.DisposeAsync();
        Assert.Equal(["restore"], borrowedEvents);
        Assert.Equal(0, borrowedScope.DisposeCount);

        var createdEvents = new List<string>();
        var createdScope = new RecordingAsyncScope(createdEvents);
        var scopeFactory = new RecordingScopeFactory(() => createdScope);
        var services = new RecordingServiceProvider();
        services.Add<IServiceScopeFactory>(scopeFactory);
        var createdSetter = new RecordingSetter(createdEvents);
        ScopeProvider<TContext, TScopeContext> createdProvider = createProvider(services, createdSetter);

        TScopeContext created = await createdProvider(createContext(null));
        await created.DisposeAsync();
        await created.DisposeAsync();
        Assert.Equal(["restore", "scope-async"], createdEvents);
        Assert.Equal(1, scopeFactory.CreateCount);
    }

    private static ExecuteContext<Arguments> CreateExecuteContext(IServiceScope? existingScope)
    {
        TestExecuteContext context = DispatchProxy.Create<TestExecuteContext, ExecuteContextProxy>();
        var proxy = (ExecuteContextProxy)(object)context;
        proxy.Transport = CreateTransportContext();
        proxy.ExistingScope = existingScope;
        return context;
    }

    private static CompensateContext<Log> CreateCompensateContext(IServiceScope? existingScope)
    {
        TestCompensateContext context = DispatchProxy.Create<TestCompensateContext, CompensateContextProxy>();
        var proxy = (CompensateContextProxy)(object)context;
        proxy.Transport = CreateTransportContext();
        proxy.ExistingScope = existingScope;
        return context;
    }

    private static ConsumeContext CreateTransportContext()
    {
        var builder = new RoutingSlipBuilder(NewId.NextGuid());
        return InMemoryOutboxTestContextFactory.Create(builder.Build()).Advanced();
    }

    private static T Proxy<T>() where T : class => DispatchProxy.Create<T, PayloadProxy>();

    private static T PayloadContext<T>(params object[] payloads) where T : class
    {
        T context = Proxy<T>();
        ((PayloadProxy)(object)context).Add(payloads);
        return context;
    }

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
        public IServiceProvider ServiceProvider { get; } = services ?? new NullServiceProvider();

        public void Dispose() => throw new InvalidOperationException("The asynchronous path must be used.");

        public ValueTask DisposeAsync()
        {
            Interlocked.Increment(ref _disposeCount);
            events.Add("scope-async");
            return default;
        }
    }

    private sealed class RecordingSyncScope(List<string> events, IServiceProvider? services = null) : IServiceScope
    {
        public IServiceProvider ServiceProvider { get; } = services ?? new NullServiceProvider();

        public void Dispose() => events.Add("scope-sync");
    }

    private sealed class RecordingScopeFactory(Func<IServiceScope> create) : IServiceScopeFactory
    {
        public int CreateCount { get; private set; }

        public IServiceScope CreateScope()
        {
            CreateCount++;
            return create();
        }
    }

    private sealed class RecordingServiceProvider : IServiceProvider
    {
        readonly Dictionary<Type, object> _services = [];

        public void Add<T>(T service) where T : class => _services.Add(typeof(T), service);

        public object? GetService(Type serviceType) => _services.GetValueOrDefault(serviceType);
    }

    private sealed class RecordingSetter(List<string>? events = null) : ISetScopedConsumeContext
    {
        public IDisposable PushContext(IServiceScope serviceProvider, ConsumeContext context) =>
            new RecordingDisposable(events ?? []);
    }

    private sealed class NullServiceProvider : IServiceProvider
    {
        public object? GetService(Type serviceType) => null;
    }

    private class PayloadProxy : DispatchProxy
    {
        readonly Dictionary<Type, object> _payloads = [];

        public bool ReturnNullServiceProvider { get; set; }

        public void Add(IEnumerable<object> payloads)
        {
            foreach (object payload in payloads)
                _payloads.Add(payload.GetType(), payload);
        }

        protected override object? Invoke(MethodInfo? targetMethod, object?[]? args)
        {
            if (targetMethod?.Name == nameof(PipeContext.TryGetPayload) && targetMethod.IsGenericMethod)
            {
                Type payloadType = targetMethod.GetGenericArguments()[0];
                if (ReturnNullServiceProvider && payloadType == typeof(IServiceProvider))
                {
                    args![0] = null;
                    return true;
                }

                object? payload = _payloads.Values.FirstOrDefault(payloadType.IsInstanceOfType);
                args![0] = payload;
                return payload is not null;
            }

            if (targetMethod?.Name == nameof(PipeContext.HasPayloadType))
                return args![0] is Type payloadType && _payloads.Values.Any(payloadType.IsInstanceOfType);

            return targetMethod?.ReturnType == typeof(void)
                ? null
                : targetMethod?.ReturnType.IsValueType == true
                    ? Activator.CreateInstance(targetMethod.ReturnType)
                    : null;
        }
    }

    private abstract class ActivityContextProxy : DispatchProxy
    {
        public IServiceScope? ExistingScope { get; set; }
        public ConsumeContext Transport { get; set; } = null!;

        protected override object? Invoke(MethodInfo? targetMethod, object?[]? args)
        {
            ArgumentNullException.ThrowIfNull(targetMethod);

            if (targetMethod.Name == "get_Arguments")
                return new Arguments();
            if (targetMethod.Name == "get_Log")
                return new Log();
            if (targetMethod.Name == nameof(PipeContext.HasPayloadType)
                && ExistingScope is not null
                && (Type)args![0]! == typeof(IServiceScope))
                return true;
            if (targetMethod.Name == nameof(PipeContext.TryGetPayload)
                && targetMethod.IsGenericMethod
                && ExistingScope is not null
                && targetMethod.GetGenericArguments()[0] == typeof(IServiceScope))
            {
                args![0] = ExistingScope;
                return true;
            }

            try
            {
                return targetMethod.Invoke(Transport, args);
            }
            catch (TargetInvocationException exception) when (exception.InnerException is not null)
            {
                throw exception.InnerException;
            }
        }
    }

    private class ExecuteContextProxy : ActivityContextProxy;
    private class CompensateContextProxy : ActivityContextProxy;

    private sealed class TestFilter : IFilter<TestPipeContext>
    {
        public Task SendAsync(TestPipeContext context, IPipe<TestPipeContext> next) => next.SendAsync(context);

        public void Probe(ProbeContext context)
        {
        }
    }

    private interface TestPipeContext : PipeContext;
    private interface TestRegistrationContext : IRegistrationContext, ISetScopedConsumeContext;
    private interface TestExecuteContext : ExecuteContext<Arguments>, ConsumeContext;
    private interface TestCompensateContext : CompensateContext<Log>, ConsumeContext;
    private sealed class Arguments;
    private sealed class Log;
    private sealed class Service;

    private delegate ValueTask<TScopeContext> ScopeProvider<in TContext, TScopeContext>(
        TContext context,
        CancellationToken cancellationToken = default);
}

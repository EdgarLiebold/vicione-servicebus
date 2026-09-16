using System.Reflection;
using Microsoft.Extensions.DependencyInjection;
using ViciOne.ServiceBus.Configuration;
using ViciOne.ServiceBus.Context;
using ViciOne.ServiceBus.DependencyInjection;
using ViciOne.ServiceBus.Tests.Infrastructure.Requirements;
using Xunit;

namespace ViciOne.ServiceBus.Tests.DependencyInjection;

public sealed class ConsumeScopeLifecycleTests
{
    [Fact]
    [RequirementCoverage("REQ-VSB-CONTAINER-CONSUME-SCOPE", "public-boundaries-reject-missing-dependencies")]
    public async Task PublicBoundaries_RejectMissingDependenciesBeforeUsingCollaboratorsAsync()
    {
        var events = new List<string>();
        var scope = new RecordingAsyncScope(events);
        ConsumeContext context = Proxy<ConsumeContext>();
        ConsumeContext<Message> typedContext = Proxy<ConsumeContext<Message>>();
        ConsumerConsumeContext<Consumer, Message> consumerContext = Proxy<ConsumerConsumeContext<Consumer, Message>>();
        var restore = new RecordingDisposable(events);
        var setter = new RecordingSetter();

        AssertParameter("restoreContext", () => new ConsumeScopeLifetime(null!));
        AssertParameter("scope", () => new CreatedConsumeScopeContext(null!, context, restore));
        AssertParameter("context", () => new CreatedConsumeScopeContext(scope, null!, restore));
        AssertParameter("disposable", () => new CreatedConsumeScopeContext(scope, context, null!));
        AssertParameter("context", () => new ExistingConsumeScopeContext(null!, restore));
        AssertParameter("disposable", () => new ExistingConsumeScopeContext(context, null!));

        AssertParameter("scope", () => new CreatedConsumeScopeContext<Message>(null!, typedContext, restore, setter));
        AssertParameter("context", () => new CreatedConsumeScopeContext<Message>(scope, null!, restore, setter));
        AssertParameter("disposable", () => new CreatedConsumeScopeContext<Message>(scope, typedContext, null!, setter));
        AssertParameter("setter", () => new CreatedConsumeScopeContext<Message>(scope, typedContext, restore, null!));
        AssertParameter("context", () => new ExistingConsumeScopeContext<Message>(null!, scope, restore, setter));
        AssertParameter("scope", () => new ExistingConsumeScopeContext<Message>(typedContext, null!, restore, setter));
        AssertParameter("disposable", () => new ExistingConsumeScopeContext<Message>(typedContext, scope, null!, setter));
        AssertParameter("setter", () => new ExistingConsumeScopeContext<Message>(typedContext, scope, restore, null!));

        AssertParameter("scope", () => new CreatedConsumerConsumeScopeContext<Consumer, Message>(null!, consumerContext, restore));
        AssertParameter("context", () => new CreatedConsumerConsumeScopeContext<Consumer, Message>(scope, null!, restore));
        AssertParameter("disposable", () => new CreatedConsumerConsumeScopeContext<Consumer, Message>(scope, consumerContext, null!));
        AssertParameter("context", () => new ExistingConsumerConsumeScopeContext<Consumer, Message>(null!, restore));
        AssertParameter("disposable", () => new ExistingConsumerConsumeScopeContext<Consumer, Message>(consumerContext, null!));

        var created = new CreatedConsumeScopeContext<Message>(scope, typedContext, restore, setter);
        var existing = new ExistingConsumeScopeContext<Message>(typedContext, scope, restore, setter);
        AssertParameter("arguments", () => created.CreateInstance<Service>(null!));
        AssertParameter("arguments", () => existing.CreateInstance<Service>(null!));
        AssertParameter("context", () => created.PushConsumeContext(null!));
        AssertParameter("context", () => existing.PushConsumeContext(null!));
        Assert.Equal(0, setter.PushCount);

        AssertParameter("scopeProvider", () => new ScopeConsumerFactory<Consumer>(null!));
        var factory = new ScopeConsumerFactory<Consumer>(Proxy<IConsumeScopeProvider>());
        await AssertParameterAsync("context", () => factory.SendAsync<Message>(null!, Proxy<IPipe<ConsumerConsumeContext<Consumer, Message>>>()));
        await AssertParameterAsync("next", () => factory.SendAsync(typedContext, null!));
        AssertParameter("context", () => ((IProbeSite)factory).Probe(null!));

        AssertParameter("context", () => new ConsumeScopeProvider((IRegistrationContext)null!));
        ArgumentException unsupportedRegistration = Assert.Throws<ArgumentException>(
            () => new ConsumeScopeProvider(Proxy<IRegistrationContext>()));
        Assert.Equal("context", unsupportedRegistration.ParamName);
        Assert.NotNull(new ConsumeScopeProvider(PayloadContext<TestRegistrationContext>()));

        ProbeContext probe = DispatchProxy.Create<ProbeContext, ProbeContextProxy>();
        var probeRecorder = (ProbeContextProxy)(object)probe;
        new ConsumeScopeProvider(new NullServiceProvider(), setter).Probe(probe);
        ((IProbeSite)factory).Probe(probe);
        Assert.Equal(1, probeRecorder.AddCount);
        Assert.Equal(1, probeRecorder.ScopeCount);
    }

    [Fact]
    [RequirementCoverage("REQ-VSB-CONTAINER-CONSUME-SCOPE", "created-scope-restores-then-releases-exactly-once")]
    public async Task CreatedScope_RestoresThenReleasesExactlyOnceAcrossConcurrentDisposalAsync()
    {
        var events = new List<string>();
        var scope = new RecordingAsyncScope(events);
        var restore = new RecordingDisposable(events);
        var context = new CreatedConsumeScopeContext(scope, Proxy<ConsumeContext>(), restore);

        await Task.WhenAll(Enumerable.Range(0, 16).Select(_ => context.DisposeAsync().AsTask()));

        Assert.Equal(["restore", "scope-async"], events);
        Assert.Equal(1, restore.DisposeCount);
        Assert.Equal(1, scope.DisposeCount);
    }

    [Fact]
    [RequirementCoverage("REQ-VSB-CONTAINER-CONSUME-SCOPE", "cleanup-attempts-owned-scope-and-preserves-both-failures")]
    public async Task CreatedScope_AttemptsOwnedScopeCleanupAndPreservesBothFailuresAsync()
    {
        var events = new List<string>();
        var restoreFailure = new InvalidOperationException("restore failed");
        var scopeFailure = new ApplicationException("scope failed");
        var scope = new RecordingAsyncScope(events, scopeFailure);
        var restore = new RecordingDisposable(events, restoreFailure);
        var context = new CreatedConsumeScopeContext(scope, Proxy<ConsumeContext>(), restore);

        AggregateException failure = await Assert.ThrowsAsync<AggregateException>(() => context.DisposeAsync().AsTask());

        Assert.Equal(["restore", "scope-async"], events);
        Assert.Equal([restoreFailure, scopeFailure], failure.InnerExceptions);
        await context.DisposeAsync();
        Assert.Equal(1, restore.DisposeCount);
        Assert.Equal(1, scope.DisposeCount);
    }

    [Fact]
    [RequirementCoverage("REQ-VSB-CONTAINER-CONSUME-SCOPE", "cleanup-preserves-single-failures-and-sync-scope-path")]
    public async Task CreatedScope_PreservesSingleFailuresAndSupportsSynchronousScopesAsync()
    {
        var restoreFailure = new InvalidOperationException("restore failed");
        var restoreEvents = new List<string>();
        var restoreScope = new RecordingSyncScope(restoreEvents);
        var restoreContext = new CreatedConsumeScopeContext(
            restoreScope,
            Proxy<ConsumeContext>(),
            new RecordingDisposable(restoreEvents, restoreFailure));

        Exception observedRestoreFailure = await Assert.ThrowsAsync<InvalidOperationException>(
            () => restoreContext.DisposeAsync().AsTask());

        Assert.Same(restoreFailure, observedRestoreFailure);
        Assert.Equal(["restore", "scope-sync"], restoreEvents);

        var scopeFailure = new ApplicationException("scope failed");
        var scopeEvents = new List<string>();
        var failingScope = new RecordingAsyncScope(scopeEvents, scopeFailure);
        var scopeContext = new CreatedConsumeScopeContext(
            failingScope,
            Proxy<ConsumeContext>(),
            new RecordingDisposable(scopeEvents));

        Exception observedScopeFailure = await Assert.ThrowsAsync<ApplicationException>(
            () => scopeContext.DisposeAsync().AsTask());

        Assert.Same(scopeFailure, observedScopeFailure);
        Assert.Equal(["restore", "scope-async"], scopeEvents);
    }

    [Fact]
    [RequirementCoverage("REQ-VSB-CONTAINER-CONSUME-SCOPE", "existing-scope-restores-without-releasing-borrowed-scope")]
    public async Task ExistingScope_RestoresWithoutReleasingBorrowedScopeAsync()
    {
        var events = new List<string>();
        var scope = new RecordingAsyncScope(events);
        var restore = new RecordingDisposable(events);
        var context = new ExistingConsumeScopeContext<Message>(
            Proxy<ConsumeContext<Message>>(),
            scope,
            restore,
            new RecordingSetter());

        await context.DisposeAsync();
        await context.DisposeAsync();

        Assert.Equal(["restore"], events);
        Assert.Equal(1, restore.DisposeCount);
        Assert.Equal(0, scope.DisposeCount);
    }

    [Fact]
    [RequirementCoverage("REQ-VSB-CONTAINER-CONSUME-SCOPE", "provider-created-and-existing-scope-ownership")]
    public async Task Provider_CreatesAndReusesScopesWithExactOwnershipAsync()
    {
        var createdEvents = new List<string>();
        var service = new Service();
        var consumer = new Consumer();
        var childServices = new RecordingServiceProvider();
        childServices.Add(service);
        childServices.Add(consumer);
        var createdScope = new RecordingAsyncScope(createdEvents, serviceProvider: childServices);
        var scopeFactory = new RecordingScopeFactory(() => createdScope);
        var rootServices = new RecordingServiceProvider();
        rootServices.Add<IServiceScopeFactory>(scopeFactory);
        var setter = new RecordingSetter(createdEvents);
        var provider = new ConsumeScopeProvider(rootServices, setter);
        ConsumeContext<Message> source = PayloadContext<TestConsumeContext>();

        IConsumeScopeContext<Message> created = await provider.GetScopeAsync(source, TestContext.Current.CancellationToken);

        Assert.IsType<CreatedConsumeScopeContext<Message>>(created);
        Assert.IsType<ConsumeContextScope<Message>>(created.Context);
        Assert.Same(service, created.GetService<Service>());
        Assert.Equal("exact", created.CreateInstance<ConstructedService>("exact").Value);
        using (created.PushConsumeContext(Assert.IsAssignableFrom<ConsumeContext>(created.Context)))
        {
            Assert.Equal(2, setter.PushCount);
        }
        await created.DisposeAsync();
        Assert.Equal(1, createdScope.DisposeCount);
        Assert.Equal(1, scopeFactory.CreateCount);

        var existingEvents = new List<string>();
        var existingServices = new RecordingServiceProvider();
        existingServices.Add(consumer);
        var existingScope = new RecordingAsyncScope(existingEvents, serviceProvider: existingServices);
        var existingSetter = new RecordingSetter(existingEvents);
        var existingProvider = new ConsumeScopeProvider(new NullServiceProvider(), existingSetter);
        ConsumeContext<Message> existingSource = PayloadContext<TestConsumeContext>(existingScope);

        IConsumerConsumeScopeContext<Consumer, Message> existing =
            await existingProvider.GetScopeAsync<Consumer, Message>(existingSource, TestContext.Current.CancellationToken);

        Assert.IsType<ExistingConsumerConsumeScopeContext<Consumer, Message>>(existing);
        Assert.Same(consumer, existing.Context.Consumer);
        await existing.DisposeAsync();
        Assert.Equal(1, existingSetter.PushCount);
        Assert.Equal(0, existingScope.DisposeCount);
    }

    [Fact]
    [RequirementCoverage("REQ-VSB-CONTAINER-CONSUME-SCOPE", "all-scope-overloads-preserve-ownership")]
    public async Task Provider_AllScopeOverloadsPreserveCreatedAndBorrowedOwnershipAsync()
    {
        var createdEvents = new List<string>();
        var createdScope = new RecordingAsyncScope(createdEvents);
        var createdFactory = new RecordingScopeFactory(() => createdScope);
        var createdServices = new RecordingServiceProvider();
        createdServices.Add<IServiceScopeFactory>(createdFactory);
        var createdProvider = new ConsumeScopeProvider(createdServices, new RecordingSetter(createdEvents));

        IConsumeScopeContext created = await createdProvider.GetScopeAsync(
            PayloadContext<ConsumeContext>(),
            TestContext.Current.CancellationToken);

        Assert.IsType<CreatedConsumeScopeContext>(created);
        await created.DisposeAsync();
        Assert.Equal(["restore", "scope-async"], createdEvents);

        var existingEvents = new List<string>();
        var service = new Service();
        var existingServices = new RecordingServiceProvider();
        existingServices.Add(service);
        var existingScope = new RecordingAsyncScope(existingEvents, serviceProvider: existingServices);
        var existingProvider = new ConsumeScopeProvider(new NullServiceProvider(), new RecordingSetter(existingEvents));

        IConsumeScopeContext untyped = await existingProvider.GetScopeAsync(
            PayloadContext<ConsumeContext>(existingScope),
            TestContext.Current.CancellationToken);
        IConsumeScopeContext<Message> typed = await existingProvider.GetScopeAsync<Message>(
            PayloadContext<TestConsumeContext>(existingScope),
            TestContext.Current.CancellationToken);

        Assert.IsType<ExistingConsumeScopeContext>(untyped);
        Assert.IsType<ExistingConsumeScopeContext<Message>>(typed);
        Assert.Same(service, typed.GetService<Service>());
        Assert.Equal("borrowed", typed.CreateInstance<ConstructedService>("borrowed").Value);
        using (typed.PushConsumeContext(Assert.IsAssignableFrom<ConsumeContext>(typed.Context)))
        {
        }
        await untyped.DisposeAsync();
        await typed.DisposeAsync();
        Assert.Equal(0, existingScope.DisposeCount);
    }

    [Fact]
    [RequirementCoverage("REQ-VSB-CONTAINER-CONSUME-SCOPE", "consumer-factory-forwards-and-releases-created-scope")]
    public async Task ConsumerFactory_ForwardsResolvedConsumerAndReleasesCreatedScopeAsync()
    {
        var events = new List<string>();
        var consumer = new Consumer();
        var childServices = new RecordingServiceProvider();
        childServices.Add(consumer);
        var scope = new RecordingAsyncScope(events, serviceProvider: childServices);
        var scopeFactory = new RecordingScopeFactory(() => scope);
        var rootServices = new RecordingServiceProvider();
        rootServices.Add<IServiceScopeFactory>(scopeFactory);
        var setter = new RecordingSetter(events);
        var provider = new ConsumeScopeProvider(rootServices, setter);
        var factory = new ScopeConsumerFactory<Consumer>(provider);
        var pipe = new RecordingConsumerPipe();

        await factory.SendAsync(PayloadContext<TestConsumeContext>(), pipe);

        Assert.Equal(1, pipe.SendCount);
        Assert.Same(consumer, pipe.Context!.Consumer);
        Assert.Equal(["restore", "scope-async"], events);
        Assert.Equal(1, scope.DisposeCount);

    }

    [Fact]
    [RequirementCoverage("REQ-VSB-CONTAINER-CONSUME-SCOPE", "failed-consumer-resolution-cleans-created-scope")]
    public async Task Provider_CleansCreatedScopeWhenConsumerResolutionFailsAsync()
    {
        var events = new List<string>();
        var scope = new RecordingAsyncScope(events);
        var scopeFactory = new RecordingScopeFactory(() => scope);
        var rootServices = new RecordingServiceProvider();
        rootServices.Add<IServiceScopeFactory>(scopeFactory);
        var setter = new RecordingSetter(events);
        var provider = new ConsumeScopeProvider(rootServices, setter);

        await Assert.ThrowsAsync<ConsumerException>(() => provider
            .GetScopeAsync<Consumer, Message>(PayloadContext<TestConsumeContext>(), TestContext.Current.CancellationToken)
            .AsTask());

        Assert.Equal(["restore", "scope-async"], events);
        Assert.Equal(1, scope.DisposeCount);

        var borrowedEvents = new List<string>();
        var borrowedScope = new RecordingAsyncScope(borrowedEvents);
        var borrowedProvider = new ConsumeScopeProvider(
            new NullServiceProvider(),
            new RecordingSetter(borrowedEvents));

        await Assert.ThrowsAsync<ConsumerException>(() => borrowedProvider
            .GetScopeAsync<Consumer, Message>(
                PayloadContext<TestConsumeContext>(borrowedScope),
                TestContext.Current.CancellationToken)
            .AsTask());

        Assert.Equal(["restore"], borrowedEvents);
        Assert.Equal(0, borrowedScope.DisposeCount);
    }

    [Fact]
    [RequirementCoverage("REQ-VSB-CONTAINER-CONSUME-SCOPE", "failed-scope-creation-preserves-operation-and-cleanup-failures")]
    public async Task Provider_PreservesOperationAndBothCleanupFailuresAsync()
    {
        var events = new List<string>();
        var restoreFailure = new InvalidOperationException("restore failed");
        var scopeFailure = new ApplicationException("scope failed");
        var scope = new RecordingAsyncScope(events, scopeFailure);
        var scopeFactory = new RecordingScopeFactory(() => scope);
        var rootServices = new RecordingServiceProvider();
        rootServices.Add<IServiceScopeFactory>(scopeFactory);
        var provider = new ConsumeScopeProvider(rootServices, new RecordingSetter(events, restoreFailure));

        AggregateException failure = await Assert.ThrowsAsync<AggregateException>(() => provider
            .GetScopeAsync<Consumer, Message>(PayloadContext<TestConsumeContext>(), TestContext.Current.CancellationToken)
            .AsTask());

        IReadOnlyCollection<Exception> failures = failure.Flatten().InnerExceptions;
        Assert.Contains(failures, exception => exception is ConsumerException);
        Assert.Contains(restoreFailure, failures);
        Assert.Contains(scopeFailure, failures);
        Assert.Equal(["restore", "scope-async"], events);
    }

    [Fact]
    [RequirementCoverage("REQ-VSB-CONTAINER-CONSUME-SCOPE", "provider-validates-context-before-cancellation")]
    public async Task Provider_ValidatesContextBeforeReturningCallerCancellationAsync()
    {
        var provider = new ConsumeScopeProvider(new NullServiceProvider(), new RecordingSetter());
        using var cancellation = new CancellationTokenSource();
        cancellation.Cancel();
        CancellationToken token = cancellation.Token;

        AssertParameter("context", () => provider.GetScopeAsync((ConsumeContext)null!, token));
        AssertParameter("context", () => provider.GetScopeAsync<Message>((ConsumeContext<Message>)null!, token));
        AssertParameter("context", () => provider.GetScopeAsync<Consumer, Message>((ConsumeContext<Message>)null!, token));
        AssertParameter("context", () => provider.Probe(null!));

        await AssertCanceledAsync(provider.GetScopeAsync(Proxy<ConsumeContext>(), token), token);
        await AssertCanceledAsync(provider.GetScopeAsync(Proxy<ConsumeContext<Message>>(), token), token);
        await AssertCanceledAsync(provider.GetScopeAsync<Consumer, Message>(Proxy<ConsumeContext<Message>>(), token), token);
    }

    private static T Proxy<T>() where T : class => DispatchProxy.Create<T, PassiveProxy>();

    private static T PayloadContext<T>(params object[] payloads) where T : class
    {
        T context = DispatchProxy.Create<T, PayloadProxy>();
        ((PayloadProxy)(object)context).Add(payloads);
        return context;
    }

    private static void AssertParameter(string expected, Action action) =>
        Assert.Equal(expected, Assert.Throws<ArgumentNullException>(action).ParamName);

    private static async Task AssertParameterAsync(string expected, Func<Task> action) =>
        Assert.Equal(expected, (await Assert.ThrowsAsync<ArgumentNullException>(action)).ParamName);

    private static async Task AssertCanceledAsync<T>(ValueTask<T> task, CancellationToken expected)
    {
        OperationCanceledException failure = await Assert.ThrowsAnyAsync<OperationCanceledException>(() => task.AsTask());
        Assert.Equal(expected, failure.CancellationToken);
    }

    private sealed class RecordingDisposable(List<string> events, Exception? failure = null) : IDisposable
    {
        int _disposeCount;

        public int DisposeCount => Volatile.Read(ref _disposeCount);

        public void Dispose()
        {
            Interlocked.Increment(ref _disposeCount);
            events.Add("restore");
            if (failure is not null)
                throw failure;
        }
    }

    private sealed class RecordingAsyncScope : IServiceScope, IAsyncDisposable
    {
        readonly List<string> _events;
        readonly Exception? _failure;
        int _disposeCount;

        public RecordingAsyncScope(List<string> events, Exception? failure = null, IServiceProvider? serviceProvider = null)
        {
            _events = events;
            _failure = failure;
            ServiceProvider = serviceProvider ?? new NullServiceProvider();
        }

        public int DisposeCount => Volatile.Read(ref _disposeCount);
        public IServiceProvider ServiceProvider { get; }

        public void Dispose() => throw new InvalidOperationException("The asynchronous disposal path must be used.");

        public ValueTask DisposeAsync()
        {
            Interlocked.Increment(ref _disposeCount);
            _events.Add("scope-async");
            return _failure is null ? default : ValueTask.FromException(_failure);
        }
    }

    private sealed class RecordingSyncScope(List<string> events) : IServiceScope
    {
        public IServiceProvider ServiceProvider { get; } = new NullServiceProvider();

        public void Dispose() => events.Add("scope-sync");
    }

    private sealed class RecordingSetter(List<string>? events = null, Exception? failure = null) : ISetScopedConsumeContext
    {
        public int PushCount { get; private set; }

        public IDisposable PushContext(IServiceScope serviceProvider, ConsumeContext context)
        {
            PushCount++;
            return new RecordingDisposable(events ?? [], failure);
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

        public void Add<T>(T service) where T : class => _services.Add(typeof(T), service);

        public object? GetService(Type serviceType) => _services.GetValueOrDefault(serviceType);
    }

    private sealed class NullServiceProvider : IServiceProvider
    {
        public object? GetService(Type serviceType) => null;
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

    private class PayloadProxy : PassiveProxy
    {
        readonly Dictionary<Type, object> _payloads = [];

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
                object? payload = _payloads.Values.FirstOrDefault(payloadType.IsInstanceOfType);
                args![0] = payload;
                return payload is not null;
            }

            if (targetMethod?.Name == nameof(PipeContext.HasPayloadType))
                return args![0] is Type payloadType && _payloads.Values.Any(payloadType.IsInstanceOfType);

            if (targetMethod?.Name == "get_Message")
                return new Message();

            if (targetMethod?.Name == "get_ReceiveContext")
                return DispatchProxy.Create<ReceiveContext, ReceiveContextProxy>();

            if (targetMethod?.Name == "get_SerializerContext")
                return Proxy<SerializerContext>();

            return base.Invoke(targetMethod, args);
        }
    }

    private class ReceiveContextProxy : PassiveProxy
    {
        protected override object? Invoke(MethodInfo? targetMethod, object?[]? args)
        {
            return targetMethod?.Name == "get_PublishEndpointProvider"
                ? Proxy<IPublishEndpointProvider>()
                : base.Invoke(targetMethod, args);
        }
    }

    private class ProbeContextProxy : PassiveProxy
    {
        public int AddCount { get; private set; }
        public int ScopeCount { get; private set; }

        protected override object? Invoke(MethodInfo? targetMethod, object?[]? args)
        {
            if (targetMethod?.Name == nameof(ProbeContext.CreateScope))
            {
                ScopeCount++;
                return DispatchProxy.Create<ProbeContext, ProbeContextProxy>();
            }

            if (targetMethod?.Name == nameof(ProbeContext.Add))
            {
                AddCount++;
                return null;
            }

            return base.Invoke(targetMethod, args);
        }
    }

    private sealed class RecordingConsumerPipe : IPipe<ConsumerConsumeContext<Consumer, Message>>
    {
        public int SendCount { get; private set; }
        public ConsumerConsumeContext<Consumer, Message>? Context { get; private set; }

        public Task SendAsync(ConsumerConsumeContext<Consumer, Message> context)
        {
            SendCount++;
            Context = context;
            return Task.CompletedTask;
        }

        public void Probe(ProbeContext context)
        {
        }
    }

    private sealed class Consumer;
    private sealed class Message;
    private sealed class Service;
    private interface TestConsumeContext : ConsumeContext<Message>, ConsumeContext;
    private interface TestRegistrationContext : IRegistrationContext, ISetScopedConsumeContext;
    private sealed class ConstructedService(string value)
    {
        public string Value { get; } = value;
    }
}

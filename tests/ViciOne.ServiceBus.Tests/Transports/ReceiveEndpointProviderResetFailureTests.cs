using System.IO;
using System.Reflection;
using ViciOne.ServiceBus.Configuration;
using ViciOne.ServiceBus.Observables;
using ViciOne.ServiceBus.Tests.Infrastructure.Requirements;
using ViciOne.ServiceBus.Transports;
using ViciOne.ServiceBus.Util;
using Xunit;

namespace ViciOne.ServiceBus.Tests.Transports;

public sealed class ReceiveEndpointProviderResetFailureTests
{
    [Theory]
    [InlineData(false)]
    [InlineData(true)]
    [RequirementCoverage("REQ-VSB-RECEIVE-STOP", "reset-attempts-both-distinct-acquired-provider-releases")]
    public async Task ResetAsync_FirstProviderReleaseFaultStillRetiresDistinctSecondProviderAsync(bool firstProviderFault)
    {
        var fixture = new Fixture(firstProviderFault);
        Task? reset = null;
        Exception? permittedOrigin = firstProviderFault ? fixture.Expected : null;
        try
        {
            var context = new Context(fixture);
            fixture.Context = context;
            var first = Assert.IsType<Provider>(context.SendEndpointProvider);
            var second = Assert.IsType<Provider>(context.PublishEndpointProvider);
            Assert.NotSame(first, second);
            Assert.Same(first, context.SendEndpointProvider);
            Assert.Same(second, context.PublishEndpointProvider);
            Assert.Equal(1, fixture.SendCreations);
            Assert.Equal(1, fixture.PublishCreations);
            Assert.Equal(1, fixture.HostContexts.Count);
            Assert.Equal(1, first.Observers.Count);
            Assert.Equal(1, second.Observers.Count);

            reset = context.ResetAsync(CancellationToken.None).AsTask();
            await JoinAsync(first.Started.Task);
            Assert.False(reset.IsCompleted);
            Assert.Equal(1, first.DisposeAttempts);
            Assert.Equal(0, second.DisposeAttempts);
            Assert.Equal(1, first.Observers.Count);
            Assert.Equal(1, second.Observers.Count);

            first.Release.TrySetResult(true);
            Exception? actual = await Record.ExceptionAsync(() => JoinAsync(reset));
            if (firstProviderFault)
                Assert.Same(fixture.Expected, actual);
            else
                Assert.Null(actual);
            Assert.Equal(0, first.Observers.Count);
            Assert.Equal(1, first.DisposeAttempts);

            // This observes the second real registration before any retry or fallback release.
            Assert.Equal(0, second.Observers.Count);
            Assert.Equal(1, second.DisposeAttempts);
            Assert.NotNull(first.RawDisposal);
            Assert.NotNull(second.RawDisposal);

            // Cache renewal is required only after a successful reset.
            if (!firstProviderFault)
            {
                var nextSend = Assert.IsType<Provider>(context.SendEndpointProvider);
                var nextPublish = Assert.IsType<Provider>(context.PublishEndpointProvider);
                Assert.NotSame(first, nextSend);
                Assert.NotSame(second, nextPublish);
                Assert.NotSame(nextSend, nextPublish);
                Assert.Equal(2, fixture.SendCreations);
                Assert.Equal(2, fixture.PublishCreations);
                Assert.Equal(1, nextSend.Observers.Count);
                Assert.Equal(1, nextPublish.Observers.Count);
            }
        }
        finally
        {
            var failures = new List<Exception>();
            foreach (Provider provider in fixture.Providers)
            {
                provider.ThrowAfterRelease = false;
                provider.Release.TrySetResult(true);
            }
            foreach (Provider provider in fixture.Providers)
            {
                if (provider.RawDisposal is { } raw)
                    await AttemptAsync(() => ObserveOriginAsync(raw, provider == fixture.FirstSend ? permittedOrigin : null), failures);
            }
            if (reset is not null)
                await AttemptAsync(() => ObserveOriginAsync(reset, permittedOrigin), failures);
            if (fixture.Context is { } context)
            {
                Task? retry = null;
                await AttemptAsync(() =>
                {
                    retry = context.ResetAsync(CancellationToken.None).AsTask();
                    return JoinAsync(retry);
                }, failures);
                if (retry is not null)
                    await AttemptAsync(() => JoinAsync(retry), failures);
            }
            foreach (Provider provider in fixture.Providers)
                await AttemptAsync(() => JoinAsync(provider.DisposeAsync().AsTask()), failures);
            foreach (Provider provider in fixture.Providers)
            {
                if (provider.RawDisposal is { } raw)
                    await AttemptAsync(() => ObserveOriginAsync(raw, provider == fixture.FirstSend ? permittedOrigin : null), failures);
                Attempt(provider.Handle.Disconnect, failures);
                Attempt(() => Assert.Equal(0, provider.Observers.Count), failures);
            }
            foreach (ConnectHandle handle in fixture.HostHandles)
                Attempt(handle.Disconnect, failures);
            Attempt(() => Assert.Equal(0, fixture.HostContexts.Count), failures);
            if (failures.Count != 0)
                throw new AggregateException("Independent reset fixture retirement failed.", failures);
        }
    }

    static Task JoinAsync(Task task) => task.WaitAsync(TimeSpan.FromSeconds(10), CancellationToken.None);

    static async Task ObserveOriginAsync(Task task, Exception? expected)
    {
        try
        {
            await JoinAsync(task);
        }
        catch (Exception exception) when (expected is not null && ReferenceEquals(exception, expected))
        {
            // Only the exact injected cause on its two actual originating operations is expected.
        }
    }

    static async Task AttemptAsync(Func<Task> action, List<Exception> failures)
    {
        try { await action(); }
        catch (Exception exception) { failures.Add(exception); }
    }

    static void Attempt(Action action, List<Exception> failures)
    {
        try { action(); }
        catch (Exception exception) { failures.Add(exception); }
    }

    sealed class Fixture
    {
        readonly bool _firstProviderFault;
        public Fixture(bool firstProviderFault) => _firstProviderFault = firstProviderFault;
        public IOException Expected { get; } = new("reset-first-provider-owned-release");
        public List<Provider> Providers { get; } = [];
        public List<ConnectHandle> HostHandles { get; } = [];
        public Connectable<ReceiveEndpointContext> HostContexts { get; } = new();
        public Context? Context { get; set; }
        public Provider? FirstSend { get; private set; }
        public int SendCreations { get; private set; }
        public int PublishCreations { get; private set; }

        public Provider CreateSend()
        {
            var provider = new Provider(SendCreations == 0, SendCreations == 0 && _firstProviderFault ? Expected : null);
            SendCreations++;
            Providers.Add(provider);
            FirstSend ??= provider;
            return provider;
        }

        public Provider CreatePublish()
        {
            var provider = new Provider(false, null);
            PublishCreations++;
            Providers.Add(provider);
            return provider;
        }

        public IHostConfiguration CreateHost() => Proxy<IHostConfiguration>((method, args) =>
        {
            if (method.Name != nameof(IHostConfiguration.ConnectReceiveEndpointContext))
                throw Unexpected(method);
            var handle = HostContexts.Connect((ReceiveEndpointContext)args![0]!);
            HostHandles.Add(handle);
            return handle;
        });

        public IReceiveEndpointConfiguration CreateConfiguration()
        {
            var endpoints = new ReceiveEndpointObservable();
            var receives = new ReceiveObservable();
            var transports = new ReceiveTransportObservable();
            var publishTopology = Unused<IPublishTopologyConfigurator>();
            var topology = Proxy<ITopologyConfiguration>((method, _) => method.Name == "get_Publish" ? publishTopology : throw Unexpected(method));
            var serialization = Unused<ISerialization>();
            var serializerConfiguration = Proxy<ISerializationConfiguration>((method, _) =>
                method.Name == nameof(ISerializationConfiguration.CreateSerializerCollection) ? serialization : throw Unexpected(method));
            var send = Unused<ISendPipeConfiguration>();
            var publish = Unused<IPublishPipeConfiguration>();
            return Proxy<IReceiveEndpointConfiguration>((method, _) => method.Name switch
            {
                "get_InputAddress" => new Uri("loopback://localhost/provider-reset"),
                "get_HostAddress" => new Uri("loopback://localhost/"),
                "get_PublishFaults" => true,
                "get_PrefetchCount" => 16,
                "get_ConcurrentMessageLimit" => (int?)2,
                "get_IsBusEndpoint" => false,
                "get_Topology" => topology,
                "get_EndpointObservers" => endpoints,
                "get_ReceiveObservers" => receives,
                "get_TransportObservers" => transports,
                "get_DependenciesReady" => Task.CompletedTask,
                "get_DependentsCompleted" => Task.CompletedTask,
                "get_Serialization" => serializerConfiguration,
                "get_Send" => send,
                "get_Publish" => publish,
                _ => throw Unexpected(method)
            });
        }
    }

    sealed class Context(Fixture fixture) : BaseReceiveEndpointContext(fixture.CreateHost(), fixture.CreateConfiguration())
    {
        protected override ISendEndpointProvider CreateSendEndpointProvider() => fixture.CreateSend();
        protected override IPublishEndpointProvider CreatePublishEndpointProvider() => fixture.CreatePublish();
        protected override ISendTransportProvider CreateSendTransportProvider() => throw new InvalidOperationException("Unexpected transport resolution.");
        protected override IPublishTransportProvider CreatePublishTransportProvider() => throw new InvalidOperationException("Unexpected transport resolution.");
        public override void AddSendAgent(IAgent agent) => throw new InvalidOperationException("Unexpected send agent.");
        public override void AddConsumeAgent(IAgent agent) => throw new InvalidOperationException("Unexpected consume agent.");
        public override Exception ConvertException(Exception exception, string message) => throw new InvalidOperationException("Unexpected exception conversion.");
    }

    sealed class Provider : ISendEndpointProvider, IPublishEndpointProvider, IAsyncDisposable
    {
        readonly object _sync = new();
        readonly IOException? _failure;
        Task? _rawDisposal;
        int _disposeAttempts;
        volatile bool _throwAfterRelease;

        public Provider(bool held, IOException? failure)
        {
            _failure = failure;
            _throwAfterRelease = failure is not null;
            Handle = Observers.Connect(new Observer());
            if (!held)
                Release.TrySetResult(true);
        }

        public ReceiveEndpointObservable Observers { get; } = new();
        public ConnectHandle Handle { get; }
        public TaskCompletionSource<bool> Started { get; } = new(TaskCreationOptions.RunContinuationsAsynchronously);
        public TaskCompletionSource<bool> Release { get; } = new(TaskCreationOptions.RunContinuationsAsynchronously);
        public int DisposeAttempts => Volatile.Read(ref _disposeAttempts);
        public bool ThrowAfterRelease { set => _throwAfterRelease = value; }
        public Task? RawDisposal { get { lock (_sync) return _rawDisposal; } }

        public ValueTask DisposeAsync()
        {
            lock (_sync)
            {
                if (_rawDisposal is not null)
                    return _rawDisposal.IsCompleted ? ValueTask.CompletedTask : new ValueTask(_rawDisposal);
                Interlocked.Increment(ref _disposeAttempts);
                _rawDisposal = ReleaseOwnedRegistrationAsync();
                return new ValueTask(_rawDisposal);
            }
        }

        async Task ReleaseOwnedRegistrationAsync()
        {
            Started.TrySetResult(true);
            await Release.Task.ConfigureAwait(false);
            Handle.Disconnect();
            if (_throwAfterRelease && _failure is not null)
                throw _failure;
        }

        public Task<ISendEndpoint> GetSendEndpointAsync(Uri address, CancellationToken cancellationToken = default) =>
            throw new InvalidOperationException("Unexpected send endpoint resolution.");
        public Task<ISendEndpoint> GetPublishSendEndpointAsync<T>(CancellationToken cancellationToken = default) where T : class =>
            throw new InvalidOperationException("Unexpected publish endpoint resolution.");
        public ConnectHandle ConnectSendObserver(ISendObserver observer) => throw new InvalidOperationException("Unexpected send observer.");
        public ConnectHandle ConnectPublishObserver(IPublishObserver observer) => throw new InvalidOperationException("Unexpected publish observer.");
    }

    sealed class Observer : IReceiveEndpointObserver
    {
        public Task ReadyAsync(ReceiveEndpointReady ready) => throw new InvalidOperationException("Unexpected endpoint event.");
        public Task StoppingAsync(ReceiveEndpointStopping stopping) => throw new InvalidOperationException("Unexpected endpoint event.");
        public Task CompletedAsync(ReceiveEndpointCompleted completed) => throw new InvalidOperationException("Unexpected endpoint event.");
        public Task FaultedAsync(ReceiveEndpointFaulted faulted) => throw new InvalidOperationException("Unexpected endpoint event.");
    }

    static T Proxy<T>(Func<MethodInfo, object?[]?, object?> invoke) where T : class
    {
        T proxy = DispatchProxy.Create<T, ConfigurationProxy>();
        ((ConfigurationProxy)(object)proxy).Handler = invoke;
        return proxy;
    }

    static T Unused<T>() where T : class => Proxy<T>((method, _) => throw Unexpected(method));
    static InvalidOperationException Unexpected(MethodInfo method) => new($"Unexpected public SPI call: {method.DeclaringType?.Name}.{method.Name}");

    public class ConfigurationProxy : DispatchProxy
    {
        public Func<MethodInfo, object?[]?, object?> Handler { get; set; } = (_, _) => throw new InvalidOperationException("Unconfigured public SPI proxy.");
        protected override object? Invoke(MethodInfo? targetMethod, object?[]? args) => Handler(targetMethod ?? throw new InvalidOperationException("Missing public SPI method."), args);
    }
}

using System.Collections.Concurrent;
using System.Reflection;
using System.Runtime.ExceptionServices;
using Microsoft.Extensions.Logging;
using ViciOne.ServiceBus.AzureServiceBus.Configuration;
using ViciOne.ServiceBus.Logging;
using ViciOne.ServiceBus.Observables;
using ViciOne.ServiceBus.Tests.Infrastructure.Requirements;
using ViciOne.ServiceBus.Transports;
using Xunit;

namespace ViciOne.ServiceBus.AzureServiceBus.Tests;

public sealed class HostOwningEndpointDiagnosticsTests
{
    static readonly TimeSpan Bound = TimeSpan.FromSeconds(10);
    const string RequestedQueue = "requested-queue";
    const string RequestedSubscription = "requested-subscription";
    const string RequestedTopic = "requested-topic";

    [Theory]
    [InlineData("queue", false)]
    [InlineData("queue", true)]
    [InlineData("named-subscription", false)]
    [InlineData("named-subscription", true)]
    [InlineData("generic-subscription", false)]
    [InlineData("generic-subscription", true)]
    [RequirementCoverage("REQ-VSB-ASB-ENDPOINT-ADOPTION", "optional-owning-connect-debug-preserves-public-build-start-ready-stop")]
    public async Task ConnectEndpoint_OptionalOwningDebugDoesNotPreventActualEndpointAdoptionAsync(string route, bool hostile)
    {
        var previous = LogContext.Current;
        var fixture = new EndpointFixture();
        var logger = new SelectedLogger(route == "queue" ? "Connect receive endpoint: {InputAddress}" : "Connect subscription endpoint: {Topic}/{SubscriptionName}", hostile);
        IHostReceiveEndpointHandle? handle = null;
        Task? readyNotification = null;
        Task<ReceiveEndpointReady>? ready = null;
        Task? stop = null;
        Exception? primary = null;
        var cleanup = new List<Exception>();
        try
        {
            LogContext.ConfigureCurrentLogContext(logger);
            fixture.LogContext = LogContext.Current!;
            var host = new ServiceBusHost(fixture.HostConfiguration, fixture.BusTopology);
            Exception? observed = Record.Exception(() =>
            {
                handle = route switch
                {
                    "queue" => host.ConnectReceiveEndpoint(RequestedQueue, fixture.QueueCallback),
                    "named-subscription" => host.ConnectSubscriptionEndpoint(RequestedSubscription, RequestedTopic, fixture.SubscriptionCallback),
                    "generic-subscription" => host.ConnectSubscriptionEndpoint<PublicMessage>(RequestedSubscription, fixture.SubscriptionCallback),
                    _ => throw new InvalidOperationException("Unknown public route.")
                };
            });
            var factory = Assert.Single(fixture.FactoryCalls);
            Assert.Equal(route == "queue" ? "CreateReceiveEndpointConfiguration" : "CreateSubscriptionEndpointConfiguration", factory.Method.Name);
            Assert.Equal(route == "queue" ? RequestedQueue : RequestedSubscription, factory.Arguments[0]);
            if (route == "named-subscription") Assert.Equal(RequestedTopic, factory.Arguments[1]);
            if (route == "generic-subscription") Assert.Equal(typeof(PublicMessage), Assert.Single(factory.Method.GetGenericArguments()));
            else Assert.False(factory.Method.IsGenericMethod);
            Assert.Same(route == "queue" ? (object)fixture.QueueCallback : fixture.SubscriptionCallback, factory.Arguments[^1]);
            Assert.Equal(1, fixture.CallbackCalls);
            Assert.Same(route == "queue" ? fixture.QueueConfigurator : (object)fixture.SubscriptionConfigurator, fixture.CallbackTarget);
            if (route == "queue") Assert.Equal(1, fixture.ValidateCalls);
            var record = Assert.Single(logger.Records);
            Assert.Equal(logger.Template, record.Fields["{OriginalFormat}"]);
            Assert.Null(record.Cause);
            if (route == "queue") Assert.Equal(fixture.InputAddress, record.Fields["InputAddress"]);
            else
            {
                Assert.Equal(EndpointFixture.ConfiguredPath, record.Fields["Topic"]);
                Assert.Equal(EndpointFixture.ConfiguredName, record.Fields["SubscriptionName"]);
            }
            Assert.Equal(hostile ? 1 : 0, logger.ThrowCount);
            if (observed is not null) Assert.Same(logger.Failure, observed);
            // FIRST causal boundary: an optional owning Debug cannot replace mandatory public endpoint adoption.
            Assert.Null(observed);
            Assert.NotNull(handle);
            Assert.Equal(1, fixture.ValidateCalls);
            Assert.Equal(1, fixture.BuildCalls);
            Assert.Same(host, fixture.BuildHost);
            Assert.Equal(1, fixture.StartCalls);
            Assert.NotNull(fixture.Endpoint);
            Assert.Same(fixture.Endpoint, handle.ReceiveEndpoint);
            Assert.Equal(ReceiveEndpoint.State.Starting, fixture.Endpoint.CurrentState);
            ready = handle.Ready;
            Assert.False(ready.IsCompleted);
            readyNotification = fixture.NotifyReadyAsync();
            await readyNotification.WaitAsync(Bound, CancellationToken.None);
            ReceiveEndpointReady actualReady = await ready.WaitAsync(Bound, CancellationToken.None);
            Assert.Same(fixture.Endpoint, actualReady.ReceiveEndpoint);
            Assert.Equal(fixture.InputAddress, actualReady.InputAddress);
            Assert.True(actualReady.IsStarted);
            Assert.Equal(ReceiveEndpoint.State.Ready, fixture.Endpoint.CurrentState);
            BusHealthResult health = host.CheckHealth(BusState.Started, "public fixture started");
            Assert.Equal(BusHealthStatus.Healthy, health.Status);
            Assert.Single(health.Endpoints);
            stop = handle.StopAsync(CancellationToken.None);
            await Task.WhenAny(stop, fixture.StopEntered.Task).WaitAsync(Bound, CancellationToken.None);
            Assert.True(fixture.StopEntered.Task.IsCompletedSuccessfully);
            Assert.False(stop.IsCompleted);
            Assert.False(fixture.RawStop.IsCompleted);
            Assert.Equal(CancellationToken.None, fixture.StopToken);
            fixture.StopRelease.TrySetResult();
            await fixture.RawStop.WaitAsync(Bound, CancellationToken.None);
            await stop.WaitAsync(Bound, CancellationToken.None);
            foreach (Task reset in fixture.ResetTasks) await reset.WaitAsync(Bound, CancellationToken.None);
            Assert.Equal(1, fixture.StopCalls);
            Assert.Equal(1, fixture.ResetCalls);
            Assert.Equal(ReceiveEndpoint.State.Stopped, fixture.Endpoint.CurrentState);
            Assert.Empty(host.CheckHealth(BusState.Started, "public fixture stopped").Endpoints);
        }
        catch (Exception exception) { primary = exception; }
        finally
        {
            try
            {
                logger.Armed = false;
                await CaptureAsync(() => { fixture.StopRelease.TrySetResult(); return Task.CompletedTask; }, cleanup);
                if (fixture.StartCalls != 0 && readyNotification is null)
                    await CaptureAsync(() => { readyNotification = fixture.NotifyReadyAsync(); return Task.CompletedTask; }, cleanup);
                if (readyNotification is not null) await CaptureAsync(() => readyNotification.WaitAsync(Bound, CancellationToken.None), cleanup);
                if (ready is not null) await CaptureAsync(() => ready.WaitAsync(Bound, CancellationToken.None), cleanup);
                if (stop is null && handle is not null)
                    await CaptureAsync(() => { stop = handle.StopAsync(CancellationToken.None); return Task.CompletedTask; }, cleanup);
                else if (stop is null && fixture.StartCalls != 0 && fixture.Endpoint is not null)
                    await CaptureAsync(() => { stop = fixture.Endpoint.StopAsync(CancellationToken.None); return Task.CompletedTask; }, cleanup);
                if (stop is not null) await CaptureAsync(() => stop.WaitAsync(Bound, CancellationToken.None), cleanup);
                if (fixture.StopEntered.Task.IsCompletedSuccessfully)
                    await CaptureAsync(() => fixture.RawStop.WaitAsync(Bound, CancellationToken.None), cleanup);
                foreach (Task reset in fixture.ResetTasks)
                    await CaptureAsync(() => reset.WaitAsync(Bound, CancellationToken.None), cleanup);
                foreach (ConnectHandle registration in fixture.Registrations)
                {
                    await CaptureAsync(() => { registration.Disconnect(); return Task.CompletedTask; }, cleanup);
                    await CaptureAsync(() => { registration.Dispose(); return Task.CompletedTask; }, cleanup);
                }
            }
            finally { LogContext.Current = previous; }
        }
        if (cleanup.Count != 0)
        {
            if (primary is not null) cleanup.Insert(0, primary);
            throw new AggregateException("Host connection control and independent retirement failed.", cleanup);
        }
        if (primary is not null) ExceptionDispatchInfo.Capture(primary).Throw();
    }

    static async Task CaptureAsync(Func<Task> action, List<Exception> failures)
    {
        try { await action(); }
        catch (Exception exception) { failures.Add(exception); }
    }

    sealed record FactoryCall(MethodInfo Method, object?[] Arguments);
    sealed record LogRecord(Dictionary<string, object?> Fields, Exception? Cause);

    sealed class EndpointFixture
    {
        public const string ConfiguredPath = "configured-path";
        public const string ConfiguredName = "configured-subscription-name";
        readonly ReceiveEndpointObservable _observers = new();
        IReceiveTransportObserver? _transportObserver;
        public Uri InputAddress { get; } = new("sb://unit.servicebus.invalid/configured-path");
        public ILogContext LogContext = null!;
        public IServiceBusHostConfiguration HostConfiguration { get; }
        public IServiceBusBusTopology BusTopology { get; }
        public IServiceBusReceiveEndpointConfigurator QueueConfigurator { get; }
        public IServiceBusSubscriptionEndpointConfigurator SubscriptionConfigurator { get; }
        public Action<IServiceBusReceiveEndpointConfigurator> QueueCallback { get; }
        public Action<IServiceBusSubscriptionEndpointConfigurator> SubscriptionCallback { get; }
        public readonly ConcurrentQueue<FactoryCall> FactoryCalls = new();
        public readonly List<ConnectHandle> Registrations = new();
        public readonly List<Task> ResetTasks = new();
        public readonly TaskCompletionSource StopEntered = new(TaskCreationOptions.RunContinuationsAsynchronously);
        public readonly TaskCompletionSource StopRelease = new(TaskCreationOptions.RunContinuationsAsynchronously);
        public Task RawStop => StopRelease.Task;
        public CancellationToken StopToken;
        public object? CallbackTarget;
        public object? BuildHost;
        public ReceiveEndpoint? Endpoint;
        public int CallbackCalls, ValidateCalls, BuildCalls, StartCalls, StopCalls, ResetCalls;

        public EndpointFixture()
        {
            QueueConfigurator = InterfaceProxy<IServiceBusReceiveEndpointConfigurator>.Create(Unexpected);
            SubscriptionConfigurator = InterfaceProxy<IServiceBusSubscriptionEndpointConfigurator>.Create(Unexpected);
            QueueCallback = value => { CallbackCalls++; CallbackTarget = value; };
            SubscriptionCallback = value => { CallbackCalls++; CallbackTarget = value; };
            BusTopology = InterfaceProxy<IServiceBusBusTopology>.Create(Unexpected);
            ReceiveSettings queueSettings = InterfaceProxy<ReceiveSettings>.Create(Settings);
            SubscriptionSettings subscriptionSettings = InterfaceProxy<SubscriptionSettings>.Create(Settings);
            ReceiveEndpointContext endpointContext = InterfaceProxy<ReceiveEndpointContext>.Create((method, args) => method.Name switch
            {
                "get_InputAddress" => InputAddress,
                "get_IsBusEndpoint" => false,
                "get_LogContext" => LogContext,
                "get_EndpointObservers" => _observers,
                "get_DependentsCompleted" => Task.CompletedTask,
                "ConnectReceiveEndpointObserver" => Register((IReceiveEndpointObserver)args![0]!),
                "ResetAsync" => ResetAsync(),
                _ => throw new NotSupportedException(method.ToString())
            });
            ReceiveTransportHandle lifecycle = InterfaceProxy<ReceiveTransportHandle>.Create((method, args) =>
            {
                if (method.Name != "StopAsync") throw new NotSupportedException(method.ToString());
                StopToken = (CancellationToken)args![0]!;
                Interlocked.Increment(ref StopCalls);
                StopEntered.TrySetResult();
                return RawStop;
            });
            IReceiveTransport transport = InterfaceProxy<IReceiveTransport>.Create((method, args) =>
            {
                if (method.Name == "ConnectReceiveTransportObserver")
                {
                    _transportObserver = (IReceiveTransportObserver)args![0]!;
                    var registration = new NoopHandle();
                    Registrations.Add(registration);
                    return registration;
                }
                if (method.Name == "Start") { StartCalls++; return lifecycle; }
                throw new NotSupportedException(method.ToString());
            });
            object? Configuration(MethodInfo method, object?[]? args, object settings)
            {
                if (method.Name == "get_InputAddress") return InputAddress;
                if (method.Name == "get_Settings") return settings;
                if (method.Name == "Validate") { ValidateCalls++; return Array.Empty<ValidationResult>(); }
                if (method.Name == "Build")
                {
                    BuildCalls++;
                    BuildHost = args![0];
                    Endpoint = new ReceiveEndpoint(transport, endpointContext);
                    ((IHost)args[0]!).AddReceiveEndpoint(ConfiguredPath, Endpoint);
                    return null;
                }
                throw new NotSupportedException(method.ToString());
            }
            var queue = InterfaceProxy<IServiceBusReceiveEndpointConfiguration>.Create((method, args) => Configuration(method, args, queueSettings));
            var subscription = InterfaceProxy<IServiceBusSubscriptionEndpointConfiguration>.Create((method, args) => Configuration(method, args, subscriptionSettings));
            HostConfiguration = InterfaceProxy<IServiceBusHostConfiguration>.Create((method, args) =>
            {
                if (method.Name == "get_LogContext") return LogContext;
                if (method.Name == "get_HostAddress") return new Uri("sb://unit.servicebus.invalid/");
                if (method.Name == "CreateReceiveEndpointConfiguration" && args is { Length: 2 })
                {
                    FactoryCalls.Enqueue(new FactoryCall(method, args.ToArray()));
                    ((Action<IServiceBusReceiveEndpointConfigurator>?)args[1])?.Invoke(QueueConfigurator);
                    return queue;
                }
                if (method.Name == "CreateSubscriptionEndpointConfiguration" && args is { Length: 2 or 3 })
                {
                    FactoryCalls.Enqueue(new FactoryCall(method, args.ToArray()));
                    ((Action<IServiceBusSubscriptionEndpointConfigurator>?)args[^1])?.Invoke(SubscriptionConfigurator);
                    return subscription;
                }
                throw new NotSupportedException(method.ToString());
            });
        }

        static object? Unexpected(MethodInfo method, object?[]? args) => throw new NotSupportedException(method.ToString());
        static object Settings(MethodInfo method, object?[]? args) => method.Name switch
        {
            "get_Path" => ConfiguredPath,
            "get_Name" => ConfiguredName,
            _ => throw new NotSupportedException(method.ToString())
        };
        ConnectHandle Register(IReceiveEndpointObserver observer)
        {
            ConnectHandle registration = _observers.Connect(observer);
            Registrations.Add(registration);
            return registration;
        }
        ValueTask ResetAsync()
        {
            ResetCalls++;
            Task reset = Task.CompletedTask;
            ResetTasks.Add(reset);
            return new ValueTask(reset);
        }
        public Task NotifyReadyAsync() => (_transportObserver ?? throw new InvalidOperationException("No actual transport observer was registered."))
            .ReadyAsync(new PublicReady(InputAddress));
    }

    sealed class PublicReady(Uri inputAddress) : ReceiveTransportReady
    {
        public Uri InputAddress => inputAddress;
        public bool IsStarted => true;
    }
    sealed class NoopHandle : ConnectHandle
    {
        public void Disconnect() { }
        public void Dispose() => Disconnect();
    }
    public sealed class PublicMessage { public string Value { get; set; } = "public message contract"; }

    sealed class SelectedLogger(string template, bool hostile) : ILogger
    {
        public string Template => template;
        public readonly IOException Failure = new("unique owning Azure host Debug failure");
        public readonly ConcurrentQueue<LogRecord> Records = new();
        public volatile bool Armed = true;
        int _throws;
        public int ThrowCount => Volatile.Read(ref _throws);
        public bool IsEnabled(LogLevel level) => true;
        public IDisposable? BeginScope<TState>(TState state) where TState : notnull => null;
        public void Log<TState>(LogLevel level, EventId eventId, TState state, Exception? error, Func<TState, Exception?, string> formatter)
        {
            if (level != LogLevel.Debug || state is not IEnumerable<KeyValuePair<string, object?>> fields) return;
            var values = fields.ToDictionary(pair => pair.Key, pair => pair.Value);
            if (!values.TryGetValue("{OriginalFormat}", out var value) || !Equals(value, template)) return;
            Records.Enqueue(new LogRecord(values, error));
            if (Armed && hostile) { Interlocked.Increment(ref _throws); throw Failure; }
        }
    }

    public class InterfaceProxy<T> : DispatchProxy where T : class
    {
        Func<MethodInfo, object?[]?, object?> _handler = null!;
        public static T Create(Func<MethodInfo, object?[]?, object?> handler)
        {
            T value = Create<T, InterfaceProxy<T>>();
            ((InterfaceProxy<T>)(object)value)._handler = handler;
            return value;
        }
        protected override object? Invoke(MethodInfo? method, object?[]? args) =>
            _handler(method ?? throw new InvalidOperationException("Missing public interface method."), args);
    }
}

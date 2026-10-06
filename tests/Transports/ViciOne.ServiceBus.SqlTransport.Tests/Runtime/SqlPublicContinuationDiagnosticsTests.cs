using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Reflection;
using System.Runtime.ExceptionServices;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.Extensions.Logging;
using ViciOne.ServiceBus.SqlTransport.Configuration;
using ViciOne.ServiceBus.Logging;
using ViciOne.ServiceBus.Middleware;
using ViciOne.ServiceBus.Observables;
using ViciOne.ServiceBus.Tests.Infrastructure.Requirements;
using ViciOne.ServiceBus.Transports;
using Xunit;

namespace ViciOne.ServiceBus.SqlTransport.Tests.Runtime;

public sealed class SqlPublicContinuationDiagnosticsTests
{
    static readonly TimeSpan Bound = TimeSpan.FromSeconds(10);
    static readonly Uri HostAddress = new("db://localhost/public_diagnostics");
    const string Entity = "public-diagnostic-continuation";
    const string ConfiguredQueue = "actual-configured-queue";

    [Theory]
    [InlineData(false, false)]
    [InlineData(false, true)]
    [InlineData(true, false)]
    [InlineData(true, true)]
    [RequirementCoverage("REQ-VSB-SQL-ENDPOINT-DIAGNOSTICS", "connect-endpoint-own-debug-preserves-real-start")]
    public async Task ConnectReceiveEndpoint_OwnDebugPreservesRegisteredEndpointStartAsync(bool definitionOverload, bool hostile)
    {
        var previous = LogContext.Current;
        var logger = new SelectedLogger("Connect receive endpoint: {InputAddress}", hostile);
        IHostReceiveEndpointHandle? handle = null;
        Task? readyNotification = null;
        Task? stop = null;
        var fixture = new EndpointFixture();
        try
        {
            LogContext.ConfigureCurrentLogContext(logger);
            fixture.LogContext = LogContext.Current!;
            var host = new SqlHost(fixture.HostConfiguration, fixture.BusTopology);
            Exception? error = Record.Exception(() =>
            {
                Action<ISqlReceiveEndpointConfigurator> configure = _ =>
                {
                    fixture.CallbackCalls++;
                    fixture.Stages.Add("callback");
                };
                handle = definitionOverload
                    ? host.ConnectReceiveEndpoint(fixture.Definition, fixture.Formatter, configure)
                    : host.ConnectReceiveEndpoint(Entity, configure);
            });
            Assert.Equal(1, fixture.CreateCalls);
            Assert.Equal(1, fixture.CallbackCalls);
            Assert.Equal(1, fixture.ValidateCalls);
            Assert.Equal(definitionOverload ? new[] { "name", "create", "apply", "callback", "validate" }
                : new[] { "create", "callback", "validate" }, fixture.Stages.ToArray());
            AssertEmission(logger, "InputAddress", fixture.InputAddress, hostile, error);
            Assert.Null(error);

            Assert.NotNull(handle);
            Assert.Equal(1, fixture.BuildCalls);
            Assert.Equal(1, fixture.StartCalls);
            Assert.Same(fixture.Endpoint, handle.ReceiveEndpoint);
            Assert.Equal(fixture.InputAddress, handle.ReceiveEndpoint.InputAddress);
            readyNotification = fixture.NotifyReadyAsync();
            await readyNotification.WaitAsync(Bound, CancellationToken.None);
            ReceiveEndpointReady ready = await handle.Ready.WaitAsync(Bound, CancellationToken.None);
            Assert.Same(fixture.Endpoint, ready.ReceiveEndpoint);
            Assert.Equal(fixture.InputAddress, ready.InputAddress);
            Assert.Equal(ReceiveEndpoint.State.Ready, fixture.Endpoint!.CurrentState);
            stop = handle.StopAsync(CancellationToken.None);
            await stop.WaitAsync(Bound, CancellationToken.None);
            Assert.Equal(1, fixture.StopCalls);
            Assert.Equal(1, fixture.ResetCalls);
            Assert.Equal(ReceiveEndpoint.State.Stopped, fixture.Endpoint.CurrentState);
        }
        finally
        {
            logger.DisableFailure();
            var failures = new List<Exception>();
            try
            {
                // Ready is a real endpoint operation, not merely the notification's completion.
                if (handle is not null)
                {
                    if (readyNotification is null)
                        await CaptureAsync(async () =>
                        {
                            readyNotification = fixture.NotifyReadyAsync();
                            await readyNotification.WaitAsync(Bound, CancellationToken.None);
                        }, failures);
                    await CaptureAsync(() => ObserveAsync(readyNotification), failures);
                    await CaptureAsync(() => handle.Ready.WaitAsync(Bound, CancellationToken.None), failures);
                    await CaptureAsync(() => ObserveAsync(stop), failures);
                    await CaptureAsync(() => handle.StopAsync(CancellationToken.None).WaitAsync(Bound, CancellationToken.None), failures);
                }
                foreach (Task task in fixture.ResetTasks)
                    await CaptureAsync(() => task.WaitAsync(Bound, CancellationToken.None), failures);
                foreach (ConnectHandle registration in fixture.Registrations)
                    await CaptureAsync(() => { registration.Dispose(); return Task.CompletedTask; }, failures);
                ThrowCleanup(failures);
            }
            finally { LogContext.Current = previous; }
        }
    }

    [Theory]
    [InlineData(false, false)]
    [InlineData(false, true)]
    [InlineData(true, false)]
    [InlineData(true, true)]
    [RequirementCoverage("REQ-VSB-SQL-SEND-LIFECYCLE", "create-send-publish-own-debug-preserves-distinct-owner-admissions")]
    public async Task CreateTransport_OwnDebugPreservesEarlyChildAndConnectionOwnershipAsync(bool publish, bool hostile)
    {
        var previous = LogContext.Current;
        var logger = new SelectedLogger("Create send transport: {DestinationAddress}", hostile);
        var parent = new Supervisor();
        var admitted = new List<IAgent>();
        ConnectionContextSupervisor? supervisor = null;
        Task<ISendTransport>? operation = null;
        Task<ISendTransport>? canceled = null;
        CancellationToken canceledToken = default;
        var stops = new List<Task>();
        var factoryCalls = 0;
        var endpointReads = 0;
        try
        {
            LogContext.ConfigureCurrentLogContext(logger);
            var topology = new SqlTopologyConfiguration(SqlBusFactory.CreateMessageTopology());
            var host = InterfaceProxy<ISqlHostConfiguration>.Create((method, _) => method.Name switch
            {
                "get_HostAddress" => HostAddress,
                "get_LogContext" => LogContext.Current!,
                _ => throw new NotSupportedException(method.ToString())
            });
            var factory = InterfaceProxy<IPipeContextFactory<ConnectionContext>>.Create((method, _) =>
            {
                Interlocked.Increment(ref factoryCalls);
                throw new NotSupportedException("Creation-only route must not acquire a SQL connection: " + method);
            });
            supervisor = new ConnectionContextSupervisor(host, topology, factory);
            var parentFacade = InterfaceProxy<IClientContextSupervisor>.Create((method, args) =>
            {
                if (method.Name == "AddSendAgent")
                {
                    var agent = (IAgent)args![0]!;
                    parent.Add(agent);
                    admitted.Add(agent);
                    return null;
                }
                throw new NotSupportedException(method.ToString());
            });
            var serialization = InterfaceProxy<ISerialization>.Create((method, _) => throw new NotSupportedException(method.ToString()));
            var endpoint = InterfaceProxy<SqlReceiveEndpointContext>.Create((method, _) =>
            {
                endpointReads++;
                return method.Name switch
                {
                    "get_Serialization" => serialization,
                    "get_ClientContextSupervisor" => parentFacade,
                    _ => throw new NotSupportedException(method.ToString())
                };
            });
            Uri address = publish ? new Uri(HostAddress + "/caller-publish-address") : new Uri("queue:" + Entity);
            using var precanceled = new CancellationTokenSource();
            precanceled.Cancel();
            canceledToken = precanceled.Token;
            canceled = publish
                ? supervisor.CreatePublishTransportAsync<PublicMessage>(endpoint, address, precanceled.Token)
                : supervisor.CreateSendTransportAsync(endpoint, address, precanceled.Token);
            var cancellation = Assert.IsAssignableFrom<OperationCanceledException>(
                await Record.ExceptionAsync(() => canceled.WaitAsync(Bound, CancellationToken.None)));
            Assert.Equal(precanceled.Token, cancellation.CancellationToken);
            Assert.True(canceled.IsCanceled);
            Assert.Equal(0, endpointReads);
            Assert.Equal(0, factoryCalls);
            Assert.Empty(admitted);
            Assert.Empty(logger.Emissions);

            Exception? error = Record.Exception(() =>
            {
                operation = publish
                    ? supervisor.CreatePublishTransportAsync<PublicMessage>(endpoint, address, CancellationToken.None)
                    : supervisor.CreateSendTransportAsync(endpoint, address, CancellationToken.None);
            });
            if (error is null && operation is not null)
                error = await Record.ExceptionAsync(() => operation.WaitAsync(Bound, CancellationToken.None));
            IAgent child = Assert.Single(admitted);
            Assert.IsType<ClientContextSupervisor>(child);
            Assert.Equal(1, parent.TotalCount);
            Assert.Equal(2, endpointReads);
            Assert.Equal(0, factoryCalls);
            AssertEmission(logger, "DestinationAddress", publish ? address : supervisor.NormalizeAddress(address), hostile, error);
            Assert.Null(error);

            Assert.NotNull(operation);
            var transport = Assert.IsType<SendTransport<ClientContext>>(await operation.WaitAsync(Bound, CancellationToken.None));
            Assert.Equal(1, transport.TotalCount);
            Assert.NotSame(child, transport);
            // The connection owns the transport; the endpoint parent owns its earlier client child.
            Task connectionStop = supervisor.StopAsync(new FixtureStopContext(), CancellationToken.None);
            stops.Add(connectionStop);
            await connectionStop.WaitAsync(Bound, CancellationToken.None);
            await supervisor.Completed.WaitAsync(Bound, CancellationToken.None);
            await transport.Completed.WaitAsync(Bound, CancellationToken.None);
            await child.Completed.WaitAsync(Bound, CancellationToken.None);
            Assert.True(transport.Stopped.IsCancellationRequested);
            Assert.True(child.Stopped.IsCancellationRequested);
            Assert.Equal(0, factoryCalls);
        }
        finally
        {
            logger.DisableFailure();
            var failures = new List<Exception>();
            try
            {
                await CaptureAsync(() => ObserveAsync(operation, logger.Failure), failures);
                if (canceled is not null)
                    await CaptureAsync(async () =>
                    {
                        try { await canceled.WaitAsync(Bound, CancellationToken.None); }
                        catch (OperationCanceledException cancellation) when (canceled.IsCanceled && cancellation.CancellationToken == canceledToken) { }
                    }, failures);
                foreach (Task task in stops)
                    await CaptureAsync(() => ObserveAsync(task), failures);
                if (supervisor is not null)
                {
                    await CaptureAsync(() => supervisor.StopAsync(new FixtureStopContext(), CancellationToken.None).WaitAsync(Bound, CancellationToken.None), failures);
                    await CaptureAsync(() => supervisor.Completed.WaitAsync(Bound, CancellationToken.None), failures);
                }
                if (operation is { IsCompletedSuccessfully: true })
                {
                    var returned = (SendTransport<ClientContext>)await operation.WaitAsync(Bound, CancellationToken.None);
                    await CaptureAsync(() => returned.StopAsync(new FixtureStopContext(), CancellationToken.None).WaitAsync(Bound, CancellationToken.None), failures);
                    await CaptureAsync(() => returned.Completed.WaitAsync(Bound, CancellationToken.None), failures);
                }
                foreach (IAgent agent in admitted)
                {
                    await CaptureAsync(() => agent.StopAsync(new FixtureStopContext(), CancellationToken.None).WaitAsync(Bound, CancellationToken.None), failures);
                    await CaptureAsync(() => agent.Completed.WaitAsync(Bound, CancellationToken.None), failures);
                }
                await CaptureAsync(() => parent.StopAsync(new FixtureStopContext(), CancellationToken.None).WaitAsync(Bound, CancellationToken.None), failures);
                await CaptureAsync(() => parent.Completed.WaitAsync(Bound, CancellationToken.None), failures);
                ThrowCleanup(failures);
            }
            finally { LogContext.Current = previous; }
        }
    }

    static void AssertEmission(SelectedLogger logger, string argument, object expected, bool hostile, Exception? error)
    {
        var emission = Assert.Single(logger.Emissions);
        Assert.Equal(logger.Template, emission["{OriginalFormat}"]);
        Assert.Equal(expected, emission[argument]);
        Assert.Equal(hostile ? 1 : 0, logger.ThrowCount);
        if (error is not null)
            Assert.Same(logger.Failure, error);
    }

    static async Task ObserveAsync(Task? task, Exception? expected = null)
    {
        if (task is null)
            return;
        try { await task.WaitAsync(Bound, CancellationToken.None); }
        catch (Exception error) when (expected is not null && ReferenceEquals(error, expected) && task.IsFaulted) { }
    }

    static async Task CaptureAsync(Func<Task> action, List<Exception> failures)
    {
        try { await action(); }
        catch (Exception error) { failures.Add(error); }
    }

    static void ThrowCleanup(List<Exception> failures)
    {
        if (failures.Count == 1)
            ExceptionDispatchInfo.Capture(failures[0]).Throw();
        if (failures.Count > 1)
            throw new AggregateException("Public SQL fixture cleanup failed.", failures);
    }

    sealed class FixtureStopContext : BasePipeContext, StopContext
    {
        public string Reason => "public SQL fixture cleanup";
    }

    sealed class SelectedLogger(string template, bool hostile) : ILogger
    {
        int _enabled = hostile ? 1 : 0;
        int _throwCount;
        public string Template => template;
        public IOException Failure { get; } = new("unique owning SQL continuation diagnostic: " + template);
        public ConcurrentQueue<IReadOnlyDictionary<string, object?>> Emissions { get; } = new();
        public int ThrowCount => Volatile.Read(ref _throwCount);
        public bool IsEnabled(LogLevel level) => true;
        public IDisposable? BeginScope<TState>(TState state) where TState : notnull => null;
        public void DisableFailure() => Volatile.Write(ref _enabled, 0);
        public void Log<TState>(LogLevel level, EventId eventId, TState state, Exception? error, Func<TState, Exception?, string> formatter)
        {
            if (state is not IEnumerable<KeyValuePair<string, object?>> pairs)
                return;
            var data = pairs.ToDictionary(pair => pair.Key, pair => pair.Value);
            if (!data.TryGetValue("{OriginalFormat}", out object? value) || !Equals(value, template))
                return;
            Emissions.Enqueue(data);
            if (Volatile.Read(ref _enabled) != 0)
            {
                Interlocked.Increment(ref _throwCount);
                throw Failure;
            }
        }
    }

    sealed class EndpointFixture
    {
        readonly ReceiveEndpointObservable _observers = new();
        IReceiveTransportObserver? _transportObserver;
        public Uri InputAddress { get; } = new(HostAddress + "/" + ConfiguredQueue);
        public ILogContext LogContext { get; set; } = null!;
        public ISqlHostConfiguration HostConfiguration { get; }
        public ISqlBusTopology BusTopology { get; }
        public ReceiveEndpoint? Endpoint { get; private set; }
        public List<ConnectHandle> Registrations { get; } = new();
        public List<Task> ResetTasks { get; } = new();
        public List<string> Stages { get; } = new();
        public IEndpointDefinition Definition { get; }
        public IEndpointNameFormatter Formatter { get; }
        public int CreateCalls;
        public int CallbackCalls;
        public int ValidateCalls;
        public int BuildCalls;
        public int StartCalls;
        public int StopCalls;
        public int ResetCalls;

        public EndpointFixture()
        {
            Formatter = InterfaceProxy<IEndpointNameFormatter>.Create((method, _) => throw new NotSupportedException(method.ToString()));
            Definition = InterfaceProxy<IEndpointDefinition>.Create((method, args) =>
            {
                if (method.Name != "GetEndpointName") throw new NotSupportedException(method.ToString());
                Assert.Same(Formatter, args![0]);
                Stages.Add("name");
                return Entity;
            });
            var settings = InterfaceProxy<ReceiveSettings>.Create((method, _) => method.Name == "get_QueueName"
                ? ConfiguredQueue : throw new NotSupportedException(method.ToString()));
            BusTopology = InterfaceProxy<ISqlBusTopology>.Create((method, _) => throw new NotSupportedException(method.ToString()));
            var context = InterfaceProxy<ReceiveEndpointContext>.Create((method, args) =>
            {
                switch (method.Name)
                {
                    case "get_InputAddress": return InputAddress;
                    case "get_IsBusEndpoint": return false;
                    case "get_LogContext": return LogContext;
                    case "get_EndpointObservers": return _observers;
                    case "get_DependentsCompleted": return Task.CompletedTask;
                    case "ConnectReceiveEndpointObserver":
                        ConnectHandle registration = _observers.Connect((IReceiveEndpointObserver)args![0]!);
                        Registrations.Add(registration);
                        return registration;
                    case "ResetAsync":
                        ResetCalls++;
                        Task reset = Task.CompletedTask;
                        ResetTasks.Add(reset);
                        return new ValueTask(reset);
                    default: throw new NotSupportedException(method.ToString());
                }
            });
            var lifecycle = InterfaceProxy<ReceiveTransportHandle>.Create((method, _) =>
            {
                if (method.Name != "StopAsync") throw new NotSupportedException(method.ToString());
                StopCalls++;
                return Task.CompletedTask;
            });
            var transport = InterfaceProxy<IReceiveTransport>.Create((method, args) =>
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
            var configuration = InterfaceProxy<ISqlReceiveEndpointConfiguration>.Create((method, args) =>
            {
                if (method.Name == "get_InputAddress") return InputAddress;
                if (method.Name == "get_Settings") return settings;
                if (method.Name == "Validate") { ValidateCalls++; Stages.Add("validate"); return Array.Empty<ValidationResult>(); }
                if (method.Name == "Build")
                {
                    BuildCalls++;
                    Endpoint = new ReceiveEndpoint(transport, context);
                    ((IHost)args![0]!).AddReceiveEndpoint(ConfiguredQueue, Endpoint);
                    return null;
                }
                throw new NotSupportedException(method.ToString());
            });
            var configurator = InterfaceProxy<ISqlReceiveEndpointConfigurator>.Create((method, _) => throw new NotSupportedException(method.ToString()));
            HostConfiguration = InterfaceProxy<ISqlHostConfiguration>.Create((method, args) =>
            {
                if (method.Name == "get_LogContext") return LogContext;
                if (method.Name == "get_HostAddress") return HostAddress;
                if (method.Name == "CreateReceiveEndpointConfiguration" && args is { Length: 2 } && args[0] is string name)
                {
                    Assert.Equal(Entity, name);
                    CreateCalls++;
                    Stages.Add("create");
                    ((Action<ISqlReceiveEndpointConfigurator>?)args[1])?.Invoke(configurator);
                    return configuration;
                }
                if (method.Name == "ApplyEndpointDefinition")
                {
                    Assert.Same(configurator, args![0]);
                    Assert.Same(Definition, args[1]);
                    Stages.Add("apply");
                    return null;
                }
                throw new NotSupportedException(method.ToString());
            });
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

    public sealed class PublicMessage { public string Value { get; set; } = "public SQL publish contract"; }

    public class InterfaceProxy<T> : DispatchProxy where T : class
    {
        Func<MethodInfo, object?[]?, object?> _handler = null!;
        public static T Create(Func<MethodInfo, object?[]?, object?> handler)
        {
            T proxy = Create<T, InterfaceProxy<T>>();
            ((InterfaceProxy<T>)(object)proxy)._handler = handler;
            return proxy;
        }
        protected override object? Invoke(MethodInfo? targetMethod, object?[]? args) =>
            _handler(targetMethod ?? throw new InvalidOperationException("Missing public interface method."), args);
    }
}

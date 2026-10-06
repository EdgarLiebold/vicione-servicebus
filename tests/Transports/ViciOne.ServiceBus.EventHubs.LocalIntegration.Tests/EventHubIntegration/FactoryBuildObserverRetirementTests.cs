using System.Reflection;
using System.Runtime.ExceptionServices;
using Microsoft.Extensions.Logging.Abstractions;
using ViciOne.ServiceBus.EventHubs.Configuration;
using ViciOne.ServiceBus.Logging;
using ViciOne.ServiceBus.Observables;
using ViciOne.ServiceBus.Tests.Infrastructure.Requirements;
using ViciOne.ServiceBus.Topology;
using ViciOne.ServiceBus.Transports;
using Xunit;

namespace ViciOne.ServiceBus.EventHubs.LocalIntegration.Tests.EventHubIntegration;

public sealed class FactoryBuildObserverRetirementTests
{
    static readonly TimeSpan Bound = TimeSpan.FromSeconds(10);

    [Theory]
    [InlineData(false)]
    [InlineData(true)]
    [RequirementCoverage("REQ-VSB-EVENTHUB-RECEIVE-ADMISSION", "factory-build-retires-returned-send-observer-registration-after-later-configuration-rejection")]
    public async Task Build_LaterConfigurationRejectionRetiresItsActuallyReturnedObserverRegistrationAsync(bool reject)
    {
        var previous = LogContext.Current;
        var factory = new EventHubFactoryConfigurator();
        var busObservers = new SendObservable();
        var probe = new NoSendObserver();
        ConnectHandle probeHandle = busObservers.Connect(probe);
        var failure = new IOException("unique actual receive configuration admission rejection");
        EventHubProducerSendTransportContext? transport = null;
        IConnectionContextSupervisor? connection = null;
        IAgent? producer = null;
        IEventHubRider? rider = null;
        RiderHandle? riderHandle = null;
        Task? riderStop = null;
        Task? connectionStop = null;
        Task? producerStop = null;
        SendObservable? subject = null;
        Exception? primary = null;
        var cleanup = new List<Exception>();
        int createCalls = 0, callbacks = 0, countAtRejection = -1;
        string? createdEndpoint = null;
        object? configurationCallback = new object();
        ISendObserver[]? connectedAtRejection = null;
        try
        {
            LogContext.ConfigureCurrentLogContext(NullLogger.Instance);
            factory.Host("Endpoint=sb://unit.servicebus.invalid/;SharedAccessKeyName=tests;SharedAccessKey=AAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAA=");
            factory.Storage(new Uri("https://unit.invalid/checkpoints"));
            IBusTopology topology = InterfaceProxy<IBusTopology>.Create((method, _) =>
                method.Name == "get_SendTopology" ? new SendTopology() : throw new NotSupportedException(method.ToString()));
            IHostConfiguration host = InterfaceProxy<IHostConfiguration>.Create((method, args) =>
            {
                if (method.Name == "CreateReceiveEndpointConfiguration")
                {
                    Interlocked.Increment(ref createCalls);
                    createdEndpoint = (string)args![0]!;
                    configurationCallback = args[1];
                    countAtRejection = subject!.Count;
                    connectedAtRejection = subject!.Connected.ToArray();
                    if (!reject) throw new InvalidOperationException("The empty healthy factory requested an endpoint.");
                    throw failure;
                }
                return method.Name switch
                {
                    "get_SendObservers" => busObservers,
                    "get_Topology" => topology,
                    "get_HostAddress" => new Uri("sb://unit.servicebus.invalid/"),
                    _ => throw new NotSupportedException(method.ToString())
                };
            });
            IBusInstance bus = InterfaceProxy<IBusInstance>.Create((method, _) =>
                method.Name == "get_HostConfiguration" ? host : throw new NotSupportedException(method.ToString()));
            IRiderRegistrationContext registration = InterfaceProxy<IRiderRegistrationContext>.Create((method, _) =>
                throw new NotSupportedException(method.ToString()));
            // Capture the original supervisor before Stop-triggered Recycle replaces its lazy owner.
            connection = factory.ConnectionContextSupervisor;
            transport = Assert.IsType<EventHubProducerSendTransportContext>(factory.CreateSendTransportContext("orders", bus));
            producer = Assert.Single(transport.GetAgentHandles());
            subject = Assert.IsType<SendObservable>(Assert.Single(transport.SendObservers.Connected));
            Assert.Equal(0, subject.Count);
            Assert.Empty(subject.Connected);
            Assert.Equal(1, busObservers.Count);
            if (reject)
                factory.ReceiveEndpoint("orders", "group", _ => Interlocked.Increment(ref callbacks));
            Exception? observed = Record.Exception(() => { rider = factory.Build(registration, bus); });
            if (reject)
            {
                Assert.Same(failure, observed);
                Assert.Null(rider);
                Assert.Equal(1, Volatile.Read(ref createCalls));
                Assert.EndsWith("/orders/group", createdEndpoint, StringComparison.Ordinal);
                Assert.Null(configurationCallback);
                Assert.Equal(1, countAtRejection);
                Assert.Same(busObservers, Assert.Single(connectedAtRejection!));
                Assert.Equal(0, Volatile.Read(ref callbacks));
                Assert.Equal(1, busObservers.Count);
                Assert.Equal(0, probe.Calls);
                // FIRST causal boundary: main observes the actual registration before any fixture retirement.
                Assert.Equal(0, subject.Count);
                Assert.Empty(subject.Connected);
            }
            else
            {
                Assert.Null(observed);
                EventHubRider healthyRider = Assert.IsType<EventHubRider>(rider);
                Assert.Equal(0, Volatile.Read(ref createCalls));
                Assert.Equal(1, subject.Count);
                Assert.Same(busObservers, Assert.Single(subject.Connected));
                Assert.Empty(healthyRider.CheckEndpointHealth());
                riderHandle = healthyRider.Start(CancellationToken.None);
                await riderHandle.Ready.WaitAsync(Bound, CancellationToken.None);
                riderStop = riderHandle.StopAsync(CancellationToken.None);
                await riderStop.WaitAsync(Bound, CancellationToken.None);
                await connection.Completed.WaitAsync(Bound, CancellationToken.None);
                await producer.Completed.WaitAsync(Bound, CancellationToken.None);
                Assert.True(riderStop.IsCompletedSuccessfully);
                Assert.Equal(0, probe.Calls);
            }
        }
        catch (Exception exception) { primary = exception; }
        finally
        {
            try
            {
                if (riderHandle is not null)
                {
                    await CaptureAsync(() => riderHandle.Ready.WaitAsync(Bound, CancellationToken.None), cleanup);
                    if (riderStop is null)
                        await CaptureAsync(() => { riderStop = riderHandle.StopAsync(CancellationToken.None); return Task.CompletedTask; }, cleanup);
                    if (riderStop is not null) await CaptureAsync(() => riderStop.WaitAsync(Bound, CancellationToken.None), cleanup);
                }
                if (connection is not null)
                {
                    await CaptureAsync(() => { connectionStop = connection.StopAsync("Independent original connection retirement", CancellationToken.None); return Task.CompletedTask; }, cleanup);
                    if (connectionStop is not null) await CaptureAsync(() => connectionStop.WaitAsync(Bound, CancellationToken.None), cleanup);
                    await CaptureAsync(() => connection.Completed.WaitAsync(Bound, CancellationToken.None), cleanup);
                }
                if (producer is not null)
                {
                    await CaptureAsync(() => { producerStop = producer.StopAsync("Independent actual producer retirement", CancellationToken.None); return Task.CompletedTask; }, cleanup);
                    if (producerStop is not null) await CaptureAsync(() => producerStop.WaitAsync(Bound, CancellationToken.None), cleanup);
                    await CaptureAsync(() => producer.Completed.WaitAsync(Bound, CancellationToken.None), cleanup);
                }
                await CaptureAsync(() => { probeHandle.Disconnect(); return Task.CompletedTask; }, cleanup);
                await CaptureAsync(() => { probeHandle.Dispose(); return Task.CompletedTask; }, cleanup);
                // The lost specification handle has no public recovery API. The Original managed-only subject stays visibly unretired.
                // No native client, send operation, partition worker, token WaitHandle or private task was activated by this fixture.
            }
            finally { LogContext.Current = previous; }
        }
        if (cleanup.Count != 0)
        {
            if (primary is not null) cleanup.Insert(0, primary);
            throw new AggregateException("Factory build control and independent retirement failed.", cleanup);
        }
        if (primary is not null) ExceptionDispatchInfo.Capture(primary).Throw();
    }

    static async Task CaptureAsync(Func<Task> action, List<Exception> failures)
    {
        try { await action(); }
        catch (Exception exception) { failures.Add(exception); }
    }
    sealed class NoSendObserver : ISendObserver
    {
        int _calls;
        public int Calls => Volatile.Read(ref _calls);
        Task UnexpectedAsync() { Interlocked.Increment(ref _calls); throw new InvalidOperationException("Unexpected SDK send activation."); }
        public Task PreSendAsync<T>(SendContext<T> context) where T : class => UnexpectedAsync();
        public Task PostSendAsync<T>(SendContext<T> context) where T : class => UnexpectedAsync();
        public Task SendFaultAsync<T>(SendContext<T> context, Exception exception) where T : class => UnexpectedAsync();
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
        protected override object? Invoke(MethodInfo? method, object?[]? args) => _handler(method ?? throw new InvalidOperationException("Missing public SPI method."), args);
    }
}

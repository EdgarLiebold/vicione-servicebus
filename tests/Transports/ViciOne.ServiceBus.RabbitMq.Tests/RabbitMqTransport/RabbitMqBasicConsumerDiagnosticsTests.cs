using System.Collections.Generic;
using System.IO;
using System.Reflection;
using System.Runtime.ExceptionServices;
using Microsoft.Extensions.Logging;
using RabbitMQ.Client;
using RabbitMQ.Client.Events;
using RabbitMQ.Client.Exceptions;
using ViciOne.ServiceBus.Advanced.Middleware;
using ViciOne.ServiceBus.Advanced.Observers;
using ViciOne.ServiceBus.Logging;
using ViciOne.ServiceBus.RabbitMq.Configuration;
using ViciOne.ServiceBus.Tests.Infrastructure.Requirements;
using ViciOne.ServiceBus.Transports;
using Xunit;

namespace ViciOne.ServiceBus.RabbitMq.Tests.RabbitMqTransport;

public sealed class RabbitMqBasicConsumerDiagnosticsTests
{
    static readonly TimeSpan WaitBound = TimeSpan.FromSeconds(10);
    const string Tag = "diagnostic-consumer-tag";

    [Theory]
    [InlineData(0, false)]
    [InlineData(0, true)]
    [InlineData(1, false)]
    [InlineData(1, true)]
    [InlineData(2, false)]
    [InlineData(2, true)]
    [InlineData(3, false)]
    [InlineData(3, true)]
    [InlineData(4, false)]
    [InlineData(4, true)]
    [InlineData(5, false)]
    [InlineData(5, true)]
    [RequirementCoverage("REQ-VSB-RABBITMQ-CONSUMER-LIFECYCLE", "owning-callback-stop-and-delivery-diagnostics-preserve-public-outcomes")]
    public async Task Callbacks_OwningDiagnosticsPreservePublicOutcomesAsync(int mode, bool hostile)
    {
        var logger = new SelectedLogger(mode, hostile);
        ILogContext? previous = LogContext.Current;
        Fixture? fixture = null;
        Task? operation = null;
        Task? delivery = null;
        Exception? primary = null;
        var cleanup = new List<Exception>();
        try
        {
            LogContext.ConfigureCurrentLogContext(logger);
            fixture = new Fixture(mode);
            if (mode != 0)
            {
                await fixture.Consumer.HandleBasicConsumeOkAsync(Tag, CancellationToken.None).WaitAsync(WaitBound, CancellationToken.None);
                await fixture.Consumer.Ready.WaitAsync(WaitBound, CancellationToken.None);
            }

            logger.EnableThrow();
            if (mode == 4)
            {
                delivery = fixture.DeliverAsync();
                await fixture.Receive.Entered.Task.WaitAsync(WaitBound, CancellationToken.None);
            }

            Exception? invocationFailure = Record.Exception(() =>
            {
                operation = mode switch
                {
                    0 => fixture.Consumer.HandleBasicConsumeOkAsync(Tag, CancellationToken.None),
                    1 => fixture.Consumer.HandleBasicCancelOkAsync(Tag, CancellationToken.None),
                    2 => fixture.Consumer.HandleBasicCancelAsync(Tag, CancellationToken.None),
                    3 => fixture.Consumer.HandleChannelShutdownAsync(fixture.Channel.Channel, fixture.Reason),
                    4 => fixture.Consumer.StopAsync("diagnostic public stop", CancellationToken.None),
                    5 => fixture.DeliverAsync(),
                    _ => throw new ArgumentOutOfRangeException(nameof(mode))
                };
            });

            if (mode == 4)
            {
                await fixture.Broker.CancelEntered.Task.WaitAsync(WaitBound, CancellationToken.None);
                fixture.Broker.ReleaseCancel();
                Exception? rawFailure = await CaptureTerminalAsync(fixture.Broker.CancelTask);
                Assert.Same(fixture.Broker.CancelFailure, rawFailure);
                fixture.Receive.Release.TrySetResult();
                await delivery!.WaitAsync(WaitBound, CancellationToken.None);
                await fixture.Receive.ActualTask!.WaitAsync(WaitBound, CancellationToken.None);
            }
            else if (mode == 5)
            {
                await fixture.Receive.Entered.Task.WaitAsync(WaitBound, CancellationToken.None);
                fixture.Receive.Release.TrySetResult();
                Exception? rawFailure = await CaptureTerminalAsync(fixture.Receive.ActualTask!);
                Assert.Same(fixture.Receive.Failure, rawFailure);
            }

            Exception? operationFailure = invocationFailure;
            if (operation is not null)
                operationFailure = await CaptureTerminalAsync(operation);
            var entry = Assert.Single(logger.Snapshot(), x => x.Template.StartsWith(logger.SelectedPrefix, StringComparison.Ordinal)
                && x.Level == logger.SelectedLevel);
            Assert.Equal(fixture.Endpoint.InputAddress, entry.Values["InputAddress"]);
            Assert.Equal(Tag, entry.Values["ConsumerTag"]);
            Assert.Equal(hostile ? 1 : 0, logger.ThrowCount);
            if (mode == 3)
            {
                Assert.Equal(fixture.Reason.ReplyCode, entry.Values["ReplyCode"]);
                Assert.Equal(fixture.Reason.ReplyText, entry.Values["ReplyText"]);
            }
            if (mode == 4) Assert.Same(fixture.Broker.CancelFailure, entry.Exception);
            if (mode == 5) Assert.Same(fixture.Receive.Failure, entry.Exception);
            if (hostile && operationFailure is not null) Assert.Same(logger.Failure, operationFailure);
            Assert.Null(operationFailure);

            if (mode == 0)
            {
                Assert.NotNull(operation);
                await fixture.Consumer.Ready.WaitAsync(WaitBound, CancellationToken.None);
                Assert.Equal(Tag, ((RabbitMqDeliveryMetrics)fixture.Consumer).ConsumerTag);
                Assert.Equal(1, fixture.Broker.ShutdownAdds);
            }
            if (mode is 4 or 5)
            {
                Assert.Equal(1, fixture.Receive.Calls);
                Assert.Equal(1, fixture.Consumer.DeliveryCount);
            }
            if (mode == 4)
            {
                Assert.Equal(1, fixture.Broker.CancelCalls);
                Assert.Equal(Tag, fixture.Broker.CancelTag);
                Assert.False(fixture.Broker.CancelNoWait);
                Assert.Equal(CancellationToken.None, fixture.Broker.CancelToken);
            }
            if (mode == 5)
            {
                await fixture.ChannelAgent.Completed.WaitAsync(WaitBound, CancellationToken.None);
                Assert.Equal(1, fixture.ChannelAgent.StopCalls);
                Assert.Same(fixture.Receive.Failure, Assert.Single(fixture.Observer.Faults));
            }
            logger.DisableThrow();
            await fixture.Consumer.StopAsync("healthy final stop", CancellationToken.None).WaitAsync(WaitBound, CancellationToken.None);
            await fixture.Consumer.Completed.WaitAsync(WaitBound, CancellationToken.None);
        }
        catch (Exception exception)
        {
            primary = exception;
        }
        finally
        {
            try
            {
                logger.DisableThrow();
                if (fixture is not null)
                {
                    fixture.Broker.ReleaseCancel();
                    fixture.Receive.Release.TrySetResult();
                    await TryCleanupAsync(() => ObserveExpectedAsync(delivery, logger, fixture), cleanup);
                    await TryCleanupAsync(() => ObserveExpectedAsync(operation, logger, fixture), cleanup);
                    await TryCleanupAsync(() => ObserveExpectedAsync(fixture.Receive.ActualTask, logger, fixture), cleanup);
                    if (fixture.Broker.CancelCalls != 0)
                        await TryCleanupAsync(() => ObserveExpectedAsync(fixture.Broker.CancelTask, logger, fixture), cleanup);
                    await TryCleanupAsync(() => fixture.Consumer.HandleBasicCancelOkAsync(Tag, CancellationToken.None), cleanup);
                    // A terminal faulted Completed remains authoritative; this retry is only observed.
                    await TryCleanupAsync(() => ObserveExpectedAsync(fixture.Consumer.StopAsync("fixture retirement", CancellationToken.None), logger, fixture), cleanup);
                    await TryCleanupAsync(() => ObserveExpectedAsync(fixture.Consumer.Completed, logger, fixture), cleanup);
                    await TryCleanupAsync(() => fixture.ChannelAgent.StopAsync("fixture channel agent retirement", CancellationToken.None), cleanup);
                    await TryCleanupAsync(() => fixture.ChannelAgent.Completed, cleanup);
                    await TryCleanupAsync(() => fixture.Channel.DisposeAsync().AsTask(), cleanup);
                    await TryCleanupAsync(() => fixture.Connection.DisposeAsync().AsTask(), cleanup);
                    await TryCleanupAsync(() => fixture.Endpoint.ResetAsync().AsTask(), cleanup);
                    await TryCleanupAsync(() => { fixture.ObserverHandle.Disconnect(); return Task.CompletedTask; }, cleanup);
                }
            }
            finally
            {
                LogContext.Current = previous;
            }
        }
        if (cleanup.Count != 0)
        {
            if (primary is not null) cleanup.Insert(0, primary);
            if (cleanup.Count == 1) ExceptionDispatchInfo.Capture(cleanup[0]).Throw();
            throw new AggregateException("Basic consumer assertions and fixture retirement failed.", cleanup);
        }
        if (primary is not null) ExceptionDispatchInfo.Capture(primary).Throw();
    }

    static async Task<Exception?> CaptureTerminalAsync(Task task)
    {
        try { await task.WaitAsync(WaitBound, CancellationToken.None); return null; }
        catch (Exception exception) when (task.IsCompleted && exception is not TimeoutException) { return exception; }
    }

    static async Task ObserveExpectedAsync(Task? task, SelectedLogger logger, Fixture fixture)
    {
        if (task is null) return;
        Exception? failure = await CaptureTerminalAsync(task);
        if (failure is not null && !ReferenceEquals(failure, logger.Failure)
            && !ReferenceEquals(failure, fixture.Receive.Failure) && !ReferenceEquals(failure, fixture.Broker.CancelFailure))
            ExceptionDispatchInfo.Capture(failure).Throw();
    }

    static async Task TryCleanupAsync(Func<Task> action, List<Exception> failures)
    {
        try { await action().WaitAsync(WaitBound, CancellationToken.None); }
        catch (Exception exception) { failures.Add(exception); }
    }

    sealed class Fixture
    {
        public Fixture(int mode)
        {
            var bus = new RabbitMqBusConfiguration(new RabbitMqTopologyConfiguration(RabbitMqBusFactory.CreateMessageTopology()));
            bus.HostConfiguration.LogContext = LogContext.Current;
            var child = bus.CreateEndpointConfiguration(false);
            child.AutoStart = false;
            var settings = new RabbitMqReceiveSettings(child, "basic-diagnostic-public", ExchangeType.Fanout, true, false) { NoAck = true };
            var configuration = new RabbitMqReceiveEndpointConfiguration(bus.HostConfiguration, settings, child);
            Receive = new HeldReceiveFilter(mode == 5);
            configuration.ConfigureReceive(x => x.UseFilter(Receive));
            Endpoint = (RabbitMqReceiveEndpointContext)configuration.CreateReceiveEndpointContext();
            ObserverHandle = Endpoint.ConnectReceiveObserver(Observer);
            IConnection nativeConnection = DispatchProxy.Create<IConnection, NativeConnection>();
            Connection = new RabbitMqConnectionContext(nativeConnection, bus.HostConfiguration, "public basic-consumer fixture", CancellationToken.None);
            IChannel nativeChannel = DispatchProxy.Create<IChannel, NativeChannel>();
            Broker = (NativeChannel)(object)nativeChannel;
            Broker.FailCancel = mode == 4;
            if (mode != 4) Broker.ReleaseCancel();
            Channel = new RabbitMqChannelContext(Connection, nativeChannel, ChannelAgent, CancellationToken.None);
            Channel.GetOrAddPayload<ReceiveSettings>(() => settings);
            Consumer = new RabbitMqBasicConsumer(Channel, Endpoint);
        }
        public RabbitMqReceiveEndpointContext Endpoint { get; }
        public RabbitMqConnectionContext Connection { get; }
        public RabbitMqChannelContext Channel { get; }
        public RabbitMqBasicConsumer Consumer { get; }
        public NativeChannel Broker { get; }
        public HeldReceiveFilter Receive { get; }
        public RecordingAgent ChannelAgent { get; } = new();
        public ReceiveObserver Observer { get; } = new();
        public ConnectHandle ObserverHandle { get; }
        public ShutdownEventArgs Reason { get; } = new(ShutdownInitiator.Peer, 320, "unique public shutdown");
        public Task DeliverAsync() => Consumer.HandleBasicDeliverAsync(Tag, 17, false, "public-exchange", "public-routing",
            new BasicProperties { MessageId = "basic-diagnostic-message" }, new byte[] { 1, 2, 3 }, CancellationToken.None);
    }

    sealed class HeldReceiveFilter(bool fail) : IFilter<ReceiveContext>
    {
        int _calls;
        public int Calls => Volatile.Read(ref _calls);
        public TaskCompletionSource Entered { get; } = new(TaskCreationOptions.RunContinuationsAsynchronously);
        public TaskCompletionSource Release { get; } = new(TaskCreationOptions.RunContinuationsAsynchronously);
        public OperationInterruptedException Failure { get; } = new(new ShutdownEventArgs(ShutdownInitiator.Peer, 541, "unique dispatch primary"));
        public Task? ActualTask { get; private set; }
        public void Probe(ProbeContext context) { }
        public Task SendAsync(ReceiveContext context, IPipe<ReceiveContext> next)
        {
            Interlocked.Increment(ref _calls);
            ActualTask = SendCoreAsync();
            Entered.TrySetResult();
            return ActualTask;
        }
        async Task SendCoreAsync()
        {
            await Release.Task.ConfigureAwait(false);
            if (fail) throw Failure;
        }
    }

    sealed class RecordingAgent : Agent
    {
        int _stopCalls;
        public int StopCalls => Volatile.Read(ref _stopCalls);
        protected override Task StopAgentAsync(StopContext context)
        {
            Interlocked.Increment(ref _stopCalls);
            return base.StopAgentAsync(context);
        }
    }

    public class NativeChannel : DispatchProxy
    {
        readonly TaskCompletionSource _cancel = new(TaskCreationOptions.RunContinuationsAsynchronously);
        int _cancelCalls;
        int _shutdownAdds;
        public bool FailCancel { get; set; }
        public IOException CancelFailure { get; } = new("unique BasicCancel backend primary intentionally handled");
        public TaskCompletionSource CancelEntered { get; } = new(TaskCreationOptions.RunContinuationsAsynchronously);
        public Task CancelTask => _cancel.Task;
        public int CancelCalls => Volatile.Read(ref _cancelCalls);
        public int ShutdownAdds => Volatile.Read(ref _shutdownAdds);
        public string? CancelTag { get; private set; }
        public bool CancelNoWait { get; private set; }
        public CancellationToken CancelToken { get; private set; }
        public void ReleaseCancel()
        {
            if (FailCancel && CancelCalls != 0) _cancel.TrySetException(CancelFailure);
            else _cancel.TrySetResult();
        }
        protected override object? Invoke(MethodInfo? method, object?[]? args)
        {
            if (method?.Name == "get_IsOpen") return true;
            if (method?.Name == "add_ChannelShutdownAsync") { Interlocked.Increment(ref _shutdownAdds); return null; }
            if (method?.Name == "remove_ChannelShutdownAsync") return null;
            if (method?.Name == nameof(IChannel.BasicCancelAsync) && args is { Length: 3 })
            {
                CancelTag = (string)args[0]!;
                CancelNoWait = (bool)args[1]!;
                CancelToken = (CancellationToken)args[2]!;
                Interlocked.Increment(ref _cancelCalls);
                CancelEntered.TrySetResult();
                return CancelTask;
            }
            if (method?.Name == nameof(IChannel.CloseAsync) && args is { Length: 4 }) return Task.CompletedTask;
            if (method?.Name == nameof(IAsyncDisposable.DisposeAsync) && args is null or { Length: 0 }) return ValueTask.CompletedTask;
            throw new NotSupportedException(method?.ToString());
        }
    }

    public class NativeConnection : DispatchProxy
    {
        protected override object? Invoke(MethodInfo? method, object?[]? args)
        {
            if (method?.Name == "get_IsOpen") return true;
            if (method?.Name == nameof(IConnection.CloseAsync) && args is { Length: 5 }) return Task.CompletedTask;
            if (method?.Name == nameof(IAsyncDisposable.DisposeAsync) && args is null or { Length: 0 }) return ValueTask.CompletedTask;
            throw new NotSupportedException(method?.ToString());
        }
    }

    sealed class ReceiveObserver : IReceiveObserver
    {
        public List<Exception> Faults { get; } = new();
        public Task PreReceiveAsync(ReceiveContext context) => Task.CompletedTask;
        public Task PostReceiveAsync(ReceiveContext context) => Task.CompletedTask;
        public Task PostConsumeAsync<T>(ConsumeContext<T> context, TimeSpan duration, string consumerType) where T : class => Task.CompletedTask;
        public Task ConsumeFaultAsync<T>(ConsumeContext<T> context, TimeSpan duration, string consumerType, Exception exception) where T : class => Task.CompletedTask;
        public Task ReceiveFaultAsync(ReceiveContext context, Exception exception) { Faults.Add(exception); return Task.CompletedTask; }
    }

    sealed class SelectedLogger(int mode, bool hostile) : ILogger
    {
        readonly object _gate = new();
        readonly List<LogEntry> _entries = new();
        int _enabled;
        int _throws;
        public IOException Failure { get; } = new("unique owning basic-consumer diagnostic failure");
        public int ThrowCount => Volatile.Read(ref _throws);
        public string SelectedPrefix => mode switch
        {
            0 => "Consumer Ok:", 1 => "Consumer Cancel Ok:", 2 => "Consumer Canceled:",
            3 => "Consumer Channel Shutdown:", 4 => "BasicCancel faulted:", 5 => "Consumer Channel Shutdown:",
            _ => throw new ArgumentOutOfRangeException(nameof(mode))
        };
        public LogLevel SelectedLevel => mode == 4 ? LogLevel.Warning : mode == 5 ? LogLevel.Error : LogLevel.Debug;
        public void EnableThrow() => Interlocked.Exchange(ref _enabled, hostile ? 1 : 0);
        public void DisableThrow() => Interlocked.Exchange(ref _enabled, 0);
        public LogEntry[] Snapshot() { lock (_gate) return _entries.ToArray(); }
        public IDisposable? BeginScope<TState>(TState state) where TState : notnull => null;
        public bool IsEnabled(LogLevel level) => true;
        public void Log<TState>(LogLevel level, EventId id, TState state, Exception? exception, Func<TState, Exception?, string> formatter)
        {
            var values = ((IEnumerable<KeyValuePair<string, object?>>)(object)state!).ToDictionary(x => x.Key, x => x.Value);
            var template = (string)values["{OriginalFormat}"]!;
            lock (_gate) _entries.Add(new LogEntry(level, template, values, exception));
            if (level == SelectedLevel && template.StartsWith(SelectedPrefix, StringComparison.Ordinal) && Volatile.Read(ref _enabled) != 0)
            {
                Interlocked.Increment(ref _throws);
                throw Failure;
            }
        }
    }

    sealed record LogEntry(LogLevel Level, string Template, Dictionary<string, object?> Values, Exception? Exception);
}

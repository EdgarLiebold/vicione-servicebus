using System.Collections.Generic;
using System.Reflection;
using System.Runtime.ExceptionServices;
using Microsoft.Extensions.Logging;
using RabbitMQ.Client;
using ViciOne.ServiceBus.RabbitMq.Configuration;
using ViciOne.ServiceBus.Tests.Infrastructure.Requirements;
using Xunit;

namespace ViciOne.ServiceBus.RabbitMq.Tests.RabbitMqTransport;

public sealed class RabbitMqConnectionDisposeDiagnosticsTests
{
    const string Description = "public-owned-rabbit-connection-disposal";
    const string Disconnect = "Disconnect: {Host}";
    const string Disconnected = "Disconnected: {Host}";
    static readonly TimeSpan WaitBound = TimeSpan.FromSeconds(10);

    [Theory]
    [InlineData(0, false)]
    [InlineData(0, true)]
    [InlineData(1, false)]
    [InlineData(1, true)]
    [InlineData(2, false)]
    [InlineData(2, true)]
    [RequirementCoverage("REQ-VSB-RABBITMQ-CONNECTION-LIFECYCLE", "connection-dispose-own-debug-preserves-native-cleanup")]
    public async Task Dispose_OwnDiagnosticsPreserveNativeCleanupAndPrimaryFailureAsync(int mode, bool hostileLogger)
    {
        using var caller = new CancellationTokenSource();
        caller.Cancel();
        var native = DispatchProxy.Create<IConnection, BrokerConnection>();
        var broker = (BrokerConnection)(object)native;
        broker.Mode = mode;
        var diagnosticFailure = new IOException("unique selected Rabbit connection diagnostic failure");
        var logger = new SelectedLogger(mode == 0 ? Disconnect : Disconnected, hostileLogger, diagnosticFailure);
        var previous = LogContext.Current;
        RabbitMqConnectionContext? context = null;
        Task? operation = null;
        Exception? primary = null;
        try
        {
            LogContext.ConfigureCurrentLogContext(logger);
            var topology = new RabbitMqTopologyConfiguration(RabbitMqBusFactory.CreateMessageTopology());
            var configuration = new RabbitMqBusConfiguration(topology);
            context = new RabbitMqConnectionContext(native, configuration.HostConfiguration, Description, caller.Token);
            Assert.True(context.CancellationToken.IsCancellationRequested);

            // The public ValueTask is materialized exactly once; a synchronous invocation fault is observed separately.
            var invocationFailure = Record.Exception(() => { operation = context.DisposeAsync().AsTask(); });
            Assert.Null(invocationFailure);
            Assert.NotNull(operation);
            await Task.WhenAny(broker.CloseEntered.Task, operation).WaitAsync(WaitBound, CancellationToken.None);
            if (broker.CloseEntered.Task.IsCompleted)
            {
                Assert.False(broker.RawCloseTask.IsCompleted);
                Assert.False(operation.IsCompleted);
                Assert.Equal(0, broker.DisposeCalls);
                broker.ReleaseClose();
                await broker.DisposeEntered.Task.WaitAsync(WaitBound, CancellationToken.None);
                Assert.True(broker.RawCloseTask.IsCompleted);
                Assert.False(broker.RawDisposeTask.IsCompleted);
                Assert.False(operation.IsCompleted);
                broker.ReleaseDispose();
            }
            var observed = await Record.ExceptionAsync(() => operation.WaitAsync(WaitBound, CancellationToken.None));
            Assert.True(operation.IsCompleted);
            if (mode == 2)
            {
                Assert.DoesNotContain(logger.Entries, x => x.Template == Disconnected);
                Assert.Equal(0, logger.ThrowCount);
            }
            else
            {
                var selected = Assert.Single(logger.Entries, x => x.Template == logger.Template);
                Assert.Equal(Description, selected.Host);
                Assert.Null(selected.Exception);
                Assert.Equal(hostileLogger ? 1 : 0, logger.ThrowCount);
                if (observed != null)
                    Assert.Same(diagnosticFailure, observed);
            }

            // First finite admission oracle: optional disconnect diagnostics must permit actual native cleanup.
            Assert.Equal(1, broker.CloseCalls);
            Assert.Equal(1, broker.DisposeCalls);
            Assert.Equal((ushort)200, broker.CloseCode);
            Assert.Equal("Connection Disposed", broker.CloseText);
            Assert.Equal(TimeSpan.FromSeconds(30), broker.CloseTimeout);
            Assert.False(broker.CloseAbort);
            Assert.Equal(CancellationToken.None, broker.CloseToken);
            Assert.Equal(new[] { "close", "dispose" }, broker.Trace);
            if (mode == 2)
            {
                Assert.True(broker.RawCloseTask.IsFaulted);
                Assert.Same(broker.CloseFailure, Assert.Single(broker.RawCloseTask.Exception!.InnerExceptions));
                Assert.True(broker.RawDisposeTask.IsFaulted);
                Assert.Same(broker.DisposeFailure, Assert.Single(broker.RawDisposeTask.Exception!.InnerExceptions));
                Assert.Same(broker.DisposeFailure, observed);
                Assert.True(operation.IsFaulted);
                Assert.Same(broker.DisposeFailure, Assert.Single(operation.Exception!.InnerExceptions));
                Assert.Equal(Disconnect, Assert.Single(logger.Entries).Template);
            }
            else
            {
                Assert.True(broker.RawCloseTask.IsCompletedSuccessfully);
                Assert.True(broker.RawDisposeTask.IsCompletedSuccessfully);
                Assert.Null(observed);
                Assert.True(operation.IsCompletedSuccessfully);
                Assert.Equal(new[] { Disconnect, Disconnected }, logger.Entries.Select(x => x.Template));
            }
        }
        catch (Exception exception)
        {
            primary = exception;
            throw;
        }
        finally
        {
            logger.DisableThrow();
            broker.ReleaseClose();
            broker.ReleaseDispose();
            var failures = new List<Exception>();
            try
            {
                await CaptureCleanupAsync(() => ObserveTerminalAsync(operation, diagnosticFailure, broker), failures);
                // A failed pre-cleanup diagnostic did not start the lifetime owner. Public cleanup is still required.
                if (context != null)
                    await CaptureCleanupAsync(() => ObserveTerminalAsync(context.DisposeAsync().AsTask(), diagnosticFailure, broker), failures);
                await CaptureCleanupAsync(() => ObserveTerminalAsync(broker.RawCloseTask, diagnosticFailure, broker), failures);
                await CaptureCleanupAsync(() => ObserveTerminalAsync(broker.RawDisposeTask, diagnosticFailure, broker), failures);
            }
            finally
            {
                LogContext.Current = previous;
            }
            if (failures.Count != 0)
            {
                if (primary != null)
                    failures.Insert(0, primary);
                if (failures.Count == 1)
                    ExceptionDispatchInfo.Capture(failures[0]).Throw();
                throw new AggregateException(failures);
            }
        }
    }

    static async Task CaptureCleanupAsync(Func<Task> action, List<Exception> failures)
    {
        try { await action(); }
        catch (Exception exception) { failures.Add(exception); }
    }

    static async Task ObserveTerminalAsync(Task? task, Exception diagnosticFailure, BrokerConnection broker)
    {
        if (task == null)
            return;
        try { await task.WaitAsync(WaitBound, CancellationToken.None); }
        catch (Exception exception) when (task.IsCompleted && exception is not TimeoutException
            && (ReferenceEquals(exception, diagnosticFailure) || ReferenceEquals(exception, broker.CloseFailure)
                || ReferenceEquals(exception, broker.DisposeFailure)))
        {
        }
    }

    public class BrokerConnection : DispatchProxy
    {
        readonly TaskCompletionSource _close = new(TaskCreationOptions.RunContinuationsAsynchronously);
        readonly TaskCompletionSource _dispose = new(TaskCreationOptions.RunContinuationsAsynchronously);
        int _closeCalls;
        int _disposeCalls;
        public int Mode { get; set; }
        public IOException CloseFailure { get; } = new("unique native close failure intentionally suppressed");
        public IOException DisposeFailure { get; } = new("unique native disposal primary failure");
        public TaskCompletionSource CloseEntered { get; } = new(TaskCreationOptions.RunContinuationsAsynchronously);
        public TaskCompletionSource DisposeEntered { get; } = new(TaskCreationOptions.RunContinuationsAsynchronously);
        public Task RawCloseTask => _close.Task;
        public Task RawDisposeTask => _dispose.Task;
        public int CloseCalls => Volatile.Read(ref _closeCalls);
        public int DisposeCalls => Volatile.Read(ref _disposeCalls);
        public ushort CloseCode { get; private set; }
        public string? CloseText { get; private set; }
        public TimeSpan CloseTimeout { get; private set; }
        public bool CloseAbort { get; private set; }
        public CancellationToken CloseToken { get; private set; }
        public List<string> Trace { get; } = new();

        public void ReleaseClose()
        {
            if (Mode == 2) _close.TrySetException(CloseFailure);
            else _close.TrySetResult();
        }
        public void ReleaseDispose()
        {
            if (Mode == 2) _dispose.TrySetException(DisposeFailure);
            else _dispose.TrySetResult();
        }
        protected override object? Invoke(MethodInfo? method, object?[]? args)
        {
            if (method?.Name == "get_IsOpen")
                return true;
            if (method?.Name == nameof(IConnection.CloseAsync) && args is { Length: 5 })
            {
                CloseCode = (ushort)args[0]!;
                CloseText = (string)args[1]!;
                CloseTimeout = (TimeSpan)args[2]!;
                CloseAbort = (bool)args[3]!;
                CloseToken = (CancellationToken)args[4]!;
                Trace.Add("close");
                Interlocked.Increment(ref _closeCalls);
                CloseEntered.TrySetResult();
                return RawCloseTask;
            }
            if (method?.Name == nameof(IAsyncDisposable.DisposeAsync) && args is null or { Length: 0 })
            {
                Trace.Add("dispose");
                Interlocked.Increment(ref _disposeCalls);
                DisposeEntered.TrySetResult();
                return new ValueTask(RawDisposeTask);
            }
            throw new NotSupportedException(method?.ToString());
        }
    }

    sealed class SelectedLogger(string template, bool hostile, Exception failure) : ILogger
    {
        int _throwEnabled = hostile ? 1 : 0;
        public string Template => template;
        public List<LogEntry> Entries { get; } = new();
        public int ThrowCount { get; private set; }
        public void DisableThrow() => Interlocked.Exchange(ref _throwEnabled, 0);
        public IDisposable? BeginScope<TState>(TState state) where TState : notnull => null;
        public bool IsEnabled(LogLevel level) => level == LogLevel.Debug;
        public void Log<TState>(LogLevel level, EventId eventId, TState state, Exception? exception,
            Func<TState, Exception?, string> formatter)
        {
            if (level != LogLevel.Debug)
                return;
            var values = ((IEnumerable<KeyValuePair<string, object?>>)(object)state!).ToDictionary(x => x.Key, x => x.Value);
            var actual = (string)values["{OriginalFormat}"]!;
            if (actual != Disconnect && actual != Disconnected)
                return;
            Entries.Add(new LogEntry(actual, (string)values["Host"]!, exception));
            if (actual == template && Volatile.Read(ref _throwEnabled) != 0)
            {
                ThrowCount++;
                throw failure;
            }
        }
    }
    sealed record LogEntry(string Template, string Host, Exception? Exception);
}

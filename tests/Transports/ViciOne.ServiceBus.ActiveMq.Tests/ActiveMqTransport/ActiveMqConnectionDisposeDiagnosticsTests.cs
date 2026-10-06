using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Runtime.ExceptionServices;
using System.Threading;
using System.Threading.Tasks;
using Apache.NMS;
using Microsoft.Extensions.Logging;
using ViciOne.ServiceBus.ActiveMq.Configuration;
using ViciOne.ServiceBus.ActiveMq.Tests.TestDoubles;
using ViciOne.ServiceBus.Advanced;
using ViciOne.ServiceBus.Tests.Infrastructure.Requirements;
using Xunit;

namespace ViciOne.ServiceBus.ActiveMq.Tests.ActiveMqTransport;

public sealed class ActiveMqConnectionDisposeDiagnosticsTests
{
    static readonly TimeSpan WaitBound = TimeSpan.FromSeconds(10);

    [Theory]
    [InlineData(0, false)]
    [InlineData(0, true)]
    [InlineData(1, false)]
    [InlineData(1, true)]
    [InlineData(2, false)]
    [InlineData(2, true)]
    [RequirementCoverage("REQ-VSB-ACTIVEMQ-LIFECYCLE", "connection-own-debug-preserves-cleanup-and-native-close-primary")]
    public async Task ConnectionDispose_OwnDebugPreservesCleanupAndCloseFailureAsync(int mode, bool hostileLogger)
    {
        var previousContext = LogContext.Current;
        var closeEntered = new TaskCompletionSource(TaskCreationOptions.RunContinuationsAsynchronously);
        var close = new TaskCompletionSource(TaskCreationOptions.RunContinuationsAsynchronously);
        var closeFailure = new NMSException("unique held native close failure");
        var diagnosticFailure = new IOException("unique selected connection diagnostic failure");
        var logger = new SelectedLogger(mode == 0 ? "Disconnect: {Host}" : "Disconnected: {Host}", hostileLogger, diagnosticFailure);
        var events = new ConcurrentQueue<string>();
        int closeCalls = 0;
        int disposeCalls = 0;
        IConnection connection = InterfaceProxy<IConnection>.Create((method, _) =>
        {
            if (method.Name == nameof(IConnection.CloseAsync))
            {
                Interlocked.Increment(ref closeCalls);
                events.Enqueue("close");
                closeEntered.TrySetResult();
                return close.Task;
            }
            if (method.Name == nameof(IDisposable.Dispose))
            {
                Interlocked.Increment(ref disposeCalls);
                events.Enqueue("dispose");
                return null;
            }
            throw new NotSupportedException(method.Name);
        });
        ActiveMqConnectionContext? context = null;
        Task? operation = null;
        Exception? primary = null;
        try
        {
            LogContext.ConfigureCurrentLogContext(logger);
            var topology = new ActiveMqTopologyConfiguration(ActiveMqBusFactory.CreateMessageTopology());
            var busConfiguration = new ActiveMqBusConfiguration(topology);
            busConfiguration.HostConfiguration.Settings = new OpenWireHostSettings(new Uri("activemq://broker.internal:61616"));
            context = new ActiveMqConnectionContext(connection, busConfiguration.HostConfiguration, CancellationToken.None);
            operation = context.DisposeAsync().AsTask();

            var ready = await Task.WhenAny(operation, closeEntered.Task).WaitAsync(WaitBound, CancellationToken.None);
            bool admitted = closeEntered.Task.IsCompletedSuccessfully;
            if (admitted)
            {
                Assert.Same(closeEntered.Task, ready);
                Assert.Equal(1, Volatile.Read(ref closeCalls));
                Assert.False(close.Task.IsCompleted);
                Assert.False(operation.IsCompleted);
                Assert.Equal(0, Volatile.Read(ref disposeCalls));
                if (mode == 2)
                    close.TrySetException(closeFailure);
                else
                    close.TrySetResult();
            }
            else
            {
                Assert.Same(operation, ready);
                Assert.True(operation.IsCompleted);
                Assert.Equal(0, mode);
                Assert.True(hostileLogger);
                Assert.Equal(0, Volatile.Read(ref disposeCalls));
            }

            var observed = await Record.ExceptionAsync(() => operation.WaitAsync(WaitBound, CancellationToken.None));
            Assert.True(operation.IsCompleted);
            var emission = Assert.Single(logger.Emissions);
            Assert.Equal(logger.Template, emission["{OriginalFormat}"]);
            Assert.Equal(context.Description, emission["Host"]);
            Assert.Equal(hostileLogger ? 1 : 0, logger.ThrowCount);
            if (admitted)
            {
                if (mode == 2)
                {
                    Assert.True(close.Task.IsFaulted);
                    Assert.Same(closeFailure, Assert.Single(close.Task.Exception!.InnerExceptions));
                }
                else
                    Assert.True(close.Task.IsCompletedSuccessfully);
            }
            if (mode == 0)
            {
                if (observed is not null)
                    Assert.Same(diagnosticFailure, observed);
                Assert.Equal(1, Volatile.Read(ref closeCalls));
                Assert.Null(observed);
            }
            else
            {
                Assert.True(admitted);
                Assert.Equal(1, Volatile.Read(ref disposeCalls));
                if (mode == 2)
                    Assert.Same(closeFailure, observed);
                else
                {
                    if (observed is not null)
                        Assert.Same(diagnosticFailure, observed);
                    Assert.Null(observed);
                }
            }
            Assert.Equal(1, Volatile.Read(ref disposeCalls));
            Assert.Equal(new[] { "close", "dispose" }, events.ToArray());
        }
        catch (Exception exception)
        {
            primary = exception;
            throw;
        }
        finally
        {
            logger.DisableThrow();
            close.TrySetResult();
            var cleanupFailures = new List<Exception>();
            try
            {
                await CaptureCleanupAsync(() => ObserveTerminalAsync(operation), cleanupFailures);
                await CaptureCleanupAsync(() => ObserveTerminalAsync(close.Task), cleanupFailures);
                // A pre-close diagnostic may have prevented all actual owned cleanup.
                // Retry only after the earlier public operation is terminal, never concurrently.
                if (context is not null && (operation is null || operation.IsCompleted)
                    && Volatile.Read(ref disposeCalls) == 0)
                {
                    await CaptureCleanupAsync(() => ObserveTerminalAsync(context.DisposeAsync().AsTask()), cleanupFailures);
                }
                if (cleanupFailures.Count != 0)
                {
                    if (primary is not null)
                        cleanupFailures.Insert(0, primary);
                    if (cleanupFailures.Count == 1)
                        ExceptionDispatchInfo.Capture(cleanupFailures[0]).Throw();
                    throw new AggregateException("Owned connection fixture cleanup did not finish.", cleanupFailures);
                }
            }
            finally
            {
                LogContext.Current = previousContext;
            }
        }
    }

    static async Task CaptureCleanupAsync(Func<Task> cleanup, List<Exception> failures)
    {
        try { await cleanup(); }
        catch (Exception exception) { failures.Add(exception); }
    }

    static async Task ObserveTerminalAsync(Task? task)
    {
        if (task is null)
            return;
        try { await task.WaitAsync(WaitBound, CancellationToken.None); }
        catch (Exception exception) when (exception is not TimeoutException
            && ((task.IsCanceled && exception is OperationCanceledException)
                || (task.IsFaulted && task.Exception!.InnerExceptions.Any(cause => ReferenceEquals(cause, exception)))))
        {
        }
    }

    sealed class SelectedLogger(string template, bool hostile, Exception failure) : ILogger
    {
        int _throwEnabled = hostile ? 1 : 0;
        int _throwCount;
        public string Template => template;
        public ConcurrentQueue<Dictionary<string, object?>> Emissions { get; } = new();
        public int ThrowCount => Volatile.Read(ref _throwCount);
        public void DisableThrow() => Volatile.Write(ref _throwEnabled, 0);
        public bool IsEnabled(LogLevel level) => level == LogLevel.Debug;
        public IDisposable BeginScope<TState>(TState state) where TState : notnull => new EmptyScope();
        public void Log<TState>(LogLevel level, EventId eventId, TState state, Exception? exception, Func<TState, Exception?, string> formatter)
        {
            if (level != LogLevel.Debug || state is not IEnumerable<KeyValuePair<string, object?>> fields)
                return;
            var values = fields.ToDictionary(pair => pair.Key, pair => pair.Value);
            if (!values.TryGetValue("{OriginalFormat}", out var value) || !Equals(value, template))
                return;
            Emissions.Enqueue(values);
            if (Volatile.Read(ref _throwEnabled) == 0)
                return;
            Interlocked.Increment(ref _throwCount);
            throw failure;
        }
        sealed class EmptyScope : IDisposable { public void Dispose() { } }
    }
}

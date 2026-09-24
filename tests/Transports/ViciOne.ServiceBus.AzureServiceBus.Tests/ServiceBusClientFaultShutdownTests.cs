using System.Collections.Concurrent;
using System.Threading.Channels;
using Microsoft.Extensions.Logging;
using ViciOne.ServiceBus.Advanced.Middleware;
using ViciOne.ServiceBus.AzureServiceBus;
using ViciOne.ServiceBus.Logging;
using ViciOne.ServiceBus.Tests.Infrastructure.Requirements;
using Xunit;

namespace ViciOne.ServiceBus.AzureServiceBus.Tests;

public sealed class ServiceBusClientFaultShutdownTests
{
    private static readonly TimeSpan TestTimeout = TimeSpan.FromSeconds(5);

    [Theory]
    [InlineData(false)]
    [InlineData(true)]
    [RequirementCoverage("REQ-VSB-ASB-PROCESSOR-LIFECYCLE", "fault-notification-defers-and-coalesces-supervisor-stop")]
    public async Task FaultNotification_ReturnsBeforeSupervisorStopsAndCoalescesConcurrentFaultsAsync(bool subscription)
    {
        var agent = new RecordingStopAgent(blockInsideStop: true);
        ClientContext context = CreateContext(subscription, agent);
        CancellationToken token = TestContext.Current.CancellationToken;
        Task firstNotification = Task.Run(() => context.NotifyFaultedAsync(
            new InvalidOperationException("first processor failure"), "orders/first", token), token);

        try
        {
            await firstNotification.WaitAsync(TestTimeout, token);
            StopCall firstStop = await agent.NextCallAsync(token).AsTask().WaitAsync(TestTimeout, token);

            var continuations = new QueuedSynchronizationContext();
            Task[] laterNotifications = Enumerable.Range(0, 8)
                .Select(_ => Task.Run(() =>
                {
                    SynchronizationContext.SetSynchronizationContext(continuations);
                    try
                    {
                        return context.NotifyFaultedAsync(new InvalidOperationException("overlapping processor failure"),
                            "orders/second", token);
                    }
                    finally
                    {
                        SynchronizationContext.SetSynchronizationContext(null);
                    }
                }, token))
                .ToArray();
            await Task.WhenAll(laterNotifications).WaitAsync(TestTimeout, token);

            Assert.Equal("Unrecoverable exception on orders/first", firstStop.Context.Reason);
            Assert.Equal(1, agent.StopCallCount);
            Assert.Equal(0, continuations.PendingCount);
            Assert.False(firstStop.Completion.Task.IsCompleted);
        }
        finally
        {
            agent.ReleasePendingStops();
        }
    }

    [Theory]
    [InlineData(false)]
    [InlineData(true)]
    [RequirementCoverage("REQ-VSB-ASB-PROCESSOR-LIFECYCLE", "simultaneous-first-faults-install-one-stop")]
    public async Task SimultaneousFirstFaults_InstallOneSupervisorStopAsync(bool subscription)
    {
        CancellationToken token = TestContext.Current.CancellationToken;
        for (int round = 0; round < 4; round++)
        {
            var agent = new RecordingStopAgent();
            ClientContext context = CreateContext(subscription, agent);
            var continuations = new QueuedSynchronizationContext();
            using var ready = new CountdownEvent(16);
            using var start = new ManualResetEventSlim();
            var notifications = new Task?[16];
            var failures = new Exception?[16];
            Thread[] workers = Enumerable.Range(0, 16).Select(index => new Thread(() =>
            {
                SynchronizationContext.SetSynchronizationContext(continuations);
                ready.Signal();
                start.Wait();
                try
                {
                    notifications[index] = context.NotifyFaultedAsync(
                        new InvalidOperationException("simultaneous fault"), "orders/simultaneous", token);
                }
                catch (Exception exception)
                {
                    failures[index] = exception;
                }
                finally
                {
                    SynchronizationContext.SetSynchronizationContext(null);
                }
            }) { IsBackground = true }).ToArray();

            try
            {
                foreach (Thread worker in workers)
                    worker.Start();
                Assert.True(ready.Wait(TestTimeout, token));
                start.Set();
                foreach (Thread worker in workers)
                    Assert.True(worker.Join(TestTimeout));

                Assert.All(failures, failure => Assert.Null(failure));
                Assert.All(notifications, notification => Assert.True(notification?.IsCompletedSuccessfully));
                Assert.Equal(1, continuations.PendingCount);
                continuations.Drain();
                StopCall stop = await agent.NextCallAsync(token).AsTask().WaitAsync(TestTimeout, token);
                Assert.Equal("Unrecoverable exception on orders/simultaneous", stop.Context.Reason);
                Assert.Equal(1, agent.StopCallCount);
            }
            finally
            {
                start.Set();
                continuations.Drain();
                agent.ReleasePendingStops();
            }
        }
    }

    [Theory]
    [InlineData(false)]
    [InlineData(true)]
    [RequirementCoverage("REQ-VSB-ASB-PROCESSOR-LIFECYCLE", "canceled-fault-notification-does-not-stop-supervisor")]
    public async Task CanceledFaultNotification_LeavesSupervisorRunningUntilANewFaultAsync(bool subscription)
    {
        var agent = new RecordingStopAgent();
        ClientContext context = CreateContext(subscription, agent);
        using var canceled = new CancellationTokenSource();
        canceled.Cancel();

        try
        {
            Task rejected = context.NotifyFaultedAsync(new InvalidOperationException("canceled"), "orders/canceled", canceled.Token);
            OperationCanceledException cancellation = await Assert.ThrowsAnyAsync<OperationCanceledException>(() => rejected);

            Assert.Equal(canceled.Token, cancellation.CancellationToken);
            Assert.Equal(0, agent.StopCallCount);

            await context.NotifyFaultedAsync(new InvalidOperationException("active"), "orders/active",
                TestContext.Current.CancellationToken).WaitAsync(TestTimeout, TestContext.Current.CancellationToken);
            StopCall accepted = await agent.NextCallAsync(TestContext.Current.CancellationToken).AsTask()
                .WaitAsync(TestTimeout, TestContext.Current.CancellationToken);

            Assert.Equal("Unrecoverable exception on orders/active", accepted.Context.Reason);
            Assert.Equal(1, agent.StopCallCount);
            Assert.False(accepted.Completion.Task.IsCompleted);
        }
        finally
        {
            agent.ReleasePendingStops();
        }
    }

    [Theory]
    [InlineData(false)]
    [InlineData(true)]
    [RequirementCoverage("REQ-VSB-ASB-PROCESSOR-LIFECYCLE", "failed-supervisor-stop-is-logged-and-a-later-fault-retries")]
    public async Task FailedSupervisorStop_IsLoggedAndLaterFaultStartsANewStopAsync(bool subscription)
    {
        var agent = new RecordingStopAgent();
        ClientContext context = CreateContext(subscription, agent);
        var stopFailure = new InvalidOperationException("supervisor could not stop");
        var logger = new StopErrorLogger(stopFailure);
        ILogContext? previous = LogContext.Current;
        CancellationToken token = TestContext.Current.CancellationToken;

        try
        {
            LogContext.ConfigureCurrentLogContext(logger);
            await context.NotifyFaultedAsync(new InvalidOperationException("first fault"), "orders/first", token)
                .WaitAsync(TestTimeout, token);
            StopCall first = await agent.NextCallAsync(token).AsTask().WaitAsync(TestTimeout, token);
            first.Completion.TrySetException(stopFailure);

            StopLogRecord logged = await logger.ErrorLogged.WaitAsync(TestTimeout, token);
            Assert.Same(stopFailure, logged.Exception);
            Assert.Equal("Stopping faulted Azure client context failed: {EntityPath}", logged.Template);
            Assert.Equal("orders/first", logged.EntityPath);

            await Task.Run(async () =>
            {
                while (agent.StopCallCount == 1)
                {
                    token.ThrowIfCancellationRequested();
                    await context.NotifyFaultedAsync(new InvalidOperationException("retry fault"), "orders/retry", token);
                    await Task.Yield();
                }
            }, token).WaitAsync(TestTimeout, token);

            StopCall retried = await agent.NextCallAsync(token).AsTask().WaitAsync(TestTimeout, token);
            Assert.Equal("Unrecoverable exception on orders/retry", retried.Context.Reason);
            Assert.Equal(2, agent.StopCallCount);
            Assert.False(retried.Completion.Task.IsCompleted);
        }
        finally
        {
            agent.ReleasePendingStops();
            LogContext.Current = previous;
        }
    }

    private static ClientContext CreateContext(bool subscription, IAgent agent)
    {
        var input = new Uri("sb://unit.servicebus.invalid/orders");
        return subscription
            ? new SubscriptionClientContext(null!, input, null!, agent)
            : new QueueClientContext(null!, input, null!, agent);
    }

    private sealed class RecordingStopAgent(bool blockInsideStop = false) : IAgent
    {
        private readonly ConcurrentBag<StopCall> _allCalls = [];
        private readonly ManualResetEventSlim _stopEntryGate = new(!blockInsideStop);
        private readonly Channel<StopCall> _calls = Channel.CreateUnbounded<StopCall>();
        private int _stopCallCount;

        public Task Ready => Task.CompletedTask;
        public Task Completed => Task.CompletedTask;
        public CancellationToken Stopping => CancellationToken.None;
        public CancellationToken Stopped => CancellationToken.None;
        public int StopCallCount => Volatile.Read(ref _stopCallCount);

        public Task StopAsync(StopContext context, CancellationToken cancellationToken = default)
        {
            var call = new StopCall(context);
            _allCalls.Add(call);
            Interlocked.Increment(ref _stopCallCount);
            _calls.Writer.TryWrite(call);
            _stopEntryGate.Wait();
            return call.Completion.Task;
        }

        public ValueTask<StopCall> NextCallAsync(CancellationToken cancellationToken) =>
            _calls.Reader.ReadAsync(cancellationToken);

        public void ReleasePendingStops()
        {
            _stopEntryGate.Set();
            foreach (StopCall call in _allCalls)
                call.Completion.TrySetResult(true);
        }
    }

    private sealed class StopCall(StopContext context)
    {
        public StopContext Context { get; } = context;
        public TaskCompletionSource<bool> Completion { get; } = new(TaskCreationOptions.RunContinuationsAsynchronously);
    }

    private sealed class QueuedSynchronizationContext : SynchronizationContext
    {
        private readonly ConcurrentQueue<(SendOrPostCallback Callback, object? State)> _pending = new();

        public int PendingCount => _pending.Count;

        public override void Post(SendOrPostCallback callback, object? state) => _pending.Enqueue((callback, state));

        public void Drain()
        {
            while (_pending.TryDequeue(out (SendOrPostCallback Callback, object? State) continuation))
                continuation.Callback(continuation.State);
        }
    }

    private sealed class StopErrorLogger(Exception expected) : ILogger
    {
        private readonly TaskCompletionSource<StopLogRecord> _errorLogged =
            new(TaskCreationOptions.RunContinuationsAsynchronously);

        public Task<StopLogRecord> ErrorLogged => _errorLogged.Task;

        public IDisposable? BeginScope<TState>(TState state) where TState : notnull => null;
        public bool IsEnabled(LogLevel logLevel) => true;

        public void Log<TState>(LogLevel logLevel, EventId eventId, TState state, Exception? exception,
            Func<TState, Exception?, string> formatter)
        {
            if (logLevel != LogLevel.Error || !ReferenceEquals(exception, expected))
                return;

            var values = ((IEnumerable<KeyValuePair<string, object?>>)state!)
                .ToDictionary(pair => pair.Key, pair => pair.Value, StringComparer.Ordinal);
            _errorLogged.TrySetResult(new StopLogRecord(exception,
                (string)values["{OriginalFormat}"]!, (string)values["EntityPath"]!));
        }
    }

    private sealed record StopLogRecord(Exception Exception, string Template, string EntityPath);
}

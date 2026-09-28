using System.Reflection;
using ViciOne.ServiceBus.Middleware.InMemoryOutbox;
using ViciOne.ServiceBus.Scheduling;
using ViciOne.ServiceBus.Tests.Infrastructure.Requirements;
using ViciOne.ServiceBus.Tests.InternalAccess.InMemoryOutbox;
using Xunit;

namespace ViciOne.ServiceBus.Tests.Middleware.InMemoryOutbox;

public sealed class InMemoryOutboxReleaseAdmissionTests
{
    private static readonly TimeSpan Timeout = TimeSpan.FromSeconds(5);

    [Fact]
    [RequirementCoverage("REQ-VSB-INMEMORY-OUTBOX-RELEASE", "concurrent-send-admission-crossing-release-is-never-lost")]
    public async Task DeferredSend_CrossingReleaseExecutesExactlyOnceInsteadOfRemainingQueuedAsync()
    {
        CancellationToken token = TestContext.Current.CancellationToken;
        var clearToSend = new TaskCompletionSource(TaskCreationOptions.RunContinuationsAsynchronously);
        var methods = new InMemoryOutboxDeferredMethodCollection(clearToSend.Task);
        var sent = 0;
        object queueLock = GetPrivateField(methods, "_pendingMethods");

        await ReleaseWhileProducerWaitsForLockAsync(
            queueLock,
            () => methods.AddAsync(() =>
            {
                Interlocked.Increment(ref sent);
                return Task.CompletedTask;
            }, token),
            () => clearToSend.SetResult(),
            () => methods.ExecuteAsync(concurrent: false, cancellationToken: token),
            token);

        Assert.Equal(1, sent);
        Assert.Equal(0, methods.CreateCheckpoint());
        await methods.ExecuteAsync(concurrent: false, cancellationToken: token);
        Assert.Equal(1, sent);
    }

    [Fact]
    [RequirementCoverage("REQ-VSB-INMEMORY-OUTBOX-RELEASE", "concurrent-schedule-cancellation-crossing-release-is-never-lost")]
    public async Task ScheduledCancellation_CrossingReleaseExecutesExactlyOnceInsteadOfRemainingQueuedAsync()
    {
        CancellationToken token = TestContext.Current.CancellationToken;
        var clearToSend = new TaskCompletionSource(TaskCreationOptions.RunContinuationsAsynchronously);
        RecordingScheduler scheduler = DispatchProxy.Create<RecordingScheduler, RecordingSchedulerProxy>();
        var observed = (RecordingSchedulerProxy)(object)scheduler;
        ConsumeContext consumed = InMemoryOutboxTestContextFactory.Create(new RaceMessage(), token).Advanced();
        var context = new InMemoryOutboxMessageSchedulerContext(consumed, _ => scheduler, clearToSend.Task);
        var destination = new Uri("loopback://localhost/release-cancellation");
        Guid scheduledId = Guid.NewGuid();
        object listLock = GetPrivateField(context, "_listLock");
        observed.AdmissionLock = listLock;

        await ReleaseWhileProducerWaitsForLockAsync(
            listLock,
            () => context.CancelScheduledSendAsync(destination, scheduledId, token),
            () => clearToSend.SetResult(),
            () => context.ExecutePendingActionsAsync(token),
            token);

        CancellationCall cancellation = Assert.Single(observed.Cancellations);
        Assert.Equal(destination, cancellation.Destination);
        Assert.Equal(scheduledId, cancellation.TokenId);
        Assert.Equal(token, cancellation.CancellationToken);
        Assert.False(cancellation.UnderAdmissionLock);
        var deferred = (InMemoryOutboxDeferredMethodCollection)GetPrivateField(context, "_cancelMessages");
        Assert.Equal(0, deferred.CreateCheckpoint());
        await context.ExecutePendingActionsAsync(token);
        Assert.Single(observed.Cancellations);
    }

    [Fact]
    [RequirementCoverage("REQ-VSB-INMEMORY-OUTBOX-RELEASE", "canceled-admissions-after-release-never-execute")]
    public async Task CanceledAdmissions_AfterReleaseNeverExecuteOrQueueAsync()
    {
        using var cancellation = new CancellationTokenSource();
        var clearToSend = new TaskCompletionSource(TaskCreationOptions.RunContinuationsAsynchronously);
        var methods = new InMemoryOutboxDeferredMethodCollection(clearToSend.Task);
        RecordingScheduler scheduler = DispatchProxy.Create<RecordingScheduler, RecordingSchedulerProxy>();
        var observed = (RecordingSchedulerProxy)(object)scheduler;
        ConsumeContext consumed = InMemoryOutboxTestContextFactory.Create(
            new RaceMessage(), TestContext.Current.CancellationToken).Advanced();
        var context = new InMemoryOutboxMessageSchedulerContext(consumed, _ => scheduler, clearToSend.Task);
        var sent = 0;
        clearToSend.SetResult();
        cancellation.Cancel();

        OperationCanceledException sendFailure = await Assert.ThrowsAnyAsync<OperationCanceledException>(() =>
            methods.AddAsync(() =>
            {
                Interlocked.Increment(ref sent);
                return Task.CompletedTask;
            }, cancellation.Token));
        OperationCanceledException cancelFailure = await Assert.ThrowsAnyAsync<OperationCanceledException>(() =>
            context.CancelScheduledSendAsync(
                new Uri("loopback://localhost/release-cancellation"), Guid.NewGuid(), cancellation.Token));

        Assert.Equal(cancellation.Token, sendFailure.CancellationToken);
        Assert.Equal(cancellation.Token, cancelFailure.CancellationToken);
        Assert.Equal(0, sent);
        Assert.Empty(observed.Cancellations);
        Assert.Equal(0, methods.CreateCheckpoint());
        var deferred = (InMemoryOutboxDeferredMethodCollection)GetPrivateField(context, "_cancelMessages");
        Assert.Equal(0, deferred.CreateCheckpoint());
    }

    [Fact]
    [RequirementCoverage("REQ-VSB-INMEMORY-OUTBOX-RELEASE", "send-canceled-during-admission-lock-wait-is-rejected")]
    public async Task DeferredSend_CanceledWhileWaitingForAdmissionDoesNotRunAsync()
    {
        using var cancellation = new CancellationTokenSource();
        var clearToSend = new TaskCompletionSource(TaskCreationOptions.RunContinuationsAsynchronously);
        var methods = new InMemoryOutboxDeferredMethodCollection(clearToSend.Task);
        var sent = 0;

        OperationCanceledException failure = await CancelProducerWhileWaitingForLockAsync(
            GetPrivateField(methods, "_pendingMethods"),
            () => methods.AddAsync(() =>
            {
                Interlocked.Increment(ref sent);
                return Task.CompletedTask;
            }, cancellation.Token),
            cancellation,
            () => clearToSend.SetResult(),
            () => methods.ExecuteAsync(concurrent: false));

        Assert.Equal(cancellation.Token, failure.CancellationToken);
        Assert.Equal(0, sent);
        Assert.Equal(0, methods.CreateCheckpoint());
    }

    [Fact]
    [RequirementCoverage("REQ-VSB-INMEMORY-OUTBOX-RELEASE", "schedule-cancellation-canceled-during-admission-lock-wait-is-rejected")]
    public async Task ScheduledCancellation_CanceledWhileWaitingForAdmissionDoesNotRunAsync()
    {
        using var cancellation = new CancellationTokenSource();
        var clearToSend = new TaskCompletionSource(TaskCreationOptions.RunContinuationsAsynchronously);
        RecordingScheduler scheduler = DispatchProxy.Create<RecordingScheduler, RecordingSchedulerProxy>();
        var observed = (RecordingSchedulerProxy)(object)scheduler;
        ConsumeContext consumed = InMemoryOutboxTestContextFactory.Create(
            new RaceMessage(), TestContext.Current.CancellationToken).Advanced();
        var context = new InMemoryOutboxMessageSchedulerContext(consumed, _ => scheduler, clearToSend.Task);

        OperationCanceledException failure = await CancelProducerWhileWaitingForLockAsync(
            GetPrivateField(context, "_listLock"),
            () => context.CancelScheduledSendAsync(
                new Uri("loopback://localhost/release-cancellation"), Guid.NewGuid(), cancellation.Token),
            cancellation,
            () => clearToSend.SetResult(),
            () => context.ExecutePendingActionsAsync());

        Assert.Equal(cancellation.Token, failure.CancellationToken);
        Assert.Empty(observed.Cancellations);
        var deferred = (InMemoryOutboxDeferredMethodCollection)GetPrivateField(context, "_cancelMessages");
        Assert.Equal(0, deferred.CreateCheckpoint());
    }

    private static async Task ReleaseWhileProducerWaitsForLockAsync(
        object admissionLock,
        Func<Task> produce,
        Action release,
        Func<Task> drain,
        CancellationToken cancellationToken)
    {
        using var started = new ManualResetEventSlim();
        var completed = new TaskCompletionSource(TaskCreationOptions.RunContinuationsAsynchronously);
        var producer = new Thread(() =>
        {
            started.Set();
            try
            {
                produce().GetAwaiter().GetResult();
                completed.TrySetResult();
            }
            catch (Exception exception)
            {
                completed.TrySetException(exception);
            }
        }) { IsBackground = true };

        lock (admissionLock)
        {
            producer.Start();
            Assert.True(started.Wait(Timeout, cancellationToken));
            Assert.True(SpinWait.SpinUntil(
                () => producer.ThreadState.HasFlag(ThreadState.WaitSleepJoin), Timeout),
                "The producer must be waiting at the admission lock before release.");
            release();
            Assert.True(drain().IsCompletedSuccessfully);
        }

        await completed.Task.WaitAsync(Timeout, cancellationToken);
        Assert.True(producer.Join(Timeout));
    }

    private static async Task<OperationCanceledException> CancelProducerWhileWaitingForLockAsync(
        object admissionLock,
        Func<Task> produce,
        CancellationTokenSource cancellation,
        Action release,
        Func<Task> drain)
    {
        using var started = new ManualResetEventSlim();
        var completed = new TaskCompletionSource(TaskCreationOptions.RunContinuationsAsynchronously);
        var producer = new Thread(() =>
        {
            started.Set();
            try
            {
                produce().GetAwaiter().GetResult();
                completed.TrySetResult();
            }
            catch (Exception exception)
            {
                completed.TrySetException(exception);
            }
        }) { IsBackground = true };

        lock (admissionLock)
        {
            producer.Start();
            Assert.True(started.Wait(Timeout, TestContext.Current.CancellationToken));
            Assert.True(SpinWait.SpinUntil(
                () => producer.ThreadState.HasFlag(ThreadState.WaitSleepJoin), Timeout),
                "The producer must be waiting at the admission lock before cancellation.");
            cancellation.Cancel();
            release();
            Assert.True(drain().IsCompletedSuccessfully);
        }

        OperationCanceledException failure = await Assert.ThrowsAnyAsync<OperationCanceledException>(
            () => completed.Task.WaitAsync(Timeout, TestContext.Current.CancellationToken));
        Assert.True(producer.Join(Timeout));
        return failure;
    }

    private static object GetPrivateField(object instance, string name) =>
        instance.GetType().GetField(name, BindingFlags.Instance | BindingFlags.NonPublic)?.GetValue(instance)
        ?? throw new InvalidOperationException($"Missing synchronization field {name}.");

    private sealed record RaceMessage;

    private sealed record CancellationCall(Uri Destination, Guid TokenId, CancellationToken CancellationToken, bool UnderAdmissionLock);

    private interface RecordingScheduler : IMessageScheduler, Advanced.IAdvancedMessageScheduler;

    private class RecordingSchedulerProxy : DispatchProxy
    {
        public List<CancellationCall> Cancellations { get; } = [];
        public object? AdmissionLock { get; set; }

        protected override object? Invoke(MethodInfo? targetMethod, object?[]? args)
        {
            if (targetMethod?.Name == nameof(IMessageScheduler.CancelScheduledSendAsync)
                && args is { Length: 3 }
                && args[0] is Uri destination
                && args[1] is Guid tokenId
                && args[2] is CancellationToken cancellationToken)
            {
                Cancellations.Add(new CancellationCall(destination, tokenId, cancellationToken,
                    AdmissionLock is not null && Monitor.IsEntered(AdmissionLock)));
                return Task.CompletedTask;
            }

            throw new InvalidOperationException($"Unexpected scheduler operation {targetMethod?.Name}.");
        }
    }
}

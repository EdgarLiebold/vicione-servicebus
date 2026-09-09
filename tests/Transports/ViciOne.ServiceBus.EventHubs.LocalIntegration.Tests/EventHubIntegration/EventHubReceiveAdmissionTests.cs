using Azure.Messaging.EventHubs.Processor;
using ViciOne.ServiceBus.Advanced.Middleware;
using ViciOne.ServiceBus.Tests.Infrastructure.Requirements;
using Xunit;

namespace ViciOne.ServiceBus.EventHubs.LocalIntegration.Tests.EventHubIntegration;

public sealed class EventHubReceiveAdmissionTests
{
    [Fact]
    [RequirementCoverage("REQ-VSB-EVENTHUB-RECEIVE-ADMISSION", "pending-failure-releases-capacity-and-faults-confirmation")]
    public async Task PendingFailure_ReleasesCapacityAndFaultsAnyPartialConfirmationAsync()
    {
        var expected = new InvalidOperationException("pending failed");
        var lockContext = new StubProcessorLockContext { PendingException = expected };
        var executor = new RecordingPartitionedTaskExecutor();
        using var admission = new EventHubReceiveAdmission(1, lockContext, executor);

        InvalidOperationException actual = await Assert.ThrowsAsync<InvalidOperationException>(() =>
            admission.EnqueueAsync(default, static () => Task.CompletedTask, TestContext.Current.CancellationToken));

        Assert.Same(expected, actual);
        Assert.Same(expected, Assert.Single(lockContext.Faults));

        lockContext.PendingException = null;
        await admission.EnqueueAsync(default, static () => Task.CompletedTask, TestContext.Current.CancellationToken)
            .WaitAsync(TimeSpan.FromSeconds(2), TestContext.Current.CancellationToken);

        await executor.ExecuteAcceptedWorkAsync();
        Assert.Equal(2, lockContext.PendingCalls);
    }

    [Fact]
    [RequirementCoverage("REQ-VSB-EVENTHUB-RECEIVE-ADMISSION", "executor-rejection-releases-capacity-and-faults-confirmation")]
    public async Task ExecutorRejection_ReleasesCapacityAndFaultsThePendingConfirmationAsync()
    {
        var expected = new InvalidOperationException("queue rejected");
        var lockContext = new StubProcessorLockContext();
        var executor = new RecordingPartitionedTaskExecutor { EnqueueException = expected };
        using var admission = new EventHubReceiveAdmission(1, lockContext, executor);

        InvalidOperationException actual = await Assert.ThrowsAsync<InvalidOperationException>(() =>
            admission.EnqueueAsync(default, static () => Task.CompletedTask, TestContext.Current.CancellationToken));

        Assert.Same(expected, actual);
        Assert.Same(expected, Assert.Single(lockContext.Faults));

        executor.EnqueueException = null;
        await admission.EnqueueAsync(default, static () => Task.CompletedTask, TestContext.Current.CancellationToken)
            .WaitAsync(TimeSpan.FromSeconds(2), TestContext.Current.CancellationToken);

        await executor.ExecuteAcceptedWorkAsync();
    }

    [Fact]
    [RequirementCoverage("REQ-VSB-EVENTHUB-RECEIVE-ADMISSION", "canceled-enqueue-releases-capacity-and-cancels-confirmation")]
    public async Task CanceledEnqueue_ReleasesCapacityAndCancelsThePendingConfirmationAsync()
    {
        using var cancellationTokenSource = new CancellationTokenSource();
        var lockContext = new StubProcessorLockContext();
        var executor = new RecordingPartitionedTaskExecutor
        {
            OnEnqueueAsync = cancellationToken =>
            {
                cancellationTokenSource.Cancel();
                return Task.FromCanceled(cancellationToken);
            }
        };
        using var admission = new EventHubReceiveAdmission(1, lockContext, executor);

        await Assert.ThrowsAnyAsync<OperationCanceledException>(() =>
            admission.EnqueueAsync(default, static () => Task.CompletedTask, cancellationTokenSource.Token));

        Assert.Single(lockContext.Cancellations);

        executor.OnEnqueueAsync = null;
        await admission.EnqueueAsync(default, static () => Task.CompletedTask, TestContext.Current.CancellationToken)
            .WaitAsync(TimeSpan.FromSeconds(2), TestContext.Current.CancellationToken);

        await executor.ExecuteAcceptedWorkAsync();
    }

    [Fact]
    [RequirementCoverage("REQ-VSB-EVENTHUB-RECEIVE-ADMISSION", "accepted-work-survives-stop-cancellation-and-releases-capacity")]
    public async Task AcceptedWork_SurvivesLaterStopCancellationAndReleasesCapacityAsync()
    {
        using var stoppingTokenSource = new CancellationTokenSource();
        var lockContext = new StubProcessorLockContext();
        var executor = new RecordingPartitionedTaskExecutor();
        using var admission = new EventHubReceiveAdmission(1, lockContext, executor);
        int handled = 0;

        await admission.EnqueueAsync(default, () =>
        {
            handled++;
            return Task.CompletedTask;
        }, stoppingTokenSource.Token);

        stoppingTokenSource.Cancel();

        await executor.ExecuteAcceptedWorkAsync();
        Assert.Equal(1, handled);

        await admission.EnqueueAsync(default, static () => Task.CompletedTask, TestContext.Current.CancellationToken)
            .WaitAsync(TimeSpan.FromSeconds(2), TestContext.Current.CancellationToken);
        await executor.ExecuteAcceptedWorkAsync();
    }

    [Fact]
    [RequirementCoverage("REQ-VSB-EVENTHUB-RECEIVE-ADMISSION", "faulted-work-still-releases-receive-capacity")]
    public async Task FaultedWork_StillReleasesReceiveCapacityAsync()
    {
        var expected = new InvalidOperationException("delivery failed");
        var lockContext = new StubProcessorLockContext();
        var executor = new RecordingPartitionedTaskExecutor();
        using var admission = new EventHubReceiveAdmission(1, lockContext, executor);

        await admission.EnqueueAsync(default, () => Task.FromException(expected), TestContext.Current.CancellationToken);
        InvalidOperationException actual = await Assert.ThrowsAsync<InvalidOperationException>(executor.ExecuteAcceptedWorkAsync);
        Assert.Same(expected, actual);

        await admission.EnqueueAsync(default, static () => Task.CompletedTask, TestContext.Current.CancellationToken)
            .WaitAsync(TimeSpan.FromSeconds(2), TestContext.Current.CancellationToken);
        await executor.ExecuteAcceptedWorkAsync();
    }

    private sealed class RecordingPartitionedTaskExecutor : IPartitionedTaskExecutor<ProcessEventArgs>
    {
        public Exception? EnqueueException { get; set; }
        public Func<CancellationToken, Task>? OnEnqueueAsync { get; set; }
        private Func<Task>? AcceptedWork { get; set; }

        public ValueTask DisposeAsync() => default;

        public async Task EnqueueAsync(
            ProcessEventArgs partition,
            Func<Task> method,
            CancellationToken cancellationToken = default)
        {
            if (OnEnqueueAsync is not null)
                await OnEnqueueAsync(cancellationToken);

            if (EnqueueException is not null)
                throw EnqueueException;

            AcceptedWork = method;
        }

        public Task ExecuteAsync(
            ProcessEventArgs partition,
            Func<Task> method,
            CancellationToken cancellationToken = default)
        {
            throw new NotSupportedException();
        }

        public Task ExecuteAcceptedWorkAsync()
        {
            Func<Task> work = Assert.IsType<Func<Task>>(AcceptedWork);
            AcceptedWork = null;
            return work();
        }
    }

    private sealed class StubProcessorLockContext : IProcessorLockContext
    {
        public List<CancellationToken> Cancellations { get; } = [];
        public List<Exception> Faults { get; } = [];
        public Exception? PendingException { get; set; }
        public int PendingCalls { get; private set; }

        public void Canceled(ProcessEventArgs eventArgs, CancellationToken cancellationToken)
        {
            Cancellations.Add(cancellationToken);
        }

        public Task CompleteAsync(ProcessEventArgs eventArgs, CancellationToken cancellationToken = default)
        {
            return Task.CompletedTask;
        }

        public ValueTask DisposeAsync() => default;

        public Task FaultedAsync(
            ProcessEventArgs eventArgs,
            Exception exception,
            CancellationToken cancellationToken = default)
        {
            Faults.Add(exception);
            return Task.CompletedTask;
        }

        public Task PendingAsync(ProcessEventArgs eventArgs, CancellationToken cancellationToken = default)
        {
            PendingCalls++;
            return PendingException is null ? Task.CompletedTask : Task.FromException(PendingException);
        }
    }
}

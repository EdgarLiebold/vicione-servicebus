using ViciOne.ServiceBus.Contracts.JobService;
using ViciOne.ServiceBus.JobService;
using ViciOne.ServiceBus.Tests.Infrastructure.Configuration;
using ViciOne.ServiceBus.Tests.Infrastructure.Requirements;
using ViciOne.ServiceBus.Tests.Testing;
using Xunit;

namespace ViciOne.ServiceBus.Tests.JobService.JobService;

public sealed class JobProgressBufferTests
{
    private static readonly DateTimeOffset StartTime =
        new(2031, 4, 5, 6, 7, 8, TimeSpan.Zero);

    [Fact]
    [RequirementCoverage("REQ-VSB-JOB-PROGRESS-TIME", "provided-clock-controls-flush-deadline")]
    public async Task FlushDeadline_UsesTheProvidedTimeProviderAndPublishesTheLatestProgressAsync()
    {
        TimeSpan operationTimeout = OperationTimeout();
        TimeSpan flushWindow = TimeSpan.FromMinutes(1);
        var timeProvider = new ObservableTimeProvider(StartTime);
        var notifications = new RecordingJobContext();
        var options = new JobProgressBufferOptions
        {
            UpdateLimit = 2,
            TimeLimit = flushWindow,
        };
        var buffer = new JobProgressBuffer(notifications, timeProvider, options);
        Guid jobId = NewId.NextGuid();
        Guid attemptId = NewId.NextGuid();

        try
        {
            await buffer.UpdateAsync(
                new JobProgressBuffer.ProgressUpdate(jobId, attemptId, 42, 100),
                TestContext.Current.CancellationToken);
            await timeProvider.WaitForTimerCountAsync(1)
                .WaitAsync(operationTimeout, TestContext.Current.CancellationToken);

            Assert.Equal(flushWindow, timeProvider.LastDueTime);
            Assert.False(notifications.Progress.IsCompleted);

            timeProvider.Advance(flushWindow - TimeSpan.FromTicks(1));
            Assert.False(notifications.Progress.IsCompleted);

            timeProvider.Advance(TimeSpan.FromTicks(1));
            SetJobProgress actual = await notifications.Progress
                .WaitAsync(operationTimeout, TestContext.Current.CancellationToken);

            Assert.Equal(jobId, actual.JobId);
            Assert.Equal(attemptId, actual.AttemptId);
            Assert.Equal(1, actual.SequenceNumber);
            Assert.Equal(42, actual.Value);
            Assert.Equal(100, actual.Limit);
        }
        finally
        {
            await buffer.FlushAsync(TestContext.Current.CancellationToken).WaitAsync(operationTimeout, CancellationToken.None);
        }

        Assert.Equal(0, timeProvider.ActiveTimerCount);
    }

    [Fact]
    [RequirementCoverage("REQ-VSB-JOB-PROGRESS-TIME", "required-collaborators")]
    public void Constructor_RejectsMissingNotificationContextAndTimeProvider()
    {
        var notifications = new RecordingJobContext();

        Assert.Equal(
            "notifyJobContext",
            Assert.Throws<ArgumentNullException>(() =>
                new JobProgressBuffer(null!, TimeProvider.System)).ParamName);
        Assert.Equal(
            "timeProvider",
            Assert.Throws<ArgumentNullException>(() =>
                new JobProgressBuffer(notifications, null!)).ParamName);
    }

    [Theory]
    [InlineData(0, 1)]
    [InlineData(1, 0)]
    [RequirementCoverage("REQ-VSB-JOB-PROGRESS-TIME", "invalid-buffer-options-are-rejected")]
    public void Constructor_RejectsNonPositiveBufferLimits(int updateLimit, int timeLimitTicks)
    {
        var options = new JobProgressBufferOptions
        {
            UpdateLimit = updateLimit,
            TimeLimit = TimeSpan.FromTicks(timeLimitTicks),
        };

        ArgumentOutOfRangeException exception = Assert.Throws<ArgumentOutOfRangeException>(() =>
            new JobProgressBuffer(new RecordingJobContext(), TimeProvider.System, options));

        Assert.Equal("options", exception.ParamName);
    }

    [Fact]
    [RequirementCoverage("REQ-VSB-JOB-PROGRESS-TIME", "publication-failure-propagates-from-flush")]
    public async Task Flush_PropagatesProgressPublicationFailureAsync()
    {
        var expected = new InvalidOperationException("progress publication refused");
        var notifications = new RecordingJobContext(expected);
        var buffer = new JobProgressBuffer(notifications, TimeProvider.System, new JobProgressBufferOptions
        {
            UpdateLimit = 1,
            TimeLimit = TimeSpan.FromDays(1),
        });

        await buffer.UpdateAsync(
            new JobProgressBuffer.ProgressUpdate(NewId.NextGuid(), NewId.NextGuid(), 1, 10),
            TestContext.Current.CancellationToken);

        Exception actual = await Assert.ThrowsAsync<InvalidOperationException>(
            () => buffer.FlushAsync(TestContext.Current.CancellationToken));

        Assert.Same(expected, actual);
    }

    private static TimeSpan OperationTimeout() => TestConfigurationProvider.ForCurrentTestRun()
        .GetValidatedOptions()
        .OperationTimeout!.Value;

    private sealed class RecordingJobContext : INotifyJobContext
    {
        private readonly Exception? _failure;
        private readonly TaskCompletionSource<SetJobProgress> _progress =
            new(TaskCreationOptions.RunContinuationsAsynchronously);

        public RecordingJobContext(Exception? failure = null)
        {
            _failure = failure;
        }

        public Task<SetJobProgress> Progress => _progress.Task;

        public Task NotifyCanceledAsync(CancellationToken cancellationToken = default) { if (cancellationToken.IsCancellationRequested) return global::System.Threading.Tasks.Task.FromCanceled(cancellationToken); return Task.CompletedTask; }
        public Task NotifyStartedAsync(CancellationToken cancellationToken = default) { if (cancellationToken.IsCancellationRequested) return global::System.Threading.Tasks.Task.FromCanceled(cancellationToken); return Task.CompletedTask; }
        public Task NotifyCompletedAsync(CancellationToken cancellationToken = default) { if (cancellationToken.IsCancellationRequested) return global::System.Threading.Tasks.Task.FromCanceled(cancellationToken); return Task.CompletedTask; }
        public Task NotifyFaultedAsync(Exception exception, TimeSpan? delay = null, CancellationToken cancellationToken = default) { if (cancellationToken.IsCancellationRequested) return global::System.Threading.Tasks.Task.FromCanceled(cancellationToken); return Task.CompletedTask; }
        public Task NotifyProgressAsync(SetJobProgress progress, CancellationToken cancellationToken = default)
        {
            if (cancellationToken.IsCancellationRequested)
                return Task.FromCanceled(cancellationToken);
            if (_failure is not null)
                return Task.FromException(_failure);

            _progress.TrySetResult(progress);
            return Task.CompletedTask;
        }
    }
}

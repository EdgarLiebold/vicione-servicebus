using System.Collections.Concurrent;
using ViciOne.ServiceBus.Quartz.Tests.Testing;
using ViciOne.ServiceBus.Scheduling;
using ViciOne.ServiceBus.Tests.Infrastructure.Configuration;
using ViciOne.ServiceBus.Tests.Infrastructure.Requirements;
using Xunit;

namespace ViciOne.ServiceBus.Quartz.Tests.QuartzIntegration;

[Collection(QuartzIntegrationCollection.Name)]
public sealed class QuartzScheduledRedeliveryIntegrationTests
{
    private static readonly Uri InputAddress = new("loopback://localhost/quartz-redelivery-input");

    [Fact]
    [RequirementCoverage("REQ-VSB-QUARTZ-REDELIVERY-CANCELLATION", "nonrequested-dependency-cancellation-is-redelivered")]
    public async Task NonrequestedDependencyCancellation_IsClassifiedAndRedeliveredAsync()
    {
        TimeSpan timeout = OperationTimeout();
        var delivered = new TaskCompletionSource<RedeliveryObservation>(
            TaskCreationOptions.RunContinuationsAsynchronously);
        var attempts = 0;
        await using QuartzTestBus fixture = await QuartzTestBus.StartAsync(
            timeout,
            configure: configurator => configurator.ReceiveEndpoint("quartz-redelivery-input", endpoint =>
            {
                endpoint.UseScheduledRedelivery(redelivery => redelivery.Intervals(TimeSpan.Zero));
                endpoint.UseExecute(context =>
                {
                    if (Interlocked.Increment(ref attempts) == 1)
                        throw new OperationCanceledException("dependency canceled its operation", context.CancellationToken);
                });

                endpoint.Handler<RedeliveryPayload>(context =>
                {
                    delivered.TrySetResult(new RedeliveryObservation(
                        context.Advanced().GetRedeliveryCount(),
                        context.CancellationToken.IsCancellationRequested));
                    return Task.CompletedTask;
                });
            }));
        var scheduledCommand = new ConsumeCompletionObserver<ScheduleMessage>(_ => true);
        using ConnectHandle observer = fixture.Bus.ConnectConsumeObserver(scheduledCommand);
        ISendEndpoint input = await fixture.Bus.GetSendEndpointAsync(InputAddress, TestContext.Current.CancellationToken).WaitAsync(timeout, TestContext.Current.CancellationToken);

        await input.SendAsync(new RedeliveryPayload("redeliver"), TestContext.Current.CancellationToken);
        await scheduledCommand.Completed.WaitAsync(timeout, TestContext.Current.CancellationToken);
        RedeliveryObservation received = await delivered.Task
            .WaitAsync(timeout, TestContext.Current.CancellationToken);

        Assert.Equal(2, attempts);
        Assert.Equal(1, received.RedeliveryCount);
        Assert.False(received.CancellationRequested);
    }

    [Fact]
    [RequirementCoverage("REQ-VSB-QUARTZ-REDELIVERY-INTERVALS", "configured-sequence-is-scheduled-in-order")]
    public async Task ScheduledRedelivery_UsesEveryConfiguredIntervalInOrderAsync()
    {
        TimeSpan timeout = OperationTimeout();
        TimeSpan[] intervals = [
            TimeSpan.FromMilliseconds(500),
            TimeSpan.FromSeconds(2),
            TimeSpan.FromSeconds(5),
        ];
        var attemptTimes = new ConcurrentQueue<DateTimeOffset>();
        var delivered = new TaskCompletionSource<int>(
            TaskCreationOptions.RunContinuationsAsynchronously);
        var attempts = 0;
        await using QuartzTestBus fixture = await QuartzTestBus.StartAsync(
            timeout,
            configure: configurator => configurator.ReceiveEndpoint("quartz-redelivery-input", endpoint =>
            {
                endpoint.UseScheduledRedelivery(redelivery => redelivery.Intervals(intervals));
                endpoint.Handler<RedeliveryPayload>(context =>
                {
                    attemptTimes.Enqueue(TimeProvider.System.GetUtcNow());
                    if (Interlocked.Increment(ref attempts) <= intervals.Length)
                        throw new ExpectedRedeliveryException();

                    delivered.TrySetResult(context.Advanced().GetRedeliveryCount());
                    return Task.CompletedTask;
                });
            }));
        var schedules = new ScheduledSequenceObserver(intervals.Length);
        using ConnectHandle observer = fixture.Bus.ConnectConsumeObserver(schedules);
        ISendEndpoint input = await fixture.Bus.GetSendEndpointAsync(InputAddress, TestContext.Current.CancellationToken).WaitAsync(timeout, TestContext.Current.CancellationToken);

        await input.SendAsync(new RedeliveryPayload("intervals"), TestContext.Current.CancellationToken);
        await schedules.Completed.WaitAsync(timeout, TestContext.Current.CancellationToken);
        int receivedRedeliveryCount = await delivered.Task
            .WaitAsync(timeout, TestContext.Current.CancellationToken);
        DateTimeOffset[] attemptsSnapshot = attemptTimes.ToArray();
        DateTimeOffset[] scheduledTimes = schedules.ScheduledTimes;

        Assert.Equal(intervals.Length + 1, attemptsSnapshot.Length);
        Assert.Equal(intervals.Length, scheduledTimes.Length);
        Assert.Equal(intervals.Length, receivedRedeliveryCount);
        for (int index = 0; index < intervals.Length; index++)
        {
            TimeSpan scheduledDelay = scheduledTimes[index] - attemptsSnapshot[index];
            Assert.InRange(scheduledDelay, intervals[index], intervals[index] + TimeSpan.FromMilliseconds(500));
        }
    }

    private static TimeSpan OperationTimeout() => TestConfigurationProvider.ForCurrentTestRun()
        .GetValidatedOptions()
        .OperationTimeout!.Value;

    private sealed class ScheduledSequenceObserver(int expectedCount) : IConsumeObserver
    {
        private readonly ConcurrentQueue<DateTimeOffset> _scheduledTimes = new();
        private readonly TaskCompletionSource _completed = new(TaskCreationOptions.RunContinuationsAsynchronously);

        public Task Completed => _completed.Task;
        public DateTimeOffset[] ScheduledTimes => _scheduledTimes.ToArray();

        public Task PreConsumeAsync<T>(ConsumeContext<T> context) where T : class => Task.CompletedTask;

        public Task PostConsumeAsync<T>(ConsumeContext<T> context) where T : class
        {
            if (context.Message is ScheduleMessage message)
            {
                _scheduledTimes.Enqueue(message.DueAt);
                if (_scheduledTimes.Count == expectedCount)
                    _completed.TrySetResult();
            }

            return Task.CompletedTask;
        }

        public Task ConsumeFaultAsync<T>(ConsumeContext<T> context, Exception exception) where T : class
        {
            if (context.Message is ScheduleMessage)
                _completed.TrySetException(exception);

            return Task.CompletedTask;
        }
    }

    private sealed class ExpectedRedeliveryException : Exception;

    private sealed record RedeliveryObservation(int RedeliveryCount, bool CancellationRequested);

    private sealed record RedeliveryPayload(string Value);
}

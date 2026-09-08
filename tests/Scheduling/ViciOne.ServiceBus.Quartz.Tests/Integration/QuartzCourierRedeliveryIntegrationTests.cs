using ViciOne.ServiceBus.Advanced.Registration;
using ViciOne.ServiceBus.Configuration;
using ViciOne.ServiceBus.Courier;
using ViciOne.ServiceBus.Courier.Contracts;
using ViciOne.ServiceBus.Quartz.Tests.Testing;
using ViciOne.ServiceBus.Scheduling;
using ViciOne.ServiceBus.Tests.Infrastructure.Configuration;
using ViciOne.ServiceBus.Tests.Infrastructure.Requirements;
using Xunit;

namespace ViciOne.ServiceBus.Quartz.Tests.Integration;

[Collection(QuartzIntegrationCollection.Name)]
public sealed class QuartzCourierRedeliveryIntegrationTests
{
    [Theory]
    [InlineData(RetryMode.InMemory)]
    [InlineData(RetryMode.Quartz)]
    [RequirementCoverage("REQ-VSB-QUARTZ-COURIER", "permanent-fault-compensates")]
    public async Task PermanentActivityFailure_FaultsTheSlipAndCompensatesTheCompletedActivityAsync(RetryMode retryMode)
    {
        CourierExecutionResult result = await ExecuteRoutingSlipAsync(retryMode, FailureMode.Permanent);

        Assert.Equal(3, result.FailureAttempts);
        Assert.Equal(1, result.RecordingExecutions);
        Assert.Equal(1, result.RecordingCompensations);
        Assert.Equal(result.TrackingNumber, result.FaultedTrackingNumber);
        Assert.Equal(result.TrackingNumber, result.CompensatedTrackingNumber);
        Assert.Equal(RecordingActivityName, result.CompensatedActivityName);
        Assert.Equal(retryMode == RetryMode.Quartz ? 2 : 0, result.ScheduledCommandCount);
    }

    [Theory]
    [InlineData(RetryMode.InMemory)]
    [InlineData(RetryMode.Quartz)]
    [RequirementCoverage("REQ-VSB-QUARTZ-COURIER", "transient-fault-recovers")]
    public async Task TransientActivityFailure_RetriesAndCompletesTheSlipAsync(RetryMode retryMode)
    {
        CourierExecutionResult result = await ExecuteRoutingSlipAsync(retryMode, FailureMode.Transient);

        Assert.Equal(2, result.FailureAttempts);
        Assert.Equal(1, result.RecordingExecutions);
        Assert.Equal(0, result.RecordingCompensations);
        Assert.Equal(result.TrackingNumber, result.CompletedTrackingNumber);
        Assert.Equal(retryMode == RetryMode.Quartz ? 1 : 0, result.ScheduledCommandCount);
    }

    private static async Task<CourierExecutionResult> ExecuteRoutingSlipAsync(RetryMode retryMode, FailureMode failureMode)
    {
        TimeSpan timeout = OperationTimeout();
        var probe = new CourierProbe();
        string prefix = $"quartz-courier-{NewId.NextGuid():N}";
        string recordingExecuteQueue = $"{prefix}-recording-execute";
        string recordingCompensateQueue = $"{prefix}-recording-compensate";
        string failingQueue = $"{prefix}-failing-execute";
        string eventsQueue = $"{prefix}-events";
        var recordingExecuteAddress = new Uri($"loopback://localhost/{recordingExecuteQueue}");
        var recordingCompensateAddress = new Uri($"loopback://localhost/{recordingCompensateQueue}");
        var failingAddress = new Uri($"loopback://localhost/{failingQueue}");
        int expectedScheduledCommands = failureMode == FailureMode.Permanent ? 2 : 1;
        var scheduledCommands = new ConsumeCompletionObserver<ScheduleMessage>(_ => true, expectedScheduledCommands);

        await using QuartzTestBus fixture = await QuartzTestBus.StartAsync(
            timeout,
            configure: configurator =>
            {
                if (retryMode == RetryMode.InMemory)
                    configurator.UseMessageRetry(retry => retry.Immediate(2));
                else
                    configurator.UseScheduledRedelivery(redelivery => redelivery.Intervals(TimeSpan.Zero, TimeSpan.Zero));

                configurator.ReceiveEndpoint(recordingCompensateQueue, endpoint =>
                    endpoint.CompensateActivityHost<RecordingActivity, RecordingLog>(() => new RecordingActivity(probe)));
                configurator.ReceiveEndpoint(recordingExecuteQueue, endpoint =>
                    endpoint.ExecuteActivityHost<RecordingActivity, RecordingArguments>(
                        recordingCompensateAddress,
                        () => new RecordingActivity(probe)));
                configurator.ReceiveEndpoint(failingQueue, endpoint =>
                    endpoint.ExecuteActivityHost<FailingActivity, FailingArguments>(
                        () => new FailingActivity(probe, failureMode)));
                configurator.ReceiveEndpoint(eventsQueue, endpoint =>
                {
                    endpoint.Handler<RoutingSlipFaulted>(context =>
                    {
                        probe.Faulted.TrySetResult(context.Message.TrackingNumber);
                        return Task.CompletedTask;
                    });
                    endpoint.Handler<RoutingSlipActivityCompensated>(context =>
                    {
                        probe.Compensated.TrySetResult((context.Message.TrackingNumber, context.Message.ActivityName));
                        return Task.CompletedTask;
                    });
                    endpoint.Handler<RoutingSlipCompleted>(context =>
                    {
                        probe.Completed.TrySetResult(context.Message.TrackingNumber);
                        return Task.CompletedTask;
                    });
                });
            });
        using ConnectHandle observer = fixture.Bus.ConnectConsumeObserver(scheduledCommands);
        Guid trackingNumber = NewId.NextGuid();
        var builder = new RoutingSlipBuilder(trackingNumber);
        builder.AddActivity(RecordingActivityName, recordingExecuteAddress, new RecordingArguments("payload"));
        builder.AddActivity(FailingActivityName, failingAddress, new FailingArguments("failure"));

        await fixture.Bus.ExecuteAsync(builder.Build());

        Guid? faultedTrackingNumber = null;
        (Guid TrackingNumber, string ActivityName)? compensated = null;
        Guid? completedTrackingNumber = null;
        if (failureMode == FailureMode.Permanent)
        {
            faultedTrackingNumber = await probe.Faulted.Task.WaitAsync(timeout, TestContext.Current.CancellationToken);
            compensated = await probe.Compensated.Task.WaitAsync(timeout, TestContext.Current.CancellationToken);
        }
        else
        {
            completedTrackingNumber = await probe.Completed.Task.WaitAsync(timeout, TestContext.Current.CancellationToken);
        }

        if (retryMode == RetryMode.Quartz)
            await scheduledCommands.Completed.WaitAsync(timeout, TestContext.Current.CancellationToken);

        return new CourierExecutionResult(
            trackingNumber,
            probe.FailureAttempts,
            probe.RecordingExecutions,
            probe.RecordingCompensations,
            scheduledCommands.ObservedCount,
            faultedTrackingNumber,
            compensated?.TrackingNumber,
            compensated?.ActivityName,
            completedTrackingNumber);
    }

    private static TimeSpan OperationTimeout() => TestConfigurationProvider.ForCurrentTestRun()
        .GetValidatedOptions()
        .OperationTimeout!.Value;

    private const string RecordingActivityName = "Recording";
    private const string FailingActivityName = "Failing";

    public enum RetryMode
    {
        InMemory,
        Quartz,
    }

    private enum FailureMode
    {
        Permanent,
        Transient,
    }

    private sealed record RecordingArguments(string Value);

    private sealed record RecordingLog(string Value);

    private sealed record FailingArguments(string Reason);

    private sealed record CourierExecutionResult(
        Guid TrackingNumber,
        int FailureAttempts,
        int RecordingExecutions,
        int RecordingCompensations,
        int ScheduledCommandCount,
        Guid? FaultedTrackingNumber,
        Guid? CompensatedTrackingNumber,
        string? CompensatedActivityName,
        Guid? CompletedTrackingNumber);

    private sealed class CourierProbe
    {
        private int _failureAttempts;
        private int _recordingCompensations;
        private int _recordingExecutions;

        public int FailureAttempts => Volatile.Read(ref _failureAttempts);
        public int RecordingCompensations => Volatile.Read(ref _recordingCompensations);
        public int RecordingExecutions => Volatile.Read(ref _recordingExecutions);
        public TaskCompletionSource<Guid> Faulted { get; } = new(TaskCreationOptions.RunContinuationsAsynchronously);
        public TaskCompletionSource<(Guid TrackingNumber, string ActivityName)> Compensated { get; } =
            new(TaskCreationOptions.RunContinuationsAsynchronously);
        public TaskCompletionSource<Guid> Completed { get; } = new(TaskCreationOptions.RunContinuationsAsynchronously);

        public int RecordFailureAttempt() => Interlocked.Increment(ref _failureAttempts);
        public void RecordExecution() => Interlocked.Increment(ref _recordingExecutions);
        public void RecordCompensation() => Interlocked.Increment(ref _recordingCompensations);
    }

    private sealed class RecordingActivity(CourierProbe probe) : IActivity<RecordingArguments, RecordingLog>
    {
        public Task<ExecutionResult> ExecuteAsync(ExecuteContext<RecordingArguments> context)
        {
            probe.RecordExecution();
            return Task.FromResult(context.Completed(new RecordingLog(context.Arguments.Value)));
        }

        public Task<CompensationResult> CompensateAsync(CompensateContext<RecordingLog> context)
        {
            probe.RecordCompensation();
            return Task.FromResult(context.Compensated());
        }
    }

    private sealed class FailingActivity(CourierProbe probe, FailureMode failureMode) : IExecuteActivity<FailingArguments>
    {
        public Task<ExecutionResult> ExecuteAsync(ExecuteContext<FailingArguments> context)
        {
            int attempt = probe.RecordFailureAttempt();
            if (failureMode == FailureMode.Permanent || attempt == 1)
                return Task.FromException<ExecutionResult>(new ExpectedCourierException(context.Arguments.Reason));

            return Task.FromResult(context.Completed());
        }
    }

    private sealed class ExpectedCourierException(string message) : Exception(message);
}

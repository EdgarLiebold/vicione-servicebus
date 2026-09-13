using ViciOne.ServiceBus.Courier.Contracts;
using ViciOne.ServiceBus.Testing;
using ViciOne.ServiceBus.Tests.Infrastructure.Configuration;
using ViciOne.ServiceBus.Tests.Infrastructure.Requirements;
using Xunit;

namespace ViciOne.ServiceBus.Tests.Testing;

public sealed class ActivityTestHarnessTests
{
    [Fact]
    [RequirementCoverage("REQ-VSB-TEST-HARNESS-COURIER", "execute-compensate-fault-lifecycle")]
    public async Task ActivityHarness_ExecutesThenCompensatesWithExactAddressesNamesAndLogAsync()
    {
        TimeSpan timeout = OperationTimeout();
        CancellationToken cancellationToken = TestContext.Current.CancellationToken;
        var executed = new TaskCompletionSource<string>(TaskCreationOptions.RunContinuationsAsynchronously);
        var compensated = new TaskCompletionSource<string>(TaskCreationOptions.RunContinuationsAsynchronously);
        using var harness = CreateHarness(timeout);
        ActivityTestHarness<RecordingActivity, RecordingArguments, RecordingLog> activity = harness.AddActivity<
            RecordingActivity,
            RecordingArguments,
            RecordingLog>(
            _ => new RecordingActivity(executed, compensated),
            _ => new RecordingActivity(executed, compensated));
        ExecuteActivityTestHarness<FailingActivity, FailingArguments> failure = harness.AddExecuteActivity<
            FailingActivity,
            FailingArguments>();
        var executeConfigured = 0;
        var compensateConfigured = 0;
        activity.ExecuteReceiveEndpointConfiguring += _ => Interlocked.Increment(ref executeConfigured);
        activity.CompensateReceiveEndpointConfiguring += _ => Interlocked.Increment(ref compensateConfigured);

        await harness.StartAsync(cancellationToken);
        try
        {
            Task<ConsumeContext<RoutingSlipFaulted>> faulted = harness.WaitForMessageAsync<RoutingSlipFaulted>(TestContext.Current.CancellationToken);
            Task<ConsumeContext<RoutingSlipActivityCompensated>> activityCompensated =
                harness.WaitForMessageAsync<RoutingSlipActivityCompensated>(TestContext.Current.CancellationToken);
            Guid trackingNumber = NewId.NextGuid();
            var builder = new RoutingSlipBuilder(trackingNumber);
            builder.AddSubscription(harness.BusAddress, RoutingSlipEvents.All);
            builder.AddActivity(activity.Name, activity.ExecuteAddress, new RecordingArguments("original"));
            builder.AddActivity(failure.Name, failure.ExecuteAddress, new FailingArguments("fail"));

            await harness.Bus.ExecuteAsync(builder.Build(), cancellationToken: TestContext.Current.CancellationToken);

            ConsumeContext<RoutingSlipFaulted> fault = await faulted.WaitAsync(timeout, cancellationToken);
            ConsumeContext<RoutingSlipActivityCompensated> compensation =
                await activityCompensated.WaitAsync(timeout, cancellationToken);
            string executedValue = await executed.Task.WaitAsync(timeout, cancellationToken);
            string compensatedValue = await compensated.Task.WaitAsync(timeout, cancellationToken);

            Assert.Equal("Recording", activity.Name);
            Assert.Equal("Recording_execute", activity.ExecuteQueueName);
            Assert.Equal("Recording_compensate", activity.CompensateQueueName);
            Assert.Equal("Failing", failure.Name);
            Assert.Equal("Failing_execute", failure.ExecuteQueueName);
            Assert.Equal(1, executeConfigured);
            Assert.Equal(1, compensateConfigured);
            Assert.Equal(new Uri(harness.BaseAddress, activity.ExecuteQueueName), activity.ExecuteAddress);
            Assert.Equal(new Uri(harness.BaseAddress, activity.CompensateQueueName), activity.CompensateAddress);
            Assert.Equal(new Uri(harness.BaseAddress, failure.ExecuteQueueName), failure.ExecuteAddress);
            Assert.Equal("original", executedValue);
            Assert.Equal("original", compensatedValue);
            Assert.Equal(trackingNumber, fault.Message.TrackingNumber);
            Assert.Equal(trackingNumber, compensation.Message.TrackingNumber);
            Assert.Equal(activity.Name, compensation.Message.ActivityName);
            Assert.Equal("original", compensation.GetResult<string>(nameof(RecordingLog.OriginalValue)));
        }
        finally
        {
            await harness.StopAsync(TestContext.Current.CancellationToken);
        }
    }

    [Fact]
    [RequirementCoverage("REQ-VSB-TEST-HARNESS-COURIER", "execute-only-success-lifecycle")]
    public async Task ExecuteActivityHarness_CompletesARoutingSlipAndExposesItsExactEndpointAsync()
    {
        TimeSpan timeout = OperationTimeout();
        CancellationToken cancellationToken = TestContext.Current.CancellationToken;
        var executed = new TaskCompletionSource<int>(TaskCreationOptions.RunContinuationsAsynchronously);
        using var harness = CreateHarness(timeout);
        ExecuteActivityTestHarness<ExecuteOnlyActivity, ExecuteOnlyArguments> activity = harness.AddExecuteActivity<
            ExecuteOnlyActivity,
            ExecuteOnlyArguments>(_ => new ExecuteOnlyActivity(executed));
        var executeConfigured = 0;
        activity.ExecuteReceiveEndpointConfiguring += _ => Interlocked.Increment(ref executeConfigured);

        await harness.StartAsync(cancellationToken);
        try
        {
            Task<ConsumeContext<RoutingSlipCompleted>> completed = harness.WaitForMessageAsync<RoutingSlipCompleted>(TestContext.Current.CancellationToken);
            Guid trackingNumber = NewId.NextGuid();
            var builder = new RoutingSlipBuilder(trackingNumber);
            builder.AddSubscription(harness.BusAddress, RoutingSlipEvents.All);
            builder.AddActivity(activity.Name, activity.ExecuteAddress, new ExecuteOnlyArguments(42));

            await harness.Bus.ExecuteAsync(builder.Build(), cancellationToken: TestContext.Current.CancellationToken);

            ConsumeContext<RoutingSlipCompleted> completion = await completed.WaitAsync(timeout, cancellationToken);
            int value = await executed.Task.WaitAsync(timeout, cancellationToken);

            Assert.Equal("ExecuteOnly", activity.Name);
            Assert.Equal("ExecuteOnly_execute", activity.ExecuteQueueName);
            Assert.Equal(new Uri(harness.BaseAddress, activity.ExecuteQueueName), activity.ExecuteAddress);
            Assert.Equal(1, executeConfigured);
            Assert.Equal(42, value);
            Assert.Equal(trackingNumber, completion.Message.TrackingNumber);
        }
        finally
        {
            await harness.StopAsync(TestContext.Current.CancellationToken);
        }
    }

    private static TimeSpan OperationTimeout() => TestConfigurationProvider.ForCurrentTestRun()
        .GetValidatedOptions()
        .OperationTimeout!.Value;

    private static InMemoryTestHarness CreateHarness(TimeSpan timeout) =>
        new($"activity-harness-{NewId.NextGuid():N}")
        {
            TestTimeout = timeout,
            TestInactivityTimeout = timeout,
        };

    public sealed record RecordingArguments(string Value);

    public sealed record RecordingLog(string OriginalValue);

    public sealed class RecordingActivity(
        TaskCompletionSource<string> executed,
        TaskCompletionSource<string> compensated) : IActivity<RecordingArguments, RecordingLog>
    {
        public Task<ExecutionResult> ExecuteAsync(ExecuteContext<RecordingArguments> context)
        {
            executed.TrySetResult(context.Arguments.Value);
            return Task.FromResult(context.Completed(new RecordingLog(context.Arguments.Value)));
        }

        public Task<CompensationResult> CompensateAsync(CompensateContext<RecordingLog> context)
        {
            compensated.TrySetResult(context.Log.OriginalValue);
            return Task.FromResult(context.Compensated());
        }
    }

    public sealed record FailingArguments(string Value);

    public sealed class FailingActivity : IExecuteActivity<FailingArguments>
    {
        public Task<ExecutionResult> ExecuteAsync(ExecuteContext<FailingArguments> context) =>
            Task.FromException<ExecutionResult>(new ExpectedActivityException(context.Arguments.Value));
    }

    public sealed record ExecuteOnlyArguments(int Value);

    public sealed class ExecuteOnlyActivity(TaskCompletionSource<int> executed) : IExecuteActivity<ExecuteOnlyArguments>
    {
        public Task<ExecutionResult> ExecuteAsync(ExecuteContext<ExecuteOnlyArguments> context)
        {
            executed.TrySetResult(context.Arguments.Value);
            return Task.FromResult(context.Completed());
        }
    }

    private sealed class ExpectedActivityException(string message) : Exception(message);
}

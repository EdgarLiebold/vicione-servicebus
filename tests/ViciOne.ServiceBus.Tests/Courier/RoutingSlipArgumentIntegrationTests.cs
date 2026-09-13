using ViciOne.ServiceBus.Courier.Contracts;
using ViciOne.ServiceBus.Testing;
using ViciOne.ServiceBus.Tests.Infrastructure.Requirements;
using Xunit;

namespace ViciOne.ServiceBus.Tests.Courier;

public sealed class RoutingSlipArgumentIntegrationTests
{
    [Theory]
    [InlineData(ArgumentShape.Explicit, "activity")]
    [InlineData(ArgumentShape.Missing, "variable")]
    [InlineData(ArgumentShape.Null, "variable")]
    [InlineData(ArgumentShape.Default, "variable")]
    [RequirementCoverage("REQ-VSB-COURIER-ARGUMENTS", "argument-precedence-and-variable-fallback")]
    public async Task ArgumentResolution_UsesExplicitValuesAndFallsBackForMissingNullOrDefaultAsync(
        ArgumentShape shape,
        string expectedValue)
    {
        TimeSpan timeout = CourierTestSupport.OperationTimeout();
        CancellationToken cancellationToken = TestContext.Current.CancellationToken;
        var observed = new TaskCompletionSource<ResolvedArguments>(TaskCreationOptions.RunContinuationsAsynchronously);
        using InMemoryTestHarness harness = CourierTestSupport.CreateHarness("courier-arguments");
        ExecuteActivityTestHarness<ResolveArgumentsActivity, ResolveArguments> activity = harness.AddExecuteActivity<
            ResolveArgumentsActivity,
            ResolveArguments>(_ => new ResolveArgumentsActivity(observed));
        using var completed = new CourierMessageRecorder<RoutingSlipCompleted>(1);
        using var activityCompleted = new CourierMessageRecorder<RoutingSlipActivityCompleted>(1);
        completed.Configure(harness);
        activityCompleted.Configure(harness);
        await harness.StartAsync(cancellationToken);

        try
        {
            Guid trackingNumber = NewId.NextGuid();
            DateTime variableTimestamp = new(2026, 9, 11, 4, 0, 0, DateTimeKind.Utc);
            DateTime activityTimestamp = variableTimestamp.AddMinutes(1);
            var builder = new RoutingSlipBuilder(trackingNumber);
            builder.AddActivity(activity.Name, activity.ExecuteAddress, ArgumentsFor(shape, trackingNumber, activityTimestamp));
            builder.SetVariable(nameof(ResolveArguments.Value), "variable");
            builder.SetVariable(nameof(ResolveArguments.GuidValue), trackingNumber);
            builder.SetVariable(nameof(ResolveArguments.Count), 27);
            builder.SetVariable(nameof(ResolveArguments.Enabled), true);
            builder.SetVariable(nameof(ResolveArguments.Timestamp), variableTimestamp);

            await harness.Bus.ExecuteAsync(builder.Build(), cancellationToken);
            await Task.WhenAll(
                completed.WaitAsync(timeout, cancellationToken),
                activityCompleted.WaitAsync(timeout, cancellationToken),
                observed.Task.WaitAsync(timeout, cancellationToken));
            await harness.StopAsync(TestContext.Current.CancellationToken);
            ResolvedArguments actual = await observed.Task;

            Assert.Equal(expectedValue, actual.Value);
            Assert.Equal(trackingNumber, actual.GuidValue);
            Assert.Equal(shape == ArgumentShape.Explicit ? 42 : 27, actual.Count);
            Assert.True(actual.Enabled);
            Assert.Equal(shape == ArgumentShape.Explicit ? activityTimestamp : variableTimestamp, actual.Timestamp);
            Assert.Equal(trackingNumber, Assert.Single(completed.Messages).Message.TrackingNumber);
            ConsumeContext<RoutingSlipActivityCompleted> activityEvent = Assert.Single(activityCompleted.Messages);
            Assert.Equal(trackingNumber, activityEvent.Message.TrackingNumber);
            Assert.Equal(expectedValue, activityEvent.GetArgument<string>(nameof(ResolveArguments.Value)));
            Assert.Equal(trackingNumber, activityEvent.GetArgument<Guid>(nameof(ResolveArguments.GuidValue)));
            Assert.Equal(shape == ArgumentShape.Explicit ? 42 : 27,
                activityEvent.GetArgument<int>(nameof(ResolveArguments.Count)));
            Assert.True(activityEvent.GetArgument<bool>(nameof(ResolveArguments.Enabled)));
            Assert.Equal(shape == ArgumentShape.Explicit ? activityTimestamp : variableTimestamp,
                activityEvent.GetArgument<DateTime>(nameof(ResolveArguments.Timestamp)));
        }
        finally
        {
            await harness.StopAsync(TestContext.Current.CancellationToken);
        }
    }

    private static object ArgumentsFor(ArgumentShape shape, Guid trackingNumber, DateTime activityTimestamp) => shape switch
    {
        ArgumentShape.Explicit => new
        {
            Value = "activity",
            GuidValue = trackingNumber,
            Count = 42,
            Enabled = true,
            Timestamp = activityTimestamp,
        },
        ArgumentShape.Missing => new { },
        ArgumentShape.Null => new { Value = (string?)null },
        ArgumentShape.Default => new
        {
            Value = (string?)null,
            GuidValue = Guid.Empty,
            Count = 0,
            Enabled = false,
            Timestamp = default(DateTime),
        },
        _ => throw new ArgumentOutOfRangeException(nameof(shape)),
    };

    public enum ArgumentShape
    {
        Explicit,
        Missing,
        Null,
        Default,
    }

    public sealed record ResolveArguments(string Value, Guid GuidValue, int Count, bool Enabled, DateTime Timestamp);

    public sealed record ResolvedArguments(string Value, Guid GuidValue, int Count, bool Enabled, DateTime Timestamp);

    public sealed class ResolveArgumentsActivity(TaskCompletionSource<ResolvedArguments> observed) :
        IExecuteActivity<ResolveArguments>
    {
        public Task<ExecutionResult> ExecuteAsync(ExecuteContext<ResolveArguments> context)
        {
            observed.TrySetResult(new ResolvedArguments(
                context.Arguments.Value,
                context.Arguments.GuidValue,
                context.Arguments.Count,
                context.Arguments.Enabled,
                context.Arguments.Timestamp));
            return Task.FromResult(context.Completed());
        }
    }
}

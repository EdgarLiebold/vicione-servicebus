using System.Collections.Concurrent;
using System.Diagnostics;
using ViciOne.ServiceBus.Courier.Contracts;
using ViciOne.ServiceBus.Monitoring;
using ViciOne.ServiceBus.Testing;
using ViciOne.ServiceBus.Tests.Infrastructure.Requirements;
using ViciOne.ServiceBus.Tests.Testing;
using Xunit;

namespace ViciOne.ServiceBus.Tests.Courier;

[Collection(OpenTelemetryGlobalCollection.Name)]
public sealed class CourierTraceTests
{
    [Fact]
    [RequirementCoverage("REQ-VSB-MESSAGE-ACTIVITY", "courier-execution-tags-and-parent-trace")]
    public async Task Execution_RecordsExactCourierIdentityAndPreservesTheCallerTraceAsync()
    {
        const string callerSource = "ViciOne.ServiceBus.Tests.CourierCaller";
        TimeSpan timeout = CourierTestSupport.OperationTimeout();
        CancellationToken token = TestContext.Current.CancellationToken;
        var recorded = new ConcurrentQueue<Activity>();
        var executions = new ConcurrentQueue<(Guid TrackingNumber, string Value, Activity? Activity)>();
        using var listener = new ActivityListener
        {
            ShouldListenTo = source => source.Name == ServiceBusTelemetry.ActivitySourceName || source.Name == callerSource,
            Sample = (ref ActivityCreationOptions<System.Diagnostics.ActivityContext> _) => ActivitySamplingResult.AllDataAndRecorded,
            SampleUsingParentId = (ref ActivityCreationOptions<string> _) => ActivitySamplingResult.AllDataAndRecorded,
            ActivityStopped = recorded.Enqueue,
        };
        ActivitySource.AddActivityListener(listener);
        using InMemoryTestHarness harness = CourierTestSupport.CreateHarness("courier-trace");
        ExecuteActivityTestHarness<TraceActivity, TraceArguments> activity = harness.AddExecuteActivity<TraceActivity, TraceArguments>(
            _ => new TraceActivity(executions));
        using var completed = new CourierMessageRecorder<IRoutingSlipCompleted>(1);
        completed.Configure(harness);
        await harness.StartAsync(token).WaitAsync(timeout, token);
        Guid trackingNumber = NewId.NextGuid();
        using var source = new ActivitySource(callerSource);
        Activity? caller = source.StartActivity("courier-caller", ActivityKind.Internal);
        try
        {
            Assert.NotNull(caller);
            var builder = new RoutingSlipBuilder(trackingNumber);
            builder.AddActivity(activity.Name, activity.ExecuteAddress, new TraceArguments("original payload"));
            await harness.Bus.ExecuteAsync(builder.Build(), token).WaitAsync(timeout, token);
            await completed.WaitAsync(timeout, token);
        }
        finally
        {
            caller?.Dispose();
            await harness.StopAsync(CancellationToken.None).WaitAsync(timeout, CancellationToken.None);
        }

        Assert.Equal(trackingNumber, Assert.Single(completed.Messages).Message.TrackingNumber);
        var execution = Assert.Single(executions);
        Assert.Equal(trackingNumber, execution.TrackingNumber);
        Assert.Equal("original payload", execution.Value);
        Activity process = Assert.IsType<Activity>(execution.Activity);
        Assert.Contains(process, recorded);
        Assert.Equal(ServiceBusTelemetry.ActivitySourceName, process.Source.Name);
        Assert.Equal(ActivityKind.Consumer, process.Kind);
        Assert.Equal("process", process.GetTagItem(ServiceBusTelemetry.Attributes.OperationType));
        Assert.Equal(trackingNumber.ToString("D"), process.GetTagItem(ServiceBusTelemetry.Attributes.CourierTrackingNumber));
        Assert.Equal(TypeCache<TraceActivity>.ShortName, process.GetTagItem(ServiceBusTelemetry.Attributes.ProcessorName));
        Assert.Equal(MessageTypeCache<TraceArguments>.DiagnosticAddress, process.GetTagItem(ServiceBusTelemetry.Attributes.MessageContract));
        Assert.NotEqual(MessageTypeCache<IRoutingSlip>.DiagnosticAddress, process.GetTagItem(ServiceBusTelemetry.Attributes.MessageContract));
        Assert.Equal(caller!.TraceId, process.TraceId);
        Activity receive = Assert.Single(recorded, span => span.SpanId == process.ParentSpanId && span.TraceId == process.TraceId);
        Assert.Equal("receive", receive.GetTagItem(ServiceBusTelemetry.Attributes.OperationType));
        Activity send = Assert.Single(recorded, span => span.SpanId == receive.ParentSpanId && span.TraceId == receive.TraceId);
        Assert.Equal("send", send.GetTagItem(ServiceBusTelemetry.Attributes.OperationType));
        Assert.Equal(caller.SpanId, send.ParentSpanId);
        Assert.Empty(harness.Published.Snapshot<IRoutingSlipFaulted>());
        Assert.Empty(harness.Published.Snapshot<Fault<IRoutingSlip>>());
    }

    public sealed record TraceArguments(string Value);

    private sealed class TraceActivity(ConcurrentQueue<(Guid TrackingNumber, string Value, Activity? Activity)> executions)
        : IExecuteActivity<TraceArguments>
    {
        public Task<ExecutionResult> ExecuteAsync(ExecuteContext<TraceArguments> context)
        {
            executions.Enqueue((context.TrackingNumber, context.Arguments.Value, Activity.Current));
            return Task.FromResult(context.Completed());
        }
    }
}

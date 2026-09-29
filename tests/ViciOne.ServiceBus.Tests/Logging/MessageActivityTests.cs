using System.Diagnostics;
using System.Net.Mime;
using System.Reflection;
using ViciOne.ServiceBus.Logging;
using ViciOne.ServiceBus.Logging.Diagnostics;
using ViciOne.ServiceBus.Middleware;
using ViciOne.ServiceBus.Monitoring;
using ViciOne.ServiceBus.Serialization;
using ViciOne.ServiceBus.Tests.Infrastructure.Requirements;
using ViciOne.ServiceBus.Tests.Testing;
using ViciOne.ServiceBus.Transports;
using Xunit;
using DiagnosticActivityContext = System.Diagnostics.ActivityContext;

namespace ViciOne.ServiceBus.Tests.Logging;

[Collection(OpenTelemetryGlobalCollection.Name)]
public sealed class MessageActivityTests
{
    [Fact]
    [RequirementCoverage("REQ-VSB-ACTIVITY-GENERIC", "sampling-lifecycle-kind-and-listener-fault-isolation")]
    public void TryStart_ObeysSamplingAndIsolatesApplicationListenerFailures()
    {
        Assert.Null(MessageActivity.TryStart("unsampled operation"));

        Activity? observed = null;
        using (var listener = new ActivityListener
        {
            ShouldListenTo = source => source.Name == ServiceBusTelemetry.ActivitySourceName,
            Sample = static (ref ActivityCreationOptions<DiagnosticActivityContext> _) => ActivitySamplingResult.AllDataAndRecorded,
            ActivityStarted = activity => observed = activity,
        })
        {
            ActivitySource.AddActivityListener(listener);

            using StartedActivity? started = MessageActivity.TryStart("topology.configure");

            Assert.NotNull(started);
            Assert.Same(started.Activity, observed);
            Activity activity = Assert.IsType<Activity>(observed);
            Assert.Equal("topology.configure", activity.OperationName);
            Assert.Equal(ActivityKind.Client, activity.Kind);
            Assert.Equal(ServiceBusTelemetry.ActivitySourceName, activity.Source.Name);
            Assert.True(activity.IsAllDataRequested);
        }

        using (var hostileListener = new ActivityListener
        {
            ShouldListenTo = source => source.Name == ServiceBusTelemetry.ActivitySourceName,
            Sample = static (ref ActivityCreationOptions<DiagnosticActivityContext> _) => ActivitySamplingResult.AllDataAndRecorded,
            ActivityStarted = _ => throw new InvalidOperationException("application listener failure"),
        })
        {
            ActivitySource.AddActivityListener(hostileListener);

            Assert.Null(MessageActivity.TryStart("fault-isolated operation"));
            Assert.Null(Activity.Current);
        }

        using var parent = new Activity("sampler business parent").Start();
        using (var hostileSampler = new ActivityListener
        {
            ShouldListenTo = source => source.Name == ServiceBusTelemetry.ActivitySourceName,
            Sample = (ref ActivityCreationOptions<DiagnosticActivityContext> _) =>
            {
                Activity.Current = null;
                throw new InvalidOperationException("application sampler failure");
            },
        })
        {
            ActivitySource.AddActivityListener(hostileSampler);
            try
            {
                Assert.Null(MessageActivity.TryStart("sampler fault"));
                Assert.Same(parent, Activity.Current);
            }
            finally
            {
                Activity.Current = parent;
            }
        }

        using var reentrantListener = new ActivityListener
        {
            ShouldListenTo = source => source.Name == ServiceBusTelemetry.ActivitySourceName,
            Sample = static (ref ActivityCreationOptions<DiagnosticActivityContext> _) =>
                ActivitySamplingResult.AllDataAndRecorded,
            ActivityStarted = _ => Activity.Current = parent,
        };
        ActivitySource.AddActivityListener(reentrantListener);
        try
        {
            using StartedActivity started = Assert.IsType<StartedActivity>(MessageActivity.TryStart("reentrant start"));
            Assert.Same(started.Activity, Activity.Current);
        }
        finally
        {
            Activity.Current = parent;
        }
        Assert.Same(parent, Activity.Current);
    }

    [Theory]
    [InlineData(false)]
    [InlineData(true)]
    [RequirementCoverage("REQ-VSB-OBSERVABILITY-ISOLATION", "listener-stops-activity-during-start")]
    public void TryStart_RejectsAnActivityStoppedByItsStartObserver(bool dispose)
    {
        using var listener = new ActivityListener
        {
            ShouldListenTo = source => source.Name == ServiceBusTelemetry.ActivitySourceName,
            Sample = static (ref ActivityCreationOptions<DiagnosticActivityContext> _) => ActivitySamplingResult.AllDataAndRecorded,
            ActivityStarted = activity =>
            {
                if (dispose)
                    activity.Dispose();
                else
                    activity.Stop();
            },
        };
        ActivitySource.AddActivityListener(listener);
        using var parent = new Activity("business parent").Start();

        Assert.Null(MessageActivity.TryStart("observer stopped operation"));
        Assert.Same(parent, Activity.Current);
    }

    [Fact]
    [RequirementCoverage("REQ-VSB-OBSERVABILITY-ISOLATION", "activity-source-does-not-require-log-context")]
    public void TryStart_DoesNotRequireALoggingContext()
    {
        ILogContext? previous = LogContext.Current;
        Activity? observed = null;
        using var listener = new ActivityListener
        {
            ShouldListenTo = source => source.Name == ServiceBusTelemetry.ActivitySourceName,
            Sample = static (ref ActivityCreationOptions<DiagnosticActivityContext> _) => ActivitySamplingResult.AllDataAndRecorded,
            ActivityStarted = activity => observed = activity,
        };

        try
        {
            LogContext.Current = null;
            ActivitySource.AddActivityListener(listener);

            using StartedActivity? started = MessageActivity.TryStart("independent tracing");

            Assert.NotNull(started);
            Assert.Same(started.Activity, observed);
        }
        finally
        {
            LogContext.Current = previous;
        }
    }

    [Fact]
    [RequirementCoverage("REQ-VSB-MESSAGE-ACTIVITY", "carrier-parent-remains-remote-with-ambient-activity")]
    public void CarrierParent_RemainsRemoteWhenAnUnrelatedAmbientActivityExists()
    {
        var headers = new DictionarySendHeaders();
        using var carrier = new Activity("carrier");
        carrier.SetIdFormat(ActivityIdFormat.W3C);
        carrier.Start();
        string carrierId = Assert.IsType<string>(carrier.Id);
        DiagnosticActivityContext carrierContext = carrier.Context;
        carrier.Stop();
        headers.Set(DiagnosticPropagationHeaders.ActivityId, carrierId);

        using var ambient = new Activity("ambient");
        ambient.SetIdFormat(ActivityIdFormat.W3C);
        ambient.Start();

        DiagnosticActivityContext extracted = MessageActivity.GetParentActivityContext(headers, isRemote: true);

        Assert.True(extracted.IsRemote);
        Assert.Equal(carrierContext.TraceId, extracted.TraceId);
        Assert.Equal(carrierContext.SpanId, extracted.SpanId);
        Assert.NotEqual(ambient.SpanId, extracted.SpanId);
    }

    [Fact]
    [RequirementCoverage("REQ-VSB-MESSAGE-ACTIVITY", "outbox-enqueue-and-delivery-form-one-trace")]
    public void OutboxActivities_FormOneTraceAcrossTheStoredCarrier()
    {
        var stopped = new List<Activity>();
        using var listener = new ActivityListener
        {
            ShouldListenTo = source => source.Name == ServiceBusTelemetry.ActivitySourceName,
            Sample = static (ref ActivityCreationOptions<DiagnosticActivityContext> _) => ActivitySamplingResult.AllDataAndRecorded,
            ActivityStopped = stopped.Add,
        };
        ActivitySource.AddActivityListener(listener);
        var sendContext = new MessageSendContext<OutboxMessage>(new OutboxMessage("deferred"));

        using (StartedActivity enqueue = Assert.IsType<StartedActivity>(MessageActivity.TryStartOutboxSend(sendContext)))
        {
            Assert.Equal("outbox send", enqueue.Activity.OperationName);
        }

        OutboxMessageContext storedMessage = DispatchProxy.Create<OutboxMessageContext, OutboxMessageContextProxy>();
        ((OutboxMessageContextProxy)(object)storedMessage).Headers = sendContext.Headers;
        using (StartedActivity delivery = Assert.IsType<StartedActivity>(MessageActivity.TryStartOutboxDelivery(storedMessage)))
        {
            Assert.Equal("outbox process", delivery.Activity.OperationName);
        }

        Activity enqueueActivity = Assert.Single(stopped, activity => activity.OperationName == "outbox send");
        Activity deliveryActivity = Assert.Single(stopped, activity => activity.OperationName == "outbox process");
        Assert.Equal(ActivityKind.Producer, enqueueActivity.Kind);
        Assert.Equal(ActivityKind.Client, deliveryActivity.Kind);
        Assert.Equal("send", Operation(enqueueActivity));
        Assert.Equal("process", Operation(deliveryActivity));
        Assert.Equal(enqueueActivity.TraceId, deliveryActivity.TraceId);
        Assert.Equal(enqueueActivity.SpanId, deliveryActivity.ParentSpanId);
        Assert.Equal(ActivityStatusCode.Ok, enqueueActivity.Status);
        Assert.Equal(ActivityStatusCode.Ok, deliveryActivity.Status);
    }

    [Theory]
    [InlineData("Link")]
    [InlineData("New")]
    [InlineData(null)]
    [RequirementCoverage("REQ-VSB-MESSAGE-ACTIVITY", "receive-parent-modes-link-new-and-carrier-parent")]
    public void ReceiveParentMode_SelectsTheRequestedTraceRelationship(string? parentMode)
    {
        var headers = new DictionarySendHeaders();
        using var carrier = new Activity("carrier");
        carrier.SetIdFormat(ActivityIdFormat.W3C);
        carrier.Start();
        string carrierId = Assert.IsType<string>(carrier.Id);
        DiagnosticActivityContext carrierContext = carrier.Context;
        carrier.Stop();
        headers.Set(DiagnosticPropagationHeaders.ActivityId, carrierId);
        if (parentMode is not null)
            headers.Set(DiagnosticPropagationHeaders.ParentMode, parentMode);
        using var listener = new ActivityListener
        {
            ShouldListenTo = source => source.Name == ServiceBusTelemetry.ActivitySourceName,
            Sample = static (ref ActivityCreationOptions<DiagnosticActivityContext> _) => ActivitySamplingResult.AllDataAndRecorded,
        };
        ActivitySource.AddActivityListener(listener);
        var receiveContext = new ParentModeReceiveContext(headers);

        using StartedActivity started = Assert.IsType<StartedActivity>(MessageActivity.TryStartReceive(
            "orders receive",
            "loopback://localhost/orders",
            "orders",
            receiveContext));
        Activity activity = started.Activity;

        Assert.Equal(ActivityKind.Consumer, activity.Kind);
        Assert.Equal("receive", Operation(activity));
        switch (parentMode)
        {
            case "Link":
                ActivityLink link = Assert.Single(activity.Links);
                Assert.Equal(carrierContext.TraceId, link.Context.TraceId);
                Assert.Equal(carrierContext.SpanId, link.Context.SpanId);
                Assert.True(link.Context.IsRemote);
                Assert.Equal(default, activity.ParentSpanId);
                Assert.NotEqual(carrierContext.TraceId, activity.TraceId);
                break;
            case "New":
                Assert.Empty(activity.Links);
                Assert.Equal(default, activity.ParentSpanId);
                Assert.NotEqual(carrierContext.TraceId, activity.TraceId);
                break;
            default:
                Assert.Empty(activity.Links);
                Assert.Equal(carrierContext.TraceId, activity.TraceId);
                Assert.Equal(carrierContext.SpanId, activity.ParentSpanId);
                break;
        }
    }

    [Fact]
    [RequirementCoverage("REQ-VSB-OBSERVABILITY-ISOLATION", "new-trace-receive-restores-existing-ambient-activity")]
    public void ReceiveWithNewTrace_RestoresTheAmbientCallerAfterStop()
    {
        var headers = new DictionarySendHeaders();
        headers.Set(DiagnosticPropagationHeaders.ParentMode, "New");
        using var listener = new ActivityListener
        {
            ShouldListenTo = source => source.Name == ServiceBusTelemetry.ActivitySourceName,
            Sample = static (ref ActivityCreationOptions<DiagnosticActivityContext> _) => ActivitySamplingResult.AllDataAndRecorded,
        };
        ActivitySource.AddActivityListener(listener);
        using var caller = new Activity("ambient caller").Start();

        StartedActivity started = Assert.IsType<StartedActivity>(MessageActivity.TryStartReceive(
            "orders receive", "loopback://localhost/orders", "orders", new ParentModeReceiveContext(headers)));
        Assert.Same(started.Activity, Activity.Current);
        Assert.Null(started.Activity.Parent);

        started.Stop();

        Assert.Same(caller, Activity.Current);
    }

    private static string? Operation(Activity activity) =>
        activity.TagObjects
            .FirstOrDefault(tag => tag.Key == ServiceBusTelemetry.Attributes.OperationType)
            .Value?
            .ToString();

    private sealed record OutboxMessage(string Value);

    private sealed class ParentModeReceiveContext(Headers headers) : BasePipeContext, ReceiveContext
    {
        public TimeSpan ElapsedTime => throw new NotSupportedException();
        public Uri InputAddress { get; } = new("loopback://localhost/orders");
        public ContentType ContentType => throw new NotSupportedException();
        public bool Redelivered => throw new NotSupportedException();
        public Headers TransportHeaders { get; } = headers;
        public Task ReceiveCompleted => throw new NotSupportedException();
        public bool IsDelivered => throw new NotSupportedException();
        public bool IsFaulted => throw new NotSupportedException();
        public ISendEndpointProvider SendEndpointProvider => throw new NotSupportedException();
        public IPublishEndpointProvider PublishEndpointProvider => throw new NotSupportedException();
        public bool PublishFaults => throw new NotSupportedException();
        public MessageBody Body => throw new NotSupportedException();

        public Task NotifyConsumedAsync<TMessage>(ConsumeContext<TMessage> context, TimeSpan duration, string consumerType,
            CancellationToken cancellationToken = default)
            where TMessage : class => throw new NotSupportedException();

        public Task NotifyFaultedAsync<TMessage>(ConsumeContext<TMessage> context, TimeSpan duration, string consumerType,
            Exception exception, CancellationToken cancellationToken = default)
            where TMessage : class => throw new NotSupportedException();

        public Task NotifyFaultedAsync(Exception exception, CancellationToken cancellationToken = default) =>
            throw new NotSupportedException();

        public void AddReceiveTask(Task task)
        {
            throw new NotSupportedException();
        }
    }

    private class OutboxMessageContextProxy : DispatchProxy
    {
        public Headers Headers { get; set; } = null!;

        protected override object? Invoke(MethodInfo? targetMethod, object?[]? args)
        {
            ArgumentNullException.ThrowIfNull(targetMethod);
            if (targetMethod.Name == "get_Headers")
                return Headers;

            throw new NotSupportedException(targetMethod.Name);
        }
    }
}

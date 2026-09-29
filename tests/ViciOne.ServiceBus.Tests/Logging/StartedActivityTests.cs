using System.Diagnostics;
using System.Reflection;
using Microsoft.Extensions.Time.Testing;
using ViciOne.ServiceBus.Logging.Diagnostics;
using ViciOne.ServiceBus.Middleware;
using ViciOne.ServiceBus.Monitoring;
using ViciOne.ServiceBus.Tests.Infrastructure.Requirements;
using Xunit;

namespace ViciOne.ServiceBus.Tests.Logging;

public sealed class StartedActivityTests
{
    private static readonly DateTimeOffset ObservationTime =
        new(2042, 3, 4, 5, 6, 7, TimeSpan.Zero);

    [Fact]
    [RequirementCoverage("REQ-VSB-ACTIVITY-OUTCOME", "successful-completion-is-ok-and-idempotent")]
    public void Stop_SetsSuccessAndCompletesTheActivityExactlyOnce()
    {
        using var activity = new Activity("successful operation").Start();
        var started = new StartedActivity(activity);

        started.SetTag("test.value", "present");
        started.SetTag("test.empty", "  ");
        started.Stop();
        TimeSpan duration = activity.Duration;
        started.Stop();
        started.Dispose();

        Assert.Equal(ActivityStatusCode.Ok, activity.Status);
        Assert.Equal("present", activity.GetTagItem("test.value"));
        Assert.Null(activity.GetTagItem("test.empty"));
        Assert.True(duration > TimeSpan.Zero);
        Assert.Equal(duration, activity.Duration);
    }

    [Fact]
    [RequirementCoverage("REQ-VSB-ACTIVITY-OUTCOME", "exception-event-and-error-status-use-current-otel-contract")]
    public void AddExceptionEvent_RecordsTheBaseFailureAndCurrentOpenTelemetryAttributes()
    {
        using var activity = new Activity("failed operation").Start();
        var started = new StartedActivity(activity, new FakeTimeProvider(ObservationTime));
        Exception failure = CaptureFailure();

        started.AddExceptionEvent(new ApplicationException("outer", failure));
        started.Stop();

        ActivityEvent exceptionEvent = Assert.Single(activity.Events);
        Assert.Equal(ServiceBusTelemetry.Events.Exception, exceptionEvent.Name);
        Assert.Equal(ObservationTime, exceptionEvent.Timestamp);
        Assert.Equal(ActivityStatusCode.Error, activity.Status);
        Assert.Equal("inner failure", activity.StatusDescription);
        Assert.Equal(typeof(InvalidOperationException).FullName, Tag(exceptionEvent, ServiceBusTelemetry.Attributes.ExceptionType));
        Assert.Equal("inner failure", Tag(exceptionEvent, ServiceBusTelemetry.Attributes.ExceptionMessage));
        Assert.Contains(nameof(CaptureFailure),
            Assert.IsType<string>(Tag(exceptionEvent, ServiceBusTelemetry.Attributes.ExceptionStackTrace)),
            StringComparison.Ordinal);
        Assert.DoesNotContain(exceptionEvent.Tags, tag => tag.Key == "exception.escaped");
    }

    [Theory]
    [InlineData(false)]
    [InlineData(true)]
    [RequirementCoverage("REQ-VSB-ACTIVITY-OUTCOME", "unsafe-base-lookup-still-records-original-failure")]
    public void AddExceptionEvent_UnsafeBaseLookupStillRecordsTheOriginalFailure(bool nullBase)
    {
        using var activity = new Activity("failed operation").Start();
        var started = new StartedActivity(activity, new FakeTimeProvider(ObservationTime));
        var failure = new UnsafeBaseException(nullBase);

        started.AddExceptionEvent(failure);
        started.Stop();

        ActivityEvent exceptionEvent = Assert.Single(activity.Events);
        Assert.Equal(ObservationTime, exceptionEvent.Timestamp);
        Assert.Equal(typeof(UnsafeBaseException).FullName,
            Tag(exceptionEvent, ServiceBusTelemetry.Attributes.ExceptionType));
        Assert.Equal("original failure", Tag(exceptionEvent, ServiceBusTelemetry.Attributes.ExceptionMessage));
        Assert.Equal(ActivityStatusCode.Error, activity.Status);
        Assert.Equal("original failure", activity.StatusDescription);
    }

    [Fact]
    [RequirementCoverage("REQ-VSB-ACTIVITY-OUTCOME", "invalid-inputs-fail-at-the-boundary")]
    public void ConstructorAndMutationMethods_RejectInvalidInputs()
    {
        Assert.Throws<ArgumentNullException>(() => new StartedActivity(null!));
        using var activity = new Activity("validation").Start();
        var started = new StartedActivity(activity);

        Assert.Throws<ArgumentException>(() => started.SetTag(" ", "value"));
        Assert.Throws<ArgumentNullException>(() => started.AddExceptionEvent(null!));
        Assert.Throws<ArgumentNullException>(() => started.Update<object>(null!));
    }

    [Fact]
    [RequirementCoverage("REQ-VSB-ACTIVITY-OUTCOME", "send-body-length-tag-is-exact")]
    public void Update_RecordsTheExactSerializedBodyLength()
    {
        using var activity = new Activity("serialized send").Start();
        var started = new StartedActivity(activity);
        SendContext<BodyLengthMessage> context = DispatchProxy.Create<SendContext<BodyLengthMessage>, BodyLengthSendContextProxy>();
        ((BodyLengthSendContextProxy)(object)context).BodyLength = 73;

        started.Update(context);

        Assert.Equal(73L, activity.GetTagItem(ServiceBusTelemetry.Attributes.MessageBodySize));
    }

    private static Exception CaptureFailure()
    {
        try
        {
            throw new InvalidOperationException("inner failure");
        }
        catch (Exception exception)
        {
            return exception;
        }
    }

    private static object? Tag(ActivityEvent activityEvent, string name) =>
        activityEvent.Tags.Single(tag => tag.Key == name).Value;

    private sealed record BodyLengthMessage;

    private sealed class UnsafeBaseException(bool nullBase) : Exception("original failure")
    {
        public override Exception GetBaseException() => nullBase
            ? null!
            : throw new InvalidOperationException("base lookup failed");
    }

    private class BodyLengthSendContextProxy : DispatchProxy
    {
        public long? BodyLength { get; set; }

        protected override object? Invoke(MethodInfo? targetMethod, object?[]? args)
        {
            ArgumentNullException.ThrowIfNull(targetMethod);
            if (targetMethod.Name == "get_BodyLength")
                return BodyLength;

            throw new NotSupportedException(targetMethod.Name);
        }
    }
}

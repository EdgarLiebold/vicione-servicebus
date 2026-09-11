using System.Diagnostics;
using Microsoft.Extensions.Logging.Abstractions;
using ViciOne.ServiceBus.Logging.Diagnostics;
using ViciOne.ServiceBus.Logging.Internal;
using ViciOne.ServiceBus.Monitoring;
using ViciOne.ServiceBus.Tests.Infrastructure.Requirements;
using ViciOne.ServiceBus.Tests.Testing;
using Xunit;
using DiagnosticActivityContext = System.Diagnostics.ActivityContext;

namespace ViciOne.ServiceBus.Tests.Logging;

[Collection(OpenTelemetryGlobalCollection.Name)]
public sealed class LogContextActivityExtensionsTests
{
    [Fact]
    [RequirementCoverage("REQ-VSB-ACTIVITY-GENERIC", "sampling-lifecycle-kind-and-listener-fault-isolation")]
    public void StartGenericActivity_ObeysSamplingAndIsolatesApplicationListenerFailures()
    {
        var logContext = new BusLogContext(NullLoggerFactory.Instance);

        Assert.Null(logContext.StartGenericActivity("unsampled operation"));

        Activity? observed = null;
        using (var listener = new ActivityListener
        {
            ShouldListenTo = source => source.Name == ServiceBusTelemetry.ActivitySourceName,
            Sample = static (ref ActivityCreationOptions<DiagnosticActivityContext> _) => ActivitySamplingResult.AllDataAndRecorded,
            ActivityStarted = activity => observed = activity,
        })
        {
            ActivitySource.AddActivityListener(listener);

            using StartedActivity? started = logContext.StartGenericActivity("topology.configure");

            Assert.NotNull(started);
            Assert.Same(started.Activity, observed);
            Activity activity = Assert.IsType<Activity>(observed);
            Assert.Equal("topology.configure", activity.OperationName);
            Assert.Equal(ActivityKind.Client, activity.Kind);
            Assert.Equal(ServiceBusTelemetry.ActivitySourceName, activity.Source.Name);
            Assert.True(activity.IsAllDataRequested);
        }

        using var hostileListener = new ActivityListener
        {
            ShouldListenTo = source => source.Name == ServiceBusTelemetry.ActivitySourceName,
            Sample = static (ref ActivityCreationOptions<DiagnosticActivityContext> _) => ActivitySamplingResult.AllDataAndRecorded,
            ActivityStarted = _ => throw new InvalidOperationException("application listener failure"),
        };
        ActivitySource.AddActivityListener(hostileListener);

        Assert.Null(logContext.StartGenericActivity("fault-isolated operation"));
        Assert.Null(Activity.Current);
    }
}

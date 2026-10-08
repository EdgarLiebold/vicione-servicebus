using System.Diagnostics;
using System.Diagnostics.Metrics;
using ViciOne.ServiceBus.Advanced.Serialization;
using ViciOne.ServiceBus.Monitoring;
using ViciOne.ServiceBus.Monitoring.Telemetry;
using ViciOne.ServiceBus.Operations;
using ViciOne.ServiceBus.Providers.Persistence;
using ViciOne.ServiceBus.Tests.Infrastructure.Requirements;
using ViciOne.ServiceBus.Tests.Testing;
using Xunit;

namespace ViciOne.ServiceBus.Tests.Monitoring;

[Collection(OpenTelemetryGlobalCollection.Name)]
public sealed class ActivitySourceActivationIsolationTests
{
    [Theory]
    [InlineData(false)]
    [InlineData(true)]
    [RequirementCoverage("REQ-VSB-OBSERVABILITY-ISOLATION", "typed-source-construction-keeps-independent-host-metrics")]
    public void Constructor_PreservesCallerAndHostMetricsWhenSourceListenerThrows(bool hostileSource)
    {
        using var caller = new Activity("typed source caller").Start();
        using var factory = new RecordingMeterFactory();
        var measurements = new List<Measurement>();
        using var meterListener = new MeterListener
        {
            InstrumentPublished = (instrument, listener) =>
            {
                if (ReferenceEquals(instrument.Meter, factory.Meter))
                    listener.EnableMeasurementEvents(instrument);
            }
        };
        meterListener.SetMeasurementEventCallback<long>((instrument, value, tags, _) =>
            measurements.Add(new Measurement(instrument.Name, value, tags.ToArray())));
        meterListener.Start();

        bool armed = false;
        int constructors = 0;
        var activities = new List<Activity>();
        using var activityListener = new ActivityListener
        {
            ShouldListenTo = source =>
            {
                if (!armed || source.Name != ServiceBusTelemetry.ActivitySourceName)
                    return false;
                constructors++;
                if (hostileSource)
                {
                    Activity.Current = null;
                    throw new InvalidOperationException("host source constructor callback failed");
                }
                return true;
            },
            Sample = static (ref ActivityCreationOptions<System.Diagnostics.ActivityContext> _) =>
                ActivitySamplingResult.AllDataAndRecorded,
            ActivityStopped = activities.Add
        };
        ActivitySource.AddActivityListener(activityListener);
        armed = true;
        ServiceBusInstrumentation<IBus>? subject = null;
        try
        {
            Exception? escaped = Record.Exception(() => subject = new ServiceBusInstrumentation<IBus>(factory));

            Assert.Equal(1, constructors);
            Assert.Null(escaped);
            Assert.Same(caller, Activity.Current);
            ServiceBusInstrumentation<IBus> instrumentation = Assert.IsType<ServiceBusInstrumentation<IBus>>(subject);
            instrumentation.RecordDurableAdmission(DurableSendAdmissionDisposition.Accepted, 5);
            instrumentation.RecordPayloadBody(PayloadAdmissionDisposition.Inline, 3, warningThresholdExceeded: false);

            Assert.Equal(1, factory.Creates);
            Meter meter = Assert.IsType<Meter>(factory.Meter);
            Assert.Equal(ServiceBusTelemetry.MeterName, meter.Name);
            Assert.Equal(typeof(IBus).FullName, meter.Tags!.Single(tag => tag.Key == ServiceBusTelemetry.Attributes.Bus).Value);
            Assert.Equal(4, measurements.Count);
            AssertMeasurement(measurements, ServiceBusTelemetry.Metrics.DurableSenderAdmission, 1, "accepted");
            AssertMeasurement(measurements, ServiceBusTelemetry.Metrics.DurableSenderAdmissionSize, 5, "accepted");
            AssertMeasurement(measurements, ServiceBusTelemetry.Metrics.PayloadAdmission, 1, "inline");
            AssertMeasurement(measurements, ServiceBusTelemetry.Metrics.PayloadBodySize, 3, "inline");

            using (SafeActivityScope scope = instrumentation.StartDurableAdmission(Message()))
            {
                if (hostileSource)
                    Assert.Same(SafeActivityScope.None, scope);
                else
                    Assert.NotSame(SafeActivityScope.None, scope);
            }
            Assert.Same(caller, Activity.Current);
            if (hostileSource)
                Assert.Empty(activities);
            else
            {
                Activity activity = Assert.Single(activities);
                Assert.True(activity.IsStopped);
                Assert.Equal("durable send admit", activity.OperationName);
                Assert.Equal(ServiceBusTelemetry.ActivitySourceName, activity.Source.Name);
                Assert.Equal(typeof(IBus).FullName, activity.GetTagItem(ServiceBusTelemetry.Attributes.Bus));
                Assert.Equal(caller.SpanId, activity.ParentSpanId);
                Assert.Equal(caller.TraceId, activity.TraceId);
            }

            instrumentation.Dispose();
            instrumentation.Dispose();
            instrumentation.RecordDurableAdmission(DurableSendAdmissionDisposition.Accepted, 20);
            instrumentation.RecordPayloadBody(PayloadAdmissionDisposition.Inline, 30, warningThresholdExceeded: false);
            Assert.Same(SafeActivityScope.None, instrumentation.StartDurableAdmission(Message()));
            Assert.Equal(4, measurements.Count);
            Assert.Equal(0, factory.Disposals);

            Counter<long> hostCounter = meter.CreateCounter<long>("host.ownership.control");
            hostCounter.Add(1);
            Measurement host = Assert.Single(measurements, item => item.Name == "host.ownership.control");
            Assert.Equal(1, host.Value);
            Assert.Empty(host.Tags);
            Assert.Equal(5, measurements.Count);
            Assert.Equal(1, factory.Creates);
            Assert.Equal(1, constructors);
        }
        finally
        {
            armed = false;
            subject?.Dispose();
            Activity.Current = caller;
        }
    }

    private static void AssertMeasurement(List<Measurement> measurements, string name, long expected, string outcome)
    {
        Measurement measurement = Assert.Single(measurements, item => item.Name == name);
        Assert.Equal(expected, measurement.Value);
        Assert.Equal(outcome, measurement.Tags.Single(tag => tag.Key == ServiceBusTelemetry.Attributes.Outcome).Value);
    }

    private static SerializedDurableSend Message() => new()
    {
        Id = new DurableSendId(Guid.Parse("aaaaaaaa-bbbb-cccc-dddd-eeeeeeeeeeee")),
        ContractIdentity = new MessageContractIdentity("vicione.tests.source-isolation", 1),
        DestinationAddress = new Uri("loopback://source-isolation/messages"),
        ContentType = "application/json",
        Body = new byte[] { 1, 2, 3 },
        Metadata = new byte[] { 4, 5 }
    };

    private sealed record Measurement(string Name, long Value, KeyValuePair<string, object?>[] Tags);

    private sealed class RecordingMeterFactory : IMeterFactory
    {
        public int Creates { get; private set; }
        public int Disposals { get; private set; }
        public Meter? Meter { get; private set; }

        public Meter Create(MeterOptions options)
        {
            Creates++;
            Meter = new Meter(options);
            return Meter;
        }

        public void Dispose()
        {
            Disposals++;
            Meter?.Dispose();
        }
    }
}

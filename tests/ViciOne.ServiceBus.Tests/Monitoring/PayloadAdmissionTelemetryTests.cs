using System.Diagnostics.Metrics;
using Microsoft.Extensions.DependencyInjection;
using ViciOne.ServiceBus.Monitoring;
using ViciOne.ServiceBus.Serialization;
using ViciOne.ServiceBus.Tests.Infrastructure.Requirements;
using Xunit;

namespace ViciOne.ServiceBus.Tests.Monitoring;

public sealed class PayloadAdmissionTelemetryTests
{
    [Fact]
    [RequirementCoverage("REQ-VSB-PAYLOAD-ADMISSION-OBSERVABILITY", "once-per-buffer-bounded-payload-free-tags")]
    public void BoundedWriterRejection_IsObservedOncePerBufferWithBoundedPayloadFreeTags()
    {
        using ServiceProvider provider = BuildProvider();
        IMeterFactory meterFactory = provider.GetRequiredService<IMeterFactory>();
        using var observations = new MetricObservationSession(meterFactory);
        IPayloadAdmissionEvaluator<IBus> evaluator = provider.GetRequiredService<IPayloadAdmissionEvaluator<IBus>>();
        IPayloadSerializationBuffer body = evaluator.CreateSerializedBodyBuffer();

        PayloadAdmissionException first = Assert.Throws<PayloadAdmissionException>(() => body.GetMemory(9));
        PayloadAdmissionException second = Assert.Throws<PayloadAdmissionException>(() => body.GetMemory(10));

        Assert.Equal(PayloadAdmissionStage.SerializedBody, first.Stage);
        Assert.Equal(PayloadAdmissionStage.SerializedBody, second.Stage);
        Assert.Equal(9, first.ActualBytes);
        Assert.Equal(10, second.ActualBytes);
        Assert.Single(observations.Measurements, IsRejectedAdmission);
        Assert.Single(observations.Measurements, IsRejectedBodySize);

        IPayloadSerializationBuffer envelope = evaluator.CreateTransportEnvelopeBuffer();
        PayloadAdmissionException envelopeFailure = Assert.Throws<PayloadAdmissionException>(() => envelope.GetSpan(9));

        Assert.Equal(PayloadAdmissionStage.TransportEnvelope, envelopeFailure.Stage);
        Assert.Equal(2, observations.Measurements.Count(IsRejectedAdmission));
        Assert.Single(observations.Measurements, IsRejectedEnvelopeSize);
        Assert.All(observations.Measurements.Where(measurement =>
            IsRejectedAdmission(measurement)
            || IsRejectedBodySize(measurement)
            || IsRejectedEnvelopeSize(measurement)), measurement =>
        {
            Assert.InRange(measurement.Tags.Count, 2, 2);
            Assert.Equal([ServiceBusTelemetry.Attributes.ErrorType, ServiceBusTelemetry.Attributes.Outcome],
                measurement.Tags.Select(tag => tag.Key).Order(StringComparer.Ordinal).ToArray());
            Assert.All(measurement.Tags, tag =>
            {
                string value = Assert.IsType<string>(tag.Value);
                Assert.InRange(value.Length, 1, 64);
                Assert.DoesNotContain("secret-customer-payload", value, StringComparison.Ordinal);
            });
        });
    }

    [Fact]
    [RequirementCoverage("REQ-VSB-PAYLOAD-ADMISSION-OBSERVABILITY", "throwing-exporter-preserves-results-and-original-failures")]
    public void ThrowingMeterExporter_CannotChangeAdmissionResultsOrFailures()
    {
        using ServiceProvider provider = BuildProvider();
        IMeterFactory meterFactory = provider.GetRequiredService<IMeterFactory>();
        var callbackCount = 0;
        using var listener = new MeterListener();
        listener.InstrumentPublished = (instrument, current) =>
        {
            if (instrument.Meter.Name == ServiceBusTelemetry.MeterName
                && ReferenceEquals(instrument.Meter.Scope, meterFactory)
                && instrument.Name.StartsWith("vicione.servicebus.payload.", StringComparison.Ordinal))
                current.EnableMeasurementEvents(instrument);
        };
        listener.SetMeasurementEventCallback<long>((_, _, _, _) =>
        {
            Interlocked.Increment(ref callbackCount);
            throw new HostileExporterException();
        });
        listener.Start();
        IPayloadAdmissionEvaluator<IBus> evaluator = provider.GetRequiredService<IPayloadAdmissionEvaluator<IBus>>();

        PayloadAdmissionResult accepted = evaluator.EvaluateSerializedBody(new byte[8], messageDataOffloadObserved: false);
        Assert.Equal(PayloadAdmissionDisposition.Inline, accepted.Disposition);
        Assert.Equal(8, accepted.SerializedBodyBytes);

        PayloadAdmissionException evaluatedFailure = Assert.Throws<PayloadAdmissionException>(
            () => evaluator.EvaluateSerializedBody(new byte[9], messageDataOffloadObserved: false));
        Assert.Equal(PayloadAdmissionStage.SerializedBody, evaluatedFailure.Stage);
        Assert.Equal(9, evaluatedFailure.ActualBytes);

        IPayloadSerializationBuffer bounded = evaluator.CreateSerializedBodyBuffer();
        PayloadAdmissionException writerFailure = Assert.Throws<PayloadAdmissionException>(() => bounded.GetMemory(9));
        Assert.Equal(PayloadAdmissionStage.SerializedBody, writerFailure.Stage);
        Assert.Equal(9, writerFailure.ActualBytes);

        evaluator.ValidateTransportEnvelope(new byte[8]);
        PayloadAdmissionException envelopeFailure = Assert.Throws<PayloadAdmissionException>(
            () => evaluator.ValidateTransportEnvelope(new byte[9]));
        Assert.Equal(PayloadAdmissionStage.TransportEnvelope, envelopeFailure.Stage);
        Assert.Equal(9, envelopeFailure.ActualBytes);
        Assert.True(Volatile.Read(ref callbackCount) >= 4);
    }

    private static ServiceProvider BuildProvider()
    {
        var services = new ServiceCollection();
        services.AddViciOnePayloadAdmission<IBus>(options =>
        {
            options.MaximumSerializedBodyBytes = 8;
            options.MaximumTransportEnvelopeBytes = 8;
        });
        return services.BuildServiceProvider(
            new ServiceProviderOptions { ValidateOnBuild = true, ValidateScopes = true });
    }

    private static bool IsRejectedAdmission(MetricMeasurement measurement) =>
        measurement.Name == ServiceBusTelemetry.Metrics.PayloadAdmission
        && Equals(measurement.Tag(ServiceBusTelemetry.Attributes.Outcome), "rejected");

    private static bool IsRejectedBodySize(MetricMeasurement measurement) =>
        measurement.Name == ServiceBusTelemetry.Metrics.PayloadBodySize
        && Equals(measurement.Tag(ServiceBusTelemetry.Attributes.Outcome), "rejected");

    private static bool IsRejectedEnvelopeSize(MetricMeasurement measurement) =>
        measurement.Name == ServiceBusTelemetry.Metrics.PayloadEnvelopeSize
        && Equals(measurement.Tag(ServiceBusTelemetry.Attributes.Outcome), "rejected");

    private sealed class HostileExporterException : Exception
    {
    }
}

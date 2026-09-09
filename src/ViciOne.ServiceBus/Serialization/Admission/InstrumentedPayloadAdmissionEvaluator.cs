using System;

using ViciOne.ServiceBus.Monitoring.Telemetry;

namespace ViciOne.ServiceBus.Advanced.Serialization;

/// <summary>Records payload-admission metrics without allowing instrumentation to alter admission decisions.</summary>
/// <typeparam name="TBus">The bus whose payload-admission metrics are recorded.</typeparam>
internal sealed class InstrumentedPayloadAdmissionEvaluator<TBus> : IPayloadAdmissionEvaluator<TBus>
    where TBus : class, IBus
{
    private readonly PayloadAdmissionEvaluator<TBus> _inner;
    private readonly ServiceBusInstrumentation<TBus> _instrumentation;
    private readonly Action<PayloadAdmissionException> _recordBoundedWriterRejection;

    public InstrumentedPayloadAdmissionEvaluator(
        PayloadAdmissionEvaluator<TBus> inner,
        ServiceBusInstrumentation<TBus> instrumentation)
    {
        _inner = inner ?? throw new ArgumentNullException(nameof(inner));
        _instrumentation = instrumentation ?? throw new ArgumentNullException(nameof(instrumentation));
        _recordBoundedWriterRejection = RecordBoundedWriterRejection;
    }

    public IPayloadSerializationBuffer CreateSerializedBodyBuffer()
        => _inner.CreateSerializedBodyBuffer(_recordBoundedWriterRejection);

    public PayloadAdmissionResult EvaluateSerializedBody(ReadOnlyMemory<byte> serializedBody, bool messageDataOffloadObserved)
    {
        try
        {
            PayloadAdmissionResult result = _inner.EvaluateSerializedBody(serializedBody, messageDataOffloadObserved);
            _instrumentation.RecordPayloadBody(result.Disposition, result.SerializedBodyBytes, result.WarningThresholdExceeded);
            return result;
        }
        catch (PayloadAdmissionException exception)
        {
            _instrumentation.RecordPayloadRejected(exception.Stage, exception.ActualBytes);
            throw;
        }
    }

    public IPayloadSerializationBuffer CreateTransportEnvelopeBuffer()
        => _inner.CreateTransportEnvelopeBuffer(_recordBoundedWriterRejection);

    public void ValidateTransportEnvelope(ReadOnlyMemory<byte> serializedEnvelope)
    {
        try
        {
            _inner.ValidateTransportEnvelope(serializedEnvelope);
            _instrumentation.RecordPayloadEnvelope(serializedEnvelope.Length, rejected: false);
        }
        catch (PayloadAdmissionException)
        {
            _instrumentation.RecordPayloadEnvelope(serializedEnvelope.Length, rejected: true);
            throw;
        }
    }

    private void RecordBoundedWriterRejection(PayloadAdmissionException exception)
        => _instrumentation.RecordPayloadRejected(exception.Stage, exception.ActualBytes);
}

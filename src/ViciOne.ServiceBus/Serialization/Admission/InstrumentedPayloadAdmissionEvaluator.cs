using System;

using ViciOne.ServiceBus.Diagnostics;


namespace ViciOne.ServiceBus.Advanced.Serialization;
/// <summary>Exception-isolated observation around the exact-byte admission evaluator.</summary>
internal sealed class InstrumentedPayloadAdmissionEvaluator<TBus> : IPayloadAdmissionEvaluator<TBus>
    where TBus : class, IBus
{
    private readonly PayloadAdmissionEvaluator<TBus> _inner;
    private readonly V5ServiceBusInstrumentation<TBus> _instrumentation;
    private readonly Action<PayloadAdmissionException> _recordBoundedWriterRejection;

    public InstrumentedPayloadAdmissionEvaluator(
        PayloadAdmissionEvaluator<TBus> inner,
        V5ServiceBusInstrumentation<TBus> instrumentation)
    {
        _inner = inner ?? throw new ArgumentNullException(nameof(inner));
        _instrumentation = instrumentation ?? throw new ArgumentNullException(nameof(instrumentation));
        _recordBoundedWriterRejection = RecordBoundedWriterRejection;
    }

    public IPayloadSerializationBuffer CreateSerializedBodyBuffer()
        => _inner.CreateSerializedBodyBuffer(_recordBoundedWriterRejection);

    public PayloadAdmissionResult EvaluateSerializedBody(ReadOnlyMemory<byte> serializedBody, bool messageDataAvailable)
    {
        try
        {
            PayloadAdmissionResult result = _inner.EvaluateSerializedBody(serializedBody, messageDataAvailable);
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

using System;

namespace ViciOne.ServiceBus.Advanced.Serialization;

/// <summary>Adapts a typed payload-admission evaluator to the transport's non-generic runtime contract.</summary>
/// <typeparam name="TBus">The bus whose payload policy is enforced.</typeparam>
internal sealed class PayloadAdmissionRuntime<TBus> : IPayloadAdmissionRuntime
    where TBus : class, IBus
{
    readonly IPayloadAdmissionEvaluator<TBus> _evaluator;

    public PayloadAdmissionRuntime(IPayloadAdmissionEvaluator<TBus> evaluator)
    {
        _evaluator = evaluator ?? throw new ArgumentNullException(nameof(evaluator));
    }

    public IPayloadSerializationBuffer CreateSerializedBodyBuffer() => _evaluator.CreateSerializedBodyBuffer();

    public PayloadAdmissionResult EvaluateSerializedBody(ReadOnlyMemory<byte> serializedBody, bool messageDataOffloadObserved) =>
        _evaluator.EvaluateSerializedBody(serializedBody, messageDataOffloadObserved);

    public IPayloadSerializationBuffer CreateTransportEnvelopeBuffer() => _evaluator.CreateTransportEnvelopeBuffer();

    public void ValidateTransportEnvelope(ReadOnlyMemory<byte> serializedEnvelope) =>
        _evaluator.ValidateTransportEnvelope(serializedEnvelope);
}

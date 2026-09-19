using System;
using ViciOne.ServiceBus.Configuration;

namespace ViciOne.ServiceBus.Advanced.Serialization;

/// <summary>Adapts a typed payload-admission evaluator to the transport's non-generic runtime contract.</summary>
/// <typeparam name="TBus">The bus whose payload policy is enforced.</typeparam>
internal sealed class PayloadAdmissionRuntime<TBus> : IPayloadAdmissionRuntime
    where TBus : class, IBus
{
    readonly IPayloadAdmissionEvaluator<TBus> _evaluator;
    readonly PayloadAdmissionPolicy _policy;

    public PayloadAdmissionRuntime(IPayloadAdmissionEvaluator<TBus> evaluator)
        : this(evaluator, evaluator is PayloadAdmissionEvaluator<TBus> builtIn
            ? builtIn.Policy
            : throw new ArgumentException("A custom payload-admission evaluator requires the bus policy.", nameof(evaluator)))
    {
    }

    public PayloadAdmissionRuntime(IPayloadAdmissionEvaluator<TBus> evaluator, PayloadAdmissionPolicyProvider<TBus> policy)
        : this(evaluator, (policy ?? throw new ArgumentNullException(nameof(policy))).Value)
    {
    }

    internal PayloadAdmissionRuntime(IPayloadAdmissionEvaluator<TBus> evaluator, PayloadAdmissionPolicy policy)
    {
        _evaluator = evaluator ?? throw new ArgumentNullException(nameof(evaluator));
        _policy = (policy ?? throw new ArgumentNullException(nameof(policy))).Validate();
    }

    public int MaximumTransportEnvelopeBytes => _policy.MaximumTransportEnvelopeBytes;

    public IPayloadSerializationBuffer CreateSerializedBodyBuffer() => _evaluator.CreateSerializedBodyBuffer();

    public PayloadAdmissionResult EvaluateSerializedBody(ReadOnlyMemory<byte> serializedBody, bool messageDataOffloadObserved)
    {
        EnforceHostLimits(serializedBody.Length);
        EnforceHostOffloadThreshold(serializedBody.Length, messageDataOffloadObserved);
        return _evaluator.EvaluateSerializedBody(serializedBody, messageDataOffloadObserved);
    }

    public IPayloadSerializationBuffer CreateTransportEnvelopeBuffer() => _evaluator.CreateTransportEnvelopeBuffer();

    public void ValidateTransportEnvelope(ReadOnlyMemory<byte> serializedEnvelope)
    {
        EnforceHostEnvelopeLimit(serializedEnvelope.Length);
        _evaluator.ValidateTransportEnvelope(serializedEnvelope);
    }

    public void ValidatePreviouslyAdmittedBodyLength(int serializedBodyBytes, bool messageDataOffloadObserved)
    {
        if (serializedBodyBytes < 0)
            throw new ArgumentOutOfRangeException(nameof(serializedBodyBytes));

        // Durable replay already passed the original evaluator. The persisted proof binds
        // that decision to the exact envelope; current host limits must still be enforced.
        EnforceHostLimits(serializedBodyBytes);
        EnforceHostOffloadThreshold(serializedBodyBytes, messageDataOffloadObserved);
    }

    void EnforceHostLimits(long bytes)
    {
        if (bytes > _policy.MaximumSerializedBodyBytes)
            throw new PayloadAdmissionException(
                PayloadAdmissionStage.SerializedBody,
                bytes,
                _policy.MaximumSerializedBodyBytes,
                $"Serialized payload body is {bytes} bytes, exceeding the configured maximum of {_policy.MaximumSerializedBodyBytes} bytes.");

        EnforceHostEnvelopeLimit(bytes);
    }

    void EnforceHostEnvelopeLimit(long bytes)
    {
        if (bytes > _policy.MaximumTransportEnvelopeBytes)
            throw new PayloadAdmissionException(
                PayloadAdmissionStage.TransportEnvelope,
                bytes,
                _policy.MaximumTransportEnvelopeBytes,
                $"Final serialized transport envelope is {bytes} bytes, exceeding the configured maximum of {_policy.MaximumTransportEnvelopeBytes} bytes.");
    }

    void EnforceHostOffloadThreshold(long bytes, bool messageDataOffloadObserved)
    {
        if (_policy.MessageDataOffloadThresholdBytes is { } threshold
            && bytes > threshold
            && !messageDataOffloadObserved)
            throw new PayloadAdmissionException(
                PayloadAdmissionStage.MessageData,
                bytes,
                threshold,
                $"Serialized payload body is {bytes} bytes and requires MessageData above {threshold} bytes, but this bus has no stored MessageData reference.");
    }
}

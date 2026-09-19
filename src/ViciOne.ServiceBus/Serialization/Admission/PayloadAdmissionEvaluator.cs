using System;

namespace ViciOne.ServiceBus.Advanced.Serialization;

/// <summary>Evaluates only exact bytes already produced by the configured serializer.</summary>
/// <typeparam name="TBus">The bus whose payload-admission policy is evaluated.</typeparam>
public sealed class PayloadAdmissionEvaluator<TBus> : IPayloadAdmissionEvaluator<TBus>
    where TBus : class, IBus
{
    private readonly PayloadAdmissionPolicy _policy;

    internal PayloadAdmissionPolicy Policy => _policy;

    internal PayloadAdmissionEvaluator(PayloadAdmissionPolicyProvider<TBus> policy)
    {
        ArgumentNullException.ThrowIfNull(policy);
        _policy = policy.Value;
    }

    /// <summary>Initializes an evaluator with an immutable payload-admission policy.</summary>
    /// <param name="policy">The policy to validate and apply.</param>
    public PayloadAdmissionEvaluator(PayloadAdmissionPolicy policy)
    {
        _policy = (policy ?? throw new ArgumentNullException(nameof(policy))).Validate();
    }

    /// <inheritdoc />
    public IPayloadSerializationBuffer CreateSerializedBodyBuffer()
        => CreateSerializedBodyBuffer(rejectionObserver: null);

    internal IPayloadSerializationBuffer CreateSerializedBodyBuffer(Action<PayloadAdmissionException>? rejectionObserver)
        => new BoundedPayloadSerializationBuffer(
            _policy.MaximumSerializedBodyBytes,
            PayloadAdmissionStage.SerializedBody,
            rejectionObserver);

    /// <inheritdoc />
    public PayloadAdmissionResult EvaluateSerializedBody(ReadOnlyMemory<byte> serializedBody, bool messageDataOffloadObserved)
    {
        int bodyBytes = serializedBody.Length;

        if (bodyBytes > _policy.MaximumSerializedBodyBytes)
        {
            throw new PayloadAdmissionException(
                PayloadAdmissionStage.SerializedBody,
                bodyBytes,
                _policy.MaximumSerializedBodyBytes,
                $"Serialized payload body is {bodyBytes} bytes, exceeding the configured maximum of {_policy.MaximumSerializedBodyBytes} bytes.");
        }

        bool warning = _policy.WarningBodyBytes is { } warningThreshold && bodyBytes > warningThreshold;
        if (_policy.MessageDataOffloadThresholdBytes is { } offloadThreshold && bodyBytes > offloadThreshold)
        {
            if (!messageDataOffloadObserved)
            {
                throw new PayloadAdmissionException(
                    PayloadAdmissionStage.MessageData,
                    bodyBytes,
                    offloadThreshold,
                    $"Serialized payload body is {bodyBytes} bytes and requires MessageData above {offloadThreshold} bytes, but this bus has no MessageData owner.");
            }

            return new PayloadAdmissionResult(PayloadAdmissionDisposition.OffloadToMessageData, bodyBytes, warning);
        }

        return new PayloadAdmissionResult(PayloadAdmissionDisposition.Inline, bodyBytes, warning);
    }

    /// <inheritdoc />
    public IPayloadSerializationBuffer CreateTransportEnvelopeBuffer()
        => CreateTransportEnvelopeBuffer(rejectionObserver: null);

    internal IPayloadSerializationBuffer CreateTransportEnvelopeBuffer(Action<PayloadAdmissionException>? rejectionObserver)
        => new BoundedPayloadSerializationBuffer(
            _policy.MaximumTransportEnvelopeBytes,
            PayloadAdmissionStage.TransportEnvelope,
            rejectionObserver);

    /// <inheritdoc />
    public void ValidateTransportEnvelope(ReadOnlyMemory<byte> serializedEnvelope)
    {
        int envelopeBytes = serializedEnvelope.Length;
        if (envelopeBytes > _policy.MaximumTransportEnvelopeBytes)
        {
            throw new PayloadAdmissionException(
                PayloadAdmissionStage.TransportEnvelope,
                envelopeBytes,
                _policy.MaximumTransportEnvelopeBytes,
                $"Final serialized transport envelope is {envelopeBytes} bytes, exceeding the configured maximum of {_policy.MaximumTransportEnvelopeBytes} bytes.");
        }
    }
}

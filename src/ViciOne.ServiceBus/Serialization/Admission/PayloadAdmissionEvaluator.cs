using System;


namespace ViciOne.ServiceBus.Advanced.Serialization;
/// <summary>Evaluates only exact bytes already produced by the configured serializer.</summary>
public sealed class PayloadAdmissionEvaluator<TBus> : IPayloadAdmissionEvaluator<TBus>
    where TBus : class, IBus
{
    private readonly PayloadAdmissionPolicy _policy;

    internal PayloadAdmissionEvaluator(PayloadAdmissionPolicyProvider<TBus> policy)
    {
        ArgumentNullException.ThrowIfNull(policy);
        _policy = policy.Value;
    }

    /// <summary>Creates an evaluator from an immutable validated policy.</summary>
    public PayloadAdmissionEvaluator(PayloadAdmissionPolicy policy)
    {
        _policy = (policy ?? throw new ArgumentNullException(nameof(policy))).Validate();
    }

    /// <summary>
    /// Creates serialized body buffer.
    /// </summary>
    /// <returns>The result of the operation.</returns>
    public IPayloadSerializationBuffer CreateSerializedBodyBuffer()
        => CreateSerializedBodyBuffer(rejectionObserver: null);

    internal IPayloadSerializationBuffer CreateSerializedBodyBuffer(Action<PayloadAdmissionException>? rejectionObserver)
        => new BoundedPayloadSerializationBuffer(
            _policy.MaximumSerializedBodyBytes
                ?? throw new ConfigurationException(
                    global::ViciOne.ServiceBus.Providers.Configuration.ConfigurationMessages.Create("Serialization", "unknown", $"{nameof(PayloadAdmissionPolicy.MaximumSerializedBodyBytes)} must be configured for bounded payload serialization.", "Correct the named configuration before starting the host")),
            PayloadAdmissionStage.SerializedBody,
            rejectionObserver);

    /// <summary>
    /// Performs the evaluate serialized body operation.
    /// </summary>
    /// <param name="serializedBody">The serialized body value.</param>
    /// <param name="messageDataAvailable">The message data available value.</param>
    /// <returns>The result of the operation.</returns>
    public PayloadAdmissionResult EvaluateSerializedBody(ReadOnlyMemory<byte> serializedBody, bool messageDataAvailable)
    {
        int bodyBytes = serializedBody.Length;

        if (_policy.MaximumSerializedBodyBytes is { } maximum && bodyBytes > maximum)
        {
            throw new PayloadAdmissionException(
                PayloadAdmissionStage.SerializedBody,
                bodyBytes,
                maximum,
                $"Serialized payload body is {bodyBytes} bytes, exceeding the configured maximum of {maximum} bytes.");
        }

        bool warning = _policy.WarningBodyBytes is { } warningThreshold && bodyBytes > warningThreshold;
        if (_policy.MessageDataOffloadThresholdBytes is { } offloadThreshold && bodyBytes > offloadThreshold)
        {
            if (!messageDataAvailable)
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

    /// <summary>
    /// Creates transport envelope buffer.
    /// </summary>
    /// <returns>The result of the operation.</returns>
    public IPayloadSerializationBuffer CreateTransportEnvelopeBuffer()
        => CreateTransportEnvelopeBuffer(rejectionObserver: null);

    internal IPayloadSerializationBuffer CreateTransportEnvelopeBuffer(Action<PayloadAdmissionException>? rejectionObserver)
        => new BoundedPayloadSerializationBuffer(
            _policy.MaximumTransportEnvelopeBytes
                ?? throw new ConfigurationException(
                    global::ViciOne.ServiceBus.Providers.Configuration.ConfigurationMessages.Create("Serialization", "unknown", $"{nameof(PayloadAdmissionPolicy.MaximumTransportEnvelopeBytes)} must be configured for bounded transport-envelope serialization.", "Correct the named configuration before starting the host")),
            PayloadAdmissionStage.TransportEnvelope,
            rejectionObserver);

    /// <summary>
    /// Validates transport envelope.
    /// </summary>
    /// <param name="serializedEnvelope">The serialized envelope value.</param>
    public void ValidateTransportEnvelope(ReadOnlyMemory<byte> serializedEnvelope)
    {
        int envelopeBytes = serializedEnvelope.Length;
        if (_policy.MaximumTransportEnvelopeBytes is { } maximum && envelopeBytes > maximum)
        {
            throw new PayloadAdmissionException(
                PayloadAdmissionStage.TransportEnvelope,
                envelopeBytes,
                maximum,
                $"Final serialized transport envelope is {envelopeBytes} bytes, exceeding the configured maximum of {maximum} bytes.");
        }
    }
}

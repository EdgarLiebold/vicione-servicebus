using System;
using System.Security.Cryptography;
using System.Threading;

namespace ViciOne.ServiceBus.Advanced.Serialization;

/// <summary>Associates one send operation with its payload-admission runtime and offload evidence.</summary>
internal sealed class PayloadAdmissionSerializationContext
{
    public PayloadAdmissionSerializationContext(IPayloadAdmissionRuntime runtime, bool messageDataOffloadObserved)
    {
        OwnerRuntime = runtime ?? throw new ArgumentNullException(nameof(runtime));
        Runtime = new TrackingRuntime(runtime);
        MessageDataOffloadObserved = messageDataOffloadObserved;
    }

    public IPayloadAdmissionRuntime OwnerRuntime { get; }

    public IPayloadAdmissionRuntime Runtime { get; }

    public bool MessageDataOffloadObserved { get; }

    public bool HasCompleteAdmissionFor(long serializedLength)
        => ((TrackingRuntime)Runtime).HasCompleteAdmissionFor(serializedLength);

    public bool TryCreateDurableProof(string contentType, out DurablePayloadAdmissionProof proof)
        => ((TrackingRuntime)Runtime).TryCreateDurableProof(contentType, out proof);

    public void AdmitDurableReplay(ReadOnlyMemory<byte> envelope, string contentType, DurablePayloadAdmissionProof proof)
    {
        if (proof.SerializedBodyBytes < 0 || proof.SerializedBodyBytes > envelope.Length
            || !proof.MatchesEnvelope(envelope.Span, contentType))
            throw new InvalidOperationException("The durable payload-admission proof does not match the copied envelope.");

        Runtime.ValidatePreviouslyAdmittedBodyLength(proof.SerializedBodyBytes, proof.MessageDataOffloadObserved);
        Runtime.ValidateTransportEnvelope(envelope);
    }

    private sealed class TrackingRuntime : IPayloadAdmissionRuntime
    {
        private readonly IPayloadAdmissionRuntime _inner;
        private int _serializedBodyBytes = -1;
        private bool _messageDataOffloadObserved;
        private long _validatedEnvelopeLength = -1;
        private byte[]? _validatedEnvelopeSha256;

        public TrackingRuntime(IPayloadAdmissionRuntime inner) => _inner = inner;

        public bool HasCompleteAdmissionFor(long serializedLength)
            => Volatile.Read(ref _serializedBodyBytes) >= 0
                && Volatile.Read(ref _validatedEnvelopeLength) == serializedLength;

        public bool TryCreateDurableProof(
            string contentType,
            out DurablePayloadAdmissionProof proof)
        {
            byte[]? hash = Volatile.Read(ref _validatedEnvelopeSha256);
            int bodyBytes = Volatile.Read(ref _serializedBodyBytes);
            if (bodyBytes < 0 || bodyBytes > Volatile.Read(ref _validatedEnvelopeLength) || hash is null)
            {
                proof = default;
                return false;
            }

            proof = DurablePayloadAdmissionProof.Create(bodyBytes, Volatile.Read(ref _messageDataOffloadObserved), hash, contentType);
            return true;
        }

        public int MaximumTransportEnvelopeBytes => _inner.MaximumTransportEnvelopeBytes;

        public IPayloadSerializationBuffer CreateSerializedBodyBuffer() => _inner.CreateSerializedBodyBuffer();

        public PayloadAdmissionResult EvaluateSerializedBody(ReadOnlyMemory<byte> serializedBody, bool messageDataOffloadObserved)
        {
            PayloadAdmissionResult result = _inner.EvaluateSerializedBody(serializedBody, messageDataOffloadObserved);
            Volatile.Write(ref _messageDataOffloadObserved, messageDataOffloadObserved);
            Volatile.Write(ref _serializedBodyBytes, serializedBody.Length);
            return result;
        }

        public IPayloadSerializationBuffer CreateTransportEnvelopeBuffer() => _inner.CreateTransportEnvelopeBuffer();

        public void ValidateTransportEnvelope(ReadOnlyMemory<byte> serializedEnvelope)
        {
            _inner.ValidateTransportEnvelope(serializedEnvelope);
            Volatile.Write(ref _validatedEnvelopeSha256, SHA256.HashData(serializedEnvelope.Span));
            Volatile.Write(ref _validatedEnvelopeLength, serializedEnvelope.Length);
        }

        public void ValidatePreviouslyAdmittedBodyLength(int serializedBodyBytes, bool messageDataOffloadObserved)
        {
            _inner.ValidatePreviouslyAdmittedBodyLength(serializedBodyBytes, messageDataOffloadObserved);
            Volatile.Write(ref _messageDataOffloadObserved, messageDataOffloadObserved);
            Volatile.Write(ref _serializedBodyBytes, serializedBodyBytes);
        }
    }
}

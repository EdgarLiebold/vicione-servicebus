using System;

namespace ViciOne.ServiceBus.Advanced.Serialization;

/// <summary>Provides non-generic access to the payload-admission policy selected for a typed bus.</summary>
internal interface IPayloadAdmissionRuntime
{
    int MaximumTransportEnvelopeBytes { get; }

    IPayloadSerializationBuffer CreateSerializedBodyBuffer();

    PayloadAdmissionResult EvaluateSerializedBody(ReadOnlyMemory<byte> serializedBody, bool messageDataOffloadObserved);

    IPayloadSerializationBuffer CreateTransportEnvelopeBuffer();

    void ValidateTransportEnvelope(ReadOnlyMemory<byte> serializedEnvelope);

    void ValidatePreviouslyAdmittedBodyLength(int serializedBodyBytes, bool messageDataOffloadObserved);
}

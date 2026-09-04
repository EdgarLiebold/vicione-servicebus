using System;
using System.ComponentModel;

namespace ViciOne.ServiceBus.Serialization;
/// <summary>
/// Evaluates the exact bytes produced at the bus-owned serialization boundaries.
/// </summary>
/// <typeparam name="TBus">The bus whose immutable policy owns the decision.</typeparam>
[EditorBrowsable(EditorBrowsableState.Never)]
public interface IPayloadAdmissionEvaluator<TBus>
    where TBus : class, IBus
{
    /// <summary>Creates the bounded target for the single application-body serialization pass.</summary>
    IPayloadSerializationBuffer CreateSerializedBodyBuffer();

    /// <summary>Evaluates the exact application-body bytes already written to the bounded buffer.</summary>
    PayloadAdmissionResult EvaluateSerializedBody(ReadOnlyMemory<byte> serializedBody, bool messageDataAvailable);

    /// <summary>Creates the bounded target for final transport-envelope materialization.</summary>
    IPayloadSerializationBuffer CreateTransportEnvelopeBuffer();

    /// <summary>Validates the exact final transport-envelope bytes.</summary>
    void ValidateTransportEnvelope(ReadOnlyMemory<byte> serializedEnvelope);
}

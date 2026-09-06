using System;

namespace ViciOne.ServiceBus.Advanced.Serialization;
/// <summary>Evaluates the exact bytes produced at the bus-owned serialization boundaries.</summary>
/// <typeparam name="TBus">The bus whose immutable policy owns the decision.</typeparam>
public interface IPayloadAdmissionEvaluator<TBus>
    where TBus : class, IBus
{
    /// <summary>Creates the bounded target for the single application-body serialization pass.</summary>
    /// <returns>The created serialized body buffer.</returns>
    IPayloadSerializationBuffer CreateSerializedBodyBuffer();

    /// <summary>Evaluates the exact application-body bytes already written to the bounded buffer.</summary>
    /// <param name="serializedBody">The serialized body.</param>
    /// <param name="messageDataAvailable">The message data available.</param>
    /// <returns>The payload admission result produced by the operation.</returns>
    PayloadAdmissionResult EvaluateSerializedBody(ReadOnlyMemory<byte> serializedBody, bool messageDataAvailable);

    /// <summary>Creates the bounded target for final transport-envelope materialization.</summary>
    /// <returns>The created transport envelope buffer.</returns>
    IPayloadSerializationBuffer CreateTransportEnvelopeBuffer();

    /// <summary>Validates the exact final transport-envelope bytes.</summary>
    /// <param name="serializedEnvelope">The serialized envelope.</param>
    void ValidateTransportEnvelope(ReadOnlyMemory<byte> serializedEnvelope);
}

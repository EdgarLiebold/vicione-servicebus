using System.Text.Json;
using ViciOne.ServiceBus.Middleware;
using ViciOne.ServiceBus.Serialization;

namespace ViciOne.ServiceBus.Tests.InternalAccess.Serialization;

public static class PayloadAdmissionSerializationTestDriver
{
    public static void AttachAdmissionEvidence<T>(SendContext<T> context, byte[]? body, byte[]? envelope)
        where T : class
    {
        int limit = Math.Max(1, Math.Max(body?.Length ?? 0, envelope?.Length ?? 0));
        var runtime = new PayloadAdmissionRuntime<IBus>(new PayloadAdmissionEvaluator<IBus>(new PayloadAdmissionPolicy
        {
            MaximumSerializedBodyBytes = limit,
            MaximumTransportEnvelopeBytes = limit,
        }));
        var admission = new PayloadAdmissionSerializationContext(runtime, messageDataOffloadObserved: false);
        context.GetOrAddPayload(() => admission);
        if (body is not null)
            _ = admission.Runtime.EvaluateSerializedBody(body, false);
        if (envelope is not null)
            admission.Runtime.ValidateTransportEnvelope(envelope);
    }

    public static (int BodyBytes, bool OffloadObserved, bool MatchesEnvelope) ReadDurableProof(
        OutboxMessageContext context, byte[] envelope, string contentType)
    {
        DurablePayloadAdmissionProof proof = ((IDurableOutboxMessageContext)context).AdmissionProof
            ?? throw new InvalidOperationException("The outbox message has no durable admission proof.");
        return (proof.SerializedBodyBytes, proof.MessageDataOffloadObserved, proof.MatchesEnvelope(envelope, contentType));
    }

    public static byte[] SerializeJsonEnvelope<T>(
        SendContext<T> context,
        JsonSerializerOptions options,
        MessageEnvelope envelope,
        PayloadAdmissionPolicy policy)
        where T : class
    {
        var runtime = new PayloadAdmissionRuntime<IBus>(
            new PayloadAdmissionEvaluator<IBus>(policy));
        context.GetOrAddPayload(
            () => new PayloadAdmissionSerializationContext(runtime, messageDataOffloadObserved: false));

        return new SystemTextJsonMessageBody<T>(context, options, envelope).ToArray();
    }
}

using System;
using ViciOne.ServiceBus.Configuration;
using ViciOne.ServiceBus.Context;
using ViciOne.ServiceBus.MessageData.Admission;
using ViciOne.ServiceBus.Transports;

namespace ViciOne.ServiceBus.Advanced.Serialization;

/// <summary>Enforces payload admission at the physical boundary shared by send and publish operations.</summary>
internal static class PayloadAdmissionTransportBoundary
{
    public static void Apply<T>(IHostConfiguration hostConfiguration, SendContext<T> context)
        where T : class
    {
        ArgumentNullException.ThrowIfNull(hostConfiguration);
        ArgumentNullException.ThrowIfNull(context);

        if (hostConfiguration is not IPayloadAdmissionHostConfiguration { PayloadAdmissionRuntime: { } runtime })
            return;

        _ = Admit(runtime, context);
    }

    internal static MessageBody Admit<T>(IPayloadAdmissionRuntime runtime, SendContext<T> context)
        where T : class
    {
        ArgumentNullException.ThrowIfNull(runtime);
        ArgumentNullException.ThrowIfNull(context);

        if (context.BodyLength.HasValue
            && !context.TryGetPayload(out PayloadAdmissionSerializationContext? _))
        {
            throw new InvalidOperationException(
                "The send body was serialized before payload admission could be attached.");
        }

        bool messageDataOffloadObserved = context.TryGetPayload(out MessageDataAdmissionEvidence? evidence)
            && evidence.HasStoredReference;

        PayloadAdmissionSerializationContext admission = context.GetOrAddPayload(
            () => new PayloadAdmissionSerializationContext(runtime, messageDataOffloadObserved));
        if (!ReferenceEquals(admission.OwnerRuntime, runtime))
        {
            throw new ConfigurationException(
                global::ViciOne.ServiceBus.Providers.Configuration.ConfigurationMessages.Create(
                    "Serialization",
                    "unknown",
                    "The send context carries payload admission from a different bus.",
                    "Create a separate send context for each bus"));
        }

        if (context is not TransportSendContext transportContext)
        {
            throw new ConfigurationException(
                global::ViciOne.ServiceBus.Providers.Configuration.ConfigurationMessages.Create(
                    "Serialization",
                    "unknown",
                    "Payload admission requires a transport send context at the provider boundary.",
                    "Correct the named configuration before starting the host"));
        }

        if (context.ContentType is not { } contentType
            || !string.Equals(contentType.ToString(), context.Serializer.ContentType.ToString(), StringComparison.Ordinal))
        {
            throw new ConfigurationException(
                global::ViciOne.ServiceBus.Providers.Configuration.ConfigurationMessages.Create(
                    "Serialization",
                    "unknown",
                    "The send content type no longer matches its serializer after payload admission.",
                    "Keep the serializer and content type paired for this send operation"));
        }

        MessageBody body = transportContext.Body;
        long serializedLength = body.Length;
        bool admittedBody = body is IPayloadAdmittedMessageBody { AdmissionContext: { } bodyAdmission }
            && ReferenceEquals(bodyAdmission, admission);
        if (!admittedBody || !admission.HasCompleteAdmissionFor(serializedLength))
        {
            throw new ConfigurationException(
                global::ViciOne.ServiceBus.Providers.Configuration.ConfigurationMessages.Create(
                    "Serialization",
                    "unknown",
                    "The send serializer did not provide an immutable body admitted for this bus and operation.",
                    "Use a payload-admission-aware serializer or CopyBodySerializer"));
        }

        return body;
    }
}

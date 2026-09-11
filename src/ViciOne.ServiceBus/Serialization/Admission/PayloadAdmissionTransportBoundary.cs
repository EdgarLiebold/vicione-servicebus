using System;
using ViciOne.ServiceBus.Configuration;
using ViciOne.ServiceBus.Context;
using ViciOne.ServiceBus.MessageData;

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

        bool messageDataOffloadObserved = context.TryGetPayload(out MessageDataAdmissionEvidence? evidence)
            && evidence.HasStoredReference;

        context.GetOrAddPayload(() => new PayloadAdmissionSerializationContext(runtime, messageDataOffloadObserved));

        if (context is not TransportSendContext transportContext)
        {
            throw new ConfigurationException(
                global::ViciOne.ServiceBus.Providers.Configuration.ConfigurationMessages.Create(
                    "Serialization",
                    "unknown",
                    "Payload admission requires a transport send context at the provider boundary.",
                    "Correct the named configuration before starting the host"));
        }

        _ = transportContext.Body.GetBytes();
    }
}

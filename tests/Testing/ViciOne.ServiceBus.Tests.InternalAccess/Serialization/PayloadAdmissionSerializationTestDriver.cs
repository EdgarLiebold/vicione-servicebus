using System.Text.Json;
using ViciOne.ServiceBus.Serialization;

namespace ViciOne.ServiceBus.Tests.InternalAccess.Serialization;

public static class PayloadAdmissionSerializationTestDriver
{
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

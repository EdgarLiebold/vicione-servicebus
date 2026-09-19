using ViciOne.ServiceBus.Advanced.Serialization;

namespace ViciOne.ServiceBus.AmazonSqs;

/// <summary>Admits the exact UTF-8 text submitted to an Amazon SQS or SNS provider.</summary>
internal static class AmazonSqsTransportTextAdmission
{
    internal static void Validate(SendContext context, string text)
    {
        ArgumentNullException.ThrowIfNull(context);
        ArgumentNullException.ThrowIfNull(text);

        if (!context.TryGetPayload(out PayloadAdmissionSerializationContext? admission))
        {
            throw new ConfigurationException(
                global::ViciOne.ServiceBus.Providers.Configuration.ConfigurationMessages.Create(
                    "Amazon SQS/SNS transport text",
                    "unknown",
                    "The send context has no bus-owned payload admission for its transport text.",
                    "Configure message limits for the owning bus and send through its transport boundary"));
        }

        int maximumBytes = admission.Runtime.MaximumTransportEnvelopeBytes;
        int actualBytes = MessageDefaults.Encoding.GetByteCount(text);
        if (actualBytes > maximumBytes)
        {
            throw new PayloadAdmissionException(
                PayloadAdmissionStage.TransportEnvelope,
                actualBytes,
                maximumBytes,
                $"Final Amazon SQS/SNS transport text is {actualBytes} UTF-8 bytes, exceeding the configured envelope maximum of {maximumBytes} bytes.");
        }
    }
}

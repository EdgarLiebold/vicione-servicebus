// ViciOne modification: WP-F2-SERVICEBUS-IDENTITY, 2026-08-07.
namespace ViciOne.ServiceBus.KafkaIntegration.Serializers
{
    using System;
    using System.Text.Json;
    using Confluent.Kafka;
    using Serialization;


    public class ViciOneServiceBusJsonDeserializer<T> :
        IDeserializer<T>
    {
        public T Deserialize(ReadOnlySpan<byte> data, bool isNull, SerializationContext context)
        {
            if (data.IsEmpty && isNull)
                return default;

            return JsonSerializer.Deserialize<T>(data, SystemTextJsonMessageSerializer.Options);
        }
    }
}

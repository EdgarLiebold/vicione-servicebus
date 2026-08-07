// ViciOne modification: WP-F2-SERVICEBUS-IDENTITY, 2026-08-07.
namespace ViciOne.ServiceBus.KafkaIntegration.Tests;

using System.Net.Mime;
using Confluent.Kafka;
using Confluent.Kafka.SyncOverAsync;
using Confluent.SchemaRegistry;
using Confluent.SchemaRegistry.Serdes;
using Serializers;


public class AvroKafkaSerializerFactory :
    IKafkaSerializerFactory
{
    readonly ISchemaRegistryClient _client;

    public AvroKafkaSerializerFactory(ISchemaRegistryClient client)
    {
        _client = client;
    }

    public ContentType ContentType => new("application/avro");

    public IDeserializer<T> GetDeserializer<T>()
    {
        return new AvroDeserializer<T>(_client).AsSyncOverAsync();
    }

    public IAsyncSerializer<T> GetSerializer<T>()
    {
        return new AvroSerializer<T>(_client);
    }
}

// ViciOne modification: WP-F2-SERVICEBUS-IDENTITY, 2026-08-07.
namespace ViciOne.ServiceBus.Tests.Serialization
{
    using System;
    using System.Linq;
    using System.Threading.Tasks;
    using ViciOne.ServiceBus.Testing;
    using Microsoft.Extensions.DependencyInjection;
    using Newtonsoft.Json;
    using NUnit.Framework;


    [TestFixture]
    public class When_using_type_name_handling_auto
    {
        [Test]
        public async Task Should_properly_deserialize_the_message()
        {
            await using var provider = new ServiceCollection()
                .AddViciOneServiceBusTestHarness(x =>
                {
                    x.AddHandler(async (SampleMessage message) =>
                    {
                    });

                    x.UsingInMemory((context, cfg) =>
                    {
                        var typeNameHandlingConverter = new TypeNameHandlingConverter(TypeNameHandling.Auto);

                        cfg.ConfigureNewtonsoftJsonSerializer(settings =>
                        {
                            settings.Converters.Add(typeNameHandlingConverter);
                            return settings;
                        });
                        cfg.ConfigureNewtonsoftJsonDeserializer(settings =>
                        {
                            settings.Converters.Add(typeNameHandlingConverter);
                            return settings;
                        });
                        cfg.UseNewtonsoftJsonSerializer();

                        cfg.ConfigureEndpoints(context);
                    });
                })
                .BuildServiceProvider(true);

            var harness = provider.GetTestHarness();

            await harness.Start();

            await harness.Bus.Publish(new SampleMessage { EventId = "667" });

            Assert.That(await harness.Consumed.Any<SampleMessage>(), Is.True);

            IReceivedMessage<SampleMessage> context = harness.Consumed.Select<SampleMessage>().First();

            Assert.That(context.Context.Message.EventId, Is.EqualTo("667"));
        }


        public class SampleMessage
        {
            public string EventId { get; set; }
        }


        class TypeNameHandlingConverter :
            JsonConverter
        {
            readonly JsonSerializer _serializer;

            public TypeNameHandlingConverter(TypeNameHandling typeNameHandling)
            {
                _serializer = new JsonSerializer { TypeNameHandling = typeNameHandling };
            }

            public override void WriteJson(JsonWriter writer, object value, JsonSerializer serializer)
            {
                _serializer.Serialize(writer, value);
            }

            public override object ReadJson(JsonReader reader, Type objectType, object existingValue, JsonSerializer serializer)
            {
                return _serializer.Deserialize(reader, objectType);
            }

            public override bool CanConvert(Type objectType)
            {
                return !IsViciOneServiceBusOrSystemType(objectType);
            }

            static bool IsViciOneServiceBusOrSystemType(Type objectType)
            {
                return objectType.Assembly == typeof(IConsumer).Assembly || // ViciOne.ServiceBus.Abstractions
                    objectType.Assembly == typeof(ViciOneServiceBusBus).Assembly || // ViciOne.ServiceBus
                    objectType.Assembly.IsDynamic ||
                    objectType.Assembly == typeof(object).Assembly;
            }
        }
    }
}

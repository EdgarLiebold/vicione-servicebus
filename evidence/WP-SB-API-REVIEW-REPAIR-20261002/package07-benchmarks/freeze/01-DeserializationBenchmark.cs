using System;
using BenchmarkDotNet.Attributes;
using ViciOne.ServiceBus.Advanced;
using ViciOne.ServiceBus.Advanced.Serialization;
using ViciOne.ServiceBus.Context;
using ViciOne.ServiceBus.MessagePack;
using ViciOne.ServiceBus.Serialization;
using ViciOne.ServiceBus.Transports;

namespace ViciOne.ServiceBus.BenchmarkConsole;

[MemoryDiagnoser]
public class DeserializationBenchmark
{
    readonly IMessageDeserializer _messagePackDeserializer;
    readonly IMessageSerializer _messagePackSerializer;
    readonly SystemTextJsonMessageSerializer _systemTextJsonSerializer;

    MessageBody _messagePackMessageBody;
    MessageBody _systemTextJsonMessageBody;

    [Params(0, 4096, 16384)]
    public int MessageBufferSize { get; set; }

    public DeserializationBenchmark()
    {
        var factory = new MessagePackSerializerFactory();
        _messagePackSerializer = factory.CreateSerializer();
        _messagePackDeserializer = factory.CreateDeserializer();
        _systemTextJsonSerializer = CreateSystemTextJsonSerializer();
    }

    static SystemTextJsonMessageSerializer CreateSystemTextJsonSerializer()
    {
        var options = SystemTextJsonSerializerOptions.CreateDefault();
        options.MakeReadOnly();
        return new SystemTextJsonMessageSerializer(options);
    }

    [GlobalSetup]
    public void Setup()
    {
        var bufferContent = new byte[MessageBufferSize];
        var random = new Random(42);
        random.NextBytes(bufferContent);

        var initialMessage = new TypeToDeserialize
        {
            StringValue = "Hello, World!",
            IntValue = 42,
            GuidValue = Guid.Parse("45f32062-cda1-4bf7-b9bd-9e89ce77012f"),
            DateTimeValue = new DateTime(2026, 1, 1, 0, 0, 0, DateTimeKind.Utc),
            ByteArrayValue = bufferContent
        };

        var sendContext = new MessageSendContext<TypeToDeserialize>(initialMessage);

        _messagePackMessageBody = _messagePackSerializer.GetMessageBody(sendContext);
        _systemTextJsonMessageBody = _systemTextJsonSerializer.GetMessageBody(sendContext);

    }

    [Benchmark]
    public bool MessagePack_Deserialize()
    {
        var serializerContext = _messagePackDeserializer.Deserialize(_messagePackMessageBody, EmptyHeaders.Instance);
        return serializerContext.TryGetMessage<TypeToDeserialize>(out _);
    }

    [Benchmark(Baseline = true)]
    public bool SystemTextJson_Deserialize()
    {
        var serializerContext = _systemTextJsonSerializer.Deserialize(_systemTextJsonMessageBody, EmptyHeaders.Instance, null);
        return serializerContext.TryGetMessage<TypeToDeserialize>(out _);
    }

    class TypeToDeserialize
    {
        public string StringValue { get; set; }
        public int IntValue { get; set; }
        public Guid GuidValue { get; set; }
        public DateTime DateTimeValue { get; set; }
        public byte[] ByteArrayValue { get; set; }
    }
}

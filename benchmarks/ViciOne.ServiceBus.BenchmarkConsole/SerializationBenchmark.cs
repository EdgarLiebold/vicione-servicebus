using System;
using BenchmarkDotNet.Attributes;
using ViciOne.ServiceBus.Context;
using ViciOne.ServiceBus.Serialization;

namespace ViciOne.ServiceBus.BenchmarkConsole;

[MemoryDiagnoser]
public class SerializationBenchmark
{
    readonly MessagePackMessageSerializer _messagepackSerializer;
    readonly SystemTextJsonMessageSerializer _systemTextJsonSerializer;
    TypeToSerialize _serializationSubject = null!;
    MessageSendContext<TypeToSerialize> _sendContext = null!;

    [Params(0, 4096)]
    public int MessageBufferSize { get; set; }

    public SerializationBenchmark()
    {
        _messagepackSerializer = new MessagePackMessageSerializer();
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

        _serializationSubject = new TypeToSerialize
        {
            StringValue = "Hello, World!",
            IntValue = 42,
            GuidValue = Guid.Parse("45f32062-cda1-4bf7-b9bd-9e89ce77012f"),
            DateTimeValue = new DateTime(2026, 1, 1, 0, 0, 0, DateTimeKind.Utc),
            ByteArrayValue = bufferContent
        };

        _sendContext = new MessageSendContext<TypeToSerialize>(_serializationSubject);
    }

    [Benchmark]
    public byte[] MessagePack_SerializeObject()
    {
        var messageBody = _messagepackSerializer.SerializeObject(_serializationSubject);

        return messageBody.GetBytes();
    }

    /// <summary>
    /// Measures the envelope path taken by a send context. <see cref="MessagePack_SerializeObject" />
    /// measures the object path and therefore exercises a different operation.
    /// </summary>
    [Benchmark]
    public byte[] MessagePack_GetMessageBody()
    {
        MessageBody messageBody = _messagepackSerializer.GetMessageBody(_sendContext);

        return messageBody.GetBytes();
    }

    [Benchmark]
    public byte[] SystemTextJson_GetMessageBody()
    {
        MessageBody messageBody = _systemTextJsonSerializer.GetMessageBody(_sendContext);

        return messageBody.GetBytes();
    }

    [Benchmark(Baseline = true)]
    public byte[] SystemTextJson_SerializeObject()
    {
        var messageBody = _systemTextJsonSerializer.SerializeObject(_serializationSubject);

        return messageBody.GetBytes();
    }


    class TypeToSerialize
    {
        public required string StringValue { get; set; }
        public required int IntValue { get; set; }
        public required Guid GuidValue { get; set; }
        public required DateTime DateTimeValue { get; set; }
        public required byte[] ByteArrayValue { get; set; }
    }
}

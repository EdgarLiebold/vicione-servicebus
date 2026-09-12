using System;
using BenchmarkDotNet.Attributes;
using ViciOne.ServiceBus.Context;
using ViciOne.ServiceBus.Serialization;
using ViciOne.ServiceBus.Transports;

namespace ViciOne.ServiceBus.BenchmarkConsole;

[MemoryDiagnoser]
public class JsonSerializationBenchmark
{
    readonly MessageSendContext<AverageMessage> _averageMessageSendContext;
    readonly SystemTextJsonMessageSerializer _serializer;

    public JsonSerializationBenchmark()
    {
        var options = SystemTextJsonSerializerOptions.CreateDefault();
        options.MakeReadOnly();
        _serializer = new SystemTextJsonMessageSerializer(options);

        _averageMessageSendContext = new MessageSendContext<AverageMessage>(new AverageMessage
        {
            CorrelationId = NewId.NextGuid(),
            Name = "Franklin",
            SomeValue = 27,
            SomeOptionalValue = 42,
            Created = DateTime.UtcNow,
            Amount = 123.45m
        })
        {
            DestinationAddress = new Uri("loopback://localhost/input-queue"),
            SourceAddress = new Uri("loopback://localhost/request-client-queue"),
            CorrelationId = NewId.NextGuid(),
            ConversationId = NewId.NextGuid(),
        };

        _averageMessageSendContext.Headers.Set("VSB-Activity-Id", NewId.NextGuid().ToString());
    }

    [Benchmark(Description = "System.Text.Json byte[]")]
    public byte[] SystemTextJson()
    {
        return _serializer.GetMessageBody(_averageMessageSendContext).ToArray();
    }

    [Benchmark(Description = "System.Text.Json string")]
    public string SystemTextJsonString()
    {
        return _serializer.GetMessageBody(_averageMessageSendContext).GetRequiredTransportText();
    }
}


public class AverageMessage
{
    public Guid CorrelationId { get; set; }
    public string Name { get; set; }
    public int SomeValue { get; set; }
    public int? SomeOptionalValue { get; set; }
    public DateTime Created { get; set; }
    public DateTime? Completed { get; set; }
    public decimal Amount { get; set; }
}

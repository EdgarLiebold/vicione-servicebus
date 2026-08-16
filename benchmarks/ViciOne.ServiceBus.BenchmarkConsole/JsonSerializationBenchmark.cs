namespace ViciOne.ServiceBus.BenchmarkConsole
{
    using System;
    using BenchmarkDotNet.Attributes;
    using Context;
    using Serialization;


    [MemoryDiagnoser]
    public class JsonSerializationBenchmark
    {
        readonly MessageSendContext<AverageMessage> _averageMessageSendContext;

        public JsonSerializationBenchmark()
        {
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
            return SystemTextJsonMessageSerializer.Instance.GetMessageBody(_averageMessageSendContext).GetBytes();
        }

        [Benchmark(Description = "System.Text.Json string")]
        public string SystemTextJsonString()
        {
            return SystemTextJsonMessageSerializer.Instance.GetMessageBody(_averageMessageSendContext).GetString();
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
}

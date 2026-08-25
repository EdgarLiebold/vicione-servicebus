#nullable enable
namespace ViciOne.ServiceBus.Monitoring;

/// <summary>
/// Stable OpenTelemetry identities emitted by ViciOne.ServiceBus through the built-in .NET
/// <c>ActivitySource</c> and <c>Meter</c> APIs.
/// </summary>
public static class ServiceBusTelemetry
{
    public const string Name = "ViciOne.ServiceBus";
    public const string ActivitySourceName = Name;
    public const string MeterName = Name;

    public static class Metrics
    {
        public const string SentMessages = "messaging.client.sent.messages";
        public const string ConsumedMessages = "messaging.client.consumed.messages";
        public const string ClientOperationDuration = "messaging.client.operation.duration";
        public const string ProcessDuration = "messaging.process.duration";
        public const string ActiveOperations = "vicione.servicebus.messaging.operations.active";
        public const string RetryAttempts = "vicione.servicebus.messaging.retry.attempts";
        public const string DeliveryDuration = "vicione.servicebus.messaging.delivery.duration";
        public const string OutboxMessages = "vicione.servicebus.outbox.messages";
    }

    public static class Attributes
    {
        public const string MessagingSystem = "messaging.system";
        public const string OperationName = "messaging.operation.name";
        public const string OperationType = "messaging.operation.type";
        public const string ErrorType = "error.type";
        public const string ProcessorKind = "vicione.servicebus.processor.kind";
        public const string OutboxOperation = "vicione.servicebus.outbox.operation";
        public const string Outcome = "vicione.servicebus.outcome";
    }

    public static class MessagingSystems
    {
        public const string ActiveMq = "activemq";
        public const string AmazonSqs = "aws_sqs";
        public const string AzureEventHubs = "eventhubs";
        public const string AzureServiceBus = "servicebus";
        public const string InMemory = "in-memory";
        public const string RabbitMq = "rabbitmq";
        public const string Sql = "sql";
        public const string Other = "other";
        public const string Unknown = "unknown";
    }
}

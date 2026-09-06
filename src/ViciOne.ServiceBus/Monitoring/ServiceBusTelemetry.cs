namespace ViciOne.ServiceBus.Monitoring;

/// <summary>
/// Stable OpenTelemetry identities emitted by ViciOne.ServiceBus through the built-in .NET
/// <c>ActivitySource</c> and <c>Meter</c> APIs.
/// </summary>
public static class ServiceBusTelemetry
{
    /// <summary>
    /// Defines the name value.
    /// </summary>
    public const string Name = "ViciOne.ServiceBus";
    /// <summary>
    /// Defines the activity source name value.
    /// </summary>
    public const string ActivitySourceName = Name;
    /// <summary>
    /// Defines the meter name value.
    /// </summary>
    public const string MeterName = Name;

    /// <summary>
    /// Provides a metrics implementation.
    /// </summary>
    public static class Metrics
    {
        /// <summary>
        /// Defines the sent messages value.
        /// </summary>
        public const string SentMessages = "messaging.client.sent.messages";
        /// <summary>
        /// Defines the consumed messages value.
        /// </summary>
        public const string ConsumedMessages = "messaging.client.consumed.messages";
        /// <summary>
        /// Defines the client operation duration value.
        /// </summary>
        public const string ClientOperationDuration = "messaging.client.operation.duration";
        /// <summary>
        /// Defines the process duration value.
        /// </summary>
        public const string ProcessDuration = "messaging.process.duration";
        /// <summary>
        /// Defines the active operations value.
        /// </summary>
        public const string ActiveOperations = "vicione.servicebus.messaging.operations.active";
        /// <summary>
        /// Defines the retry attempts value.
        /// </summary>
        public const string RetryAttempts = "vicione.servicebus.messaging.retry.attempts";
        /// <summary>
        /// Defines the delivery duration value.
        /// </summary>
        public const string DeliveryDuration = "vicione.servicebus.messaging.delivery.duration";
        /// <summary>
        /// Defines the outbox messages value.
        /// </summary>
        public const string OutboxMessages = "vicione.servicebus.outbox.messages";
    }

    /// <summary>
    /// Provides an attributes implementation.
    /// </summary>
    public static class Attributes
    {
        /// <summary>
        /// Defines the messaging system value.
        /// </summary>
        public const string MessagingSystem = "messaging.system";
        /// <summary>
        /// Defines the operation name value.
        /// </summary>
        public const string OperationName = "messaging.operation.name";
        /// <summary>
        /// Defines the operation type value.
        /// </summary>
        public const string OperationType = "messaging.operation.type";
        /// <summary>
        /// Defines the error type value.
        /// </summary>
        public const string ErrorType = "error.type";
        /// <summary>
        /// Defines the processor kind value.
        /// </summary>
        public const string ProcessorKind = "vicione.servicebus.processor.kind";
        /// <summary>
        /// Defines the outbox operation value.
        /// </summary>
        public const string OutboxOperation = "vicione.servicebus.outbox.operation";
        /// <summary>
        /// Defines the outcome value.
        /// </summary>
        public const string Outcome = "vicione.servicebus.outcome";
    }

    /// <summary>
    /// Provides a messaging systems implementation.
    /// </summary>
    public static class MessagingSystems
    {
        /// <summary>
        /// Defines the active mq value.
        /// </summary>
        public const string ActiveMq = "activemq";
        /// <summary>
        /// Defines the amazon sqs value.
        /// </summary>
        public const string AmazonSqs = "aws_sqs";
        /// <summary>
        /// Defines the azure event hubs value.
        /// </summary>
        public const string AzureEventHubs = "eventhubs";
        /// <summary>
        /// Defines the azure service bus value.
        /// </summary>
        public const string AzureServiceBus = "servicebus";
        /// <summary>
        /// Defines the in memory value.
        /// </summary>
        public const string InMemory = "in-memory";
        /// <summary>
        /// Defines the rabbit mq value.
        /// </summary>
        public const string RabbitMq = "rabbitmq";
        /// <summary>
        /// Defines the sql value.
        /// </summary>
        public const string Sql = "sql";
        /// <summary>
        /// Defines the other value.
        /// </summary>
        public const string Other = "other";
        /// <summary>
        /// Defines the unknown value.
        /// </summary>
        public const string Unknown = "unknown";
    }
}

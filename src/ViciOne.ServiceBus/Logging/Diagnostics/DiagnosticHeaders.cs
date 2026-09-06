namespace ViciOne.ServiceBus.Logging;

/// <summary>Defines headers for diagnostic.</summary>
public static class DiagnosticHeaders
{
    /// <summary>Exposes the default listener name used by the containing type.</summary>
    public const string DefaultListenerName = Monitoring.ServiceBusTelemetry.ActivitySourceName;

    /// <summary>Exposes the diagnostic id used by the containing type.</summary>
    public const string DiagnosticId = "Diagnostic-Id";
    /// <summary>Exposes the activity id used by the containing type.</summary>
    public const string ActivityId = MessageHeaders.Prefix + "Activity-Id";
    /// <summary>Exposes the activity correlation context used by the containing type.</summary>
    public const string ActivityCorrelationContext = MessageHeaders.Prefix + "Activity-Correlation-Context";
    /// <summary>Exposes the activity trace state used by the containing type.</summary>
    public const string ActivityTraceState = MessageHeaders.Prefix + "Activity-Trace-State";
    /// <summary>Exposes the activity propagation used by the containing type.</summary>
    public const string ActivityPropagation = MessageHeaders.Prefix + "Activity-Propagation";

    /// <summary>Exposes the message id used by the containing type.</summary>
    public const string MessageId = "messaging.vicione-servicebus.message_id";
    /// <summary>Exposes the correlation id used by the containing type.</summary>
    public const string CorrelationId = "messaging.vicione-servicebus.correlation_id";
    /// <summary>Exposes the initiator id used by the containing type.</summary>
    public const string InitiatorId = "messaging.vicione-servicebus.initiator_id";
    /// <summary>Exposes the request id used by the containing type.</summary>
    public const string RequestId = "messaging.vicione-servicebus.request_id";
    /// <summary>Exposes the source address used by the containing type.</summary>
    public const string SourceAddress = "messaging.vicione-servicebus.source_address";
    /// <summary>Exposes the destination address used by the containing type.</summary>
    public const string DestinationAddress = "messaging.vicione-servicebus.destination_address";
    /// <summary>Exposes the input address used by the containing type.</summary>
    public const string InputAddress = "messaging.vicione-servicebus.input_address";
    /// <summary>Exposes the tracking number used by the containing type.</summary>
    public const string TrackingNumber = "messaging.vicione-servicebus.tracking_number";

    /// <summary>Exposes the message types used by the containing type.</summary>
    public const string MessageTypes = "messaging.vicione-servicebus.message_types";

    /// <summary>Exposes the consumer type used by the containing type.</summary>
    public const string ConsumerType = "messaging.vicione-servicebus.consumer_type";

    /// <summary>Exposes the peer address used by the containing type.</summary>
    public const string PeerAddress = "peer.address";

    /// <summary>Exposes the begin state used by the containing type.</summary>
    public const string BeginState = "messaging.vicione-servicebus.begin_state";
    /// <summary>Exposes the end state used by the containing type.</summary>
    public const string EndState = "messaging.vicione-servicebus.end_state";
    /// <summary>Exposes the saga id used by the containing type.</summary>
    public const string SagaId = "messaging.vicione-servicebus.saga_id";


    /// <summary>Defines diagnostic header names that describe an exception.</summary>
    public class Exceptions
    {
        /// <summary>Exposes the event name used by the containing type.</summary>
        public const string EventName = "exception";
        /// <summary>Exposes the type used by the containing type.</summary>
        public const string Type = "exception.type";
        /// <summary>Exposes the message used by the containing type.</summary>
        public const string Message = "exception.message";
        /// <summary>Exposes the escaped used by the containing type.</summary>
        public const string Escaped = "exception.escaped";
        /// <summary>Exposes the stacktrace used by the containing type.</summary>
        public const string Stacktrace = "exception.stacktrace";
    }


    /// <summary>Defines diagnostic header names that describe a messaging operation.</summary>
    public static class Messaging
    {
        /// <summary>Exposes the body length used by the containing type.</summary>
        public const string BodyLength = "messaging.message.body.size";
        /// <summary>Exposes the conversation id used by the containing type.</summary>
        public const string ConversationId = "messaging.message.conversation_id";
        /// <summary>Exposes the destination name used by the containing type.</summary>
        public const string DestinationName = "messaging.destination.name";
        /// <summary>Exposes the transport message id used by the containing type.</summary>
        public const string TransportMessageId = "messaging.message.id";
        /// <summary>Exposes the operation used by the containing type.</summary>
        public const string Operation = "messaging.operation";
        /// <summary>Exposes the system used by the containing type.</summary>
        public const string System = "messaging.system";


        /// <summary>Defines RabbitMQ-specific diagnostic header names.</summary>
        public static class RabbitMq
        {
            /// <summary>Exposes the routing key used by the containing type.</summary>
            public const string RoutingKey = "messaging.rabbitmq.destination.routing_key";
        }
    }
}

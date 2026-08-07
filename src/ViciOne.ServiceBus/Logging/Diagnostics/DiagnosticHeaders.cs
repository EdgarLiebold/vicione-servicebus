// ViciOne modification: WP-F2-SERVICEBUS-IDENTITY, 2026-08-07.
#nullable enable
namespace ViciOne.ServiceBus.Logging
{
    public static class DiagnosticHeaders
    {
        public const string DefaultListenerName = "ViciOne.ServiceBus";

        public const string DiagnosticId = "Diagnostic-Id";
        public const string ActivityId = "ViciOne-ServiceBus-Activity-Id";
        public const string ActivityCorrelationContext = "ViciOne-ServiceBus-Activity-Correlation-Context";
        public const string ActivityPropagation = "ViciOne-ServiceBus-Activity-Propagation";

        public const string MessageId = "messaging.vicione-servicebus.message_id";
        public const string CorrelationId = "messaging.vicione-servicebus.correlation_id";
        public const string InitiatorId = "messaging.vicione-servicebus.initiator_id";
        public const string RequestId = "messaging.vicione-servicebus.request_id";
        public const string SourceAddress = "messaging.vicione-servicebus.source_address";
        public const string DestinationAddress = "messaging.vicione-servicebus.destination_address";
        public const string InputAddress = "messaging.vicione-servicebus.input_address";
        public const string TrackingNumber = "messaging.vicione-servicebus.tracking_number";

        public const string MessageTypes = "messaging.vicione-servicebus.message_types";

        public const string ConsumerType = "messaging.vicione-servicebus.consumer_type";

        public const string PeerAddress = "peer.address";

        public const string BeginState = "messaging.vicione-servicebus.begin_state";
        public const string EndState = "messaging.vicione-servicebus.end_state";
        public const string SagaId = "messaging.vicione-servicebus.saga_id";


        public class Exceptions
        {
            public const string EventName = "exception";
            public const string Type = "exception.type";
            public const string Message = "exception.message";
            public const string Escaped = "exception.escaped";
            public const string Stacktrace = "exception.stacktrace";
        }


        public static class Messaging
        {
            public const string BodyLength = "messaging.message.body.size";
            public const string ConversationId = "messaging.message.conversation_id";
            public const string DestinationName = "messaging.destination.name";
            public const string TransportMessageId = "messaging.message.id";
            public const string Operation = "messaging.operation";
            public const string System = "messaging.system";


            public static class RabbitMq
            {
                public const string RoutingKey = "messaging.rabbitmq.destination.routing_key";
            }
        }
    }
}

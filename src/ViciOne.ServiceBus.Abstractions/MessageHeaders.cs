namespace ViciOne.ServiceBus;

/// <summary>Defines the canonical wire names of Service Bus message headers.</summary>
public static class MessageHeaders
{
    /// <summary>The prefix applied to Service Bus-owned transport headers.</summary>
    public const string Prefix = "VSB-";

    /// <summary>The prefix shared by fault-detail headers.</summary>
    public const string FaultPrefix = Prefix + "Fault-";

    /// <summary>The reason associated with a message action.</summary>
    public const string Reason = Prefix + "Reason";

    /// <summary>The assembly-qualified type of the reported fault exception.</summary>
    public const string FaultExceptionType = FaultPrefix + "ExceptionType";

    /// <summary>The input address of the endpoint where the fault occurred.</summary>
    public const string FaultInputAddress = FaultPrefix + "InputAddress";

    /// <summary>The message of the reported fault exception.</summary>
    public const string FaultMessage = FaultPrefix + "Message";

    /// <summary>The message contract type associated with the fault.</summary>
    public const string FaultMessageType = FaultPrefix + "MessageType";

    /// <summary>The consumer type that reported the fault.</summary>
    public const string FaultConsumerType = FaultPrefix + "ConsumerType";

    /// <summary>The timestamp at which the fault occurred.</summary>
    public const string FaultTimestamp = FaultPrefix + "Timestamp";

    /// <summary>The stack trace of the reported fault exception.</summary>
    public const string FaultStackTrace = FaultPrefix + "StackTrace";

    /// <summary>The number of retry attempts performed before the fault.</summary>
    public const string FaultRetryCount = FaultPrefix + "RetryCount";

    /// <summary>The number of redeliveries performed before the fault.</summary>
    public const string FaultRedeliveryCount = FaultPrefix + "RedeliveryCount";

    /// <summary>The address of the endpoint that forwarded the message.</summary>
    public const string ForwarderAddress = Prefix + "Forwarder-Address";

    /// <summary>The scheduler token that identifies a scheduled message.</summary>
    public const string SchedulingTokenId = Prefix + "Scheduling-TokenId";

    /// <summary>The number of completed message redeliveries.</summary>
    public const string RedeliveryCount = Prefix + "Redelivery-Count";

    /// <summary>The Quartz trigger key that delivered the scheduled message.</summary>
    public const string QuartzTriggerKey = Prefix + "Quartz-TriggerKey";

    /// <summary>The identifier of the client that sent a request.</summary>
    public const string ClientId = Prefix + "Request-ClientId";

    /// <summary>The identifier of the endpoint that handled a request.</summary>
    public const string EndpointId = Prefix + "Request-EndpointId";

    /// <summary>The identifier assigned when this message started a conversation.</summary>
    public const string InitiatingConversationId = Prefix + "InitiatingConversationId";

    /// <summary>The envelope message identifier.</summary>
    public const string MessageId = "MessageId";

    /// <summary>The envelope correlation identifier.</summary>
    public const string CorrelationId = "CorrelationId";

    /// <summary>The envelope conversation identifier.</summary>
    public const string ConversationId = "ConversationId";

    /// <summary>The envelope request identifier.</summary>
    public const string RequestId = "RequestId";

    /// <summary>The identifier of the message that initiated the current conversation.</summary>
    public const string InitiatorId = Prefix + "InitiatorId";

    /// <summary>The address from which the message was sent.</summary>
    public const string SourceAddress = Prefix + "Source-Address";

    /// <summary>The destination to which a response should be sent.</summary>
    public const string ResponseAddress = Prefix + "Response-Address";

    /// <summary>The destination to which a fault should be sent.</summary>
    public const string FaultAddress = Prefix + "Fault-Address";

    /// <summary>The message contract identifiers carried by the envelope.</summary>
    public const string MessageType = Prefix + "MessageType";

    /// <summary>The transport-native message identifier represented as text.</summary>
    public const string TransportMessageId = "TransportMessageId";

    /// <summary>The send timestamp supplied by the transport, when available.</summary>
    public const string TransportSentTime = "TransportSentTime";

    /// <summary>The message identifier that preceded a redelivery or scheduled delivery.</summary>
    public const string OriginalMessageId = Prefix + "OriginalMessageId";

    /// <summary>The MIME content type of the transport body.</summary>
    public const string ContentType = "Content-Type";

    /// <summary>The future identifier stored in routing-slip variables.</summary>
    public const string FutureId = "FutureId";

    /// <summary>Defines message header names that describe the producing host.</summary>
    public static class Host
    {
        /// <summary>The prefix shared by producing-host headers.</summary>
        public const string Prefix = MessageHeaders.Prefix + "Host-";

        /// <summary>The serialized producing-host information.</summary>
        public const string Info = Prefix + "Info";

        /// <summary>The producing machine name.</summary>
        public const string MachineName = Prefix + "MachineName";

        /// <summary>The producing process name.</summary>
        public const string ProcessName = Prefix + "ProcessName";

        /// <summary>The producing process identifier.</summary>
        public const string ProcessId = Prefix + "ProcessId";

        /// <summary>The producing assembly name.</summary>
        public const string Assembly = Prefix + "Assembly";

        /// <summary>The producing assembly version.</summary>
        public const string AssemblyVersion = Prefix + "AssemblyVersion";

        /// <summary>The Service Bus version used by the producer.</summary>
        public const string ViciOneServiceBusVersion = Prefix + "ViciOneServiceBusVersion";

        /// <summary>The .NET runtime version used by the producer.</summary>
        public const string FrameworkVersion = Prefix + "FrameworkVersion";

        /// <summary>The operating-system version of the producing host.</summary>
        public const string OperatingSystemVersion = Prefix + "OperatingSystemVersion";
    }

    /// <summary>Defines message header names used by request-response conversations.</summary>
    public static class Request
    {
        /// <summary>The response contract identifiers accepted by a request client.</summary>
        public const string Accept = MessageHeaders.Prefix + "Request-AcceptType";

        /// <summary>The retry count maintained by a routing-slip request proxy.</summary>
        public const string RoutingSlipRetryCount = MessageHeaders.Prefix + "RoutingSlip-RetryCount";
    }

    /// <summary>Defines message header names used by Quartz scheduling.</summary>
    public static class Quartz
    {
        /// <summary>The timestamp at which Quartz scheduled the message.</summary>
        public const string Scheduled = MessageHeaders.Prefix + "Quartz-Scheduled";

        /// <summary>The timestamp at which Quartz fired the message trigger.</summary>
        public const string Sent = MessageHeaders.Prefix + "Quartz-Sent";

        /// <summary>The next scheduled delivery timestamp.</summary>
        public const string NextScheduled = MessageHeaders.Prefix + "Quartz-NextScheduled";

        /// <summary>The previous delivery timestamp.</summary>
        public const string PreviousSent = MessageHeaders.Prefix + "Quartz-PreviousSent";

        /// <summary>The Quartz schedule identifier.</summary>
        public const string ScheduleId = MessageHeaders.Prefix + "Quartz-ScheduleId";

        /// <summary>The Quartz schedule group.</summary>
        public const string ScheduleGroup = MessageHeaders.Prefix + "Quartz-ScheduleGroup";
    }
}

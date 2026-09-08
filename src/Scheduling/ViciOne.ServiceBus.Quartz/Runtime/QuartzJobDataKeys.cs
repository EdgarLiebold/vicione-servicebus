namespace ViciOne.ServiceBus.Quartz.Runtime;

internal static class QuartzJobDataKeys
{
    public const string Body = "Body";
    public const string ContentType = "ContentType";
    public const string DestinationAddress = "DestinationAddress";
    public const string MessageTypes = "MessageTypes";
    public const string MessageId = "MessageId";
    public const string MessageIdSeed = "MessageIdSeed";
    public const string CorrelationId = "CorrelationId";
    public const string ConversationId = "ConversationId";
    public const string InitiatorId = "InitiatorId";
    public const string RequestId = "RequestId";
    public const string SourceAddress = "SourceAddress";
    public const string ResponseAddress = "ResponseAddress";
    public const string FaultAddress = "FaultAddress";
    public const string ExpirationTime = "ExpirationTime";
    public const string SchedulingTokenId = "SchedulingTokenId";
    public const string ScheduleId = "ScheduleId";
    public const string ScheduleGroup = "ScheduleGroup";
    public const string Headers = "Headers";
    public const string TransportProperties = "TransportProperties";
}

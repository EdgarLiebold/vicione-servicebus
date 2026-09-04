namespace ViciOne.ServiceBus.RabbitMq;

internal static class RabbitMqAddressOptionNames
{
    public const string AlternateExchange = "alternateexchange";
    public const string AutoDelete = "autodelete";
    public const string BindExchange = "bindexchange";
    public const string BindQueue = "bind";
    public const string DelayedType = "delayedtype";
    public const string Durable = "durable";
    public const string ExchangeType = "type";
    public const string Heartbeat = "heartbeat";
    public const string Prefetch = "prefetch";
    public const string QueueName = "queue";
    public const string SingleActiveConsumer = "singleactiveconsumer";
    public const string Temporary = "temporary";
    public const string TimeToLive = "ttl";

    public static bool IsEndpointOption(string key) => key is
        AlternateExchange or AutoDelete or BindExchange or BindQueue or DelayedType or Durable or ExchangeType
        or QueueName or SingleActiveConsumer or Temporary;
}

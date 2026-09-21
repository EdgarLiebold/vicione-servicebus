using System.Collections.Generic;

namespace ViciOne.ServiceBus.RabbitMq;

internal static class RabbitMqAddressOptionNames
{
    static readonly HashSet<string> _endpointOptions =
    [
        AlternateExchange,
        AutoDelete,
        BindExchange,
        BindQueue,
        DelayedType,
        Durable,
        ExchangeType,
        QueueName,
        SingleActiveConsumer,
        Temporary,
    ];

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

    public static bool IsEndpointOption(string key) => _endpointOptions.Contains(key);
}

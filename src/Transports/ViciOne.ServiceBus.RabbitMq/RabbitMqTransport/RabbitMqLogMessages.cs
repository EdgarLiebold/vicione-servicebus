using Microsoft.Extensions.Logging;
using ViciOne.ServiceBus.Logging;
using ViciOne.ServiceBus.RabbitMq.Topology;

namespace ViciOne.ServiceBus.RabbitMq;

/// <summary>Defines strongly typed log messages emitted by the RabbitMQ transport.</summary>
public static class RabbitMqLogMessages
{
    /// <summary>Logs an exchange-to-exchange binding.</summary>
    public static readonly LogMessage<ExchangeToExchangeBinding> BindToExchange = LogContext.Define<ExchangeToExchangeBinding>(LogLevel.Debug,
        "Bind exchange: {ExchangeBinding}");

    /// <summary>Logs an exchange-to-queue binding.</summary>
    public static readonly LogMessage<ExchangeToQueueBinding> BindToQueue = LogContext.Define<ExchangeToQueueBinding>(LogLevel.Debug,
        "Bind queue: {QueueBinding}");

    /// <summary>Logs an exchange declaration.</summary>
    public static readonly LogMessage<Exchange> DeclareExchange = LogContext.Define<Exchange>(LogLevel.Debug,
        "Declare exchange: {Exchange}");

    /// <summary>Logs a queue declaration and its current broker counts.</summary>
    public static readonly LogMessage<Queue, uint, uint> DeclareQueue = LogContext.Define<Queue, uint, uint>(LogLevel.Debug,
        "Declare queue: {Queue}, consumer-count: {ConsumerCount} message-count: {MessageCount}");

    /// <summary>Logs a consumer prefetch update.</summary>
    public static readonly LogMessage<ushort> PrefetchCount = LogContext.Define<ushort>(LogLevel.Debug,
        "Set Prefetch Count: {PrefetchCount}");
}

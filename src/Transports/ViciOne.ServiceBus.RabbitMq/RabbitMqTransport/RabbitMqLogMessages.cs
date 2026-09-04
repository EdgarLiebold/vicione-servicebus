using Microsoft.Extensions.Logging;
using ViciOne.ServiceBus.Logging;
using ViciOne.ServiceBus.RabbitMq.Topology;

namespace ViciOne.ServiceBus.RabbitMq;

/// <summary>
/// Provides a rabbit mq log messages implementation.
/// </summary>
public static class RabbitMqLogMessages
{
    /// <summary>
    /// Defines the bind to exchange value.
    /// </summary>
    public static readonly LogMessage<ExchangeToExchangeBinding> BindToExchange = LogContext.Define<ExchangeToExchangeBinding>(LogLevel.Debug,
        "Bind exchange: {ExchangeBinding}");

    /// <summary>
    /// Defines the bind to queue value.
    /// </summary>
    public static readonly LogMessage<ExchangeToQueueBinding> BindToQueue = LogContext.Define<ExchangeToQueueBinding>(LogLevel.Debug,
        "Bind queue: {QueueBinding}");

    /// <summary>
    /// Defines the declare exchange value.
    /// </summary>
    public static readonly LogMessage<Exchange> DeclareExchange = LogContext.Define<Exchange>(LogLevel.Debug,
        "Declare exchange: {Exchange}");

    /// <summary>
    /// Defines the declare queue value.
    /// </summary>
    public static readonly LogMessage<Queue, uint, uint> DeclareQueue = LogContext.Define<Queue, uint, uint>(LogLevel.Debug,
        "Declare queue: {Queue}, consumer-count: {ConsumerCount} message-count: {MessageCount}");

    /// <summary>
    /// Defines the prefetch count value.
    /// </summary>
    public static readonly LogMessage<ushort> PrefetchCount = LogContext.Define<ushort>(LogLevel.Debug,
        "Set Prefetch Count: {PrefetchCount}");
}

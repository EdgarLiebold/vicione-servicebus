using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using RabbitMQ.Client;
using ViciOne.ServiceBus.RabbitMq.Configuration;
using ViciOne.ServiceBus.RabbitMq.Topology;

#nullable enable
namespace ViciOne.ServiceBus.RabbitMq;
/// <summary>
/// Immutable RabbitMQ-native technical redelivery topology for one receive queue.
/// </summary>
public sealed class RabbitMqQueueRedeliveryPlan
{
    const string QueueTypeArgument = "x-queue-type";
    const string QuorumInitialGroupSizeArgument = "x-quorum-initial-group-size";
    const string MessageTtlArgument = "x-message-ttl";
    const string DeadLetterExchangeArgument = "x-dead-letter-exchange";
    const string DeadLetterRoutingKeyArgument = "x-dead-letter-routing-key";

    readonly IReadOnlyDictionary<long, string> _routingKeys;
    readonly IReadOnlyDictionary<string, object?> _sourceQueueArguments;

    /// <summary>
    /// Initializes a new instance of the containing type.
    /// </summary>
    /// <param name="settings">The settings value.</param>
    /// <param name="intervals">The intervals value.</param>
    public RabbitMqQueueRedeliveryPlan(RabbitMqReceiveSettings settings, IEnumerable<TimeSpan> intervals)
    {
        ArgumentNullException.ThrowIfNull(settings);
        ArgumentNullException.ThrowIfNull(intervals);

        if (string.IsNullOrWhiteSpace(settings.QueueName))
            throw new ConfigurationException("RabbitMQ queue redelivery requires a named receive queue.");
        if (!settings.BindQueue)
            throw new ConfigurationException("RabbitMQ queue redelivery requires a receive queue; exchange-only endpoints are not supported.");

        RabbitMqEntityNameValidator.Validator.ThrowIfInvalidEntityName(settings.QueueName);

        QueueName = settings.QueueName;
        Durable = settings.Durable;
        AutoDelete = settings.AutoDelete;
        _sourceQueueArguments = new Dictionary<string, object?>(settings.QueueArguments, StringComparer.Ordinal);

        if (_sourceQueueArguments.TryGetValue(QueueTypeArgument, out var queueType)
            && string.Equals(Convert.ToString(queueType, CultureInfo.InvariantCulture), "stream", StringComparison.OrdinalIgnoreCase))
            throw new ConfigurationException("RabbitMQ stream queues do not support TTL/DLX technical redelivery.");

        var delayValues = intervals.Select(ValidateInterval).Distinct().OrderBy(value => value).ToArray();
        if (delayValues.Length == 0)
            throw new ConfigurationException("RabbitMQ queue redelivery requires at least one positive interval.");

        DelayExchangeName = $"{QueueName}.redelivery";
        ReturnExchangeName = $"{QueueName}.redelivery.return";
        RabbitMqEntityNameValidator.Validator.ThrowIfInvalidEntityName(DelayExchangeName);
        RabbitMqEntityNameValidator.Validator.ThrowIfInvalidEntityName(ReturnExchangeName);
        foreach (var milliseconds in delayValues)
            RabbitMqEntityNameValidator.Validator.ThrowIfInvalidEntityName($"{QueueName}.redelivery.{milliseconds}");

        _routingKeys = delayValues.ToDictionary(
            milliseconds => milliseconds,
            milliseconds => milliseconds.ToString(CultureInfo.InvariantCulture));
        Intervals = delayValues.Select(milliseconds => TimeSpan.FromMilliseconds(milliseconds)).ToArray();
    }

    /// <summary>
    /// Gets the queue name value.
    /// </summary>
    public string QueueName { get; }
    /// <summary>
    /// Gets the delay exchange name value.
    /// </summary>
    public string DelayExchangeName { get; }
    /// <summary>
    /// Gets the return exchange name value.
    /// </summary>
    public string ReturnExchangeName { get; }
    /// <summary>
    /// Gets the durable value.
    /// </summary>
    public bool Durable { get; }
    /// <summary>
    /// Gets the auto delete value.
    /// </summary>
    public bool AutoDelete { get; }
    /// <summary>
    /// Gets the intervals value.
    /// </summary>
    public IReadOnlyList<TimeSpan> Intervals { get; }

    /// <summary>
    /// Gets routing key.
    /// </summary>
    /// <param name="delay">The delay value.</param>
    /// <returns>The result of the operation.</returns>
    public string GetRoutingKey(TimeSpan delay)
    {
        var milliseconds = ValidateInterval(delay);
        if (!_routingKeys.TryGetValue(milliseconds, out var routingKey))
        {
            throw new ConfigurationException(
                $"RabbitMQ queue redelivery delay '{delay}' was not declared at endpoint startup. Configure every technical-redelivery interval explicitly.");
        }

        return routingKey;
    }

    /// <summary>
    /// Performs the configure operation.
    /// </summary>
    /// <param name="context">The operation context.</param>
    /// <param name="cancellationToken">The token used to cancel the operation.</param>
    /// <returns>The result of the operation.</returns>
    public async Task ConfigureAsync(ChannelContext context, CancellationToken cancellationToken)
    {
        ArgumentNullException.ThrowIfNull(context);

        var cache = context.ConnectionContext.TopologyEntityCache;
        var exchange = new RedeliveryExchange(DelayExchangeName, Durable, AutoDelete);
        var returnExchange = new RedeliveryExchange(ReturnExchangeName, Durable, AutoDelete);

        await cache.DeclareExchangeAsync(exchange,
            token => context.ExchangeDeclareAsync(exchange.ExchangeName, ExchangeType.Direct, exchange.Durable, exchange.AutoDelete,
                exchange.ExchangeArguments, token), cancellationToken).ConfigureAwait(false);
        await cache.DeclareExchangeAsync(returnExchange,
            token => context.ExchangeDeclareAsync(returnExchange.ExchangeName, ExchangeType.Direct, returnExchange.Durable, returnExchange.AutoDelete,
                returnExchange.ExchangeArguments, token), cancellationToken).ConfigureAwait(false);

        var sourceQueue = new RedeliveryQueue(
            QueueName,
            Durable,
            false,
            AutoDelete,
            new Dictionary<string, object?>(_sourceQueueArguments, StringComparer.Ordinal));
        var returnBinding = new ReturnBinding(returnExchange, sourceQueue, QueueName);
        await cache.BindAsync(returnBinding,
            token => context.QueueBindAsync(QueueName, ReturnExchangeName, QueueName, returnBinding.Arguments, token), cancellationToken)
            .ConfigureAwait(false);

        foreach (var pair in _routingKeys)
        {
            var queueName = $"{QueueName}.redelivery.{pair.Key}";
            var queue = new RedeliveryQueue(queueName, Durable, false, AutoDelete, CreateDelayQueueArguments(pair.Key));
            await cache.DeclareQueueAsync(queue, async token =>
            {
                await context.QueueDeclareAsync(queue.QueueName, queue.Durable, queue.Exclusive, queue.AutoDelete, queue.QueueArguments, token)
                    .ConfigureAwait(false);
            }, cancellationToken).ConfigureAwait(false);

            var binding = new ReturnBinding(exchange, queue, pair.Value);
            await cache.BindAsync(binding,
                token => context.QueueBindAsync(queue.QueueName, DelayExchangeName, pair.Value, binding.Arguments, token), cancellationToken)
                .ConfigureAwait(false);
        }
    }

    Dictionary<string, object?> CreateDelayQueueArguments(long milliseconds)
    {
        var arguments = new Dictionary<string, object?>(StringComparer.Ordinal)
        {
            [MessageTtlArgument] = checked((int)milliseconds),
            [DeadLetterExchangeArgument] = ReturnExchangeName,
            [DeadLetterRoutingKeyArgument] = QueueName
        };

        if (_sourceQueueArguments.TryGetValue(QueueTypeArgument, out var queueType))
            arguments[QueueTypeArgument] = queueType;
        if (_sourceQueueArguments.TryGetValue(QuorumInitialGroupSizeArgument, out var groupSize))
            arguments[QuorumInitialGroupSizeArgument] = groupSize;

        return arguments;
    }

    static long ValidateInterval(TimeSpan interval)
    {
        if (interval <= TimeSpan.Zero)
            throw new ArgumentOutOfRangeException(nameof(interval), interval, "RabbitMQ technical-redelivery intervals must be positive.");
        if (interval.Ticks % TimeSpan.TicksPerMillisecond != 0)
        {
            throw new ArgumentOutOfRangeException(nameof(interval), interval,
                "RabbitMQ technical-redelivery intervals must resolve to a whole number of milliseconds.");
        }

        var milliseconds = interval.Ticks / TimeSpan.TicksPerMillisecond;
        if (milliseconds > int.MaxValue)
        {
            throw new ArgumentOutOfRangeException(nameof(interval), interval,
                $"RabbitMQ queue TTL is limited to {int.MaxValue} milliseconds for technical redelivery.");
        }

        return milliseconds;
    }


    sealed record RedeliveryExchange(string ExchangeName, bool Durable, bool AutoDelete) : Exchange
    {
        public string ExchangeType => RabbitMQ.Client.ExchangeType.Direct;
        public IDictionary<string, object?> ExchangeArguments { get; } = new Dictionary<string, object?>();
    }


    sealed record RedeliveryQueue(
        string QueueName,
        bool Durable,
        bool Exclusive,
        bool AutoDelete,
        IDictionary<string, object?> QueueArguments) : Queue;


    sealed record ReturnBinding(Exchange Source, Queue Destination, string RoutingKey) : ExchangeToQueueBinding
    {
        public IDictionary<string, object?> Arguments { get; } = new Dictionary<string, object?>();
    }
}

#nullable enable
namespace ViciOne.ServiceBus.RabbitMqTransport;

using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Configuration;
using RabbitMQ.Client;
using Topology;


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
    readonly IReadOnlyDictionary<string, object> _sourceQueueArguments;

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
        _sourceQueueArguments = new Dictionary<string, object>(settings.QueueArguments, StringComparer.Ordinal);

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

    public string QueueName { get; }
    public string DelayExchangeName { get; }
    public string ReturnExchangeName { get; }
    public bool Durable { get; }
    public bool AutoDelete { get; }
    public IReadOnlyList<TimeSpan> Intervals { get; }

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

    public async Task Configure(ChannelContext context, CancellationToken cancellationToken)
    {
        ArgumentNullException.ThrowIfNull(context);

        var cache = context.ConnectionContext.TopologyEntityCache;
        var exchange = new RedeliveryExchange(DelayExchangeName, Durable, AutoDelete);
        var returnExchange = new RedeliveryExchange(ReturnExchangeName, Durable, AutoDelete);

        await cache.DeclareExchange(exchange,
            token => context.ExchangeDeclare(exchange.ExchangeName, ExchangeType.Direct, exchange.Durable, exchange.AutoDelete,
                exchange.ExchangeArguments, token), cancellationToken).ConfigureAwait(false);
        await cache.DeclareExchange(returnExchange,
            token => context.ExchangeDeclare(returnExchange.ExchangeName, ExchangeType.Direct, returnExchange.Durable, returnExchange.AutoDelete,
                returnExchange.ExchangeArguments, token), cancellationToken).ConfigureAwait(false);

        var sourceQueue = new RedeliveryQueue(
            QueueName,
            Durable,
            false,
            AutoDelete,
            new Dictionary<string, object>(_sourceQueueArguments, StringComparer.Ordinal));
        var returnBinding = new ReturnBinding(returnExchange, sourceQueue, QueueName);
        await cache.Bind(returnBinding,
            token => context.QueueBind(QueueName, ReturnExchangeName, QueueName, returnBinding.Arguments, token), cancellationToken)
            .ConfigureAwait(false);

        foreach (var pair in _routingKeys)
        {
            var queueName = $"{QueueName}.redelivery.{pair.Key}";
            var queue = new RedeliveryQueue(queueName, Durable, false, AutoDelete, CreateDelayQueueArguments(pair.Key));
            await cache.DeclareQueue(queue, async token =>
            {
                await context.QueueDeclare(queue.QueueName, queue.Durable, queue.Exclusive, queue.AutoDelete, queue.QueueArguments, token)
                    .ConfigureAwait(false);
            }, cancellationToken).ConfigureAwait(false);

            var binding = new ReturnBinding(exchange, queue, pair.Value);
            await cache.Bind(binding,
                token => context.QueueBind(queue.QueueName, DelayExchangeName, pair.Value, binding.Arguments, token), cancellationToken)
                .ConfigureAwait(false);
        }
    }

    Dictionary<string, object> CreateDelayQueueArguments(long milliseconds)
    {
        var arguments = new Dictionary<string, object>(StringComparer.Ordinal)
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
        public IDictionary<string, object> ExchangeArguments { get; } = new Dictionary<string, object>();
    }


    sealed record RedeliveryQueue(
        string QueueName,
        bool Durable,
        bool Exclusive,
        bool AutoDelete,
        IDictionary<string, object> QueueArguments) : Queue;


    sealed record ReturnBinding(Exchange Source, Queue Destination, string RoutingKey) : ExchangeToQueueBinding
    {
        public IDictionary<string, object> Arguments { get; } = new Dictionary<string, object>();
    }
}

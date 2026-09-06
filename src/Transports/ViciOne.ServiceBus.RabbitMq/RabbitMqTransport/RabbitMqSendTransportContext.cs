using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Globalization;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using RabbitMQ.Client;
using ViciOne.ServiceBus.Internals;
using ViciOne.ServiceBus.Logging;
using ViciOne.ServiceBus.RabbitMq.Configuration;
using ViciOne.ServiceBus.RabbitMq.Middleware;
using ViciOne.ServiceBus.Transports;

namespace ViciOne.ServiceBus.RabbitMq;

/// <summary>Creates RabbitMQ send contexts, maps AMQP properties, declares topology, and publishes messages.</summary>
public class RabbitMqSendTransportContext :
    BaseSendTransportContext,
    SendTransportContext<ChannelContext>
{
    readonly ConfigureRabbitMqTopologyFilter<SendSettings> _configureTopologyFilter;
    readonly IPipe<ChannelContext> _delayConfigureTopologyPipe;
    readonly string _delayExchange;
    readonly string _exchange;

    readonly IRabbitMqHostConfiguration _hostConfiguration;
    readonly IChannelContextSupervisor _supervisor;

    /// <summary>Creates a send context bound to destination and delayed-delivery topology.</summary>
    /// <param name="hostConfiguration">The owning RabbitMQ host configuration.</param>
    /// <param name="receiveEndpointContext">The endpoint serialization and observer context.</param>
    /// <param name="supervisor">The RabbitMQ channel supervisor.</param>
    /// <param name="configureTopologyFilter">The destination topology filter.</param>
    /// <param name="exchange">The destination exchange name.</param>
    /// <param name="delayConfigureTopologyPipe">The delayed-exchange topology pipeline.</param>
    /// <param name="delayExchange">The delayed-delivery exchange name.</param>
    public RabbitMqSendTransportContext(IRabbitMqHostConfiguration hostConfiguration, ReceiveEndpointContext receiveEndpointContext,
        IChannelContextSupervisor supervisor,
        ConfigureRabbitMqTopologyFilter<SendSettings> configureTopologyFilter, string exchange,
        IPipe<ChannelContext> delayConfigureTopologyPipe, string delayExchange)
        : base(hostConfiguration, receiveEndpointContext.Serialization)
    {
        _hostConfiguration = hostConfiguration;
        _supervisor = supervisor;

        _configureTopologyFilter = configureTopologyFilter;
        _exchange = exchange;

        _delayConfigureTopologyPipe = delayConfigureTopologyPipe;
        _delayExchange = delayExchange;
    }

    /// <summary>Gets the destination exchange name.</summary>
    public override string EntityName => _exchange;
    /// <summary>Gets the activity system.</summary>
    public override string ActivitySystem => "rabbitmq";

    /// <summary>Runs a channel send pipeline under the host's transient connection retry policy.</summary>
    /// <param name="pipe">The channel pipeline to execute.</param>
    /// <param name="cancellationToken">Cancellation for retry waits and channel execution.</param>
    /// <returns>A task that completes with the channel pipeline.</returns>
    public Task SendAsync(IPipe<ChannelContext> pipe, CancellationToken cancellationToken = default)
    {
        return _hostConfiguration.RetryAsync(() => _supervisor.SendAsync(pipe, cancellationToken),
            stoppingToken: _supervisor.SendStopping, cancellationToken: cancellationToken);
    }

    /// <summary>Delegates diagnostic probing to the channel supervisor.</summary>
    /// <param name="context">The probe context that receives channel-supervisor details.</param>
    public void Probe(ProbeContext context)
    {
        _supervisor.Probe(context);
    }

    /// <summary>Returns the channel supervisor as this transport's lifecycle dependency.</summary>
    /// <returns>The single supervised channel agent.</returns>
    public override IEnumerable<IAgent> GetAgentHandles()
    {
        return [_supervisor];
    }

    /// <summary>Creates and configures a typed RabbitMQ send context for an existing channel.</summary>
    /// <typeparam name="T">The message contract.</typeparam>
    /// <param name="context">The active RabbitMQ channel context.</param>
    /// <param name="message">The message being sent.</param>
    /// <param name="pipe">The send-context configuration pipeline.</param>
    /// <param name="cancellationToken">Cancellation for the send context.</param>
    /// <returns>The configured RabbitMQ send context.</returns>
    public async Task<SendContext<T>> CreateSendContextAsync<T>(ChannelContext context, T message, IPipe<SendContext<T>> pipe,
        CancellationToken cancellationToken)
        where T : class
    {
        var properties = new BasicProperties();

        var sendContext = new RabbitMqMessageSendContext<T>(properties, _exchange, message, cancellationToken);

        await pipe.SendAsync(sendContext).ConfigureAwait(false);

        CopyIncomingPropertiesIfPresent(sendContext);

        if (sendContext.Exchange.Equals(RabbitMqExchangeNames.ReplyTo) && string.IsNullOrWhiteSpace(sendContext.RoutingKey))
        {
            var destinationAddress = sendContext.DestinationAddress
                ?? throw new InvalidOperationException("The RabbitMQ send context does not have a destination address.");
            throw new TransportException(destinationAddress, "RoutingKey must be specified when sending to reply-to address");
        }

        return sendContext;
    }

    /// <summary>Creates and configures a typed RabbitMQ send context.</summary>
    /// <typeparam name="T">The message contract.</typeparam>
    /// <param name="message">The message being sent.</param>
    /// <param name="pipe">The send-context configuration pipeline.</param>
    /// <param name="cancellationToken">Cancellation for the send context.</param>
    /// <returns>The configured RabbitMQ send context.</returns>
    public override async Task<SendContext<T>> CreateSendContextAsync<T>(T message, IPipe<SendContext<T>> pipe, CancellationToken cancellationToken = default)
        where T : class
    {
        var properties = new BasicProperties();

        var sendContext = new RabbitMqMessageSendContext<T>(properties, _exchange, message, cancellationToken);

        await pipe.SendAsync(sendContext).ConfigureAwait(false);

        CopyIncomingPropertiesIfPresent(sendContext);

        if (sendContext.Exchange.Equals(RabbitMqExchangeNames.ReplyTo) && string.IsNullOrWhiteSpace(sendContext.RoutingKey))
        {
            var destinationAddress = sendContext.DestinationAddress
                ?? throw new InvalidOperationException("The RabbitMQ send context does not have a destination address.");
            throw new TransportException(destinationAddress, "RoutingKey must be specified when sending to reply-to address");
        }

        return sendContext;
    }

    /// <summary>Maps a send context to AMQP properties and publishes it to RabbitMQ.</summary>
    /// <typeparam name="T">The message contract.</typeparam>
    /// <param name="transportContext">The active RabbitMQ channel context.</param>
    /// <param name="sendContext">The configured RabbitMQ message send context.</param>
    /// <param name="cancellationToken">Cancellation checked before publishing; the send context governs broker operations.</param>
    /// <returns>A task that follows the publish according to the context's acknowledgement setting.</returns>
    public async Task SendAsync<T>(ChannelContext transportContext, SendContext<T> sendContext, CancellationToken cancellationToken = default)
        where T : class
    {
        cancellationToken.ThrowIfCancellationRequested(); RabbitMqMessageSendContext<T> context = sendContext as RabbitMqMessageSendContext<T>
                    ?? throw new ArgumentException("Invalid SendContext<T> type", nameof(sendContext));

        sendContext.CancellationToken.ThrowIfCancellationRequested();

        OneTimeContext<ConfigureTopologyContext<SendSettings>> oneTimeContext =
            await _configureTopologyFilter.ConfigureAsync(transportContext, sendContext.CancellationToken).ConfigureAwait(false);

        sendContext.CancellationToken.ThrowIfCancellationRequested();

        var exchange = context.Exchange;
        if (exchange.Equals(RabbitMqExchangeNames.ReplyTo))
            exchange = "";

        var body = context.Body.GetBytes();

        if (context.TryGetPayload(out PublishContext? publishContext))
            context.Mandatory = context.Mandatory || publishContext.Mandatory;

        context.BasicProperties.Headers ??= new Dictionary<string, object?>();

        context.BasicProperties.ContentType = context.ContentType?.ToString();

        SetHeaders(context.BasicProperties.Headers, context.Headers);

        context.BasicProperties.Persistent = context.Durable;

        if (context.MessageId.HasValue)
            context.BasicProperties.MessageId = context.MessageId.ToString();

        if (context.CorrelationId.HasValue)
            context.BasicProperties.CorrelationId = context.CorrelationId.ToString();

        if (context.TimeToLive.HasValue)
        {
            context.BasicProperties.Expiration =
                (context.TimeToLive > TimeSpan.Zero ? context.TimeToLive.Value : TimeSpan.FromSeconds(1))
                .TotalMilliseconds
                .ToString("F0", CultureInfo.InvariantCulture);
        }

        if (context.RequestId.HasValue && context.ResponseAddress?.IsReplyToAddress() == true)
            context.BasicProperties.ReplyTo ??= RabbitMqExchangeNames.ReplyTo;

        var delay = context.Delay?.TotalMilliseconds;
        if (delay > 0 && exchange != "")
        {
            await _delayConfigureTopologyPipe.SendAsync(transportContext).ConfigureAwait(false);
            context.SetTransportHeader("x-delay", (long)delay.Value);

            exchange = _delayExchange;
        }

        var routingKey = context.RoutingKey ?? "";

        if (Activity.Current?.IsAllDataRequested ?? false)
        {
            if (!string.IsNullOrEmpty(routingKey))
                Activity.Current.SetTag(DiagnosticHeaders.Messaging.RabbitMq.RoutingKey, routingKey);
        }

        var publishTask = transportContext.BasicPublishAsync(exchange, routingKey, context.Mandatory, context.BasicProperties, body,
            context.AwaitAck, sendContext.CancellationToken);

        try
        {
            await publishTask.OrCanceledAsync(context.CancellationToken).ConfigureAwait(false);
        }
        catch (OperationCanceledException)
        {
            throw;
        }
        catch (Exception)
        {
            oneTimeContext.Evict();
            transportContext.ConnectionContext.TopologyEntityCache.Invalidate();
            throw;
        }
    }

    static void SetHeaders(IDictionary<string, object?> dictionary, SendHeaders headers)
    {
        foreach (KeyValuePair<string, object> header in headers.GetAll())
        {
            if (header.Key is RabbitMqHeaders.Exchange or RabbitMqHeaders.RoutingKey or RabbitMqHeaders.DeliveryTag or RabbitMqHeaders.ConsumerTag)
                continue;

            if (dictionary.ContainsKey(header.Key))
                continue;

            switch (header.Value)
            {
                case DateTimeOffset value:
                    dictionary.SetAmqpTimestamp(header.Key, value.UtcDateTime);
                    break;

                case DateTime value:
                    if (value.Kind == DateTimeKind.Local)
                        value = value.ToUniversalTime();
                    dictionary.SetAmqpTimestamp(header.Key, value);
                    break;

                case Guid value:
                    dictionary[header.Key] = value.ToString("D");
                    break;

                case string value when header.Key == "CC" || header.Key == "BCC":
                    dictionary[header.Key] = new[] { value };
                    break;

                case IEnumerable<string> strings when header.Key == "CC" || header.Key == "BCC":
                    dictionary[header.Key] = strings.ToArray();
                    break;

                case Uri value:
                    dictionary[header.Key] = value.ToString();
                    break;

                case string value:
                    dictionary[header.Key] = value;
                    break;

                case bool value when value:
                    dictionary[header.Key] = bool.TrueString;
                    break;

                case IFormattable formatValue:
                    if (header.Value.GetType().IsValueType)
                        dictionary[header.Key] = header.Value;
                    else
                        dictionary[header.Key] = formatValue.ToString(null, CultureInfo.InvariantCulture);
                    break;
            }
        }
    }

    static void CopyIncomingPropertiesIfPresent<T>(RabbitMqSendContext<T> context)
        where T : class
    {
        if (context.TryGetPayload<ConsumeContext>(out var consumeContext)
            && consumeContext.TryGetPayload<RabbitMqBasicConsumeContext>(out var basicConsumeContext))
        {
            if (context.BasicProperties.IsPriorityPresent() == false)
            {
                if (basicConsumeContext.Properties.IsPriorityPresent())
                    context.TrySetPriority(basicConsumeContext.Properties.Priority);
            }

            if (!string.IsNullOrWhiteSpace(basicConsumeContext.Properties.ReplyTo) && context.ResponseAddress?.IsReplyToAddress() == true)
                context.BasicProperties.ReplyTo = basicConsumeContext.Properties.ReplyTo;
        }
    }
}

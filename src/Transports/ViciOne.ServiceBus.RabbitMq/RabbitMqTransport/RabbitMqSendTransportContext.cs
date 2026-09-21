using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Globalization;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using RabbitMQ.Client;
using RabbitMQ.Client.Exceptions;
using ViciOne.ServiceBus.Internals;
using ViciOne.ServiceBus.Logging;
using ViciOne.ServiceBus.Monitoring;
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
        cancellationToken.ThrowIfCancellationRequested();
        RabbitMqMessageSendContext<T> context = sendContext as RabbitMqMessageSendContext<T>
            ?? throw new ArgumentException("Invalid SendContext<T> type", nameof(sendContext));

        sendContext.CancellationToken.ThrowIfCancellationRequested();

        if (context.TryGetPayload<RabbitMqTransportAcceptanceRequirement>(out var acceptanceRequirement)
            && !transportContext.ConnectionContext.PublisherConfirmation)
        {
            throw new ConfigurationException(
                global::ViciOne.ServiceBus.Providers.Configuration.ConfigurationMessages.Create(
                    "RabbitMQ durable transport acceptance",
                    context.DestinationAddress?.ToString() ?? _exchange,
                    "RabbitMQ publisher confirmations are disabled, so broker acceptance cannot be proven",
                    "Enable publisher confirmations on the RabbitMQ host"));
        }

        if (acceptanceRequirement is not null
            && (!string.Equals(context.Exchange, acceptanceRequirement.Exchange, StringComparison.Ordinal)
                || !string.Equals(_exchange, acceptanceRequirement.Exchange, StringComparison.Ordinal)
                || context.Delay.GetValueOrDefault() > TimeSpan.Zero
                || context.TimeToLive.HasValue
                || !context.Durable
                || !context.Mandatory
                || !context.AwaitAck))
        {
            throw new ConfigurationException(
                global::ViciOne.ServiceBus.Providers.Configuration.ConfigurationMessages.Create(
                    "RabbitMQ durable transport acceptance",
                    context.DestinationAddress?.ToString() ?? _exchange,
                    "The publish route or delivery properties differ from the validated durable queue destination",
                    "Use the validated durable queue without delayed routing or transport-property overrides"));
        }

        OneTimeContext<ConfigureTopologyContext<SendSettings>> oneTimeContext =
            await _configureTopologyFilter.ConfigureAsync(transportContext, sendContext.CancellationToken).ConfigureAwait(false);

        sendContext.CancellationToken.ThrowIfCancellationRequested();

        if (acceptanceRequirement?.RequiresExistingQueueProof == true)
        {
            try
            {
                await VerifyExistingQuorumQueueAsync(transportContext, acceptanceRequirement.Exchange,
                        context.DestinationAddress?.ToString() ?? _exchange, sendContext.CancellationToken)
                    .ConfigureAwait(false);
            }
            catch (OperationCanceledException)
            {
                throw;
            }
            catch (Exception)
            {
                InvalidateTopology(oneTimeContext, transportContext);
                throw;
            }
        }

        var exchange = context.Exchange;
        if (exchange.Equals(RabbitMqExchangeNames.ReplyTo))
            exchange = "";

        byte[] body = context.Body.ToArray();

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
                Activity.Current.SetTag(ServiceBusTelemetry.Attributes.RabbitMqRoutingKey, routingKey);
        }

        try
        {
            await transportContext.BasicPublishAsync(exchange, routingKey, context.Mandatory, context.BasicProperties, body,
                    context.AwaitAck, sendContext.CancellationToken)
                .OrCanceledAsync(context.CancellationToken)
                .ConfigureAwait(false);

            if (acceptanceRequirement?.RequiresExistingQueueProof == true)
            {
                await VerifyExistingQuorumQueueAsync(transportContext, acceptanceRequirement.Exchange,
                        context.DestinationAddress?.ToString() ?? _exchange, sendContext.CancellationToken)
                    .ConfigureAwait(false);
            }

            acceptanceRequirement?.MarkAccepted();
        }
        catch (OperationCanceledException)
        {
            throw;
        }
        catch (Exception)
        {
            InvalidateTopology(oneTimeContext, transportContext);
            throw;
        }
    }

    static void InvalidateTopology(OneTimeContext<ConfigureTopologyContext<SendSettings>> oneTimeContext,
        ChannelContext transportContext)
    {
        try
        {
            oneTimeContext.Evict();
        }
        catch (InvalidOperationException)
        {
            // Another sender already evicted this generation and is rebuilding topology. Preserve
            // the broker failure from this sender while the shared replacement setup finishes.
        }
        finally
        {
            transportContext.ConnectionContext.TopologyEntityCache.Invalidate();
        }
    }

    static async Task VerifyExistingQuorumQueueAsync(ChannelContext transportContext, string queueName,
        string destination, CancellationToken cancellationToken)
    {
        try
        {
            // Receive InputAddress retains its exchange-form public URI. Verify its same-name queue
            // without changing routing. Repeating the proof after publisher confirmation detects a
            // replacement that remains in place through the post-check. AMQP does not expose a stable
            // queue identity, so privileged concurrent queue deletion or redeclaration remains an
            // operational boundary.
            await transportContext.QueueDeclarePassiveAsync(queueName, cancellationToken).ConfigureAwait(false);
            await transportContext.QueueDeclareAsync(queueName, durable: true, exclusive: false,
                    autoDelete: false, new Dictionary<string, object?>
                    {
                        [RabbitMQ.Client.Headers.XQueueType] = "quorum"
                    }, cancellationToken)
                .ConfigureAwait(false);
        }
        catch (Exception exception)
        {
            // A broker 4xx reply to this required queue proof is a rejected destination,
            // not a transient transport failure. Preserve the broker reason for diagnostics.
            if (exception is OperationInterruptedException interrupted
                && interrupted.ShutdownReason?.ReplyCode is >= 400 and < 500)
            {
                throw new ConfigurationException(
                    global::ViciOne.ServiceBus.Providers.Configuration.ConfigurationMessages.Create(
                        "RabbitMQ durable transport acceptance",
                        destination,
                        $"RabbitMQ rejected the existing durable quorum queue proof with AMQP reply code {interrupted.ShutdownReason.ReplyCode}",
                        "Verify the destination queue, its durable quorum properties, its binding, and broker permissions"),
                    interrupted);
            }

            throw;
        }
    }

    static void SetHeaders(IDictionary<string, object?> dictionary, SendHeaders headers)
    {
        foreach (KeyValuePair<string, object> header in headers.GetAll())
        {
            if (IsReceiveOnlyHeader(header.Key) || dictionary.ContainsKey(header.Key))
                continue;

            SetHeader(dictionary, header.Key, header.Value);
        }
    }

    static bool IsReceiveOnlyHeader(string key) => key is
        RabbitMqHeaders.Exchange or RabbitMqHeaders.RoutingKey or RabbitMqHeaders.DeliveryTag or RabbitMqHeaders.ConsumerTag;

    static void SetHeader(IDictionary<string, object?> dictionary, string key, object value)
    {
        if (value is DateTimeOffset offset)
        {
            dictionary.SetAmqpTimestamp(key, offset.UtcDateTime);
            return;
        }

        if (value is DateTime dateTime)
        {
            dictionary.SetAmqpTimestamp(key, dateTime.Kind == DateTimeKind.Local ? dateTime.ToUniversalTime() : dateTime);
            return;
        }

        if (value is Guid identifier)
        {
            dictionary[key] = identifier.ToString("D");
            return;
        }

        if (value is string text)
        {
            dictionary[key] = IsCopyHeader(key) ? new[] { text } : text;
            return;
        }

        if (IsCopyHeader(key) && value is IEnumerable<string> addresses)
        {
            dictionary[key] = addresses.ToArray();
            return;
        }

        if (value is Uri uri)
        {
            dictionary[key] = uri.ToString();
            return;
        }

        if (value is bool boolean)
        {
            dictionary[key] = boolean ? bool.TrueString : bool.FalseString;
            return;
        }

        if (value is IFormattable formattable)
            dictionary[key] = value.GetType().IsValueType ? value : formattable.ToString(null, CultureInfo.InvariantCulture);
    }

    static bool IsCopyHeader(string key) => key is "CC" or "BCC";

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

            if (!context.BasicProperties.IsReplyToPresent()
                && !string.IsNullOrWhiteSpace(basicConsumeContext.Properties.ReplyTo)
                && context.ResponseAddress?.IsReplyToAddress() == true)
                context.BasicProperties.ReplyTo = basicConsumeContext.Properties.ReplyTo;
        }
    }
}

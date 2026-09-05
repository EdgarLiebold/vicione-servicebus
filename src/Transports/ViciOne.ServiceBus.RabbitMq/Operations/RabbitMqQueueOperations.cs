using System;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using RabbitMQ.Client;
using ViciOne.ServiceBus.DependencyInjection;
using ViciOne.ServiceBus.RabbitMq.Configuration;
using ViciOne.ServiceBus.Transports;

#nullable enable
namespace ViciOne.ServiceBus.RabbitMq.Operations;

internal sealed class RabbitMqQueueOperations : IRabbitMqQueueOperations
{
    readonly Bind<IBus, IBusInstance> _busInstance;

    public RabbitMqQueueOperations(Bind<IBus, IBusInstance> busInstance)
    {
        _busInstance = busInstance;
    }

    public Task<RabbitMqFaultRedriveResult> RedriveFaultedMessagesAsync(
        RabbitMqFaultRedriveRequest request,
        CancellationToken cancellationToken = default)
    {
        return RabbitMqFaultRedriveExecutor.ExecuteAsync(_busInstance.Value, request, cancellationToken);
    }
}


internal sealed class RabbitMqQueueOperations<TBus> : IRabbitMqQueueOperations<TBus>
    where TBus : class, IBus
{
    readonly IBusInstance<TBus> _busInstance;

    public RabbitMqQueueOperations(IBusInstance<TBus> busInstance)
    {
        _busInstance = busInstance;
    }

    public Task<RabbitMqFaultRedriveResult> RedriveFaultedMessagesAsync(
        RabbitMqFaultRedriveRequest request,
        CancellationToken cancellationToken = default)
    {
        return RabbitMqFaultRedriveExecutor.ExecuteAsync(_busInstance.BusInstance, request, cancellationToken);
    }
}


internal static class RabbitMqFaultRedriveExecutor
{
    public static async Task<RabbitMqFaultRedriveResult> ExecuteAsync(
        IBusInstance busInstance,
        RabbitMqFaultRedriveRequest request,
        CancellationToken cancellationToken)
    {
        ArgumentNullException.ThrowIfNull(busInstance);
        ArgumentNullException.ThrowIfNull(request);

        RabbitMqFaultRedriveLoop.Validate(request);

        if (busInstance.HostConfiguration is not IRabbitMqHostConfiguration hostConfiguration)
            throw new ConfigurationException(global::ViciOne.ServiceBus.Providers.Configuration.ConfigurationMessages.Create("RabbitMQ", "unknown", $"The bus '{busInstance.InstanceType.Name}' is not configured with the RabbitMQ transport.", "Correct the named configuration before starting the host"));

        var sourceQueueName = GetValidatedSourceQueueName(
            request,
            hostConfiguration.Topology.SendTopology.EntityNameValidator,
            hostConfiguration.Topology.SendTopology.ErrorQueueNameFormatter);

        RabbitMqFaultRedriveResult? result = null;
        await hostConfiguration.ConnectionContextSupervisor.SendAsync(Pipe.ExecuteAsync<ConnectionContext>(async connectionContext =>
        {
            result = await ExecuteAsync(connectionContext, request, sourceQueueName, cancellationToken).ConfigureAwait(false);
        }), cancellationToken).ConfigureAwait(false);

        return result ?? throw new InvalidOperationException("RabbitMQ fault redrive completed without producing a result.");
    }

    static async Task<RabbitMqFaultRedriveResult> ExecuteAsync(
        ConnectionContext connectionContext,
        RabbitMqFaultRedriveRequest request,
        string sourceQueueName,
        CancellationToken cancellationToken)
    {
        var options = CreateOperationsChannelOptions();
        await using var channel = await connectionContext.Connection.CreateChannelAsync(options, cancellationToken).ConfigureAwait(false);
        channel.ContinuationTimeout = connectionContext.ContinuationTimeout;

        var adapter = new RabbitMqFaultRedriveChannel(channel);
        await adapter.VerifyQueueAsync(sourceQueueName, cancellationToken).ConfigureAwait(false);
        await adapter.VerifyQueueAsync(request.EndpointQueueName, cancellationToken).ConfigureAwait(false);

        return await RabbitMqFaultRedriveLoop.ExecuteAsync(adapter, request, sourceQueueName, cancellationToken).ConfigureAwait(false);
    }

    internal static CreateChannelOptions CreateOperationsChannelOptions() => new(
        publisherConfirmationsEnabled: true,
        publisherConfirmationTrackingEnabled: true);

    internal static string GetValidatedSourceQueueName(
        RabbitMqFaultRedriveRequest request,
        IEntityNameValidator entityNameValidator,
        IErrorQueueNameFormatter errorQueueNameFormatter)
    {
        ArgumentNullException.ThrowIfNull(request);
        ArgumentNullException.ThrowIfNull(entityNameValidator);
        ArgumentNullException.ThrowIfNull(errorQueueNameFormatter);

        entityNameValidator.ThrowIfInvalidEntityName(request.EndpointQueueName);
        string sourceQueueName = errorQueueNameFormatter.FormatErrorQueueName(request.EndpointQueueName);
        entityNameValidator.ThrowIfInvalidEntityName(sourceQueueName);
        return sourceQueueName;
    }
}


internal static class RabbitMqFaultRedriveLoop
{
    public static async Task<RabbitMqFaultRedriveResult> ExecuteAsync(
        IRabbitMqFaultRedriveChannel channel,
        RabbitMqFaultRedriveRequest request,
        string sourceQueueName,
        CancellationToken cancellationToken)
    {
        ArgumentNullException.ThrowIfNull(channel);
        ArgumentNullException.ThrowIfNull(request);
        ArgumentException.ThrowIfNullOrWhiteSpace(sourceQueueName);
        Validate(request);

        var scanned = 0;
        var matched = 0;
        var redriven = 0;
        var sourceExhausted = false;

        while (scanned < request.MaxScanCount && redriven < request.MaxMessages)
        {
            cancellationToken.ThrowIfCancellationRequested();

            RabbitMqFaultRedriveDelivery? message = await channel.GetAsync(sourceQueueName, cancellationToken).ConfigureAwait(false);
            if (message == null)
            {
                sourceExhausted = true;
                break;
            }

            scanned++;
            if (!Matches(message.Properties, request))
                continue;

            matched++;
            await channel.PublishAsync(
                request.EndpointQueueName,
                message.RoutingKey,
                message.Properties,
                message.Body,
                cancellationToken).ConfigureAwait(false);
            await channel.AcknowledgeAsync(message.DeliveryTag, cancellationToken).ConfigureAwait(false);
            redriven++;
        }

        var scanLimitReached = !sourceExhausted && scanned >= request.MaxScanCount && redriven < request.MaxMessages;
        return new RabbitMqFaultRedriveResult(
            request.EndpointQueueName,
            sourceQueueName,
            scanned,
            matched,
            redriven,
            sourceExhausted,
            scanLimitReached);
    }

    internal static bool Matches(IReadOnlyBasicProperties properties, RabbitMqFaultRedriveRequest request)
    {
        ArgumentNullException.ThrowIfNull(properties);
        ArgumentNullException.ThrowIfNull(request);

        if (request.MessageId.HasValue && !MatchesGuid(properties.MessageId, request.MessageId.Value))
            return false;
        if (request.CorrelationId.HasValue && !MatchesGuid(properties.CorrelationId, request.CorrelationId.Value))
            return false;

        if (request.FaultExceptionType != null)
        {
            if (properties.Headers == null
                || !properties.Headers.TryGetValue(MessageHeaders.FaultExceptionType, out var value)
                || !string.Equals(GetHeaderString(value), request.FaultExceptionType, StringComparison.Ordinal))
                return false;
        }

        return true;
    }

    internal static void Validate(RabbitMqFaultRedriveRequest request)
    {
        ArgumentNullException.ThrowIfNull(request);

        if (string.IsNullOrWhiteSpace(request.EndpointQueueName))
            throw new ArgumentException("The endpoint queue name is required.", nameof(request));
        if (request.MaxMessages is <= 0 or > RabbitMqFaultRedriveRequest.AbsoluteMaxMessages)
        {
            throw new ArgumentOutOfRangeException(
                nameof(request),
                request.MaxMessages,
                $"MaxMessages must be between 1 and {RabbitMqFaultRedriveRequest.AbsoluteMaxMessages}.");
        }

        if (request.MaxScanCount is <= 0 or > RabbitMqFaultRedriveRequest.AbsoluteMaxScanCount)
        {
            throw new ArgumentOutOfRangeException(
                nameof(request),
                request.MaxScanCount,
                $"MaxScanCount must be between 1 and {RabbitMqFaultRedriveRequest.AbsoluteMaxScanCount}.");
        }

        if (request.MaxScanCount < request.MaxMessages)
            throw new ArgumentException("MaxScanCount must be greater than or equal to MaxMessages.", nameof(request));
        if (request.FaultExceptionType != null && string.IsNullOrWhiteSpace(request.FaultExceptionType))
            throw new ArgumentException("FaultExceptionType must be null or a non-empty exact type name.", nameof(request));
    }

    static bool MatchesGuid(string? value, Guid expected) =>
        Guid.TryParse(value, out var parsed) && parsed == expected;

    static string? GetHeaderString(object? value)
    {
        return value switch
        {
            null => null,
            string text => text,
            byte[] bytes => Encoding.UTF8.GetString(bytes),
            ReadOnlyMemory<byte> bytes => Encoding.UTF8.GetString(bytes.Span),
            _ => Convert.ToString(value, System.Globalization.CultureInfo.InvariantCulture)
        };
    }
}


internal interface IRabbitMqFaultRedriveChannel
{
    Task<RabbitMqFaultRedriveDelivery?> GetAsync(string queueName, CancellationToken cancellationToken);

    Task PublishAsync(
        string exchangeName,
        string routingKey,
        BasicProperties properties,
        ReadOnlyMemory<byte> body,
        CancellationToken cancellationToken);

    Task AcknowledgeAsync(ulong deliveryTag, CancellationToken cancellationToken);
}


internal sealed record RabbitMqFaultRedriveDelivery(
    ulong DeliveryTag,
    string RoutingKey,
    BasicProperties Properties,
    ReadOnlyMemory<byte> Body);


sealed class RabbitMqFaultRedriveChannel : IRabbitMqFaultRedriveChannel
{
    readonly IChannel _channel;

    public RabbitMqFaultRedriveChannel(IChannel channel)
    {
        _channel = channel;
    }

    public async Task VerifyQueueAsync(string queueName, CancellationToken cancellationToken)
    {
        await _channel.QueueDeclarePassiveAsync(queueName, cancellationToken).ConfigureAwait(false);
    }

    public async Task<RabbitMqFaultRedriveDelivery?> GetAsync(string queueName, CancellationToken cancellationToken)
    {
        BasicGetResult? message = await _channel.BasicGetAsync(queueName, autoAck: false, cancellationToken).ConfigureAwait(false);
        return message == null
            ? null
            : new RabbitMqFaultRedriveDelivery(
                message.DeliveryTag,
                message.RoutingKey,
                new BasicProperties(message.BasicProperties),
                message.Body);
    }

    public async Task PublishAsync(
        string exchangeName,
        string routingKey,
        BasicProperties properties,
        ReadOnlyMemory<byte> body,
        CancellationToken cancellationToken)
    {
        await _channel.BasicPublishAsync(
            exchangeName,
            routingKey,
            mandatory: true,
            properties,
            body,
            cancellationToken).ConfigureAwait(false);
    }

    public async Task AcknowledgeAsync(ulong deliveryTag, CancellationToken cancellationToken)
    {
        await _channel.BasicAckAsync(deliveryTag, multiple: false, cancellationToken).ConfigureAwait(false);
    }
}

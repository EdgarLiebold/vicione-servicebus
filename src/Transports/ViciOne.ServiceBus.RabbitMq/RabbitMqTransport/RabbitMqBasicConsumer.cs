using System;
using System.IO;
using System.Threading;
using System.Threading.Tasks;
using RabbitMQ.Client;
using RabbitMQ.Client.Events;
using RabbitMQ.Client.Exceptions;
using ViciOne.ServiceBus.Context;
using ViciOne.ServiceBus.Transports;

namespace ViciOne.ServiceBus.RabbitMq;

/// <summary>
/// Receives messages from RabbitMQ, pushing them to the InboundPipe of the service endpoint.
/// </summary>
public class RabbitMqBasicConsumer :
    ConsumerAgent<ulong>,
    IAsyncBasicConsumer,
    RabbitMqDeliveryMetrics
{
    readonly ChannelContext _channel;
    readonly RabbitMqReceiveEndpointContext _context;
    readonly ReceiveSettings _receiveSettings;

    string _consumerTag = "";

    /// <summary>
    /// The basic consumer receives messages pushed from the broker.
    /// </summary>
    /// <param name="channel">The channel context for the consumer</param>
    /// <param name="context">The topology</param>
    public RabbitMqBasicConsumer(ChannelContext channel, RabbitMqReceiveEndpointContext context)
        : base(context)
    {
        _channel = channel;
        _context = context;

        _receiveSettings = channel.GetPayload<ReceiveSettings>();

        TrySetManualConsumeTask();
    }

    /// <summary>
    /// Performs the handle basic consume ok operation.
    /// </summary>
    /// <param name="consumerTag">The consumer tag value.</param>
    /// <param name="cancellationToken">The token used to cancel the operation.</param>
    /// <returns>The result of the operation.</returns>
    public Task HandleBasicConsumeOkAsync(string consumerTag, CancellationToken cancellationToken)
    {
        if (cancellationToken.IsCancellationRequested) return global::System.Threading.Tasks.Task.FromCanceled(cancellationToken); LogContext.Current = _context.LogContext;

        LogContext.Debug?.Log("Consumer Ok: {InputAddress} - {ConsumerTag}", _context.InputAddress, consumerTag);

        _channel.Channel.ChannelShutdownAsync += ObserveChannelShutdownAsync;
        Completed.GetAwaiter().OnCompleted(() =>
            _channel.Channel.ChannelShutdownAsync -= ObserveChannelShutdownAsync);

        _consumerTag = consumerTag;

        SetReady();

        return Task.CompletedTask;
    }

    /// <summary>
    /// Performs the handle basic cancel ok operation.
    /// </summary>
    /// <param name="consumerTag">The consumer tag value.</param>
    /// <param name="cancellationToken">The token used to cancel the operation.</param>
    /// <returns>The result of the operation.</returns>
    public Task HandleBasicCancelOkAsync(string consumerTag, CancellationToken cancellationToken)
    {
        if (cancellationToken.IsCancellationRequested) return global::System.Threading.Tasks.Task.FromCanceled(cancellationToken); LogContext.Current = _context.LogContext;

        LogContext.Debug?.Log("Consumer Cancel Ok: {InputAddress} - {ConsumerTag}", _context.InputAddress, consumerTag);

        TrySetConsumeCompleted();

        return Task.CompletedTask;
    }

    /// <summary>
    /// Performs the handle basic cancel operation.
    /// </summary>
    /// <param name="consumerTag">The consumer tag value.</param>
    /// <param name="cancellationToken">The token used to cancel the operation.</param>
    /// <returns>The result of the operation.</returns>
    public async Task HandleBasicCancelAsync(string consumerTag, CancellationToken cancellationToken)
    {
        LogContext.Current = _context.LogContext;

        LogContext.Debug?.Log("Consumer Canceled: {InputAddress} - {ConsumerTag}", _context.InputAddress, consumerTag);

        // An unsolicited broker cancel commonly means that a queue was deleted externally. Any
        // successful declaration/binding knowledge for this connection is now stale.
        _channel.ConnectionContext.TopologyEntityCache.Invalidate();
        TrySetConsumeCanceled(cancellationToken);
    }

    /// <summary>
    /// Performs the handle channel shutdown operation.
    /// </summary>
    /// <param name="channel">The channel value.</param>
    /// <param name="reason">The reason value.</param>
    /// <returns>The result of the operation.</returns>
    public Task HandleChannelShutdownAsync(object channel, ShutdownEventArgs reason)
    {
        LogContext.Current = _context.LogContext;

        LogContext.Debug?.Log(
            "Consumer Channel Shutdown: {InputAddress} - {ConsumerTag}, Concurrent Peak: {MaxConcurrentDeliveryCount}, {ReplyCode}-{ReplyText}",
            _context.InputAddress, _consumerTag, ConcurrentDeliveryCount, reason.ReplyCode, reason.ReplyText);

        TrySetConsumeCanceled();

        return Task.CompletedTask;
    }

    /// <summary>
    /// Performs the handle basic deliver operation.
    /// </summary>
    /// <param name="consumerTag">The consumer tag value.</param>
    /// <param name="deliveryTag">The delivery tag value.</param>
    /// <param name="redelivered">The redelivered value.</param>
    /// <param name="exchange">The exchange value.</param>
    /// <param name="routingKey">The routing key value.</param>
    /// <param name="properties">The properties value.</param>
    /// <param name="body">The body value.</param>
    /// <param name="cancellationToken">The token used to cancel the operation.</param>
    /// <returns>The result of the operation.</returns>
    public async Task HandleBasicDeliverAsync(string consumerTag, ulong deliveryTag, bool redelivered, string exchange, string routingKey,
        IReadOnlyBasicProperties properties, ReadOnlyMemory<byte> body, CancellationToken cancellationToken)
    {
        cancellationToken.ThrowIfCancellationRequested(); LogContext.Current = _context.LogContext;

        var context = new RabbitMqReceiveContext(exchange, routingKey, _consumerTag, deliveryTag, body, redelivered, properties,
            _context, _receiveSettings, _channel, _channel.ConnectionContext);

        try
        {
            if (IsStopping)
                return;

            await DispatchAsync(deliveryTag, context, _receiveSettings.NoAck
                    ? NoLockReceiveContext.Instance
                    : new RabbitMqReceiveLockContext(_channel, deliveryTag, context.CancellationToken))
                .ConfigureAwait(false);
        }
        catch (OperationInterruptedException exception)
        {
            LogContext.Error?.Log(exception,
                "Consumer Channel Shutdown: {InputAddress} - {ConsumerTag}, Concurrent Peak: {MaxConcurrentDeliveryCount}",
                _context.InputAddress, _consumerTag, ConcurrentDeliveryCount);

            _channel.NotifyFaulted(exception, _context.InputAddress);

            TrySetConsumeException(exception);
        }
        catch (EndOfStreamException exception)
        {
            LogContext.Error?.Log(exception,
                "Consumer Channel Shutdown: {InputAddress} - {ConsumerTag}, Concurrent Peak: {MaxConcurrentDeliveryCount}",
                _context.InputAddress, _consumerTag, ConcurrentDeliveryCount);

            _channel.NotifyFaulted(exception, _context.InputAddress);

            TrySetConsumeException(exception);
        }
        catch (Exception exception)
        {
            context.LogTransportFaulted(exception);
        }
        finally
        {
            context.Dispose();
        }
    }

    /// <summary>
    /// Gets the channel value.
    /// </summary>
    public IChannel Channel => _channel.Channel;

    string RabbitMqDeliveryMetrics.ConsumerTag => _consumerTag;

    /// <summary>
    /// Determines whether trackable.
    /// </summary>
    /// <param name="deliveryTag">The delivery tag value.</param>
    /// <returns><see langword="true" /> when the condition is satisfied; otherwise, <see langword="false" />.</returns>
    protected override bool IsTrackable(ulong deliveryTag)
    {
        return deliveryTag != 1 || _context.IsNotReplyTo;
    }

    /// <summary>
    /// Performs the active and actual agents completed operation.
    /// </summary>
    /// <param name="context">The operation context.</param>
    /// <returns>The result of the operation.</returns>
    protected override async Task ActiveAndActualAgentsCompletedAsync(StopContext context)
    {
        try
        {
            if (IsGracefulShutdown && _channel.Channel.IsOpen)
                await _channel.BasicCancelAsync(_consumerTag, context.CancellationToken).ConfigureAwait(false);
        }
        catch (Exception exception)
        {
            LogContext.Warning?.Log(exception, "BasicCancel faulted: {InputAddress} - {ConsumerTag}", _context.InputAddress, _consumerTag);
        }

        await base.ActiveAndActualAgentsCompletedAsync(context).ConfigureAwait(false);
    }

    Task ObserveChannelShutdownAsync(object channel, ShutdownEventArgs reason)
    {
        LogContext.Current = _context.LogContext;

        LogContext.Debug?.Log(
            "Channel Shutdown: {InputAddress} - {ConsumerTag}, Concurrent Peak: {MaxConcurrentDeliveryCount}, {ReplyCode}-{ReplyText}",
            _context.InputAddress, _consumerTag, ConcurrentDeliveryCount, reason.ReplyCode, reason.ReplyText);

        TrySetConsumeCanceled();

        return Task.CompletedTask;
    }
}

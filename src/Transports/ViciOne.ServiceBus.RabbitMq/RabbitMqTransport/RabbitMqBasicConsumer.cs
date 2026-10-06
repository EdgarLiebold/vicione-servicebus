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

/// <summary>Receives RabbitMQ deliveries and dispatches them through the endpoint receive pipeline.</summary>
public class RabbitMqBasicConsumer :
    ConsumerAgent<ulong>,
    IAsyncBasicConsumer,
    RabbitMqDeliveryMetrics
{
    readonly ChannelContext _channel;
    readonly RabbitMqReceiveEndpointContext _context;
    readonly ReceiveSettings _receiveSettings;

    string _consumerTag = "";

    /// <summary>Receives messages delivered by RabbitMQ and dispatches them to the receive pipeline.</summary>
    /// <param name="channel">The channel context for the consumer.</param>
    /// <param name="context">The receive endpoint context and pipeline.</param>
    public RabbitMqBasicConsumer(ChannelContext channel, RabbitMqReceiveEndpointContext context)
        : base(context)
    {
        _channel = channel;
        _context = context;

        _receiveSettings = channel.GetPayload<ReceiveSettings>();

        TrySetManualConsumeTask();
    }

    /// <summary>Records the broker-assigned consumer tag and marks the consumer ready.</summary>
    /// <param name="consumerTag">The consumer tag assigned by RabbitMQ.</param>
    /// <param name="cancellationToken">Cancellation checked before updating consumer state.</param>
    /// <returns>A task completed after the consumer is marked ready.</returns>
    public Task HandleBasicConsumeOkAsync(string consumerTag, CancellationToken cancellationToken)
    {
        if (cancellationToken.IsCancellationRequested) return global::System.Threading.Tasks.Task.FromCanceled(cancellationToken); LogContext.Current = _context.LogContext;

        try
        {
            LogContext.Debug?.Log("Consumer Ok: {InputAddress} - {ConsumerTag}", _context.InputAddress, consumerTag);
        }
        catch (Exception)
        {
            // Optional diagnostics cannot prevent the owning callback or lifecycle transition.
        }

        _channel.Channel.ChannelShutdownAsync += ObserveChannelShutdownAsync;
        Completed.GetAwaiter().OnCompleted(() =>
            _channel.Channel.ChannelShutdownAsync -= ObserveChannelShutdownAsync);

        _consumerTag = consumerTag;

        SetReady();

        return Task.CompletedTask;
    }

    /// <summary>Marks the consumer complete after RabbitMQ confirms cancellation.</summary>
    /// <param name="consumerTag">The canceled consumer tag.</param>
    /// <param name="cancellationToken">Cancellation checked before updating consumer state.</param>
    /// <returns>A task completed after the consumer is marked complete.</returns>
    public Task HandleBasicCancelOkAsync(string consumerTag, CancellationToken cancellationToken)
    {
        if (cancellationToken.IsCancellationRequested) return global::System.Threading.Tasks.Task.FromCanceled(cancellationToken); LogContext.Current = _context.LogContext;

        try
        {
            LogContext.Debug?.Log("Consumer Cancel Ok: {InputAddress} - {ConsumerTag}", _context.InputAddress, consumerTag);
        }
        catch (Exception)
        {
            // Optional diagnostics cannot prevent the owning callback or lifecycle transition.
        }

        TrySetConsumeCompleted();

        return Task.CompletedTask;
    }

    /// <summary>Invalidates declared topology and marks the consumer canceled after an unsolicited broker cancellation.</summary>
    /// <param name="consumerTag">The consumer tag canceled by RabbitMQ.</param>
    /// <param name="cancellationToken">The cancellation associated with the callback.</param>
    /// <returns>A task completed after consumer state is updated.</returns>
    public async Task HandleBasicCancelAsync(string consumerTag, CancellationToken cancellationToken)
    {
        LogContext.Current = _context.LogContext;

        try
        {
            LogContext.Debug?.Log("Consumer Canceled: {InputAddress} - {ConsumerTag}", _context.InputAddress, consumerTag);
        }
        catch (Exception)
        {
            // Optional diagnostics cannot prevent the owning callback or lifecycle transition.
        }

        // An unsolicited broker cancel commonly means that a queue was deleted externally. Any
        // successful declaration/binding knowledge for this connection is now stale.
        _channel.ConnectionContext.TopologyEntityCache.Invalidate();
        TrySetConsumeCanceled(cancellationToken);
    }

    /// <summary>Marks the consumer canceled after its channel shuts down.</summary>
    /// <param name="channel">The RabbitMQ channel that shut down.</param>
    /// <param name="reason">The broker or library shutdown reason.</param>
    /// <returns>A task completed after consumer state is updated.</returns>
    public Task HandleChannelShutdownAsync(object channel, ShutdownEventArgs reason)
    {
        LogContext.Current = _context.LogContext;

        try
        {
            LogContext.Debug?.Log(
                "Consumer Channel Shutdown: {InputAddress} - {ConsumerTag}, Concurrent Peak: {MaxConcurrentDeliveryCount}, {ReplyCode}-{ReplyText}",
                _context.InputAddress, _consumerTag, MaxConcurrentDeliveryCount, reason.ReplyCode, reason.ReplyText);
        }
        catch (Exception)
        {
            // Optional diagnostics cannot prevent the owning callback or lifecycle transition.
        }

        TrySetConsumeCanceled();

        return Task.CompletedTask;
    }

    /// <summary>Builds a receive context for one broker delivery and dispatches it with manual acknowledgement when required.</summary>
    /// <param name="consumerTag">The consumer tag that received the delivery.</param>
    /// <param name="deliveryTag">The channel-scoped delivery tag.</param>
    /// <param name="redelivered">Whether RabbitMQ previously delivered this message.</param>
    /// <param name="exchange">The source exchange.</param>
    /// <param name="routingKey">The delivery routing key.</param>
    /// <param name="properties">The immutable AMQP message properties.</param>
    /// <param name="body">The message body.</param>
    /// <param name="cancellationToken">Cancellation supplied by RabbitMQ.Client for this callback.</param>
    /// <returns>A task that completes after the delivery leaves the receive pipeline.</returns>
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
            try
            {
                LogContext.Error?.Log(exception,
                    "Consumer Channel Shutdown: {InputAddress} - {ConsumerTag}, Concurrent Peak: {MaxConcurrentDeliveryCount}",
                    _context.InputAddress, _consumerTag, MaxConcurrentDeliveryCount);
            }
            catch (Exception)
            {
                // Optional diagnostics cannot prevent the owning callback or lifecycle transition.
            }

            _channel.NotifyFaulted(exception, _context.InputAddress);

            TrySetConsumeException(exception);
        }
        catch (EndOfStreamException exception)
        {
            try
            {
                LogContext.Error?.Log(exception,
                    "Consumer Channel Shutdown: {InputAddress} - {ConsumerTag}, Concurrent Peak: {MaxConcurrentDeliveryCount}",
                    _context.InputAddress, _consumerTag, MaxConcurrentDeliveryCount);
            }
            catch (Exception)
            {
                // Optional diagnostics cannot prevent the owning callback or lifecycle transition.
            }

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

    /// <summary>Gets the RabbitMQ channel that owns this consumer.</summary>
    public IChannel Channel => _channel.Channel;

    string RabbitMqDeliveryMetrics.ConsumerTag => _consumerTag;

    /// <summary>Excludes the first direct-reply-to delivery from delivery-tag tracking.</summary>
    /// <param name="deliveryTag">The channel-scoped delivery tag.</param>
    /// <returns><see langword="true" /> for normal deliveries and subsequent direct replies.</returns>
    protected override bool IsTrackable(ulong deliveryTag)
    {
        return deliveryTag != 1 || _context.IsNotReplyTo;
    }

    /// <summary>Cancels the RabbitMQ consumer during graceful shutdown, then completes base agent shutdown.</summary>
    /// <param name="context">The stop context and cancellation deadline.</param>
    /// <returns>A task that completes after consumer and base-agent shutdown.</returns>
    protected override async Task ActiveAndActualAgentsCompletedAsync(StopContext context)
    {
        try
        {
            if (IsGracefulShutdown && _channel.Channel.IsOpen)
                await _channel.BasicCancelAsync(_consumerTag, context.CancellationToken).ConfigureAwait(false);
        }
        catch (Exception exception)
        {
            try
            {
                LogContext.Warning?.Log(exception, "BasicCancel faulted: {InputAddress} - {ConsumerTag}", _context.InputAddress, _consumerTag);
            }
            catch (Exception)
            {
                // Optional diagnostics cannot prevent the owning callback or lifecycle transition.
            }
        }

        await base.ActiveAndActualAgentsCompletedAsync(context).ConfigureAwait(false);
    }

    Task ObserveChannelShutdownAsync(object channel, ShutdownEventArgs reason)
    {
        LogContext.Current = _context.LogContext;

        try
        {
            LogContext.Debug?.Log(
                "Channel Shutdown: {InputAddress} - {ConsumerTag}, Concurrent Peak: {MaxConcurrentDeliveryCount}, {ReplyCode}-{ReplyText}",
                _context.InputAddress, _consumerTag, MaxConcurrentDeliveryCount, reason.ReplyCode, reason.ReplyText);
        }
        catch (Exception)
        {
            // Optional diagnostics cannot prevent the owning callback or lifecycle transition.
        }

        TrySetConsumeCanceled();

        return Task.CompletedTask;
    }
}

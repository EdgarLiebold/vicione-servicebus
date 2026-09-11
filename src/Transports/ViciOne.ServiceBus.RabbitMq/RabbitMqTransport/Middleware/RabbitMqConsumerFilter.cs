using System;
using System.Threading.Tasks;
using RabbitMQ.Client;
using RabbitMQ.Client.Events;
using RabbitMQ.Client.Exceptions;
using ViciOne.ServiceBus.Transports;

namespace ViciOne.ServiceBus.RabbitMq.Middleware;
/// <summary>Starts a RabbitMQ consumer, reports its lifecycle, and keeps the channel pipeline active until it completes.</summary>
public class RabbitMqConsumerFilter :
    IFilter<ChannelContext>
{
    readonly RabbitMqReceiveEndpointContext _context;
    string _consumerTag;

    /// <summary>Creates the consumer filter for a receive endpoint.</summary>
    /// <param name="context">The endpoint context that receives deliveries and lifecycle notifications.</param>
    public RabbitMqConsumerFilter(RabbitMqReceiveEndpointContext context)
    {
        _context = context;

        _consumerTag = "";
    }

    /// <summary>Participates in probing without adding provider-specific values.</summary>
    /// <param name="context">The probe context supplied by the receive pipeline.</param>
    public void Probe(ProbeContext context)
    {
    }

    /// <summary>Starts the broker consumer and waits for its completion before continuing the channel pipeline.</summary>
    /// <param name="context">The active RabbitMQ channel context.</param>
    /// <param name="next">The remainder of the channel pipeline.</param>
    /// <returns>A task that completes after the consumer and remaining pipeline complete.</returns>
    public async Task SendAsync(ChannelContext context, IPipe<ChannelContext> next)
    {
        var receiveSettings = context.GetPayload<ReceiveSettings>();

        if (string.IsNullOrWhiteSpace(_consumerTag) && !string.IsNullOrWhiteSpace(receiveSettings.ConsumerTag))
            _consumerTag = receiveSettings.ConsumerTag;

        var consumer = new RabbitMqBasicConsumer(context, _context);

        try
        {
            _consumerTag = await context.BasicConsumeAsync(receiveSettings.QueueName, receiveSettings.NoAck, _context.ExclusiveConsumer,
                receiveSettings.ConsumeArguments, consumer, _consumerTag, context.CancellationToken).ConfigureAwait(false);
        }
        catch (OperationCanceledException)
        {
            // Local cancellation remains cancellation and returns the channel to its single lifetime owner.
            if (context is RabbitMqChannelContext owned)
                await owned.DisposeAsync().ConfigureAwait(false);

            throw;
        }

        await consumer.Ready.ConfigureAwait(false);

        _context.AddConsumeAgent(consumer);

        await _context.TransportObservers.NotifyReadyAsync(_context.InputAddress).ConfigureAwait(false);

        try
        {
            await consumer.Completed.ConfigureAwait(false);
        }
        finally
        {
            RabbitMqDeliveryMetrics metrics = consumer;
            await _context.TransportObservers.NotifyCompletedAsync(_context.InputAddress, metrics).ConfigureAwait(false);

            _context.LogConsumerCompleted(metrics.DeliveryCount, metrics.MaxConcurrentDeliveryCount, metrics.ConsumerTag);
        }

        await next.SendAsync(context).ConfigureAwait(false);
    }
}

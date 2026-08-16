namespace ViciOne.ServiceBus.RabbitMqTransport.Middleware;

using System;
using System.Threading.Tasks;
using RabbitMQ.Client;
using RabbitMQ.Client.Events;
using RabbitMQ.Client.Exceptions;
using Transports;


/// <summary>
/// A filter that uses the channel context to create a basic consumer and connect it to the channel
/// </summary>
public class RabbitMqConsumerFilter :
    IFilter<ChannelContext>
{
    readonly RabbitMqReceiveEndpointContext _context;
    string _consumerTag;

    public RabbitMqConsumerFilter(RabbitMqReceiveEndpointContext context)
    {
        _context = context;

        _consumerTag = "";
    }

    public void Probe(ProbeContext context)
    {
    }

    public async Task Send(ChannelContext context, IPipe<ChannelContext> next)
    {
        var receiveSettings = context.GetPayload<ReceiveSettings>();

        if (string.IsNullOrWhiteSpace(_consumerTag) && !string.IsNullOrWhiteSpace(receiveSettings.ConsumerTag))
            _consumerTag = receiveSettings.ConsumerTag;

        var consumer = new RabbitMqBasicConsumer(context, _context);

        try
        {
            _consumerTag = await context.BasicConsume(receiveSettings.QueueName, receiveSettings.NoAck, _context.ExclusiveConsumer,
                receiveSettings.ConsumeArguments, consumer, _consumerTag, context.CancellationToken).ConfigureAwait(false);
        }
        catch (OperationCanceledException)
        {
            // A cancellation this process asked for stays a cancellation. Dressing it up as a broker
            // answer told every layer above that the peer had closed the channel, which was never true
            // and which the retry policy has to reason about. The channel is handed back to its owner
            // rather than closed here, so the single disposal path still applies.
            if (context is RabbitMqChannelContext owned)
                await owned.DisposeAsync().ConfigureAwait(false);

            throw;
        }

        await consumer.Ready.ConfigureAwait(false);

        _context.AddConsumeAgent(consumer);

        await _context.TransportObservers.NotifyReady(_context.InputAddress).ConfigureAwait(false);

        try
        {
            await consumer.Completed.ConfigureAwait(false);
        }
        finally
        {
            RabbitMqDeliveryMetrics metrics = consumer;
            await _context.TransportObservers.NotifyCompleted(_context.InputAddress, metrics).ConfigureAwait(false);

            _context.LogConsumerCompleted(metrics.DeliveryCount, metrics.ConcurrentDeliveryCount, metrics.ConsumerTag);
        }

        await next.Send(context).ConfigureAwait(false);
    }
}

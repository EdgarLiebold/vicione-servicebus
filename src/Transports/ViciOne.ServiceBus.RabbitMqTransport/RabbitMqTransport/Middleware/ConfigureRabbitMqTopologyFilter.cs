using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using RabbitMQ.Client;
using RabbitMQ.Client.Events;
using RabbitMQ.Client.Exceptions;
using ViciOne.ServiceBus.RabbitMqTransport.Topology;

namespace ViciOne.ServiceBus.RabbitMqTransport.Middleware;
/// <summary>
/// Configures the broker with the supplied topology once the channel is created, to ensure
/// that the exchanges, queues, and bindings for the channel are properly configured in RabbitMQ.
/// </summary>
public class ConfigureRabbitMqTopologyFilter<TSettings> :
    IFilter<ChannelContext>
    where TSettings : class
{
    readonly BrokerTopology _brokerTopology;
    readonly TSettings _settings;

    public ConfigureRabbitMqTopologyFilter(TSettings settings, BrokerTopology brokerTopology)
    {
        _settings = settings;
        _brokerTopology = brokerTopology;
    }

    public async Task SendAsync(ChannelContext context, IPipe<ChannelContext> next)
    {
        OneTimeContext<ConfigureTopologyContext<TSettings>> oneTimeContext = await ConfigureAsync(context, context.CancellationToken);

        try
        {
            await next.SendAsync(context).ConfigureAwait(false);
        }
        catch (Exception)
        {
            oneTimeContext.Evict();
            context.ConnectionContext.TopologyEntityCache.Invalidate();

            throw;
        }
    }

    public void Probe(ProbeContext context)
    {
        var scope = context.CreateFilterScope("configureTopology");

        _brokerTopology.Probe(scope);
    }

    public async Task<OneTimeContext<ConfigureTopologyContext<TSettings>>> ConfigureAsync(ChannelContext context, CancellationToken cancellationToken)
    {
        return await context.OneTimeSetupAsync<ConfigureTopologyContext<TSettings>>(() =>
        {
            context.GetOrAddPayload(() => _settings);
            return ConfigureTopologyAsync(context, cancellationToken);
        }, cancellationToken: cancellationToken).ConfigureAwait(false);
    }

    /// <summary>
    /// Declares the topology one operation at a time, and stops at the first failure.
    /// <para>
    /// These operations share one channel, and the first error the broker answers with closes it. Run
    /// in parallel through Task.WhenAll, the others then failed against a channel that was already
    /// going away, and whichever of those failures the await happened to surface could hide the one
    /// that mattered. Sequential execution means the first failure is the broker's own, every time.
    /// </para>
    /// <para>
    /// The ObjectDisposedException handler that used to sit here is gone. It replaced a local failure
    /// with a fabricated ShutdownEventArgs claiming ShutdownInitiator.Peer — a broker answer that never
    /// existed — and that fabrication is what the channel ownership now makes unnecessary: an operation
    /// holds a lease, so the channel is not disposed underneath it.
    /// </para>
    /// </summary>
    /// <param name="cancellationToken">The token used to cancel the operation.</param>
    /// <param name="context">The context for the operation.</param>
    async Task ConfigureTopologyAsync(ChannelContext context, CancellationToken cancellationToken)
    {
        foreach (var queue in _brokerTopology.Queues)
            await DeclareAsync(context, queue, cancellationToken).ConfigureAwait(false);

        foreach (var exchange in _brokerTopology.Exchanges)
            await DeclareAsync(context, exchange, cancellationToken).ConfigureAwait(false);

        foreach (var binding in _brokerTopology.QueueBindings)
            await BindAsync(context, binding, cancellationToken).ConfigureAwait(false);

        foreach (var binding in _brokerTopology.ExchangeBindings)
            await BindAsync(context, binding, cancellationToken).ConfigureAwait(false);
    }

    static Task DeclareAsync(ChannelContext context, Exchange exchange, CancellationToken cancellationToken)
    {
        return context.ConnectionContext.TopologyEntityCache.DeclareExchangeAsync(exchange, async declarationToken =>
        {
            RabbitMqLogMessages.DeclareExchange(exchange);
            await context.ExchangeDeclareAsync(
                exchange.ExchangeName,
                exchange.ExchangeType,
                exchange.Durable,
                exchange.AutoDelete,
                exchange.ExchangeArguments,
                declarationToken).ConfigureAwait(false);
        }, cancellationToken);
    }

    static async Task DeclareAsync(ChannelContext context, Queue queue, CancellationToken cancellationToken)
    {

        try
        {
            await context.ConnectionContext.TopologyEntityCache.DeclareQueueAsync(queue, async declarationToken =>
            {
                var ok = await context.QueueDeclareAsync(
                    queue.QueueName,
                    queue.Durable,
                    queue.Exclusive,
                    queue.AutoDelete,
                    queue.QueueArguments,
                    declarationToken).ConfigureAwait(false);

                RabbitMqLogMessages.DeclareQueue(queue, ok.ConsumerCount, ok.MessageCount);
            }, cancellationToken).ConfigureAwait(false);

        }
        catch (Exception exception)
        {


            LogContext.Error?.Log(exception, "Declare queue faulted: {Queue}", queue);

            throw;
        }
    }

    static async Task BindAsync(ChannelContext context, ExchangeToExchangeBinding binding, CancellationToken cancellationToken)
    {
        RabbitMqLogMessages.BindToExchange(binding);

        await context.ConnectionContext.TopologyEntityCache.BindAsync(binding,
            declarationToken => context.ExchangeBindAsync(
                binding.Destination.ExchangeName,
                binding.Source.ExchangeName,
                binding.RoutingKey,
                binding.Arguments,
                declarationToken), cancellationToken).ConfigureAwait(false);
    }

    static async Task BindAsync(ChannelContext context, ExchangeToQueueBinding binding, CancellationToken cancellationToken)
    {
        RabbitMqLogMessages.BindToQueue(binding);

        await context.ConnectionContext.TopologyEntityCache.BindAsync(binding,
            declarationToken => context.QueueBindAsync(
                binding.Destination.QueueName,
                binding.Source.ExchangeName,
                binding.RoutingKey,
                binding.Arguments,
                declarationToken), cancellationToken).ConfigureAwait(false);
    }
}

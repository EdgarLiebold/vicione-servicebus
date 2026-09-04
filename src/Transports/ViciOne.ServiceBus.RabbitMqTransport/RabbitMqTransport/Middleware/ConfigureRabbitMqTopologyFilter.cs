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

    public async Task Send(ChannelContext context, IPipe<ChannelContext> next)
    {
        OneTimeContext<ConfigureTopologyContext<TSettings>> oneTimeContext = await Configure(context, context.CancellationToken);

        try
        {
            await next.Send(context).ConfigureAwait(false);
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

    public async Task<OneTimeContext<ConfigureTopologyContext<TSettings>>> Configure(ChannelContext context, CancellationToken cancellationToken)
    {
        return await context.OneTimeSetup<ConfigureTopologyContext<TSettings>>(() =>
        {
            context.GetOrAddPayload(() => _settings);
            return ConfigureTopology(context, cancellationToken);
        }).ConfigureAwait(false);
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
    async Task ConfigureTopology(ChannelContext context, CancellationToken cancellationToken)
    {
        foreach (var queue in _brokerTopology.Queues)
            await Declare(context, queue, cancellationToken).ConfigureAwait(false);

        foreach (var exchange in _brokerTopology.Exchanges)
            await Declare(context, exchange, cancellationToken).ConfigureAwait(false);

        foreach (var binding in _brokerTopology.QueueBindings)
            await Bind(context, binding, cancellationToken).ConfigureAwait(false);

        foreach (var binding in _brokerTopology.ExchangeBindings)
            await Bind(context, binding, cancellationToken).ConfigureAwait(false);
    }

    static Task Declare(ChannelContext context, Exchange exchange, CancellationToken cancellationToken)
    {
        return context.ConnectionContext.TopologyEntityCache.DeclareExchange(exchange, async declarationToken =>
        {
            RabbitMqLogMessages.DeclareExchange(exchange);
            await context.ExchangeDeclare(
                exchange.ExchangeName,
                exchange.ExchangeType,
                exchange.Durable,
                exchange.AutoDelete,
                exchange.ExchangeArguments,
                declarationToken).ConfigureAwait(false);
        }, cancellationToken);
    }

    static async Task Declare(ChannelContext context, Queue queue, CancellationToken cancellationToken)
    {

        try
        {
            await context.ConnectionContext.TopologyEntityCache.DeclareQueue(queue, async declarationToken =>
            {
                var ok = await context.QueueDeclare(
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

    static async Task Bind(ChannelContext context, ExchangeToExchangeBinding binding, CancellationToken cancellationToken)
    {
        RabbitMqLogMessages.BindToExchange(binding);

        await context.ConnectionContext.TopologyEntityCache.Bind(binding,
            declarationToken => context.ExchangeBind(
                binding.Destination.ExchangeName,
                binding.Source.ExchangeName,
                binding.RoutingKey,
                binding.Arguments,
                declarationToken), cancellationToken).ConfigureAwait(false);
    }

    static async Task Bind(ChannelContext context, ExchangeToQueueBinding binding, CancellationToken cancellationToken)
    {
        RabbitMqLogMessages.BindToQueue(binding);

        await context.ConnectionContext.TopologyEntityCache.Bind(binding,
            declarationToken => context.QueueBind(
                binding.Destination.QueueName,
                binding.Source.ExchangeName,
                binding.RoutingKey,
                binding.Arguments,
                declarationToken), cancellationToken).ConfigureAwait(false);
    }
}

using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using RabbitMQ.Client;
using RabbitMQ.Client.Events;
using RabbitMQ.Client.Exceptions;
using ViciOne.ServiceBus.RabbitMq.Topology;

namespace ViciOne.ServiceBus.RabbitMq.Middleware;
/// <summary>Declares the required exchanges, queues, and bindings once for each RabbitMQ channel context.</summary>
/// <typeparam name="TSettings">The settings type.</typeparam>
public class ConfigureRabbitMqTopologyFilter<TSettings> :
    IFilter<ChannelContext>
    where TSettings : class
{
    readonly BrokerTopology _brokerTopology;
    readonly TSettings _settings;

    /// <summary>Creates a filter for an immutable broker-topology plan.</summary>
    /// <param name="settings">The transport settings exposed as a channel payload.</param>
    /// <param name="brokerTopology">The exchanges, queues, and bindings to declare.</param>
    public ConfigureRabbitMqTopologyFilter(TSettings settings, BrokerTopology brokerTopology)
    {
        _settings = settings;
        _brokerTopology = brokerTopology;
    }

    /// <summary>Ensures topology once per context, then invokes the remaining channel pipeline.</summary>
    /// <param name="context">The active RabbitMQ channel context.</param>
    /// <param name="next">The remainder of the channel pipeline.</param>
    /// <returns>A task that completes with the remaining pipeline.</returns>
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

    /// <summary>Adds the planned broker topology to the diagnostic probe.</summary>
    /// <param name="context">The probe context that receives the topology.</param>
    public void Probe(ProbeContext context)
    {
        var scope = context.CreateFilterScope("configureTopology");

        _brokerTopology.Probe(scope);
    }

    /// <summary>Runs or joins the one-time topology declaration for a channel context.</summary>
    /// <param name="context">The active RabbitMQ channel context.</param>
    /// <param name="cancellationToken">Cancellation while waiting for topology declaration.</param>
    /// <returns>The one-time setup handle, which can evict the cached result after downstream failure.</returns>
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
    /// All operations share one channel, and a broker error closes that channel. Sequential execution
    /// preserves the first broker failure as the reported cause.
    /// </para>
    /// <para>
    /// Each operation holds a channel lease, preventing disposal while the declaration or binding is active.
    /// </para>
    /// </summary>
    /// <param name="context">The channel context used for every declaration and binding.</param>
    /// <param name="cancellationToken">Cancellation while declaring or binding topology.</param>
    /// <returns>A task that completes when the entire topology has been declared.</returns>
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

using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Threading.Tasks;
using ViciOne.ServiceBus.Util;

#nullable enable
namespace ViciOne.ServiceBus.Transports.Fabric;

/// <summary>
/// Provides a message direct exchange implementation.
/// </summary>
/// <typeparam name="T">The t type.</typeparam>
public class MessageDirectExchange<T> :
    IMessageExchange<T>
    where T : class
{
    readonly ConcurrentDictionary<string, Connectable<IMessageSink<T>>> _sinks;

    /// <summary>
    /// Initializes a new instance of the containing type.
    /// </summary>
    /// <param name="name">The name value.</param>
    /// <param name="comparer">The comparer value.</param>
    public MessageDirectExchange(string name, StringComparer? comparer = default)
    {
        Name = name;

        _sinks = new ConcurrentDictionary<string, Connectable<IMessageSink<T>>>(comparer ?? StringComparer.Ordinal);
    }

    /// <summary>
    /// Gets the sinks value.
    /// </summary>
    public IEnumerable<IMessageSink<T>> Sinks
    {
        get
        {
            var sinks = new List<IMessageSink<T>>();

            foreach (KeyValuePair<string, Connectable<IMessageSink<T>>> sink in _sinks)
                sink.Value.ForEach(s => sinks.Add(s));

            return sinks;
        }
    }

    /// <summary>
    /// Gets the name value.
    /// </summary>
    public string Name { get; }

    /// <summary>
    /// Performs the deliver operation.
    /// </summary>
    /// <param name="context">The operation context.</param>
    /// <param name="cancellationToken">The token used to cancel the operation.</param>
    /// <returns>The result of the operation.</returns>
    public async Task DeliverAsync(DeliveryContext<T> context, CancellationToken cancellationToken = default)
    {
        if (_sinks.TryGetValue(context.RoutingKey ?? "", out Connectable<IMessageSink<T>>? forKey))
        {
            await forKey.ForEachAsync(async sink =>
            {
                if (context.WasAlreadyDelivered(sink))
                    return;

                await sink.DeliverAsync(context, cancellationToken: cancellationToken).ConfigureAwait(false);

                context.Delivered(sink);
            }, cancellationToken: cancellationToken).ConfigureAwait(false);
        }
    }

    /// <summary>
    /// Performs the connect operation.
    /// </summary>
    /// <param name="sink">The sink value.</param>
    /// <param name="routingKey">The routing key value.</param>
    /// <returns>The result of the operation.</returns>
    public ConnectHandle Connect(IMessageSink<T> sink, string? routingKey)
    {
        Connectable<IMessageSink<T>> forKey = _sinks.GetOrAdd(routingKey ?? "", key => new Connectable<IMessageSink<T>>());

        return forKey.Connect(sink);
    }

    /// <summary>
    /// Performs the probe operation.
    /// </summary>
    /// <param name="context">The operation context.</param>
    public void Probe(ProbeContext context)
    {
        var scope = context.CreateScope("exchange");
        scope.Add("name", Name);
        scope.Add("type", "direct");

        var sinkScope = scope.CreateScope("keys");

        foreach (KeyValuePair<string, Connectable<IMessageSink<T>>> sink in _sinks)
        {
            var routingKeyScope = sinkScope.CreateScope(string.IsNullOrWhiteSpace(sink.Key) ? "<empty>" : sink.Key);

            sink.Value.ForEach(s => s.Probe(routingKeyScope));
        }
    }

    /// <summary>
    /// Returns the string representation of this instance.
    /// </summary>
    /// <returns>The result of the operation.</returns>
    public override string ToString()
    {
        return $"Exchange({Name})";
    }
}

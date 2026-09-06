using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Threading.Tasks;
using ViciOne.ServiceBus.Util;

namespace ViciOne.ServiceBus.Transports.Fabric;

/// <summary>Routes message direct messages through an exchange.</summary>
/// <typeparam name="T">The value type.</typeparam>
public class MessageDirectExchange<T> :
    IMessageExchange<T>
    where T : class
{
    readonly ConcurrentDictionary<string, Connectable<IMessageSink<T>>> _sinks;

    /// <summary>Initializes a new instance.</summary>
    /// <param name="name">The name.</param>
    /// <param name="comparer">The comparer.</param>
    public MessageDirectExchange(string name, StringComparer? comparer = default)
    {
        Name = name;

        _sinks = new ConcurrentDictionary<string, Connectable<IMessageSink<T>>>(comparer ?? StringComparer.Ordinal);
    }

    /// <summary>Gets the sinks.</summary>
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

    /// <summary>Gets the name.</summary>
    public string Name { get; }

    /// <summary>Delivers the current message.</summary>
    /// <param name="context">The context associated with the operation.</param>
    /// <param name="cancellationToken">The token used to cancel the operation.</param>
    /// <returns>A task that represents the asynchronous operation.</returns>
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

    /// <summary>Connects the configured observer or endpoint.</summary>
    /// <param name="sink">The sink.</param>
    /// <param name="routingKey">The routing key.</param>
    /// <returns>A handle that disconnects the registration.</returns>
    public ConnectHandle Connect(IMessageSink<T> sink, string? routingKey)
    {
        Connectable<IMessageSink<T>> forKey = _sinks.GetOrAdd(routingKey ?? "", key => new Connectable<IMessageSink<T>>());

        return forKey.Connect(sink);
    }

    /// <summary>Writes diagnostic information to the probe context.</summary>
    /// <param name="context">The context associated with the operation.</param>
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

    /// <summary>Returns the string representation of this instance.</summary>
    /// <returns>The converted string.</returns>
    public override string ToString()
    {
        return $"Exchange({Name})";
    }
}

using System.Collections.Concurrent;
using ViciOne.ServiceBus.Providers.Transports;
using ViciOne.ServiceBus.Util;

namespace ViciOne.ServiceBus.Transports.Fabric;

/// <summary>Routes messages to destinations bound to an exact routing key.</summary>
/// <typeparam name="TMessage">The message type.</typeparam>
internal sealed class DirectMessageExchange<TMessage> :
    IMessageExchange<TMessage>
    where TMessage : class
{
    readonly ConcurrentDictionary<string, Connectable<IMessageSink<TMessage>>> _destinations;

    /// <summary>Initializes an exchange with the specified name and routing-key comparer.</summary>
    /// <param name="name">The exchange name.</param>
    /// <param name="comparer">The comparer used for routing keys.</param>
    public DirectMessageExchange(string name, StringComparer? comparer = null)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(name);

        Name = name;
        _destinations = new ConcurrentDictionary<string, Connectable<IMessageSink<TMessage>>>(comparer ?? StringComparer.Ordinal);
    }

    /// <inheritdoc />
    public IEnumerable<IMessageSink<TMessage>> Sinks
    {
        get
        {
            var sinks = new List<IMessageSink<TMessage>>();
            foreach (Connectable<IMessageSink<TMessage>> destinations in _destinations.Values)
                destinations.ForEach(sinks.Add);

            return sinks;
        }
    }

    /// <inheritdoc />
    public string Name { get; }

    /// <inheritdoc />
    public InMemoryExchangeType ExchangeType => InMemoryExchangeType.Direct;

    /// <inheritdoc />
    public Task DeliverAsync(IMessageDeliveryContext<TMessage> context, CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(context);

        if (!_destinations.TryGetValue(context.RoutingKey ?? string.Empty, out Connectable<IMessageSink<TMessage>>? destinations))
            return Task.CompletedTask;

        return destinations.ForEachAsync(
            sink => context.TryReserveDelivery(sink)
                ? sink.DeliverAsync(context, cancellationToken)
                : Task.CompletedTask,
            cancellationToken);
    }

    /// <inheritdoc />
    public ConnectHandle Connect(IMessageSink<TMessage> sink, string? routingKey)
    {
        ArgumentNullException.ThrowIfNull(sink);

        Connectable<IMessageSink<TMessage>> destinations = _destinations.GetOrAdd(
            routingKey ?? string.Empty,
            static _ => new Connectable<IMessageSink<TMessage>>());

        return destinations.Connect(sink);
    }

    /// <inheritdoc />
    public void Probe(ProbeContext context)
    {
        ArgumentNullException.ThrowIfNull(context);

        ProbeContext scope = context.CreateScope("exchange");
        scope.Add("name", Name);
        scope.Add("type", "direct");

        ProbeContext keys = scope.CreateScope("keys");
        foreach (KeyValuePair<string, Connectable<IMessageSink<TMessage>>> destination in _destinations)
        {
            ProbeContext routingKey = keys.CreateScope(string.IsNullOrEmpty(destination.Key) ? "<empty>" : destination.Key);
            destination.Value.ForEach(sink => sink.Probe(routingKey));
        }
    }

    /// <inheritdoc />
    public override string ToString() => $"Exchange({Name})";
}

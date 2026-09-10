using System.Collections.Concurrent;
using ViciOne.ServiceBus.Providers.Transports;
using ViciOne.ServiceBus.Util;

namespace ViciOne.ServiceBus.Transports.Fabric;

/// <summary>Routes messages to destinations whose topic patterns match the routing key.</summary>
/// <typeparam name="TMessage">The message type.</typeparam>
internal sealed class TopicMessageExchange<TMessage> :
    IMessageExchange<TMessage>
    where TMessage : class
{
    readonly StringComparer _comparer;
    readonly ConcurrentDictionary<string, Connectable<IMessageSink<TMessage>>> _destinations;

    /// <summary>Initializes an exchange with the specified name and topic-segment comparer.</summary>
    /// <param name="name">The exchange name.</param>
    /// <param name="comparer">The comparer used for literal topic segments.</param>
    public TopicMessageExchange(string name, StringComparer? comparer = null)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(name);

        Name = name;
        _comparer = comparer ?? StringComparer.Ordinal;
        _destinations = new ConcurrentDictionary<string, Connectable<IMessageSink<TMessage>>>(_comparer);
    }

    /// <inheritdoc />
    public IEnumerable<IMessageSink<TMessage>> Sinks
    {
        get
        {
            var sinks = new HashSet<IMessageSink<TMessage>>(ReferenceEqualityComparer.Instance);
            foreach (Connectable<IMessageSink<TMessage>> destinations in _destinations.Values)
                destinations.ForEach(sink => sinks.Add(sink));

            return sinks;
        }
    }

    /// <inheritdoc />
    public string Name { get; }

    /// <inheritdoc />
    public InMemoryExchangeType ExchangeType => InMemoryExchangeType.Topic;

    /// <inheritdoc />
    public async Task DeliverAsync(IMessageDeliveryContext<TMessage> context, CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(context);

        string routingKey = context.RoutingKey ?? string.Empty;
        ValidateRoutingKey(routingKey);

        foreach (KeyValuePair<string, Connectable<IMessageSink<TMessage>>> destination in _destinations)
        {
            if (!Matches(destination.Key, routingKey))
                continue;

            await destination.Value.ForEachAsync(
                sink => context.TryReserveDelivery(sink)
                    ? sink.DeliverAsync(context, cancellationToken)
                    : Task.CompletedTask,
                cancellationToken).ConfigureAwait(false);
        }
    }

    /// <inheritdoc />
    public ConnectHandle Connect(IMessageSink<TMessage> sink, string? routingKey)
    {
        ArgumentNullException.ThrowIfNull(sink);

        string pattern = routingKey ?? string.Empty;
        ValidatePattern(pattern);
        Connectable<IMessageSink<TMessage>> destinations = _destinations.GetOrAdd(
            pattern,
            static _ => new Connectable<IMessageSink<TMessage>>());

        return destinations.Connect(sink);
    }

    /// <inheritdoc />
    public void Probe(ProbeContext context)
    {
        ArgumentNullException.ThrowIfNull(context);

        ProbeContext scope = context.CreateScope("exchange");
        scope.Add("name", Name);
        scope.Add("type", "topic");

        ProbeContext topics = scope.CreateScope("topics");
        foreach (KeyValuePair<string, Connectable<IMessageSink<TMessage>>> destination in _destinations)
        {
            ProbeContext pattern = topics.CreateScope(string.IsNullOrEmpty(destination.Key) ? "<empty>" : destination.Key);
            destination.Value.ForEach(sink => sink.Probe(pattern));
        }
    }

    /// <inheritdoc />
    public override string ToString() => $"Exchange({Name})";

    bool Matches(string pattern, string routingKey)
    {
        string[] patternSegments = Split(pattern);
        string[] routingSegments = Split(routingKey);
        var states = new bool[patternSegments.Length + 1];
        states[0] = true;
        ExpandHashTransitions(states);

        foreach (string routingSegment in routingSegments)
        {
            var next = new bool[states.Length];
            for (var patternIndex = 0; patternIndex < patternSegments.Length; patternIndex++)
            {
                if (!states[patternIndex])
                    continue;

                string patternSegment = patternSegments[patternIndex];
                if (patternSegment == "#")
                    next[patternIndex] = true;
                else if (patternSegment == "*" || _comparer.Equals(patternSegment, routingSegment))
                    next[patternIndex + 1] = true;
            }

            states = next;
            ExpandHashTransitions(states);
        }

        return states[patternSegments.Length];

        void ExpandHashTransitions(bool[] activeStates)
        {
            for (var patternIndex = 0; patternIndex < patternSegments.Length; patternIndex++)
            {
                if (activeStates[patternIndex] && patternSegments[patternIndex] == "#")
                    activeStates[patternIndex + 1] = true;
            }
        }
    }

    static string[] Split(string value) => value.Length == 0 ? [] : value.Split('.');

    static void ValidatePattern(string pattern)
    {
        if (pattern.Length == 0)
            return;

        foreach (string segment in pattern.Split('.'))
        {
            if (segment.Length == 0)
                throw new ArgumentException("A topic pattern cannot contain an empty segment.", nameof(pattern));
            if (segment != "*" && segment != "#" && (segment.Contains('*') || segment.Contains('#')))
                throw new ArgumentException("Topic wildcards must occupy an entire segment.", nameof(pattern));
        }
    }

    static void ValidateRoutingKey(string routingKey)
    {
        if (routingKey.Length == 0)
            return;

        foreach (string segment in routingKey.Split('.'))
        {
            if (segment.Length == 0)
                throw new ArgumentException("A topic routing key cannot contain an empty segment.", nameof(routingKey));
            if (segment.Contains('*') || segment.Contains('#'))
                throw new ArgumentException("A topic routing key cannot contain wildcard characters.", nameof(routingKey));
        }
    }
}

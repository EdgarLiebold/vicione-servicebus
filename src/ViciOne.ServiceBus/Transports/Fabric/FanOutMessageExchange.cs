using ViciOne.ServiceBus.Providers.Transports;
using ViciOne.ServiceBus.Util;

namespace ViciOne.ServiceBus.Transports.Fabric;

/// <summary>Routes each message to every connected destination.</summary>
/// <typeparam name="TMessage">The message type.</typeparam>
internal sealed class FanOutMessageExchange<TMessage> :
    IMessageExchange<TMessage>
    where TMessage : class
{
    readonly Connectable<IMessageSink<TMessage>> _destinations = new();

    /// <summary>Initializes an exchange with the specified name.</summary>
    /// <param name="name">The exchange name.</param>
    public FanOutMessageExchange(string name)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(name);
        Name = name;
    }

    /// <inheritdoc />
    public IEnumerable<IMessageSink<TMessage>> Sinks
    {
        get
        {
            var sinks = new List<IMessageSink<TMessage>>();
            _destinations.ForEach(sinks.Add);
            return sinks;
        }
    }

    /// <inheritdoc />
    public string Name { get; }

    /// <inheritdoc />
    public InMemoryExchangeType ExchangeType => InMemoryExchangeType.FanOut;

    /// <inheritdoc />
    public Task DeliverAsync(IMessageDeliveryContext<TMessage> context, CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(context);

        return _destinations.ForEachAsync(
            sink => context.TryReserveDelivery(sink)
                ? sink.DeliverAsync(context, cancellationToken)
                : Task.CompletedTask,
            cancellationToken);
    }

    /// <inheritdoc />
    public ConnectHandle Connect(IMessageSink<TMessage> sink, string? routingKey)
    {
        ArgumentNullException.ThrowIfNull(sink);
        return _destinations.Connect(sink);
    }

    /// <inheritdoc />
    public void Probe(ProbeContext context)
    {
        ArgumentNullException.ThrowIfNull(context);

        ProbeContext scope = context.CreateScope("exchange");
        scope.Add("name", Name);
        scope.Add("type", "fanOut");

        ProbeContext destinations = scope.CreateScope("destinations");
        _destinations.ForEach(sink => sink.Probe(destinations));
    }

    /// <inheritdoc />
    public override string ToString() => $"Exchange({Name})";
}

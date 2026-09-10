using System.Runtime.CompilerServices;
using ViciOne.ServiceBus.Internals.GraphValidation;
using ViciOne.ServiceBus.Middleware;
using ViciOne.ServiceBus.Providers.Transports;

namespace ViciOne.ServiceBus.Transports.Fabric;

/// <summary>Owns the exchanges and queues used by one in-memory transport instance.</summary>
/// <typeparam name="TMessage">The message envelope type carried by the fabric.</typeparam>
internal sealed class MessageFabric<TMessage> :
    Supervisor,
    IMessageFabric<TMessage>
    where TMessage : class
{
    static readonly StringComparer EntityNameComparer = StringComparer.OrdinalIgnoreCase;

    readonly InMemoryDelayProvider _delayProvider;
    readonly HashSet<ExchangeBinding> _exchangeBindings = new(ExchangeBindingComparer.Instance);
    readonly Dictionary<string, IMessageExchange<TMessage>> _exchanges = new(EntityNameComparer);
    readonly object _topologyLock = new();
    readonly int _queueCapacity;
    readonly HashSet<QueueBinding> _queueBindings = new(QueueBindingComparer.Instance);
    readonly Dictionary<string, IMessageQueue<TMessage>> _queues = new(EntityNameComparer);

    /// <summary>Initializes a fabric with the maximum number of messages admitted by each queue.</summary>
    /// <param name="queueCapacity">The capacity assigned to each declared queue.</param>
    public MessageFabric(int queueCapacity = 1024)
    {
        if (queueCapacity <= 0)
            throw new ArgumentOutOfRangeException(nameof(queueCapacity), queueCapacity, "Queue capacity must be greater than zero.");

        _queueCapacity = queueCapacity;
        _delayProvider = new InMemoryDelayProvider();
    }

    /// <inheritdoc />
    public IInMemoryDelayProvider DelayProvider => _delayProvider;

    /// <inheritdoc />
    public void ExchangeDeclare(string name, InMemoryExchangeType exchangeType)
    {
        ValidateName(name);
        ValidateExchangeType(exchangeType);

        lock (_topologyLock)
            GetOrAddExchange(name, exchangeType, requireTypeMatch: true);
    }

    /// <inheritdoc />
    public void ExchangeBind(string source, string destination, string? routingKey)
    {
        ValidateName(source);
        ArgumentException.ThrowIfNullOrWhiteSpace(destination);

        if (EntityNameComparer.Equals(source, destination))
            throw new ArgumentException("The source and destination exchange must be different.", nameof(destination));

        lock (_topologyLock)
        {
            IMessageExchange<TMessage> sourceExchange = GetOrAddExchange(
                source,
                InMemoryExchangeType.FanOut,
                requireTypeMatch: false);
            IMessageExchange<TMessage> destinationExchange = GetOrAddExchange(
                destination,
                InMemoryExchangeType.FanOut,
                requireTypeMatch: false);
            string? effectiveRoutingKey = sourceExchange.ExchangeType == InMemoryExchangeType.FanOut
                || string.IsNullOrEmpty(routingKey)
                    ? null
                    : routingKey;
            var binding = new ExchangeBinding(source, destination, effectiveRoutingKey);
            if (_exchangeBindings.Contains(binding))
                return;

            ValidateBinding(destinationExchange, sourceExchange);
            sourceExchange.Connect(destinationExchange, effectiveRoutingKey);
            _exchangeBindings.Add(binding);
        }
    }

    /// <inheritdoc />
    public void QueueDeclare(string name)
    {
        ValidateName(name);

        lock (_topologyLock)
            GetOrAddQueue(name);
    }

    /// <inheritdoc />
    public void QueueBind(string source, string destination)
    {
        ValidateName(source);
        ArgumentException.ThrowIfNullOrWhiteSpace(destination);

        lock (_topologyLock)
        {
            var binding = new QueueBinding(source, destination);
            if (_queueBindings.Contains(binding))
                return;

            IMessageExchange<TMessage> sourceExchange = GetOrAddExchange(
                source,
                InMemoryExchangeType.FanOut,
                requireTypeMatch: false);
            IMessageQueue<TMessage> destinationQueue = GetOrAddQueue(destination);

            ValidateBinding(destinationQueue, sourceExchange);
            sourceExchange.Connect(destinationQueue, null);
            _queueBindings.Add(binding);
        }
    }

    /// <inheritdoc />
    public void Probe(ProbeContext context)
    {
        ArgumentNullException.ThrowIfNull(context);

        IMessageExchange<TMessage>[] exchanges;
        IMessageQueue<TMessage>[] queues;
        lock (_topologyLock)
        {
            exchanges = [.. _exchanges.Values];
            queues = [.. _queues.Values];
        }

        ProbeContext scope = context.CreateScope("messageFabric");
        foreach (IMessageExchange<TMessage> exchange in exchanges)
            exchange.Probe(scope);

        foreach (IMessageQueue<TMessage> queue in queues)
            queue.Probe(scope);
    }

    /// <inheritdoc />
    public IMessageExchange<TMessage> GetExchange(string name, InMemoryExchangeType exchangeType)
    {
        ValidateName(name);
        ValidateExchangeType(exchangeType);

        lock (_topologyLock)
            return GetOrAddExchange(name, exchangeType, requireTypeMatch: true);
    }

    /// <inheritdoc />
    public IMessageQueue<TMessage> GetQueue(string name)
    {
        ValidateName(name);

        lock (_topologyLock)
            return GetOrAddQueue(name);
    }

    /// <inheritdoc />
    protected override async Task StopSupervisorAsync(StopSupervisorContext context)
    {
        await base.StopSupervisorAsync(context).ConfigureAwait(false);
        await _delayProvider.DisposeAsync().ConfigureAwait(false);
    }

    IMessageExchange<TMessage> GetOrAddExchange(
        string name,
        InMemoryExchangeType exchangeType,
        bool requireTypeMatch)
    {
        if (_exchanges.TryGetValue(name, out IMessageExchange<TMessage>? existing))
        {
            if (requireTypeMatch && existing.ExchangeType != exchangeType)
            {
                throw new InvalidOperationException(
                    $"Exchange '{name}' is already declared as {existing.ExchangeType} and cannot be redeclared as {exchangeType}.");
            }

            return existing;
        }

        IMessageExchange<TMessage> exchange = exchangeType switch
        {
            InMemoryExchangeType.FanOut => new FanOutMessageExchange<TMessage>(name),
            InMemoryExchangeType.Direct => new DirectMessageExchange<TMessage>(name),
            InMemoryExchangeType.Topic => new TopicMessageExchange<TMessage>(name),
            _ => throw new ArgumentOutOfRangeException(nameof(exchangeType), exchangeType, "The exchange type is not supported.")
        };

        _exchanges.Add(name, exchange);
        return exchange;
    }

    IMessageQueue<TMessage> GetOrAddQueue(string name)
    {
        if (_queues.TryGetValue(name, out IMessageQueue<TMessage>? existing))
            return existing;

        var queue = new MessageQueue<TMessage>(name, _delayProvider, _queueCapacity);
        _queues.Add(name, queue);
        Add(queue);
        return queue;
    }

    void ValidateBinding(IMessageSink<TMessage> destination, IMessageSink<TMessage> sourceExchange)
    {
        try
        {
            var graph = new DependencyGraph<IMessageSink<TMessage>>(_exchanges.Count + 1);
            foreach (IMessageExchange<TMessage> exchange in _exchanges.Values)
            {
                foreach (IMessageSink<TMessage> sink in exchange.Sinks)
                    graph.Add(sink, exchange);
            }

            graph.Add(destination, sourceExchange);
            graph.EnsureGraphIsAcyclic();
        }
        catch (CyclicGraphException exception)
        {
            throw new InvalidOperationException("The exchange binding would create a cycle in the message fabric.", exception);
        }
    }

    static void ValidateName(
        string name,
        [CallerArgumentExpression(nameof(name))] string? parameterName = null) =>
        ArgumentException.ThrowIfNullOrWhiteSpace(name, parameterName);

    static void ValidateExchangeType(InMemoryExchangeType exchangeType)
    {
        if (!Enum.IsDefined(exchangeType))
            throw new ArgumentOutOfRangeException(nameof(exchangeType), exchangeType, "The exchange type is not supported.");
    }

    readonly record struct ExchangeBinding(string Source, string Destination, string? RoutingKey);

    sealed class ExchangeBindingComparer : IEqualityComparer<ExchangeBinding>
    {
        public static ExchangeBindingComparer Instance { get; } = new();

        public bool Equals(ExchangeBinding x, ExchangeBinding y) =>
            EntityNameComparer.Equals(x.Source, y.Source)
            && EntityNameComparer.Equals(x.Destination, y.Destination)
            && StringComparer.Ordinal.Equals(x.RoutingKey, y.RoutingKey);

        public int GetHashCode(ExchangeBinding value) => HashCode.Combine(
            EntityNameComparer.GetHashCode(value.Source),
            EntityNameComparer.GetHashCode(value.Destination),
            value.RoutingKey is null ? 0 : StringComparer.Ordinal.GetHashCode(value.RoutingKey));
    }

    readonly record struct QueueBinding(string Source, string Destination);

    sealed class QueueBindingComparer : IEqualityComparer<QueueBinding>
    {
        public static QueueBindingComparer Instance { get; } = new();

        public bool Equals(QueueBinding x, QueueBinding y) =>
            EntityNameComparer.Equals(x.Source, y.Source)
            && EntityNameComparer.Equals(x.Destination, y.Destination);

        public int GetHashCode(QueueBinding value) => HashCode.Combine(
            EntityNameComparer.GetHashCode(value.Source),
            EntityNameComparer.GetHashCode(value.Destination));
    }
}

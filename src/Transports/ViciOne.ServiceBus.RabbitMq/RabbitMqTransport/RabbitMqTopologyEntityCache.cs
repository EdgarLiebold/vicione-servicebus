using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using ViciOne.ServiceBus.RabbitMq.Topology;

namespace ViciOne.ServiceBus.RabbitMq;
/// <summary>Owns stable RabbitMQ topology declarations for the lifetime of one connection.</summary>
/// <remarks>
/// Stable declarations are single-flight and retain an immutable structural fingerprint. A second
/// declaration of the same entity with a different definition fails before provider work starts.
/// Transient topology remains channel-owned because RabbitMQ may remove it while the connection is
/// still open.
/// </remarks>
public sealed class RabbitMqTopologyEntityCache
{
    readonly ConcurrentDictionary<EntityKey, Entry> _entries = new();
    long _generation;

    /// <summary>Runs a stable exchange declaration once per cache generation; transient exchanges bypass the cache.</summary>
    /// <param name="exchange">The immutable exchange definition.</param>
    /// <param name="declare">The broker declaration callback.</param>
    /// <param name="cancellationToken">Cancellation for this caller's wait.</param>
    /// <returns>A task that completes when the shared declaration completes.</returns>
    public Task DeclareExchangeAsync(Exchange exchange, Func<CancellationToken, Task> declare, CancellationToken cancellationToken)
    {
        ArgumentNullException.ThrowIfNull(exchange);
        ArgumentNullException.ThrowIfNull(declare);

        if (!IsStable(exchange))
            return declare(cancellationToken);

        return ExecuteAsync(
            EntityKey.Exchange(exchange.ExchangeName),
            Definition.Exchange(exchange),
            declare,
            cancellationToken);
    }

    /// <summary>Runs a stable queue declaration once per cache generation; transient queues bypass the cache.</summary>
    /// <param name="queue">The immutable queue definition.</param>
    /// <param name="declare">The broker declaration callback.</param>
    /// <param name="cancellationToken">Cancellation for this caller's wait.</param>
    /// <returns>A task that completes when the shared declaration completes.</returns>
    public Task DeclareQueueAsync(Queue queue, Func<CancellationToken, Task> declare, CancellationToken cancellationToken)
    {
        ArgumentNullException.ThrowIfNull(queue);
        ArgumentNullException.ThrowIfNull(declare);

        if (!IsStable(queue))
            return declare(cancellationToken);

        return ExecuteAsync(
            EntityKey.Queue(queue.QueueName),
            Definition.Queue(queue),
            declare,
            cancellationToken);
    }

    /// <summary>Runs a stable exchange-to-queue binding once per cache generation.</summary>
    /// <param name="binding">The immutable binding definition.</param>
    /// <param name="bind">The broker binding callback.</param>
    /// <param name="cancellationToken">Cancellation for this caller's wait.</param>
    /// <returns>A task that completes when the shared binding completes.</returns>
    public Task BindAsync(ExchangeToQueueBinding binding, Func<CancellationToken, Task> bind, CancellationToken cancellationToken)
    {
        ArgumentNullException.ThrowIfNull(binding);
        ArgumentNullException.ThrowIfNull(bind);

        if (!IsStable(binding.Source) || !IsStable(binding.Destination))
            return bind(cancellationToken);

        return ExecuteAsync(
            EntityKey.ExchangeToQueueBinding(
                binding.Source.ExchangeName,
                binding.Destination.QueueName,
                binding.RoutingKey),
            Definition.ExchangeToQueue(binding),
            bind,
            cancellationToken);
    }

    /// <summary>Runs a stable exchange-to-exchange binding once per cache generation.</summary>
    /// <param name="binding">The immutable binding definition.</param>
    /// <param name="bind">The broker binding callback.</param>
    /// <param name="cancellationToken">Cancellation for this caller's wait.</param>
    /// <returns>A task that completes when the shared binding completes.</returns>
    public Task BindAsync(ExchangeToExchangeBinding binding, Func<CancellationToken, Task> bind, CancellationToken cancellationToken)
    {
        ArgumentNullException.ThrowIfNull(binding);
        ArgumentNullException.ThrowIfNull(bind);

        if (!IsStable(binding.Source) || !IsStable(binding.Destination))
            return bind(cancellationToken);

        return ExecuteAsync(
            EntityKey.ExchangeToExchangeBinding(
                binding.Source.ExchangeName,
                binding.Destination.ExchangeName,
                binding.RoutingKey),
            Definition.ExchangeToExchange(binding),
            bind,
            cancellationToken);
    }

    /// <summary>
    /// Invalidates every declaration and dependent binding after a channel, connection, topology,
    /// or send fault. A subsequent operation redeclares from its immutable definition.
    /// </summary>
    public void Invalidate()
    {
        Interlocked.Increment(ref _generation);
        _entries.Clear();
    }

    async Task ExecuteAsync(EntityKey key, Definition definition, Func<CancellationToken, Task> action, CancellationToken cancellationToken)
    {
        while (true)
        {
            cancellationToken.ThrowIfCancellationRequested();

            var generation = Volatile.Read(ref _generation);
            var candidate = new Entry(definition, generation, action);
            var entry = _entries.GetOrAdd(key, candidate);

            if (!entry.Definition.Equals(definition))
                throw new ConfigurationException(global::ViciOne.ServiceBus.Providers.Configuration.ConfigurationMessages.Create("RabbitMQ", "unknown", $"RabbitMQ topology entity '{key}' was configured with conflicting definitions.", "Correct the named configuration before starting the host"));

            if (entry.Generation != Volatile.Read(ref _generation))
            {
                _entries.TryRemove(new KeyValuePair<EntityKey, Entry>(key, entry));
                continue;
            }

            Task completion;
            try
            {
                completion = entry.Completion;
            }
            catch
            {
                _entries.TryRemove(new KeyValuePair<EntityKey, Entry>(key, entry));
                throw;
            }

            try
            {
                await completion.WaitAsync(cancellationToken).ConfigureAwait(false);
            }
            catch (OperationCanceledException) when (cancellationToken.IsCancellationRequested && !completion.IsCompleted)
            {
                // The declaration belongs to the connection. This caller canceled only its wait;
                // removing the shared entry here would allow a duplicate declaration to start.
                throw;
            }
            catch
            {
                _entries.TryRemove(new KeyValuePair<EntityKey, Entry>(key, entry));
                throw;
            }

            // Invalidation may race a successful declaration. Never let a waiter rely on knowledge
            // captured before the latest fault generation.
            if (entry.Generation != Volatile.Read(ref _generation))
            {
                _entries.TryRemove(new KeyValuePair<EntityKey, Entry>(key, entry));
                continue;
            }

            return;
        }
    }

    static bool IsStable(Exchange exchange) => exchange.Durable && !exchange.AutoDelete;

    static bool IsStable(Queue queue) => queue.Durable && !queue.AutoDelete && !queue.Exclusive;


    sealed class Entry
    {
        readonly Lazy<Task> _completion;

        public Entry(Definition definition, long generation, Func<CancellationToken, Task> action)
        {
            Definition = definition;
            Generation = generation;
            _completion = new Lazy<Task>(
                () => action(CancellationToken.None),
                LazyThreadSafetyMode.ExecutionAndPublication);
        }

        public Definition Definition { get; }
        public long Generation { get; }
        public Task Completion => _completion.Value;
    }


    enum EntityKind
    {
        /// <summary>An exchange declaration.</summary>
        Exchange,
        /// <summary>A queue declaration.</summary>
        Queue,
        /// <summary>An exchange-to-queue binding.</summary>
        ExchangeToQueueBinding,
        /// <summary>An exchange-to-exchange binding.</summary>
        ExchangeToExchangeBinding
    }


    readonly record struct EntityKey(EntityKind Kind, string Source, string? Destination, string? RoutingKey)
    {
        public static EntityKey Exchange(string name) => new(EntityKind.Exchange, name, null, null);

        public static EntityKey Queue(string name) => new(EntityKind.Queue, name, null, null);

        public static EntityKey ExchangeToQueueBinding(string source, string destination, string routingKey) =>
            new(EntityKind.ExchangeToQueueBinding, source, destination, routingKey);

        public static EntityKey ExchangeToExchangeBinding(string source, string destination, string routingKey) =>
            new(EntityKind.ExchangeToExchangeBinding, source, destination, routingKey);

        public override string ToString() => Destination == null
            ? $"{Kind}:{Source}"
            : $"{Kind}:{Source}->{Destination} ({RoutingKey})";
    }


    readonly record struct Definition(string Value)
    {
        public static Definition Exchange(Exchange exchange) => new(Canonical(
            exchange.ExchangeType,
            exchange.Durable,
            exchange.AutoDelete,
            exchange.ExchangeArguments));

        public static Definition Queue(Queue queue) => new(Canonical(
            queue.Durable,
            queue.Exclusive,
            queue.AutoDelete,
            queue.QueueArguments));

        public static Definition ExchangeToQueue(ExchangeToQueueBinding binding) => new(Canonical(
            binding.Source.ExchangeName,
            binding.Destination.QueueName,
            binding.RoutingKey,
            binding.Arguments));

        public static Definition ExchangeToExchange(ExchangeToExchangeBinding binding) => new(Canonical(
            binding.Source.ExchangeName,
            binding.Destination.ExchangeName,
            binding.RoutingKey,
            binding.Arguments));

        static string Canonical(params object?[] values) => Frame('r', string.Concat(values.Select(Format)));

        static string Format(object? value)
        {
            return value switch
            {
                null => "n0:",
                string text => Frame('s', text),
                bool boolean => boolean ? "b1:1" : "b1:0",
                byte[] bytes => Frame('x', Convert.ToHexString(bytes)),
                ReadOnlyMemory<byte> bytes => Frame('x', Convert.ToHexString(bytes.Span)),
                Memory<byte> bytes => Frame('x', Convert.ToHexString(bytes.Span)),
                IDictionary<string, object?> dictionary => FormatDictionary(dictionary.Select(pair =>
                    Frame('k', pair.Key) + Format(pair.Value))),
                System.Collections.IDictionary dictionary => FormatDictionary(dictionary.Keys.Cast<object?>().Select(key =>
                    Format(key) + Format(key == null ? null : dictionary[key]))),
                System.Collections.IEnumerable enumerable when value is not string =>
                    Frame('a', string.Concat(enumerable.Cast<object?>().Select(item => Frame('e', Format(item))))),
                IFormattable formattable => FormatScalar(value.GetType(), formattable.ToString(null, CultureInfo.InvariantCulture)),
                _ => FormatScalar(value.GetType(), value.ToString())
            };
        }

        static string FormatDictionary(IEnumerable<string> entries) =>
            Frame('d', string.Concat(entries.OrderBy(entry => entry, StringComparer.Ordinal).Select(entry => Frame('e', entry))));

        static string FormatScalar(Type type, string? value) =>
            Frame('t', type.FullName ?? type.Name) + Frame('v', value ?? string.Empty);

        static string Frame(char kind, string value) => $"{kind}{value.Length.ToString(CultureInfo.InvariantCulture)}:{value}";
    }
}

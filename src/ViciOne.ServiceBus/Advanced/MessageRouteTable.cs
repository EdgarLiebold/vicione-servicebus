using System;
using System.Collections.Generic;

namespace ViciOne.ServiceBus.Advanced;

/// <summary>Stores bus-owned message routes that become immutable when configuration ends or resolution begins.</summary>
public sealed class MessageRouteTable :
    IMessageRouteTable
{
    internal static IMessageRouteTable Empty { get; } = CreateEmpty();

    readonly object _lock = new();
    readonly Dictionary<Type, Route> _routes = new();
    bool _frozen;

    static IMessageRouteTable CreateEmpty()
    {
        var table = new MessageRouteTable();
        table.Freeze();
        return table;
    }

    /// <summary>Resolves the destination configured for a message contract.</summary>
    /// <typeparam name="T">The message contract type.</typeparam>
    /// <param name="destinationAddress">Receives the configured destination when a route exists.</param>
    /// <returns><see langword="true"/> when a route resolves to a destination; otherwise <see langword="false"/>.</returns>
    public bool TryGetDestinationAddress<T>(out Uri destinationAddress)
        where T : class
    {
        return TryGetDestinationAddress(typeof(T), out destinationAddress);
    }

    /// <summary>Resolves the destination configured for a runtime message contract.</summary>
    /// <param name="messageType">The runtime type of the message contract.</param>
    /// <param name="destinationAddress">Receives the configured destination when a route exists.</param>
    /// <returns><see langword="true"/> when a route resolves to a destination; otherwise <see langword="false"/>.</returns>
    public bool TryGetDestinationAddress(Type messageType, out Uri destinationAddress)
    {
        ArgumentNullException.ThrowIfNull(messageType);

        Route? route;
        lock (_lock)
        {
            _frozen = true;
            route = FindRoute(messageType);
        }

        Uri? address = route?.Resolve();
        if (address is null)
        {
            destinationAddress = default!;
            return false;
        }

        destinationAddress = address;
        return true;
    }

    internal void Map<T>(Uri destinationAddress)
        where T : class
    {
        ArgumentNullException.ThrowIfNull(destinationAddress);
        Map(typeof(T), Route.ForAddress(destinationAddress));
    }

    internal void Map<T>(EndpointAddressProvider<T> endpointAddressProvider)
        where T : class
    {
        ArgumentNullException.ThrowIfNull(endpointAddressProvider);
        Map(typeof(T), Route.ForProvider(() => endpointAddressProvider(out Uri address) ? address : null));
    }

    internal void Freeze()
    {
        lock (_lock)
            _frozen = true;
    }

    void Map(Type messageType, Route route)
    {
        lock (_lock)
        {
            if (_frozen)
                throw new InvalidOperationException("Message routes are immutable after the bus has been built.");

            if (_routes.TryGetValue(messageType, out Route? existing))
            {
                if (existing.HasSameFixedAddress(route))
                    return;

                throw new ConfigurationException(global::ViciOne.ServiceBus.Providers.Configuration.ConfigurationMessages.Create("Message Route Table", "unknown", $"A message route is already configured for {TypeCache.GetShortName(messageType)}.", "Correct the named configuration before starting the host"));
            }

            _routes.Add(messageType, route);
        }
    }

    Route? FindRoute(Type messageType)
    {
        if (_routes.TryGetValue(messageType, out Route? exact))
            return exact;

        IReadOnlyList<Type> implementedTypes = MessageTypeCache.GetMessageTypes(messageType);
        Route? candidate = null;
        Type? candidateType = null;

        for (var i = 0; i < implementedTypes.Count; i++)
        {
            Type type = implementedTypes[i];
            if (type == messageType || !_routes.TryGetValue(type, out Route? route))
                continue;

            if (candidate is not null)
            {
                throw new ConfigurationException(
                    global::ViciOne.ServiceBus.Providers.Configuration.ConfigurationMessages.Create("Message Route Table", "unknown", $"Message route for {TypeCache.GetShortName(messageType)} is ambiguous between "
                    + $"{TypeCache.GetShortName(candidateType!)} and {TypeCache.GetShortName(type)}.", "Correct the named configuration before starting the host"));
            }

            candidate = route;
            candidateType = type;
        }

        return candidate;
    }
    sealed class Route
    {
        readonly Uri? _address;
        readonly Func<Uri?>? _provider;

        Route(Uri address)
        {
            _address = address;
        }

        Route(Func<Uri?> provider)
        {
            _provider = provider;
        }

        internal static Route ForAddress(Uri address)
        {
            return new Route(address);
        }

        internal static Route ForProvider(Func<Uri?> provider)
        {
            return new Route(provider);
        }

        internal Uri? Resolve()
        {
            return _address ?? _provider!();
        }

        internal bool HasSameFixedAddress(Route other)
        {
            return _provider is null && other._provider is null && _address == other._address;
        }
    }
}

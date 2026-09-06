using System;
using System.Diagnostics.CodeAnalysis;

namespace ViciOne.ServiceBus.Advanced;

/// <summary>Resolves message routes from the bus that owns an active send provider.</summary>
internal static class EndpointConvention
{
    internal static IMessageRouteTable GetMessageRoutes(ISendEndpointProvider provider)
    {
        ArgumentNullException.ThrowIfNull(provider);

        if (provider is IMessageRouteProvider routeProvider)
            return routeProvider.MessageRoutes;

        if (provider is ConsumeContext consumeContext)
            return GetMessageRoutes(consumeContext.ReceiveContext.SendEndpointProvider);

        throw new ConfigurationException(
            global::ViciOne.ServiceBus.Providers.Configuration.ConfigurationMessages.Create("Endpoint Convention", "unknown", $"The send endpoint provider {provider.GetType().Name} does not expose its owning bus message routes.", "Correct the named configuration before starting the host"));
    }

    internal static bool TryGetDestinationAddress<T>(ISendEndpointProvider provider, [NotNullWhen(true)] out Uri? destinationAddress)
        where T : class
    {
        return GetMessageRoutes(provider).TryGetDestinationAddress<T>(out destinationAddress);
    }

    internal static bool TryGetDestinationAddress(ISendEndpointProvider provider, Type messageType,
        [NotNullWhen(true)] out Uri? destinationAddress)
    {
        ArgumentNullException.ThrowIfNull(messageType);

        return GetMessageRoutes(provider).TryGetDestinationAddress(messageType, out destinationAddress);
    }

    internal static Uri GetDestinationAddress<T>(ISendEndpointProvider provider)
        where T : class
    {
        return TryGetDestinationAddress<T>(provider, out Uri? destinationAddress)
            ? destinationAddress
            : throw new ConfigurationException(global::ViciOne.ServiceBus.Providers.Configuration.ConfigurationMessages.Create("Endpoint Convention", "unknown", $"A message route for {TypeCache<T>.ShortName} is not configured on this bus.", "Correct the named configuration before starting the host"));
    }
}

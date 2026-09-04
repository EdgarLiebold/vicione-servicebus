using System;

namespace ViciOne.ServiceBus;

/// <summary>
/// Resolves routes from the bus that owns the active send provider.
/// </summary>
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
            $"The send endpoint provider {provider.GetType().Name} does not expose its owning bus message routes.");
    }

    internal static bool TryGetDestinationAddress<T>(ISendEndpointProvider provider, out Uri destinationAddress)
        where T : class
    {
        return GetMessageRoutes(provider).TryGetDestinationAddress<T>(out destinationAddress);
    }

    internal static bool TryGetDestinationAddress(ISendEndpointProvider provider, Type messageType, out Uri destinationAddress)
    {
        return GetMessageRoutes(provider).TryGetDestinationAddress(messageType, out destinationAddress);
    }

    internal static Uri GetDestinationAddress<T>(ISendEndpointProvider provider)
        where T : class
    {
        return TryGetDestinationAddress<T>(provider, out Uri destinationAddress)
            ? destinationAddress
            : throw new ConfigurationException($"A message route for {TypeCache<T>.ShortName} is not configured on this bus.");
    }
}

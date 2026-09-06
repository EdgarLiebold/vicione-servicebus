namespace ViciOne.ServiceBus.Advanced;

/// <summary>Exposes the message routes owned by a bus endpoint provider.</summary>
internal interface IMessageRouteProvider
{
    IMessageRouteTable MessageRoutes { get; }
}

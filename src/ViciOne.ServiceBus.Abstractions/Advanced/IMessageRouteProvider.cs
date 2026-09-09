namespace ViciOne.ServiceBus.Advanced;

/// <summary>Exposes the message routes owned by a bus endpoint provider.</summary>
internal interface IMessageRouteProvider
{
    /// <summary>Gets the route table used to resolve message destinations.</summary>
    IMessageRouteTable MessageRoutes { get; }
}

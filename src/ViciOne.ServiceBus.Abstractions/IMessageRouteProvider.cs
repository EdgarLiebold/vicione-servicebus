namespace ViciOne.ServiceBus.Advanced;

internal interface IMessageRouteProvider
{
    IMessageRouteTable MessageRoutes { get; }
}

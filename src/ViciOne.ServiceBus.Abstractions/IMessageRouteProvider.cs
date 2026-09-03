namespace ViciOne.ServiceBus
{
    internal interface IMessageRouteProvider
    {
        IMessageRouteTable MessageRoutes { get; }
    }
}

namespace ViciOne.ServiceBus
{
    using System;


    /// <summary>
    /// Read-only message routes owned by a single bus instance.
    /// </summary>
    public interface IMessageRouteTable
    {
        bool TryGetDestinationAddress<T>(out Uri destinationAddress)
            where T : class;

        bool TryGetDestinationAddress(Type messageType, out Uri destinationAddress);
    }
}

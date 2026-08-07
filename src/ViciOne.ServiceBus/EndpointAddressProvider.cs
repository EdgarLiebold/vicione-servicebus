// ViciOne modification: WP-F2-SERVICEBUS-IDENTITY, 2026-08-07.
namespace ViciOne.ServiceBus
{
    using System;


    /// <summary>
    /// Returns the address for the message provided
    /// </summary>
    /// <param name="address"></param>
    /// <returns></returns>
    public delegate bool EndpointAddressProvider<in T>(out Uri address)
        where T : class;
}

using System;
using System.Linq;
using System.Net.NetworkInformation;

namespace ViciOne.ServiceBus.NewIdProviders;

/// <summary>Provides network address worker id services.</summary>
public class NetworkAddressWorkerIdProvider :
    IWorkerIdProvider
{
    /// <summary>Gets worker id.</summary>
    /// <param name="index">The index.</param>
    /// <returns>The worker id.</returns>
    public byte[] GetWorkerId(int index)
    {
        return GetNetworkAddress(index);
    }

    static byte[] GetNetworkAddress(int index)
    {
        var network = NetworkInterface
            .GetAllNetworkInterfaces()
            .Where(x => x.NetworkInterfaceType == NetworkInterfaceType.Ethernet
                || x.NetworkInterfaceType == NetworkInterfaceType.GigabitEthernet
                || x.NetworkInterfaceType == NetworkInterfaceType.Wireless80211
                || x.NetworkInterfaceType == NetworkInterfaceType.FastEthernetFx
                || x.NetworkInterfaceType == NetworkInterfaceType.FastEthernetT)
            .Select(x => x.GetPhysicalAddress())
            .Where(x => x != null)
            .Select(x => x.GetAddressBytes())
            .Where(x => x.Length == 6)
            .Skip(index)
            .FirstOrDefault();

        if (network == null)
            throw new InvalidOperationException("Unable to find usable network adapter for unique address");

        return network;
    }
}

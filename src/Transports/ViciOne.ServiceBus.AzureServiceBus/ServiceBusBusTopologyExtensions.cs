using System;

namespace ViciOne.ServiceBus.AzureServiceBus;

/// <summary>Provides access to the Azure Service Bus-specific topology of a bus.</summary>
public static class ServiceBusBusTopologyExtensions
{
    /// <summary>Gets the provider-specific topology or rejects a bus that uses another transport.</summary>
    /// <param name="bus">The bus whose topology is requested.</param>
    /// <returns>The Azure Service Bus topology.</returns>
    public static IServiceBusBusTopology GetServiceBusBusTopology(this IBus bus)
    {
        if (bus.Topology is IServiceBusBusTopology hostTopology)
            return hostTopology;

        throw new ArgumentException("The bus is not an Azure Service Bus bus", nameof(bus));
    }
}

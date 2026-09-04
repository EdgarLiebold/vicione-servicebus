using System;

namespace ViciOne.ServiceBus.AzureServiceBus;

/// <summary>
/// Provides extension methods for service bus bus topology.
/// </summary>
public static class ServiceBusBusTopologyExtensions
{
    /// <summary>
    /// Gets service bus bus topology.
    /// </summary>
    /// <param name="bus">The bus value.</param>
    /// <returns>The result of the operation.</returns>
    public static IServiceBusBusTopology GetServiceBusBusTopology(this IBus bus)
    {
        if (bus.Topology is IServiceBusBusTopology hostTopology)
            return hostTopology;

        throw new ArgumentException("The bus is not an Azure Service Bus bus", nameof(bus));
    }
}

using System;
using System.Linq;

namespace ViciOne.ServiceBus.Transports;

/// <summary>Provides extension methods for endpoint address.</summary>
public static class EndpointAddressExtensions
{
    /// <summary>
    /// Returns the endpoint name (the last part of the URI, without the query string or preceding path)
    /// from the address.
    /// </summary>
    /// <param name="address">The address.</param>
    /// <returns>The endpoint name.</returns>
    public static string? GetEndpointName(this Uri? address)
    {
        return address?.AbsolutePath?.Split('/').LastOrDefault();
    }

    /// <summary>Gets diagnostic endpoint name.</summary>
    /// <param name="address">The address.</param>
    /// <returns>The diagnostic endpoint name.</returns>
    public static string GetDiagnosticEndpointName(this Uri address)
    {
        var endpointName = address.GetEndpointName();
        if (string.IsNullOrWhiteSpace(endpointName))
            return "";

        return endpointName!.Contains("_bus_") ? "bus" : endpointName;
    }
}

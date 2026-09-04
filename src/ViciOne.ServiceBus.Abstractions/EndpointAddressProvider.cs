using System;

namespace ViciOne.ServiceBus;

/// <summary>
/// Resolves an endpoint address for a message type.
/// </summary>
/// <param name="address">The resolved address when the provider returns <see langword="true" />.</param>
/// <returns><see langword="true" /> when an address is available; otherwise <see langword="false" />.</returns>
public delegate bool EndpointAddressProvider<in T>(out Uri address)
    where T : class;

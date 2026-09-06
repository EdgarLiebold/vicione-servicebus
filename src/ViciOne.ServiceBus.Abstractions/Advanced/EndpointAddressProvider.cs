using System;

namespace ViciOne.ServiceBus.Advanced;

/// <summary>Resolves the current destination of a dynamically routed message contract.</summary>
/// <returns>The destination address, or <see langword="null" /> when no destination is currently available.</returns>
public delegate Uri? EndpointAddressProvider();

using System;
using ViciOne.ServiceBus.Transports;

namespace ViciOne.ServiceBus.Advanced.Registration;

/// <summary>Creates typed receive endpoint dispatchers for registrations owned by a consumer kind.</summary>
public interface IConsumerKindDispatcherProvider
{
    /// <summary>Attempts to create a dispatcher for a registration type.</summary>
    /// <param name="registrationType">The registration type.</param>
    /// <param name="factory">Creates the underlying receive endpoint dispatcher.</param>
    /// <param name="formatter">Formats the endpoint name.</param>
    /// <param name="dispatcher">Receives the created dispatcher when the type is owned by this consumer kind.</param>
    /// <returns><see langword="true"/> when a dispatcher was created; otherwise, <see langword="false"/>.</returns>
    bool TryCreateDispatcher(Type registrationType, IReceiveEndpointDispatcherFactory factory,
        IEndpointNameFormatter formatter, out IReceiveEndpointDispatcher? dispatcher);
}

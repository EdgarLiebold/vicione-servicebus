using System;

namespace ViciOne.ServiceBus.Transports;

/// <summary>Creates receive endpoint dispatcher instances.</summary>
public interface IReceiveEndpointDispatcherFactory :
    IAsyncDisposable
{
    /// <summary>
    /// Creates a single receiver with all configured consumers, sagas, etc.
    /// Note that if any other receivers are created for specific consumers or sagas, those consumers and sagas will
    /// not be included in this receiver as they've already been configured.
    /// </summary>
    /// <param name="queueName">The queue name.</param>
    /// <returns>The created receiver.</returns>
    IReceiveEndpointDispatcher CreateReceiver(string queueName);

    /// <summary>Creates a receiver and applies capability-owned endpoint configuration.</summary>
    /// <param name="queueName">The queue name.</param>
    /// <param name="configure">The callback used to configure the component.</param>
    /// <returns>The created receiver.</returns>
    IReceiveEndpointDispatcher CreateReceiver(string queueName,
        Action<IReceiveEndpointConfigurator, IRegistrationContext> configure);

    /// <summary>Creates a dispatcher for a registered handler type using its owning consumer kind.</summary>
    /// <param name="registrationType">The runtime registration type used by the operation.</param>
    /// <param name="fallbackQueueName">The fallback queue name.</param>
    /// <param name="formatter">The formatter.</param>
    /// <returns>The created registration receiver.</returns>
    IReceiveEndpointDispatcher CreateRegistrationReceiver(Type registrationType, string fallbackQueueName,
        IEndpointNameFormatter formatter);

}
